// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore;
using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class OneDriveLinkService : IOneDriveLinkService
	{
		private readonly IGraphClientAppContext graphClientContext;
        private readonly IProposalManagerClientFactory proposalManagerClientFactory;
		private readonly IOpportunityRepository opportunityRepository;
		private readonly OneDriveConfiguration oneDriveConfiguration;
		private readonly Dynamics365Configuration dynamics365Configuration;
        private readonly AzureAdOptions azureAdOptions;
        private readonly AppOptions appOptions;
		private readonly IDeltaLinksStorage deltaLinksStorage;

		public OneDriveLinkService(
			IGraphClientAppContext graphClientContext,
            IProposalManagerClientFactory proposalManagerClientFactory,
            IConfiguration configuration,
			IOpportunityRepository opportunityRepository,
			IDeltaLinksStorage deltaLinksStorage)
		{
			this.graphClientContext = graphClientContext;
            this.proposalManagerClientFactory = proposalManagerClientFactory;

			oneDriveConfiguration = new OneDriveConfiguration();
			configuration.Bind(OneDriveConfiguration.ConfigurationName, oneDriveConfiguration);

			dynamics365Configuration = new Dynamics365Configuration();
			configuration.Bind(Dynamics365Configuration.ConfigurationName, dynamics365Configuration);

            azureAdOptions = new AzureAdOptions();
            configuration.Bind("AzureAd", azureAdOptions);

            appOptions = new AppOptions();
            configuration.Bind("ProposalManagement", appOptions);

			this.opportunityRepository = opportunityRepository;
			this.deltaLinksStorage = deltaLinksStorage;

			InitializeTemporaryFolderDelta();
		}

		public void InitializeTemporaryFolderDelta()
		{
			if (deltaLinksStorage.ProposalManagerDeltaLink is null)
			{
				// We first try to get a stateful delta link that will bring changes only from this point forward.
				try
				{
					var graph = graphClientContext.GraphClient;
					deltaLinksStorage.ProposalManagerDeltaLink = (string)graph.Sites[appOptions.ProposalManagementRootSiteId].Drive.Root.Delta("latest").Request().GetAsync().Result.AdditionalData["@odata.deltaLink"];
				}
				// If, for any reason, we don't succeed, we set the link to be the stateless default link that brings in the entire hierarchy to analyze
				catch
				{
					deltaLinksStorage.ProposalManagerDeltaLink = $"https://graph.microsoft.com/v1.0/sites/{appOptions.ProposalManagementRootSiteId}/drive/root/delta";
				}
			}
		}

		public async Task EnsureTempFolderForOpportunityExistsAsync(string opportunityName)
		{
			var graph = graphClientContext.GraphClient;
			var tempFolder = graph.Sites[appOptions.ProposalManagementRootSiteId].Drive.Root.ItemWithPath($"/{SharePointLocationRepository.TempFolderName}");
			var folderExists = (await tempFolder.Children.Request().GetAsync()).Any(di => di.Name == opportunityName);
			if (!folderExists)
			{
				await tempFolder.Children.Request().AddAsync(
					new DriveItem
					{
						Name = opportunityName,
						Folder = new Folder { }
					});
			}
		}

		public async Task EnsureChannelFoldersForOpportunityExistAsync(string opportunityName, IEnumerable<string> locations)
		{
            Debug.WriteLine($"Ensuring channel folders exist for opportunity {opportunityName}...");
            Debug.Indent();
            GraphServiceClient graph;
            Group opportunityGroup = null;
            var retryPolicy = new int[] { 0, 10, 10, 30 };
            foreach (var delay in retryPolicy)
            {
                Debug.WriteLine($"Waiting {delay} seconds...");
                Thread.Sleep(delay * 1000);
                try
                {
                    graph = graphClientContext.GraphClient;
                    opportunityGroup = (await graph.Groups.Request().Filter($"displayName eq '{opportunityName}'").Select("id").GetAsync()).First();
                    var opportunityDriveRoot = graph.Groups[opportunityGroup.Id].Sites["root"].Drive.Root;
                    var existingFolders = (await opportunityDriveRoot.Children.Request().GetAsync());
                    var missingFolders = locations.Except(from ef in existingFolders select ef.Name);
                    foreach (var missingFolder in missingFolders)
                    {
                        await opportunityDriveRoot.Children.Request().AddAsync(new DriveItem
                        {
                            Name = missingFolder,
                            Folder = new Folder { }
                        });
                    }
                    Debug.WriteLine("Folder syncing succeded.");
                    Debug.Unindent();
                    return;
                }
                catch(ServiceException ex) when (ex.IsMatch("ResourceNotFound"))
                {
                    Debug.WriteLine($"Folder syncing failed with the following exception: {ex} of type {ex.GetType().FullName}.");
                }
                catch (Exception ex)
                {
                    throw new Exception($@"DYNAMICS INTEGRATION ENGINE: Something went wrong when ensuring folders exist for each channel in the opportunity specified. Relevant context is as follows:
    Opportunity: {opportunityName}
    [INNER EXCEPTION]:
        Message: {ex.Message}
        Stacktrace: {ex.StackTrace}", ex);
                }
            }
            Debug.Unindent();
            throw new Exception($"Ensuring channel folders exist for opportunity {opportunityName} failed after retrying {retryPolicy.Count() - 1} times.");
		}
		
		public async Task RegisterOpportunityDeltaLinkAsync(string opportunityName, string resource)
		{
			if (!deltaLinksStorage.OpportunityDeltaLinks.ContainsKey(opportunityName))
			{
				deltaLinksStorage.OpportunityDeltaLinks.TryAdd(opportunityName, (string)(await new DriveItemRequestBuilder($"https://graph.microsoft.com/v1.0{resource}", graphClientContext.GraphClient).Delta("latest").Request().GetAsync()).AdditionalData["@odata.deltaLink"]);
			}
		}

		public async Task ProcessFormalProposalChangesAsync(string opportunityName, string resource)
		{
            var resetLink = $"https://graph.microsoft.com/v1.0{resource}/delta";
            var deltaLink = deltaLinksStorage.OpportunityDeltaLinks.ContainsKey(opportunityName) ? deltaLinksStorage.OpportunityDeltaLinks[opportunityName] : resetLink;
			var result = ProcessDriveChangesAsync(
				ref deltaLink,
				di =>
				(
					di.ParentReference?.Path == "/drive/root:/Formal Proposal" &&
					di.Name.EndsWith(".docx") &&
					true // change by CreatedBy check
				),
                async batch =>
				{
					var preResult = new List<Task>();
					var actions = batch.Select(d => new { FileName = d.Name, OpportunityName = d.ParentReference.Path.Split('/').Last() });
					var graphClient = graphClientContext.GraphClient;
					var item = batch.Last();

					var fileUri = $"https://graph.microsoft.com/v1.0{resource}:/Formal Proposal/{item.Name}?$select=@content.downloadUrl";

					var parts = new Uri(fileUri).Segments;
					var partsLength = parts.Length;
					var docName = parts[partsLength - 1];
					var oppName = parts[partsLength - 2].Replace("/", "");
                    try
                    {
                        var hrm = new HttpRequestMessage(HttpMethod.Get, fileUri);
						var response = new HttpResponseMessage();
                        
						// Authenticate (add access token) to the HttpRequestMessage
						await graphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);
                        
						// Send the request and get the response
						var jsonResponse = await graphClient.HttpProvider.SendAsync(hrm);
                        
						var dataJson = JsonConvert.DeserializeObject<GraphMetadata>(jsonResponse.Content.ReadAsStringAsync().Result);
                        
						var hrmContent = new HttpRequestMessage(HttpMethod.Get, dataJson.DownloadUrl);
                        
						await graphClient.AuthenticationProvider.AuthenticateRequestAsync(hrmContent);
                        
						var responseContent = await graphClient.HttpProvider.SendAsync(hrmContent);
                        
						var arr = await responseContent.Content.ReadAsByteArrayAsync();
                        
						var fileContent = new ByteArrayContent(arr);

                        var multiContent = new MultipartFormDataContent() {

                            { fileContent, "file", docName },
                            { new StringContent(opportunityName), "opportunityName"},
                            { new StringContent("ProposalTemplate"), "docType"}
                        };

                        var client = await proposalManagerClientFactory.GetProposalManagerClientAsync();
                        
                        var pmResult = await client.PutAsync($"/api/Document/UploadFile/{opportunityName}/ProposalTemplate", multiContent);

                        if(!pmResult.IsSuccessStatusCode)
                            throw new Exception($@"DYNAMICS INTEGRATION ENGINE: Something went wrong when expecting Proposal Manager to parse the document. Relevant context is as follows:
    Opportunity: {opportunityName}
    Resource: {resource}
    Proposal Manager result: {await pmResult.Content.ReadAsStringAsync()}");

                    }
                    catch (Exception ex)
                    {
                        throw new Exception($@"DYNAMICS INTEGRATION ENGINE: Something went wrong when processing file changes for the opportunity specified. Relevant context is as follows:
    Opportunity: {opportunityName}
    Resource: {resource}
    [INNER EXCEPTION]:
        Message: {ex.Message}
        Stacktrace: {ex.StackTrace}", ex);
                    }
                }, resetLink);
			/* This causes the next immediate request to be bypassed, given that it will correspond to the
			 * file overwriting that Proposal Manager performs after analyzing the document. */
			deltaLinksStorage.OpportunityDeltaLinks.Remove(opportunityName);
			await result;
		}

		public Task ProcessAttachmentChangesAsync(string resource)
		{
			var deltaLink = deltaLinksStorage.ProposalManagerDeltaLink;
			var result = ProcessDriveChangesAsync(
				ref deltaLink,
				di => di.ParentReference?.Path?.StartsWith("/drive/root:/TempFolder/") ?? false,
                async batch =>
				{
					var preResult = new List<Task>();
					var actions = batch.Select(d => new { FileName = d.Name, OpportunityName = d.ParentReference.Path.Split('/').Last() });
					var groupedActions = from a in actions
										 group a by a.OpportunityName into ga
										 select ga;
					foreach (var item in groupedActions)
					{
                        var opportunityName = item.Key;
                        var files = item.Select(i => i.FileName);
                        var client = await proposalManagerClientFactory.GetProposalManagerClientAsync();
                        var opportunityResult = await client.GetAsync($"/api/Opportunity?name={opportunityName}");
                        var opportunity = JsonConvert.DeserializeObject<JObject>(await opportunityResult.Content.ReadAsStringAsync());
                        var attachments = opportunity["documentAttachments"];
                        var diff = files.Except(attachments.Select(a => a["fileName"].ToString()));
                        foreach (var file in diff)
                        {
                            (attachments as JArray).Add(JObject.Parse($@"
								{{
									""fileName"": ""{file}"",
									""note"": """",
									""category"": {{
										""typeName"": ""Category"",
										""id"": """",
										""name"": """"
									}},
									""tags"": """",
									""documentUri"": ""TempFolder"",
									""typeName"": ""DocumentAttachment"",
									""id"": ""00000000-0000-0000-0000-000000000000""
								}}
							"));
                        }
                        preResult.Add(client.PatchAsync("/api/Opportunity", new StringContent(opportunity.ToString(), Encoding.UTF8, "application/json")));
                    }
                    await Task.WhenAll(preResult);
				}, $"https://graph.microsoft.com/v1.0{resource}/delta");
			deltaLinksStorage.ProposalManagerDeltaLink = deltaLink;
			return result;
		}

		private Task ProcessDriveChangesAsync(ref string deltaLink, Func<DriveItem, bool> relevancePredicate, Func<IEnumerable<DriveItem>, Task> action, string resetLink)
		{
			var graph = graphClientContext.GraphClient;
			IDriveItemDeltaRequest deltaRequest = new DriveItemDeltaRequest(deltaLink, graph, null);
			IDriveItemDeltaCollectionPage delta = null;
			var preResult = new List<Task>();
			do
			{
				try
				{
					delta = deltaRequest.GetAsync().Result;
				}
				catch (Exception e) when ((e.InnerException as ServiceException)?.Error?.Code == "resyncRequired")
				{
					delta = new DriveItemDeltaRequest(resetLink, graph, null).GetAsync().Result;
				}
				var relevantItems = delta.Where(relevancePredicate);
				if (!relevantItems.Any())
				{
					continue;
				}
				preResult.Add(action.Invoke(relevantItems));
			} while ((deltaRequest = delta.NextPageRequest) != null);
			deltaLink = (string)delta.AdditionalData["@odata.deltaLink"];
			return Task.WhenAll(preResult);
		}

		public async Task SubscribeToFormalProposalChangesAsync(string opportunityName)
		{
			var graph = graphClientContext.GraphClient;
			var opportunityGroup = (await graph.Groups.Request().Filter($"displayName eq '{opportunityName}'").Select("id").GetAsync()).First();
			var resource = $"/groups/{opportunityGroup.Id}/sites/root/drive/root";
			await graph.Subscriptions.Request().AddAsync(
				new Subscription
				{
					Resource = resource,
					ChangeType = "updated",
					ClientState = new SubscriptionClientStateDto
					{
						Secret = oneDriveConfiguration.WebhookSecret,
						Data = opportunityName
					}.ToJson(),
					ExpirationDateTime = DateTimeOffset.Now.AddDays(3),
					NotificationUrl =  azureAdOptions.BaseUrl + oneDriveConfiguration.FormalProposalCallbackRelativeUrl
				});
			await RegisterOpportunityDeltaLinkAsync(opportunityName, resource);
		}

		public async Task SubscribeToTempFolderChangesAsync()
		{
			var graph = graphClientContext.GraphClient;
			var subscriptionsRequest = graph.Subscriptions.Request();
			var subscriptions = await subscriptionsRequest.GetAsync();
			var ids = subscriptions.Where(s => s.Resource == $"/sites/{appOptions.ProposalManagementRootSiteId}/drive/root").Select(s => s.Id);
			await Task.WhenAll(ids.Select(id => graph.Subscriptions[id].Request().DeleteAsync()));
			await subscriptionsRequest.AddAsync(
				new Subscription
				{
					Resource = $"/sites/{appOptions.ProposalManagementRootSiteId}/drive/root",
					ChangeType = "updated",
					ClientState = new SubscriptionClientStateDto
					{
						Secret = oneDriveConfiguration.WebhookSecret,
						Data = null
					}.ToJson(),
					ExpirationDateTime = DateTimeOffset.Now.AddDays(3),
					NotificationUrl = azureAdOptions.BaseUrl + oneDriveConfiguration.AttachmentCallbackRelativeUrl
				});
		}

		public async Task RenewAllSubscriptionsAsync()
		{
			var graph = graphClientContext.GraphClient;
			var subscriptionsRequest = graph.Subscriptions.Request();
			var subscriptions = await subscriptionsRequest.GetAsync();

			Console.WriteLine("Before:");
			Console.WriteLine(JsonConvert.SerializeObject(subscriptions, Formatting.Indented));

			await Task.WhenAll(subscriptions.Select(s =>
			{
				s.ExpirationDateTime = DateTimeOffset.Now.AddDays(3);
				return graph.Subscriptions[s.Id].Request().UpdateAsync(s);
			}));

			Console.WriteLine("After:");
			Console.WriteLine(JsonConvert.SerializeObject(await subscriptionsRequest.GetAsync(), Formatting.Indented));
		}
	}

	public class SubscriptionClientStateDto
	{
		public string Secret { get; set; }
		public object Data { get; set; }
		public string ToJson() => JsonConvert.SerializeObject(this);
		public static SubscriptionClientStateDto FromJson(string json) => JsonConvert.DeserializeObject<SubscriptionClientStateDto>(json);
	}

	public class GraphMetadata
	{
		[JsonProperty("@microsoft.graph.downloadUrl")]
		public string DownloadUrl { get; set; }
		[JsonProperty("@odata.deltalink")]
		public string DeltaLink { get; set; }
	}
}