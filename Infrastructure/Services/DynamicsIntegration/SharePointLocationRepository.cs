// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore;
using ApplicationCore.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
	public class SharePointLocationRepository : ISharePointLocationRepository
	{
		private readonly Dynamics365Configuration dynamicsConfiguration;
        private readonly AppOptions appOptions;
		private readonly IDynamicsClientFactory dynamicsClientFactory;
		private readonly GraphServiceClient graphClient;
		private readonly ISharePointLocationsCache sharePointLocationsCache;

        public const string TempFolderName = "TempFolder";

		public SharePointLocationRepository(
			IConfiguration configuration,
			IDynamicsClientFactory dynamicsClientFactory,
			ISharePointLocationsCache sharePointLocationsCache,
			IGraphClientAppContext graphClientContext)
		{
			dynamicsConfiguration = new Dynamics365Configuration();
            appOptions = new AppOptions();
			configuration.Bind(Dynamics365Configuration.ConfigurationName, dynamicsConfiguration);
            configuration.Bind("ProposalManagement", appOptions);
			this.sharePointLocationsCache = sharePointLocationsCache;
			graphClient = graphClientContext.GraphClient;
			this.dynamicsClientFactory = dynamicsClientFactory;
			if(!Guid.TryParse(sharePointLocationsCache.ProposalManagerSiteId, out Guid existingSiteId))
			{
				var result = dynamicsClientFactory.GetDynamicsAuthorizedWebClientAsync().Result.GetAsync($"/api/data/v9.0/sharepointsites?$filter=absoluteurl eq 'https://{appOptions.SharePointHostName}'").Result;
                if (!Guid.TryParse(
                    sharePointLocationsCache.ProposalManagerSiteId =
                        JsonConvert.DeserializeObject<JObject>(result.Content.ReadAsStringAsync().Result)["value"].AsJEnumerable()
                        .Single()["sharepointsiteid"].ToString()
                    , out existingSiteId))
                    throw new Exception("DYNAMICS INTEGRATION ENGINE: Tenant Site is not registered in Dynamics.");
			}
			if(!Guid.TryParse(sharePointLocationsCache.ProposalManagerBaseSiteId, out Guid existingBaseSiteId))
			{
				var result = dynamicsClientFactory.GetDynamicsAuthorizedWebClientAsync().Result.GetAsync($"/api/data/v9.0/sharepointsites?$filter=parentsite/sharepointsiteid eq '{sharePointLocationsCache.ProposalManagerSiteId}' and relativeurl eq 'sites/{appOptions.SharePointSiteRelativeName}'").Result;
                if (!Guid.TryParse(
                    sharePointLocationsCache.ProposalManagerBaseSiteId =
					    JsonConvert.DeserializeObject<JObject>(result.Content.ReadAsStringAsync().Result)["value"].AsJEnumerable()
					    .Single()["sharepointsiteid"].ToString()
                    , out existingSiteId))
                    throw new Exception("DYNAMICS INTEGRATION ENGINE: Proposal Manager Site is not registered in Dynamics.");
            }
			if(!Guid.TryParse(sharePointLocationsCache.RootDriveLocationId, out Guid existingDriveId))
			{
				var result = dynamicsClientFactory.GetDynamicsAuthorizedWebClientAsync().Result.GetAsync($"/api/data/v9.0/sharepointdocumentlocations?$filter=parentsiteorlocation_sharepointsite/sharepointsiteid eq '{sharePointLocationsCache.ProposalManagerBaseSiteId}' and relativeurl eq '{dynamicsConfiguration.RootDrive}'").Result;
                if (!Guid.TryParse(
                    sharePointLocationsCache.RootDriveLocationId =
					    JsonConvert.DeserializeObject<JObject>(result.Content.ReadAsStringAsync().Result)["value"].AsJEnumerable()
					    .Single()["sharepointdocumentlocationid"].ToString()
                    , out existingSiteId))
                    throw new Exception("DYNAMICS INTEGRATION ENGINE: Proposal Manager Site Drive is not registered in Dynamics.");
            }
			if(!Guid.TryParse(sharePointLocationsCache.TempFolderLocationId, out Guid existingTempFolderId))
			{
				var result = dynamicsClientFactory.GetDynamicsAuthorizedWebClientAsync().Result.GetAsync($"/api/data/v9.0/sharepointdocumentlocations?$filter=parentsiteorlocation_sharepointdocumentlocation/sharepointdocumentlocationid eq '{sharePointLocationsCache.RootDriveLocationId}' and relativeurl eq '{TempFolderName}'").Result;
                if (!Guid.TryParse(
                    sharePointLocationsCache.TempFolderLocationId = 
					    JsonConvert.DeserializeObject<JObject>(result.Content.ReadAsStringAsync().Result)["value"].AsJEnumerable()
					    .Single()["sharepointdocumentlocationid"].ToString()
                    , out existingSiteId))
                    throw new Exception("DYNAMICS INTEGRATION ENGINE: Proposal Manager Temporary Folder is not registered in Dynamics.");
            }
		}

		public string ProposalManagerSiteId => sharePointLocationsCache.ProposalManagerSiteId;

		public async Task CreateTemporaryLocationForOpportunityAsync(string opportunityId, string opportunityName)
		{
			var jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

			var client = await dynamicsClientFactory.GetDynamicsAuthorizedWebClientAsync();

			var result = await client.PostAsync("/api/data/v9.0/sharepointdocumentlocations",
				new StringContent(
				    JsonConvert.SerializeObject(new SharePointDocumentLocation
				    {
					    ParentLocationId = $"sharepointdocumentlocations({sharePointLocationsCache.TempFolderLocationId})",
					    RegardingObjectId = $"opportunities({opportunityId})",
					    RelativeUrl = opportunityName,
					    Name = "General"
				    }, jsonSerializerSettings), Encoding.UTF8, "application/json"));

            if (!result.IsSuccessStatusCode)
                throw new Exception(await result.Content.ReadAsStringAsync());

		}

		public async Task DeleteTemporaryLocationForOpportunityAsync(string opportunityName)
		{
			var jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

			var client = await dynamicsClientFactory.GetDynamicsAuthorizedWebClientAsync();

			var result = dynamicsClientFactory.GetDynamicsAuthorizedWebClientAsync().Result.GetAsync($"/api/data/v9.0/sharepointdocumentlocations?$filter=parentsiteorlocation_sharepointdocumentlocation/sharepointdocumentlocationid eq '{sharePointLocationsCache.TempFolderLocationId}' and relativeurl eq '{opportunityName}'").Result;
			var location = JsonConvert.DeserializeObject<JObject>(result.Content.ReadAsStringAsync().Result)["value"].AsJEnumerable()
				.SingleOrDefault();

			if (location != null)
				await client.DeleteAsync($"/api/data/v9.0/sharepointdocumentlocations({location["sharepointdocumentlocationid"].ToString()})");
		}

		public async Task CreateLocationsForOpportunityAsync(string opportunityId, string opportunityName, IEnumerable<string> locations)
		{
			var jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

			var client = await dynamicsClientFactory.GetDynamicsAuthorizedWebClientAsync();

			var opportunityGroup = (await graphClient.Groups.Request().Filter($"displayName eq '{opportunityName}'").Select("id").GetAsync()).First();
			var siteUrl = (await graphClient.Groups[opportunityGroup.Id].Sites["root"].Request().GetAsync()).WebUrl;
			var driveUrl = (await graphClient.Groups[opportunityGroup.Id].Sites["root"].Drive.Request().GetAsync()).WebUrl;

			client.DefaultRequestHeaders.Add("Prefer", "return=representation");
			var siteResult = client.PostAsync("/api/data/v9.0/sharepointsites?$select=sharepointsiteid",
				new StringContent(
					JsonConvert.SerializeObject(new SharePointSite
					{
						Name = opportunityName,
						ParentSiteId = $"sharepointsites({sharePointLocationsCache.ProposalManagerSiteId})",
						RelativeUrl = siteUrl.Replace($"https://{appOptions.SharePointHostName}", string.Empty, StringComparison.InvariantCultureIgnoreCase).TrimStart('/')
					}, jsonSerializerSettings), Encoding.UTF8, "application/json")).Result;
			var createdSharePointSiteId = JsonConvert.DeserializeObject<JObject>(siteResult.Content.ReadAsStringAsync().Result)["sharepointsiteid"].ToString();
			var masterLocationResult = await client.PostAsync("/api/data/v9.0/sharepointdocumentlocations?$select=sharepointdocumentlocationid",
					new StringContent(
					JsonConvert.SerializeObject(new SharePointDocumentLocation
					{
						ParentSiteId = $"sharepointsites({createdSharePointSiteId})",
						RelativeUrl = WebUtility.UrlDecode(driveUrl.Replace(siteUrl, string.Empty, StringComparison.InvariantCultureIgnoreCase).TrimStart('/')),
						Name = opportunityName
					}, jsonSerializerSettings), Encoding.UTF8, "application/json"));
			var createdSharePointDocumentLocationId = JsonConvert.DeserializeObject<JObject>(masterLocationResult.Content.ReadAsStringAsync().Result)["sharepointdocumentlocationid"].ToString();
			client.DefaultRequestHeaders.Remove("Prefer");
			foreach (var location in locations)
				await client.PostAsync("/api/data/v9.0/sharepointdocumentlocations",
					new StringContent(
					JsonConvert.SerializeObject(new SharePointDocumentLocation
					{
						ParentLocationId = $"sharepointdocumentlocations({createdSharePointDocumentLocationId})",
						RegardingObjectId = $"opportunities({opportunityId})",
						RelativeUrl = location,
						Name = location
					}, jsonSerializerSettings), Encoding.UTF8, "application/json"));
		}

		private class SharePointSite
		{
			[JsonProperty("name")]
			public string Name { get; set; }
			[JsonProperty("parentsite@odata.bind")]
			public string ParentSiteId { get; set; }
			[JsonProperty("relativeurl")]
			public string RelativeUrl { get; set; }
		}

		private class SharePointDocumentLocation
		{
			[JsonProperty("parentsiteorlocation_sharepointsite@odata.bind")]
			public string ParentSiteId { get; set; }
			[JsonProperty("parentsiteorlocation_sharepointdocumentlocation@odata.bind")]
			public string ParentLocationId { get; set; }
			[JsonProperty("regardingobjectid_opportunity@odata.bind")]
			public string RegardingObjectId { get; set; }
			[JsonProperty("relativeurl")]
			public string RelativeUrl { get; set; }
			[JsonProperty("name")]
			public string Name { get; set; }
		}
	}
}