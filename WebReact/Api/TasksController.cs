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
using System.Linq;
using System.Threading.Tasks;

namespace WebReact.Api
{
    [Authorize(AuthenticationSchemes = "AzureAdBearer")]
    public class TasksController : BaseApiController<TasksController>
    {
        private readonly ITasksService _tasksService;

        public TasksController(
            ILogger<TasksController> logger,
            IOptions<AppOptions> appOptions,
            ITasksService tasksService) : base(logger, appOptions)
        {
            Guard.Against.Null(tasksService, nameof(tasksService));
            _tasksService = tasksService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JObject jsonObject)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Tasks_Create called.");

            try
            {
                if (jsonObject == null)
                {
                    _logger.LogError($"RequestID:{requestId} - Tasks_Create error: null");
                    var errorResponse = JsonErrorResponse.BadRequest($"Tasks_Create error: null", requestId);

                    return BadRequest(errorResponse);
                }

                var modelObject = JsonConvert.DeserializeObject<TasksModel>(jsonObject.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(modelObject.Name))
                {
                    _logger.LogError($"RequestID:{requestId} - Tasks_Create error: invalid name");
                    var errorResponse = JsonErrorResponse.BadRequest($"Tasks_Create error: invalid name", requestId);

                    return BadRequest(errorResponse);
                }

                var resultCode = await _tasksService.CreateItemAsync(modelObject, requestId);

                if (resultCode != ApplicationCore.StatusCodes.Status201Created)
                {
                    _logger.LogError($"RequestID:{requestId} - Tasks_Create error: {resultCode.Name}");
                    var errorResponse = JsonErrorResponse.BadRequest($"Tasks_Create error: {resultCode.Name}", requestId);

                    return BadRequest(errorResponse);
                }

                var location = "/Tasks/Create/new"; // TODO: Get the id from the results but need to wire from factory to here

                return Created(location, $"RequestId: {requestId} - Tasks created.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} Tasks_Create error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Tasks_Create error: {ex} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        [HttpPatch]
        public async Task<IActionResult> Update([FromBody] JObject jsonObject)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Tasks_Update called.");

            try
            {
                if (jsonObject == null)
                {
                    _logger.LogError($"RequestID:{requestId} - Tasks_Update error: null");
                    var errorResponse = JsonErrorResponse.BadRequest($"Tasks_Update error: null", requestId);

                    return BadRequest(errorResponse);
                }

                var modelObject = JsonConvert.DeserializeObject<TasksModel>(jsonObject.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(modelObject.Id))
                {
                    _logger.LogError($"RequestID:{requestId} - Tasks_Update error: invalid id");
                    var errorResponse = JsonErrorResponse.BadRequest($"Tasks_Update error: invalid id", requestId);

                    return BadRequest(errorResponse);
                }

                var resultCode = await _tasksService.UpdateItemAsync(modelObject, requestId);

                if (resultCode != ApplicationCore.StatusCodes.Status200OK)
                {
                    _logger.LogError($"RequestID:{requestId} - Tasks_Update error: {resultCode.Name}");
                    var errorResponse = JsonErrorResponse.BadRequest($"Tasks_Update error: {resultCode.Name} ", requestId);

                    return BadRequest(errorResponse);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - Tasks_Update error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Tasks_Update error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Tasks_Delete called.");

            if (String.IsNullOrEmpty(id))
            {
                _logger.LogError($"RequestID:{requestId} - Tasks_Delete id == null.");
                return NotFound($"RequestID:{requestId} - Tasks_Delete Null ID passed");
            }

            var resultCode = await _tasksService.DeleteItemAsync(id, requestId);

            if (resultCode != ApplicationCore.StatusCodes.Status204NoContent)
            {
                _logger.LogError($"RequestID:{requestId} - Tasks_Delete error: " + resultCode);
                var errorResponse = JsonErrorResponse.BadRequest($"Tasks_Delete error: {resultCode.Name} ", requestId);

                return BadRequest(errorResponse);
            }

            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - GetAll called.");

            try
            {
                var modelList = (await _tasksService.GetAllAsync(requestId)).ToList();
                Guard.Against.Null(modelList, nameof(modelList), requestId);

                if (modelList.Count == 0)
                {
                    _logger.LogError($"RequestID:{requestId} - GetAll no items found.");
                    return NotFound($"RequestID:{requestId} - GetAll no items found");
                }

                return Ok(modelList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - GetAll error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"GetAll error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }
    }
}
