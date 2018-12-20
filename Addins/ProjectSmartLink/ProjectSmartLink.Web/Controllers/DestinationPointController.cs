// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectSmartLink.Entity;
using ProjectSmartLink.Service;
using ProjectSmartLink.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace ProjectSmartLink.Web.Controllers
{
	[Authorize]
    public class DestinationPointController : BaseController
    {
        protected readonly IDestinationService _destinationService;
        protected readonly IMapper _mapper;
		private readonly string clientId;
		private readonly string aadInstance;
		private readonly string tenantId;
		private readonly string appKey;
		private readonly string resourceId;
		private readonly string sharePointUrl;
		private readonly string authority;

		public DestinationPointController(IConfiguration config, IDestinationService destinationService, IMapper mapper) : 
			base(config)
		{
			_destinationService = destinationService;
			_mapper = mapper;
			clientId = AzureAdConfig.ClientId;
			aadInstance = AzureAdConfig.Instance;
			tenantId = AzureAdConfig.TenantId;
			appKey = AzureAdConfig.ClientSecret;
			resourceId = AzureAdConfig.ResourceId;
			sharePointUrl = AzureAdConfig.SharePointUrl;
			authority = aadInstance + tenantId;
		}

        [HttpPost]
        [Route("api/DestinationPoint")]
        public async Task<IActionResult> Post([FromForm]DestinationPointForm destinationPointAdded)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid posted data.");
            }

            try
            {
                var destinationPoint = _mapper.Map<DestinationPoint>(destinationPointAdded);
                var catalogName = HttpUtility.UrlDecode(destinationPointAdded.CatalogName);
                var documentId = HttpUtility.UrlDecode(destinationPointAdded.DocumentId);
                return Ok(await _destinationService.AddDestinationPoint(catalogName, documentId, destinationPoint));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        [HttpDelete]
        [Route("api/DestinationPoint")]
        public async Task<IActionResult> DeleteSourcePoint(string id)
        {
            await _destinationService.DeleteDestinationPoint(Guid.Parse(id));
            return Ok();
        }

        [HttpPost]
        [Route("api/DeleteSelectedDestinationPoint")]
        public async Task<IActionResult> DeleteSelectedDestinationPoint([FromForm]IEnumerable<Guid> seletedIds)
        {
            await _destinationService.DeleteSelectedDestinationPoint(seletedIds);
            return Ok();
        }

        [HttpGet]
        [Route("api/DestinationPointCatalog")]
        public async Task<IActionResult> GetDestinationPointCatalog(string fileName, string documentId)
        {
            var retValue = await _destinationService.GetDestinationCatalog(HttpUtility.UrlDecode(fileName), HttpUtility.UrlDecode(documentId));
            return Ok(retValue);
        }

        [HttpGet]
        [Route("api/DestinationPoint")]
        public async Task<IActionResult> GetDestinationPointBySourcePoint(string sourcePointId)
        {
            var retValue = await _destinationService.GetDestinationPointBySourcePoint(Guid.Parse(sourcePointId));
            return Ok(retValue);
        }

		[HttpGet]
		[Route("api/GraphAccessToken")]
		public async Task<IActionResult> GetGraphAccessToken()
		{
			try
			{
				ClientCredential clientCred = new ClientCredential(appKey);
				string userAccessToken = ((ClaimsIdentity)HttpContext.User.Identity).BootstrapContext.ToString();
				UserAssertion userAssertion = new UserAssertion(userAccessToken, "urn:ietf:params:oauth:grant-type:jwt-bearer");


				const string siteUrl = "smartlink.azurewebsites.net";

				ConfidentialClientApplication cca =
					new ConfidentialClientApplication(clientId,
														$"https://{siteUrl}", clientCred, null, null);

				
				AuthenticationResult result = await cca.AcquireTokenOnBehalfOfAsync(new[] { resourceId }, userAssertion, "https://login.microsoftonline.com/common/oauth2/v2.0");
				return Ok(result.AccessToken);
			}
			catch(Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

        [HttpGet]
        [Route("api/CustomFormats")]
        public async Task<IActionResult> GetCustomFormats()
        {
            var retValue = await _destinationService.GetCustomFormats();
            return Ok(retValue);
        }

        [HttpPut]
        [Route("api/UpdateDestinationPointCustomFormat")]
        public async Task<IActionResult> UpdateDestinationPointCustomFormat([FromForm]DestinationPointForm destinationPointAdded)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid posted data.");
            }

            try
            {
                var destinationPoint = _mapper.Map<DestinationPoint>(destinationPointAdded);
                return Ok(await _destinationService.UpdateDestinationPointCustomFormat(destinationPoint));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

		[HttpGet]
		[Route("api/SharePointAccessToken")]
		public async Task<IActionResult> GetSharePointAccessToken()
		{
			try
			{
				ClientCredential clientCred = new ClientCredential(appKey);
				string userAccessToken = ((ClaimsIdentity)HttpContext.User.Identity).BootstrapContext.ToString();
				UserAssertion userAssertion = new UserAssertion(userAccessToken, "urn:ietf:params:oauth:grant-type:jwt-bearer");


				const string siteUrl = "smartlink.azurewebsites.net";

				ConfidentialClientApplication cca =
					new ConfidentialClientApplication(clientId,
														$"https://{siteUrl}", clientCred, null, null);


				AuthenticationResult result = await cca.AcquireTokenOnBehalfOfAsync(new[] { sharePointUrl }, userAssertion, $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0");
				return Ok(result.AccessToken);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}
	}
}
