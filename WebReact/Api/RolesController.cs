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
    public class RolesController : BaseApiController<RolesController>
    {
        public readonly IRoleService _roleService;

        public RolesController(
            ILogger<RolesController> logger,
            IOptions<AppOptions> appOptions,
            IRoleService roleService) : base(logger, appOptions)
        {
            Guard.Against.Null(roleService, nameof(roleService));
            _roleService = roleService;
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JObject jsonObject)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Role_Create called.");

            try
            {
                if (jsonObject == null)
                {
                    _logger.LogError($"RequestID:{requestId} - Role_Create error: null");
                    var errorResponse = JsonErrorResponse.BadRequest($"Role_Create error: null", requestId);

                    return BadRequest(errorResponse);
                }

                var modelObject = JsonConvert.DeserializeObject<RoleModel>(jsonObject.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });


                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(modelObject.DisplayName))
                {
                    _logger.LogError($"RequestID:{requestId} - Role_Create error: invalid name");
                    var errorResponse = JsonErrorResponse.BadRequest($"Role_Create error: invalid name", requestId);

                    return BadRequest(errorResponse);
                }

                var resultCode = await _roleService.CreateItemAsync(modelObject, requestId);

                if (resultCode != ApplicationCore.StatusCodes.Status201Created)
                {
                    _logger.LogError($"RequestID:{requestId} - Role_Create error: {resultCode.Name}");
                    var errorResponse = JsonErrorResponse.BadRequest($"Role_Create error: {resultCode.Name}", requestId);

                    return BadRequest(errorResponse);
                }

                var location = "/Role/Create/new"; // TODO: Get the id from the results but need to wire from factory to here

                return Created(location, $"RequestId: {requestId} - Role created.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} Role_Create error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Role_Create error: {ex} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Role_Delete called.");

            if (String.IsNullOrEmpty(id))
            {
                _logger.LogError($"RequestID:{requestId} - Role_Delete name == null.");
                return NotFound($"RequestID:{requestId} - Role_Delete Null name passed");
            }

            var resultCode = await _roleService.DeleteItemAsync(id, requestId);

            if (resultCode != ApplicationCore.StatusCodes.Status204NoContent)
            {
                _logger.LogError($"RequestID:{requestId} - Role_Delete error: " + resultCode);
                var errorResponse = JsonErrorResponse.BadRequest($"Role_Delete error: {resultCode.Name} ", requestId);

                return BadRequest(errorResponse);
            }

            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Role_GetAll called.");

            try
            {
                var modelList = (await _roleService.GetAllAsync(requestId)).ToList();
                Guard.Against.Null(modelList, nameof(modelList), requestId);

                if (modelList.Count == 0)
                {
                    _logger.LogError($"RequestID:{requestId} - Role_GetAll no items found.");
                    return NotFound($"RequestID:{requestId} - Role_GetAll no items found");
                }

                return Ok(modelList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - Role_GetAll error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Role_GetAll error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }
    }
}
