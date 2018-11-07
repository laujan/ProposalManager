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
using Microsoft.AspNetCore.Authorization;
using Infrastructure.Services;

namespace WebReact.Api
{
    [Route("api/[controller]")]
    [ApiController]

    [Authorize(AuthenticationSchemes = "AzureAdBearer")]
    public class PowerBiController : BaseApiController<PowerBiController>
    {

        private readonly IPowerBIService _pbiService;

        public PowerBiController(
            ILogger<PowerBiController> logger,
            IOptions<AppOptions> appOptions,
            IPowerBIService pbiService) : base(logger, appOptions)
        {
            Guard.Against.Null(pbiService, nameof(pbiService));
            _pbiService = pbiService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - PowerBIController_GetPBIToken called.");

            try
            {
                var pbiToken = await _pbiService.GenerateTokenAsync(requestId);

                if (String.IsNullOrEmpty(pbiToken))
                {
                    _logger.LogError($"RequestID:{requestId} PowerBIController_GetPBIToken error: could not get on behalf access token");
                    var errorResponse = JsonErrorResponse.BadRequest($"RequestID:{requestId} PowerBIController_GetPBIToken error: could not get on behalf access token", requestId);
                    return BadRequest(errorResponse);
                }

                return Ok(pbiToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - PowerBIController_GetPBIToken error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"PowerBIController_GetPBIToken error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }
    }

}