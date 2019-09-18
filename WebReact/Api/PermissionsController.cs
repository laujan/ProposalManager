﻿// Copyright(c) Microsoft Corporation.
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Linq;
using System.Threading.Tasks;
using ApplicationCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Interfaces;
using ApplicationCore.Helpers;
using Newtonsoft.Json.Linq;
using ApplicationCore.Models;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;

namespace WebReact.Api
{
    [Authorize(AuthenticationSchemes = "AzureAdBearer")]
    public class PermissionsController : BaseApiController<PermissionsController>
    {
        public readonly IPermissionService _permissionService;

        public PermissionsController(
            ILogger<PermissionsController> logger,
            IOptions<AppOptions> appOptions,
            IPermissionService permissionService) : base(logger, appOptions)
        {
            Guard.Against.Null(permissionService, nameof(permissionService));
            _permissionService = permissionService;
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JObject jsonObject)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Permission_Create called.");

            try
            {
                if (jsonObject == null)
                {
                    _logger.LogError($"RequestID:{requestId} - Permission_Create error: null");
                    var errorResponse = JsonErrorResponse.BadRequest($"Permission_Create error: null", requestId);

                    return BadRequest(errorResponse);
                }

                var modelObject = JsonConvert.DeserializeObject<PermissionModel>(jsonObject.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });


                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(modelObject.Name))
                {
                    _logger.LogError($"RequestID:{requestId} - Permission_Create error: invalid name");
                    var errorResponse = JsonErrorResponse.BadRequest($"Permission_Create error: invalid name", requestId);

                    return BadRequest(errorResponse);
                }

                JObject result = await _permissionService.CreateItemAsync(modelObject, requestId);

                return new CreatedResult(result.SelectToken("id").ToString(), null);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} Permission_Create error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Permission_Create error: {ex} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Permission_Delete called.");

            if (String.IsNullOrEmpty(id))
            {
                _logger.LogError($"RequestID:{requestId} - Permission_Delete name == null.");
                return NotFound($"RequestID:{requestId} - Permission_Delete Null name passed");
            }

            var resultCode = await _permissionService.DeleteItemAsync(id, requestId);

            if (resultCode != ApplicationCore.StatusCodes.Status204NoContent)
            {
                _logger.LogError($"RequestID:{requestId} - Permission_Delete error: " + resultCode);
                var errorResponse = JsonErrorResponse.BadRequest($"Permission_Delete error: {resultCode.Name} ", requestId);

                return BadRequest(errorResponse);
            }

            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Permission_GetAll called.");

            try
            {
                var modelList = (await _permissionService.GetAllAsync(requestId)).ToList();
                Guard.Against.Null(modelList, nameof(modelList), requestId);

                if (modelList.Count == 0)
                {
                    _logger.LogError($"RequestID:{requestId} - Permission_GetAll no items found.");
                    return NotFound($"RequestID:{requestId} - Permission_GetAll no items found");
                }

                return Ok(modelList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - Permission_GetAll error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Permission_GetAll error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }
    }
}
