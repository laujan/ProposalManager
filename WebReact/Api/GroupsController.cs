// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using ApplicationCore.Helpers;
using ApplicationCore.Interfaces;
using ApplicationCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace WebReact.Api
{
    [Authorize(AuthenticationSchemes = "AzureAdBearer")]
    public class GroupsController : BaseApiController<GroupsController>
    {
        private readonly IGroupsService _groupsService;
        public GroupsController(
            ILogger<GroupsController> logger,
            IOptions<AppOptions> appOptions,
            IGroupsService groupsService) : base(logger, appOptions)
        {
            Guard.Against.Null(groupsService, nameof(groupsService));
            _groupsService = groupsService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Groups_GetAll called.");

            try
            {
                var modelList = (await _groupsService.GetAllAsync(requestId));
                Guard.Against.Null(modelList, nameof(modelList), requestId);

                if (modelList.Count == 0)
                {
                    _logger.LogError($"RequestID:{requestId} - Groups_GetAll no items found.");
                    return NotFound($"RequestID:{requestId} - Groups_GetAll no items found");
                }

                return Ok(modelList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - Groups_GetAll error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Groups_GetAll error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JObject jsonObject)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Groups_Create called.");

            try
            {
                if (jsonObject == null)
                {
                    _logger.LogError($"RequestID:{requestId} - Groups_Create error: null");
                    var errorResponse = JsonErrorResponse.BadRequest($"Groups_Create error: null", requestId);

                    return BadRequest(errorResponse);
                }

                var modelObject = JsonConvert.DeserializeObject<GroupModel>(jsonObject.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });


                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(modelObject.GroupName))
                {
                    _logger.LogError($"RequestID:{requestId} - Groups_Create error: invalid name");
                    var errorResponse = JsonErrorResponse.BadRequest($"Groups_Create error: invalid name", requestId);

                    return BadRequest(errorResponse);
                }

                var resultCode = await _groupsService.CreateItemAsync(modelObject, requestId);

                if (resultCode != ApplicationCore.StatusCodes.Status201Created)
                {
                    _logger.LogError($"RequestID:{requestId} - Groups_Create error: {resultCode.Name}");
                    var errorResponse = JsonErrorResponse.BadRequest($"Groups_Create error: {resultCode.Name}", requestId);

                    return BadRequest(errorResponse);
                }


                return Created("Group/Create/New", $"RequestId: {requestId} - Group created.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} Groups_Create error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Groups_Create error: {ex} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Groups_Delete called.");

            if (String.IsNullOrEmpty(id))
            {
                _logger.LogError($"RequestID:{requestId} - Groups_Delete name == null.");
                return NotFound($"RequestID:{requestId} - Groups_Delete Null name passed");
            }

            var resultCode = await _groupsService.DeleteItemAsync(id, requestId);

            if (resultCode != StatusCodes.Status204NoContent)
            {
                _logger.LogError($"RequestID:{requestId} - Groups_Delete error: " + resultCode);
                var errorResponse = JsonErrorResponse.BadRequest($"Groups_Delete error: {resultCode.Name} ", requestId);

                return BadRequest(errorResponse);
            }

            return NoContent();
        }
    }
}