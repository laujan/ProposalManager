// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using CommLendingWeb.Extensions;
using CommLendingWeb.Helpers;
using CommLendingWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CommLendingWeb.Controllers
{
	[Authorize]
	public class DocumentController : BaseController
    {
		private readonly string SiteId;
		private readonly string ProposalManagerApiUrl;
		public DocumentController(IConfiguration configuration, IGraphSdkHelper graphSdkHelper) :
			base(configuration, graphSdkHelper)
		{
			// Get from config
			var appOptions = new AppOptions();
			configuration.Bind("AppOptions", appOptions);
			ProposalManagerApiUrl = appOptions.ProposalManagerApiUrl;
			SiteId = appOptions.SiteId;
		}

		[HttpGet]
		public async Task<string> GetFormalProposal(string id)
		{
			try
			{
				var graphClient = GraphHelper.GetAuthenticatedClient();
				var uri = $"https://graph.microsoft.com/v1.0/sites/{SiteId}:/sites/{id}?$select=displayName";
				var request = new HttpRequestMessage(HttpMethod.Get, uri);

				await graphClient.AuthenticationProvider.AuthenticateRequestAsync(request);

				var response = await graphClient.HttpProvider.SendAsync(request);

				if (!response.IsSuccessStatusCode)
				{
					throw new Exception($"Error retrieving opportunity name: {response.ReasonPhrase}");
				}

				var json = await response.Content.ReadAsStringAsync();

				var oppData = JsonConvert.DeserializeObject<JObject>(json); 

				var opportunityName = oppData?["displayName"]?.ToString();

				if (string.IsNullOrEmpty(opportunityName))
				{
					throw new ArgumentException($"No site found {id}");
				}

				var client = GetAuthorizedWebClient();
				
				var opportunity = await client.GetStringAsync($"{ProposalManagerApiUrl}/api/Opportunity?name={System.Web.HttpUtility.UrlEncode(opportunityName)}");

				return opportunity;
			}catch(Exception ex)
			{
				throw ex;
			}
		}

		[HttpGet]
		public async Task<IEnumerable<Document>> List(string id)
		{
			//TODO: try to use graph client proxy entities
			// if not feasible then filter in odata query by displayName eq 'Documents' to reduce payload
			//var items = await graphClient.Sites[$"{SiteId}"].Sites[id].Lists.Request().Expand("Items").GetAsync();
			// Initialize the GraphServiceClient.
			try
			{
				var graphClient = GraphHelper.GetAuthenticatedClient();
				var uri = $"https://graph.microsoft.com/v1.0/sites/{SiteId}:/sites/{id}:/lists?$expand=items";
				var request = new HttpRequestMessage(HttpMethod.Get, uri);

				await graphClient.AuthenticationProvider.AuthenticateRequestAsync(request);

				var response = await graphClient.HttpProvider.SendAsync(request);

				if (!response.IsSuccessStatusCode)
				{
					throw new Exception($"Error retrieving documents: {response.ReasonPhrase}");
				}

				var json = await response.Content.ReadAsStringAsync();

				dynamic items = JsonConvert.DeserializeObject(json);

				if(!items.value.HasValues)
				{
					return Enumerable.Empty<Document>();
				}

				var result = new List<Document>();

				foreach (var list in items.value)
				{
					if (list.displayName == "Documents")
					{
						foreach (var item in list.items)
						{
							var webUrl = item.webUrl.ToString();
							result.Add(
								new Document()
								{
									Id = item.id,
									WebUrl = item.webUrl,
									CreatedByUser = new User() { Id = item.createdBy.user.id, DisplayName = item.createdBy.user.displayName },
									LastModifiedByUser = new User() { Id = item.lastModifiedBy.user.id, DisplayName = item.lastModifiedBy.user.displayName },
									LastModifiedDateTime = item.lastModifiedDateTime,
									CreatedDateTime = item.createdDateTime,
									Type = webUrl.Substring(webUrl.LastIndexOf('.') + 1),
									Name = webUrl.Substring(webUrl.LastIndexOf('/') + 1)
								});
						}
					}
				}

				return result.OrderBy(x => x.Name);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}

		private HttpClient GetAuthorizedWebClient()
		{
			var client = new HttpClient();
			var token = GraphHelper.GetProposalManagerToken().GetAwaiter().GetResult();

			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(
				new MediaTypeWithQualityHeaderValue("application/json"));
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			return client;
		}
	}
}
