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

                var resultCode = await _permissionService.CreateItemAsync(modelObject, requestId);

                if (resultCode != ApplicationCore.StatusCodes.Status201Created)
                {
                    _logger.LogError($"RequestID:{requestId} - Permission_Create error: {resultCode.Name}");
                    var errorResponse = JsonErrorResponse.BadRequest($"Permission_Create error: {resultCode.Name}", requestId);

                    return BadRequest(errorResponse);
                }

                var location = "/Role/Create/new"; // TODO: Get the id from the results but need to wire from factory to here

                return Created(location, $"RequestId: {requestId} - Role created.");
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
