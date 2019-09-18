// Copyright(c) Microsoft Corporation. 
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
    /// <summary>
    /// Category Controller
    /// </summary>
    [Authorize(AuthenticationSchemes = "AzureAdBearer")]
    public class MetaDataController : BaseApiController<MetaDataController>
    {
        /// <summary>
        /// Cartegory service object
        /// </summary>
        public readonly IMetaDataService _metaDataService;

        /// <summary>
        /// Category constructor
        /// </summary>
        public MetaDataController(
            ILogger<MetaDataController> logger,
            IOptions<AppOptions> appOptions,
            IMetaDataService metaDataService) : base(logger, appOptions)
        {
            Guard.Against.Null(metaDataService, nameof(metaDataService));
            _metaDataService = metaDataService;
        }

        /// <summary>
        /// [Creates a new MetaData.]
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
            _logger.LogInformation($"RequestID:{requestId} - MetaData_Create called.");

            try
            {
                if (jsonObject == null)
                {
                    _logger.LogError($"RequestID:{requestId} - MetaData_Create error: null");
                    var errorResponse = JsonErrorResponse.BadRequest($"MetaData_Create error: null", requestId);

                    return BadRequest(errorResponse);
                }

                var modelObject = JsonConvert.DeserializeObject<MetaDataModel>(jsonObject.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(modelObject.DisplayName))
                {
                    _logger.LogError($"RequestID:{requestId} - MetaData_Create error: invalid name");
                    var errorResponse = JsonErrorResponse.BadRequest($"MetaData_Create error: invalid name", requestId);

                    return BadRequest(errorResponse);
                }

                var result = await _metaDataService.CreateItemAsync(modelObject, requestId);

                return new CreatedResult(result.SelectToken("id").ToString(), null);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} MetaData_Create error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"MetaData_Create error: {ex} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        /// <summary>
        /// [Update the MetaData.]
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
            _logger.LogInformation($"RequestID:{requestId} - MetaData_Update called.");

            try
            {
                if (jsonObject == null)
                {
                    _logger.LogError($"RequestID:{requestId} - MetaData_Update error: null");
                    var errorResponse = JsonErrorResponse.BadRequest($"MetaData_Update error: null", requestId);

                    return BadRequest(errorResponse);
                }

                var modelObject = JsonConvert.DeserializeObject<MetaDataModel>(jsonObject.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(modelObject.Id))
                {
                    _logger.LogError($"RequestID:{requestId} - MetaData_Update error: invalid id");
                    var errorResponse = JsonErrorResponse.BadRequest($"Metadata_Update error: invalid id", requestId);

                    return BadRequest(errorResponse);
                }

                var resultCode = await _metaDataService.UpdateItemAsync(modelObject, requestId);

                if (resultCode != ApplicationCore.StatusCodes.Status200OK)
                {
                    _logger.LogError($"RequestID:{requestId} - MetaData_Update error: {resultCode.Name}");
                    var errorResponse = JsonErrorResponse.BadRequest($"MetaData_Update error: {resultCode.Name} ", requestId);

                    return BadRequest(errorResponse);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - MetaData_Update error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"MetaData_Update error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        /// <summary>
        /// [Delete the MetaData]
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
            _logger.LogInformation($"RequestID:{requestId} - MetaData_Delete called.");

            if (String.IsNullOrEmpty(id))
            {
                _logger.LogError($"RequestID:{requestId} - MetaData_Delete id == null.");
                return NotFound($"RequestID:{requestId} - MetaData_Delete Null ID passed");
            }

            var resultCode = await _metaDataService.DeleteItemAsync(id, requestId);

            if (resultCode != ApplicationCore.StatusCodes.Status204NoContent)
            {
                _logger.LogError($"RequestID:{requestId} - MetaData_Delete error: " + resultCode);
                var errorResponse = JsonErrorResponse.BadRequest($"MetaData_Delete error: {resultCode.Name} ", requestId);

                return BadRequest(errorResponse);
            }

            return NoContent();
        }

        /// <summary>
        /// [Get MetaData List]
        /// </summary>
        /// <returns>A status code of either 201/404 </returns>
        /// <response code="201">return the metaData as a json array</response>
        /// <response code="400">if value is null</response> 
        /// <response code="401">Unauthorized</response> 
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - MetaData_GetAll called.");

            try
            {
                var modelList = (await _metaDataService.GetAllAsync(requestId)).ToList();
                Guard.Against.Null(modelList, nameof(modelList), requestId);

                if (modelList.Count == 0)
                {
                    _logger.LogError($"RequestID:{requestId} - MetaData_GetAll no items found.");
                    return NotFound($"RequestID:{requestId} - MetaData_GetAll no items found");
                }

                return Ok(modelList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - MetaData_GetAll error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Metadata_GetAll error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }
    }
}
