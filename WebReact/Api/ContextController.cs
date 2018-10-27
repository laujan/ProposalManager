// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Interfaces;
using ApplicationCore.Helpers;
using ApplicationCore.Artifacts;
using Newtonsoft.Json.Linq;
using ApplicationCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace WebReact.Api
{
	[Authorize(AuthenticationSchemes = "AzureAdBearer")]
	public class ContextController : BaseApiController<ContextController>
    {
        private readonly IContextService _contextService;
        private readonly IOpportunityService _opportunityService;

        public ContextController(
            ILogger<ContextController> logger, 
            IOptions<AppOptions> appOptions,
            IContextService contextService,
            IOpportunityService opportunityService) : base(logger, appOptions)
        {
            Guard.Against.Null(contextService, nameof(contextService));
            Guard.Against.Null(opportunityService, nameof(opportunityService));
            _contextService = contextService;
            _opportunityService = opportunityService;
        }

        [HttpGet("GetClientSettings", Name = "GetClientSettings")]
        public async Task<IActionResult> GetClientSettings()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} GetClientSettings called.");

            try
            {
                var response = await _contextService.GetClientSetingsAsync();
                var responseJObject = JObject.FromObject(response);
                return Ok(responseJObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} GetClientSettings error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"GetClientSettings error: {ex.Message}", requestId);

                return BadRequest(errorResponse);
            }
        }
        // Get: /Context/GetSiteDrive
        [HttpGet("GetSiteDrive/{siteName}", Name = "GetSiteDrive")]
        public async Task<IActionResult> GetSiteDrive(string siteName)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} GetSiteDrive called.");

            try
            {
                if (siteName == null)
                {
                    _logger.LogError($"RequestID:{requestId} GetSiteDrive error: siteName null");
                    var errorResponse = JsonErrorResponse.BadRequest("GetSiteDrive error: siteName null", requestId);

                    return BadRequest(errorResponse);
                }

                var response = await _contextService.GetSiteDriveAsync(siteName);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} GetSiteDrive error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"RequestID:{requestId} GetSiteDrive error: {ex.Message}", requestId);

                return BadRequest(errorResponse);
            }
        }

        // Get: /Context/GetSiteDrive
        [HttpGet("GetSiteId/{siteName}", Name = "GetSiteId")]
        public async Task<IActionResult> GetSiteId(string siteName)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} GetSiteId called.");

            try
            {
                if (siteName == null)
                {
                    _logger.LogError($"RequestID:{requestId} GetSiteId error: siteName null");
                    var errorResponse = JsonErrorResponse.BadRequest("GetSiteId error: siteName null", requestId);

                    return BadRequest(errorResponse);
                }

                var response = await _contextService.GetSiteIdAsync(siteName);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} GetSiteId error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"GetSiteId error: {ex.Message}", requestId);

                return BadRequest(errorResponse);
            }
        }


		[HttpGet("GetOpportunityStatusAll", Name = "GetOpportunityStatusAll")]
		public async Task<IActionResult> GetOpportunityStatusAll()
		{
			var requestId = Guid.NewGuid().ToString();
			_logger.LogInformation($"RequestID:{requestId} GetOpportunityStatusAll called.");

			try
			{
				var response = await _contextService.GetOpportunityStatusAllAsync();

				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError($"RequestID:{requestId} GetOpportunityStatusAll error: {ex.Message}");
				var errorResponse = JsonErrorResponse.BadRequest($"GetOpportunityStatusAll error: {ex.Message}", requestId);

				return BadRequest(errorResponse);
			}
		}

		[HttpGet("GetActionStatusAll", Name = "GetActionStatusAll")]
		public async Task<IActionResult> GetActionStatusAll()
		{
			var requestId = Guid.NewGuid().ToString();
			_logger.LogInformation($"RequestID:{requestId} GetActionStatusAll called.");

			try
			{
				var response = await _contextService.GetActionStatusAllAsync();

				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError($"RequestID:{requestId} GetActionStatusAll error: {ex.Message}");
				var errorResponse = JsonErrorResponse.BadRequest($"GetActionStatusAll error: {ex.Message}", requestId);

				return BadRequest(errorResponse);
			}
		}

        //[HttpGet("GetPermissionsAll", Name = "GetPermissionsAll")]
        //public async Task<IActionResult> GetPermissionsAll()
        //{
        //    var requestId = Guid.NewGuid().ToString();
        //    _logger.LogInformation($"RequestID:{requestId} GetPermissionsAll called.");

        //    try
        //    {
        //        var response = await _contextService.GetPermissionsAllAsync(requestId);

        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"RequestID:{requestId} GetPermissionsAll error: {ex.Message}");
        //        var errorResponse = JsonErrorResponse.BadRequest($"GetPermissionsAll error: {ex.Message}", requestId);

        //        return BadRequest(errorResponse);
        //    }
        //}
    }
}
