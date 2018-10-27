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
using Newtonsoft.Json;
using ApplicationCore.ViewModels;

namespace WebReact.Api
{
    [Authorize(AuthenticationSchemes = "AzureAdBearer")]
    public class TemplateController : BaseApiController<TemplateController>
    {
        private readonly ITemplateService _templateService;

        public TemplateController(
            ILogger<TemplateController> logger,
            IOptions<AppOptions> appOptions,
            ITemplateService templateService) : base(logger, appOptions)
        {
            Guard.Against.Null(templateService, nameof(templateService));
            _templateService = templateService;
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Template_GetAll called.");

            try
            {
                var modelList = (await _templateService.GetAllAsync(requestId));
                Guard.Against.Null(modelList, nameof(modelList), requestId);

                if (modelList.ItemsList.Count == 0)
                {
                    _logger.LogError($"RequestID:{requestId} - Template_GetAll no items found.");
                    return NotFound($"RequestID:{requestId} - Template_GetAll no items found");
                }

                return Ok(modelList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - Template_GetAll error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Template_GetAll error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JObject jsonObject)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Template_Create called.");

            try
            {
                if (jsonObject == null)
                {
                    _logger.LogError($"RequestID:{requestId} - Template_Create error: null");
                    var errorResponse = JsonErrorResponse.BadRequest($"Template_Create error: null", requestId);

                    return BadRequest(errorResponse);
                }

                var modelObject = JsonConvert.DeserializeObject<TemplateViewModel>(jsonObject.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                //check for duplicate processes
                bool check = await _templateService.ProcessCheckAsync(modelObject.ProcessList, requestId);
                if (!check)
                {
                    _logger.LogError($"RequestID:{requestId} - Create error: Duplicate Process Types");
                    var errorResponse = JsonErrorResponse.BadRequest($"Create error: Duplicate Process Types", requestId);

                    return BadRequest(errorResponse);
                }
                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(modelObject.TemplateName))
                {
                    _logger.LogError($"RequestID:{requestId} - Template_Create error: invalid name");
                    var errorResponse = JsonErrorResponse.BadRequest($"Template_Create error: invalid name", requestId);

                    return BadRequest(errorResponse);
                }

                var resultCode = await _templateService.CreateItemAsync(modelObject, requestId);

                if (resultCode != ApplicationCore.StatusCodes.Status201Created)
                {
                    _logger.LogError($"RequestID:{requestId} - Template_Create error: {resultCode.Name}");
                    var errorResponse = JsonErrorResponse.BadRequest($"Template_Create error: {resultCode.Name}", requestId);

                    return BadRequest(errorResponse);
                }

                var location = "/Template/Create/new"; // TODO: Get the id from the results but need to wire from factory to here

                return Created(location, $"RequestId: {requestId} - Template created.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} Template_Create error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Template_Create error: {ex} ", requestId);

                return BadRequest(errorResponse);
            }
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Delete called.");

            if (String.IsNullOrEmpty(id))
            {
                _logger.LogError($"RequestID:{requestId} - Delete id == null.");
                return NotFound($"RequestID:{requestId} - Delete Null ID passed");
            }

            var resultCode = await _templateService.DeleteItemAsync(id, requestId);

            if (resultCode != ApplicationCore.StatusCodes.Status204NoContent)
            {
                _logger.LogError($"RequestID:{requestId} - Delete error: " + resultCode);
                var errorResponse = JsonErrorResponse.BadRequest($"Delete error: {resultCode.Name} ", requestId);

                return BadRequest(errorResponse);
            }

            return NoContent();
        }
        [Authorize]
        [HttpPatch]
        public async Task<IActionResult> Update([FromBody] JObject jsonObject)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Template_Update called.");

            try
            {
                if (jsonObject == null)
                {
                    _logger.LogError($"RequestID:{requestId} - Template_Update error: null");
                    var errorResponse = JsonErrorResponse.BadRequest($"Template_Update error: null", requestId);

                    return BadRequest(errorResponse);
                }

                var modelObject = JsonConvert.DeserializeObject<TemplateViewModel>(jsonObject.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                //check for duplicate processes
                bool check = await _templateService.ProcessCheckAsync(modelObject.ProcessList, requestId);
                if (!check)
                {
                    _logger.LogError($"RequestID:{requestId} - Create error: Duplicate Process Types");
                    var errorResponse = JsonErrorResponse.BadRequest($"Create error: Duplicate Process Types", requestId);

                    return BadRequest(errorResponse);
                }

                //set todays date as the last used date
                modelObject.LastUsed = DateTime.Now.Date;

                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(modelObject.TemplateName))
                {
                    _logger.LogError($"RequestID:{requestId} - Template_Update error: invalid name");
                    var errorResponse = JsonErrorResponse.BadRequest($"Template_Update error: invalid name", requestId);

                    return BadRequest(errorResponse);
                }

                var resultCode = await _templateService.UpdateItemAsync(modelObject, requestId);

                if (resultCode != ApplicationCore.StatusCodes.Status201Created)
                {
                    _logger.LogError($"RequestID:{requestId} - Template_Update error: {resultCode.Name}");
                    var errorResponse = JsonErrorResponse.BadRequest($"Template_Update error: {resultCode.Name}", requestId);

                    return BadRequest(errorResponse);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} Template_Update error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Template_Update error: {ex} ", requestId);

                return BadRequest(errorResponse);
            }
        }
    }
}
