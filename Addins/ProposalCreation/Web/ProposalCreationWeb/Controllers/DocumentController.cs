// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ProposalCreation.Core.Helpers;
using ProposalCreation.Core.Models;
using ProposalCreation.Core.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProposalCreationWeb.Controllers
{
	[Authorize]
	public class DocumentController : BaseController
	{
		private readonly string SiteId;
		private readonly string ProposalManagerApiUrl;
		private readonly IGraphSdkHelper httpHelper;

		public DocumentController(
			IGraphSdkHelper graphSdkHelper,
			IRootConfigurationProvider rootConfigurationProvider) : base(graphSdkHelper)
		{
			// Get from config
			var appOptions        = rootConfigurationProvider.GeneralConfiguration;

			ProposalManagerApiUrl = appOptions.ProposalManagerApiUrl;
			SiteId                = appOptions.SiteId;

			httpHelper = graphSdkHelper;
		}

		[HttpPost]
		public async Task<IActionResult> UpdateTask(string opportunityId, string documentData)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(opportunityId))
				{
					return BadRequest($"{nameof(opportunityId)} is required");
				}
				if (string.IsNullOrWhiteSpace(documentData))
				{
					return BadRequest($"{nameof(documentData)} is required");
				}

				var uri = $"{ProposalManagerApiUrl}/api/Opportunity";
				var client = await httpHelper.GetProposalManagerWebClientAsync();
				var content = new StringContent(documentData, Encoding.UTF8, "application/json");
				var request = new HttpRequestMessage(new HttpMethod("PATCH"), uri)
				{
					Content = content
				};

				var response = await client.SendAsync(request);

				if (!response.IsSuccessStatusCode)
				{
					return BadRequest(response.ReasonPhrase);
				}

				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest($"Error updating Opportunity: {ex.Message}");
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetFormalProposal(string id)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(id))
				{
					return BadRequest($"{nameof(id)} is required");
				}

				var graphClient = GraphHelper.GetAuthenticatedClient();
				var uri = $"https://graph.microsoft.com/v1.0/sites/{SiteId}:/sites/{id}?$select=displayName";
				var request = new HttpRequestMessage(HttpMethod.Get, uri);

				await graphClient.AuthenticationProvider.AuthenticateRequestAsync(request);

				var response = await graphClient.HttpProvider.SendAsync(request);

				if (!response.IsSuccessStatusCode)
				{
					return BadRequest($"Error retrieving opportunity name: {response.ReasonPhrase}");
				}

				var json = await response.Content.ReadAsStringAsync();

				var oppData = JsonConvert.DeserializeObject<JObject>(json);

				var opportunityName = oppData?["displayName"]?.ToString();

				if (string.IsNullOrEmpty(opportunityName))
				{
					return BadRequest($"No site found {id}");
				}

				var client = await httpHelper.GetProposalManagerWebClientAsync();

				var opportunity = await client.GetStringAsync($"{ProposalManagerApiUrl}/api/Opportunity?name={System.Web.HttpUtility.UrlEncode(opportunityName)}");

				return Ok(opportunity);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpGet]
		public async Task<IActionResult> List(string id)
		{
			//TODO: try to use graph client proxy entities
			// if not feasible then filter in odata query by displayName eq 'Documents' to reduce payload
			//var items = await graphClient.Sites[$"{SiteId}"].Sites[id].Lists.Request().Expand("Items").GetAsync();
			// Initialize the GraphServiceClient.
			try
			{
				if (string.IsNullOrWhiteSpace(id))
				{
					return BadRequest($"{nameof(id)} is required.");
				}

				var graphClient = GraphHelper.GetAuthenticatedClient();
				var uri = $"https://graph.microsoft.com/v1.0/sites/{SiteId}:/sites/{id}:/lists?$expand=items";
				var request = new HttpRequestMessage(HttpMethod.Get, uri);

				await graphClient.AuthenticationProvider.AuthenticateRequestAsync(request);

				var response = await graphClient.HttpProvider.SendAsync(request);

				if (!response.IsSuccessStatusCode)
				{
					return BadRequest($"Error retrieving documents: {response.ReasonPhrase}");
				}

				var json = await response.Content.ReadAsStringAsync();

				dynamic items = JsonConvert.DeserializeObject(json);

				if (!items.value.HasValues)
				{
					return Ok(Enumerable.Empty<Document>());
				}

				var result = new List<Document>();

				foreach (var listItem in items.value)
				{
					var template = listItem.list?.template;
					
					if (template != null && template.ToString().Equals("documentLibrary", StringComparison.InvariantCultureIgnoreCase))
					{
						foreach (var item in listItem.items)
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

				return Ok(result.OrderBy(x => x.Name));
			}
			catch (Exception ex)
			{
				return BadRequest($"An error occurred: {ex.Message}");
			}
		}

	}

}