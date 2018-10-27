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
    public class ProcessController : BaseApiController<ProcessController>
    {
        private readonly IProcessService _processService;

        public ProcessController(
            ILogger<ProcessController> logger,
            IOptions<AppOptions> appOptions,
            IProcessService processService) : base(logger, appOptions)
        {
            Guard.Against.Null(processService, nameof(processService));
            _processService = processService;
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Process_GetAll called.");

            try
            {
                var modelList = (await _processService.GetAllAsync(requestId));
                Guard.Against.Null(modelList, nameof(modelList), requestId);

                if (modelList.ItemsList.Count == 0)
                {
                    _logger.LogError($"RequestID:{requestId} - Process_GetAll no items found.");
                    return NotFound($"RequestID:{requestId} - Process_GetAll no items found");
                }

                return Ok(modelList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - Process_GetAll error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Process_GetAll error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JObject jsonObject)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Process_Create called.");

            try
            {
                if (jsonObject == null)
                {
                    _logger.LogError($"RequestID:{requestId} - Process_Create error: null");
                    var errorResponse = JsonErrorResponse.BadRequest($"Process_Create error: null", requestId);

                    return BadRequest(errorResponse);
                }

                var modelObject = JsonConvert.DeserializeObject<ProcessTypeViewModel>(jsonObject.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(modelObject.ProcessStep))
                {
                    _logger.LogError($"RequestID:{requestId} - Process_Create error: invalid name");
                    var errorResponse = JsonErrorResponse.BadRequest($"Process_Create error: invalid name", requestId);

                    return BadRequest(errorResponse);
                }

                var resultCode = await _processService.CreateItemAsync(modelObject, requestId);

                if (resultCode != ApplicationCore.StatusCodes.Status201Created)
                {
                    _logger.LogError($"RequestID:{requestId} - Process_Create error: {resultCode.Name}");
                    var errorResponse = JsonErrorResponse.BadRequest($"Process_Create error: {resultCode.Name}", requestId);

                    return BadRequest(errorResponse);
                }

                var location = "/Process/Create/new";

                return Created(location, $"RequestId: {requestId} - Process created.");
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

            var resultCode = await _processService.DeleteItemAsync(id, requestId);

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
            _logger.LogInformation($"RequestID:{requestId} - Process_Update called.");

            try
            {
                if (jsonObject == null)
                {
                    _logger.LogError($"RequestID:{requestId} - Process_Update error: null");
                    var errorResponse = JsonErrorResponse.BadRequest($"Process_Update error: null", requestId);

                    return BadRequest(errorResponse);
                }

                var modelObject = JsonConvert.DeserializeObject<ProcessTypeViewModel>(jsonObject.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(modelObject.ProcessStep))
                {
                    _logger.LogError($"RequestID:{requestId} - Process_Update error: invalid name");
                    var errorResponse = JsonErrorResponse.BadRequest($"Process_Update error: invalid name", requestId);

                    return BadRequest(errorResponse);
                }

                var resultCode = await _processService.UpdateItemAsync(modelObject, requestId);

                if (resultCode != ApplicationCore.StatusCodes.Status201Created)
                {
                    _logger.LogError($"RequestID:{requestId} - Process_Update error: {resultCode.Name}");
                    var errorResponse = JsonErrorResponse.BadRequest($"Process_Update error: {resultCode.Name}", requestId);

                    return BadRequest(errorResponse);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} Process_Update error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Process_Update error: {ex} ", requestId);

                return BadRequest(errorResponse);
            }
        }

    }
}
