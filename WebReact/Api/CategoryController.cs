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
    /// <summary>
    /// Category Controller
    /// </summary>
    [Authorize(AuthenticationSchemes = "AzureAdBearer")]
    public class CategoryController : BaseApiController<CategoryController>
    {
        /// <summary>
        /// Cartegory service object
        /// </summary>
        public readonly ICategoryService _categoryService;

        /// <summary>
        /// Category constructor
        /// </summary>
        public CategoryController(
            ILogger<CategoryController> logger,
            IOptions<AppOptions> appOptions,
            ICategoryService categoryService) : base(logger, appOptions)
        {
            Guard.Against.Null(categoryService, nameof(categoryService));
            _categoryService = categoryService;
        }

        /// <summary>
        /// [Creates a new Category.]
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///POST /Todo
        ///{
        ///  "id": "",
        ///  "name": "Retail"
        ///}
        ///
        ///Select Content_type : application/json
        /// </remarks>
        /// <param name="jsonObject"></param>
        /// <returns>A status code of either 201/404 </returns>
        /// <response code="201">Returns the new categories's requestId</response>
        /// <response code="400">If name is null</response> 
        /// <response code="401">Unauthorized</response> 
        [HttpPost]
        [ProducesResponseType(typeof(string), 201)]
        [ProducesResponseType(typeof(JsonErrorResponse), 400)]
        [ProducesResponseType(typeof(JsonErrorResponse), 401)]
        public async Task<IActionResult> CreateAsync([FromBody] JObject jsonObject)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Category_Create called.");

            try
            {
                if (jsonObject == null)
                {
                    _logger.LogError($"RequestID:{requestId} - Category_Create error: null");
                    var errorResponse = JsonErrorResponse.BadRequest($"Category_Create error: null", requestId);

                    return BadRequest(errorResponse);
                }

                var modelObject = JsonConvert.DeserializeObject<CategoryModel>(jsonObject.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(modelObject.Name))
                {
                    _logger.LogError($"RequestID:{requestId} - Category_Create error: invalid name");
                    var errorResponse = JsonErrorResponse.BadRequest($"Category_Create error: invalid name", requestId);

                    return BadRequest(errorResponse);
                }

                var resultCode = await _categoryService.CreateItemAsync(modelObject, requestId);

                if (resultCode != ApplicationCore.StatusCodes.Status201Created)
                {
                    _logger.LogError($"RequestID:{requestId} - Category_Create error: {resultCode.Name}");
                    var errorResponse = JsonErrorResponse.BadRequest($"Category_Create error: {resultCode.Name}", requestId);

                    return BadRequest(errorResponse);
                }

                var location = "/Category/Create/new"; // TODO: Get the id from the results but need to wire from factory to here

                return Created(location, $"RequestId: {requestId} - Category created.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} Category_Create error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Category_Create error: {ex} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        /// <summary>
        /// [Update the Category.]
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///PATCH /Todo
        ///{
        ///  "id": 5,
        ///  "name": "Retail"
        ///}
        /// </remarks>
        /// <returns>A status code of either 201/404 </returns>
        /// <response code="201">Returns the new categories's requestId</response>
        /// <response code="400">If name is null</response> 
        /// <response code="401">Unauthorized</response> 
        [ProducesResponseType(typeof(string), 201)]
        [ProducesResponseType(typeof(JsonErrorResponse), 400)]
        [ProducesResponseType(typeof(JsonErrorResponse), 401)]
        [HttpPatch]
        public async Task<IActionResult> UpdateAsync([FromBody] JObject jsonObject)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Category_Update called.");

            try
            {
                if (jsonObject == null)
                {
                    _logger.LogError($"RequestID:{requestId} - Category_Update error: null");
                    var errorResponse = JsonErrorResponse.BadRequest($"Category_Update error: null", requestId);

                    return BadRequest(errorResponse);
                }

                var modelObject = JsonConvert.DeserializeObject<CategoryModel>(jsonObject.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(modelObject.Id))
                {
                    _logger.LogError($"RequestID:{requestId} - Category_Update error: invalid id");
                    var errorResponse = JsonErrorResponse.BadRequest($"Category_Update error: invalid id", requestId);

                    return BadRequest(errorResponse);
                }

                var resultCode = await _categoryService.UpdateItemAsync(modelObject, requestId);

                if (resultCode != ApplicationCore.StatusCodes.Status200OK)
                {
                    _logger.LogError($"RequestID:{requestId} - Category_Update error: {resultCode.Name}");
                    var errorResponse = JsonErrorResponse.BadRequest($"Category_Update error: {resultCode.Name} ", requestId);

                    return BadRequest(errorResponse);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - Category_Update error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Category_Update error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        /// <summary>
        /// [Delete the Category]
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///DELETE /Todo
        ///{
        ///  "id": 5
        ///}
        /// </remarks>
        /// <returns>A status code of either 201/404 </returns>
        /// <response code="201">Returns the new categories's requestId</response>
        /// <response code="400">If name is null</response> 
        /// <response code="401">Unauthorized</response> 
        [ProducesResponseType(typeof(string), 201)]
        [ProducesResponseType(typeof(JsonErrorResponse), 400)]
        [ProducesResponseType(typeof(JsonErrorResponse), 401)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Category_Delete called.");

            if (String.IsNullOrEmpty(id))
            {
                _logger.LogError($"RequestID:{requestId} - Category_Delete id == null.");
                return NotFound($"RequestID:{requestId} - Category_Delete Null ID passed");
            }

            var resultCode = await _categoryService.DeleteItemAsync(id, requestId);

            if (resultCode != ApplicationCore.StatusCodes.Status204NoContent)
            {
                _logger.LogError($"RequestID:{requestId} - Category_Delete error: " + resultCode);
                var errorResponse = JsonErrorResponse.BadRequest($"Category_Delete error: {resultCode.Name} ", requestId);

                return BadRequest(errorResponse);
            }

            return NoContent();
        }

        /// <summary>
        /// [Get Category List]
        /// </summary>
        /// <returns>A status code of either 201/404 </returns>
        /// <response code="201">return the category as a json array</response>
        /// <response code="400">if value is null</response> 
        /// <response code="401">Unauthorized</response> 
        [HttpGet]
        [ProducesResponseType(typeof(CategoryModel), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetAllAsync()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Category_GetAll called.");

            try
            {
                var modelList = (await _categoryService.GetAllAsync(requestId)).ToList();
                Guard.Against.Null(modelList, nameof(modelList), requestId);

                if (modelList.Count == 0)
                {
                    _logger.LogError($"RequestID:{requestId} - Category_GetAll no items found.");
                    return Ok(modelList);
                }

                return Ok(modelList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - Category_GetAll error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Category_GetAll error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }
    }
}
