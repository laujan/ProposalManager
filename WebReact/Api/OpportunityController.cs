// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Threading.Tasks;
using ApplicationCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Interfaces;
using ApplicationCore.Helpers;
using Newtonsoft.Json.Linq;
using ApplicationCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace WebReact.Api
{
    [Authorize(AuthenticationSchemes = "AzureAdBearer")]
    public class OpportunityController : BaseApiController<OpportunityController>
    {
        private readonly IOpportunityService _opportunityService;

        public OpportunityController(
            ILogger<OpportunityController> logger, 
            IOptions<AppOptions> appOptions,
            IOpportunityService opportunityService) : base(logger, appOptions)
        {
            Guard.Against.Null(opportunityService, nameof(opportunityService));
            _opportunityService = opportunityService;
        }

        // POST: /Opportunity
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] JObject opportunityJson)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Create called.");

            try
            {
                if (opportunityJson == null)
                {
                    _logger.LogError($"RequestID:{requestId} - Create error: oportunity null");
                    var errorResponse = JsonErrorResponse.BadRequest($"Create error: oportunity null", requestId);

                    return BadRequest(errorResponse);
                }

                var opportunity = JsonConvert.DeserializeObject<OpportunityViewModel>(opportunityJson.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(opportunity.DisplayName))
                {
                    _logger.LogError($"RequestID:{requestId} - Create error: invalid oportunity name");
                    var errorResponse = JsonErrorResponse.BadRequest($"Create error: invalid oportunity name", requestId);

                    return BadRequest(errorResponse);
                }

                var resultCode = await _opportunityService.CreateItemAsync(opportunity, requestId);

                if (resultCode != ApplicationCore.StatusCodes.Status201Created)
                {
                    _logger.LogError($"RequestID:{requestId} - Create error: {resultCode.Name}");
                    var errorResponse = JsonErrorResponse.BadRequest($"Create error: oportunity {resultCode.Name}", requestId);

                    return BadRequest(errorResponse);
                }

                var location = "/Opportunity/Create/new"; // TODO: Get the id from the results but need to wire from factory to here

                return Created(location, $"RequestId: {requestId} - Opportunity created.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} Create error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Create error: {ex} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        // PUT: /Opportunity?id={id}
        [HttpPatch]
        public async Task<IActionResult> Update([FromBody] JObject opportunityJson)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Update called.");

            try
            {
                if (opportunityJson == null)
                {
                    _logger.LogError($"RequestID:{requestId} - Update error: oportunity null");
                    var errorResponse = JsonErrorResponse.BadRequest($"Update error: oportunity null", requestId);

                    return BadRequest(errorResponse);
                }

                var opportunity = JsonConvert.DeserializeObject<OpportunityViewModel>(opportunityJson.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                //TODO: P2 Refactor into Guard
                if (String.IsNullOrEmpty(opportunity.Id))
                {
                    _logger.LogError($"RequestID:{requestId} - Update error: invalid oportunity id");
                    var errorResponse = JsonErrorResponse.BadRequest($"Update error: invalid oportunity id", requestId);

                    return BadRequest(errorResponse);
                }

                var resultCode = await _opportunityService.UpdateItemAsync(opportunity, requestId);

                if (resultCode != ApplicationCore.StatusCodes.Status200OK)
                {
                    _logger.LogError($"RequestID:{requestId} - Update error: {resultCode.Name}");
                    var errorResponse = JsonErrorResponse.BadRequest($"Update error: {resultCode.Name} ", requestId);

                    return BadRequest(errorResponse);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - Update error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Update error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        // DELETE: /Opportunity?id={id}
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

            var resultCode = await _opportunityService.DeleteItemAsync(id, requestId);

            if (resultCode != ApplicationCore.StatusCodes.Status204NoContent)
            {
                _logger.LogError($"RequestID:{requestId} - Delete error: " + resultCode);
                var errorResponse = JsonErrorResponse.BadRequest($"Delete error: {resultCode.Name} ", requestId);

                return BadRequest(errorResponse);
            }

            return NoContent();
        }

        // Get: /Opportunity
        [HttpGet]
        public async Task<IActionResult> GetAll(int? page, [FromQuery] string name, [FromQuery] string id, [FromQuery] string reference, [FromQuery] string checkName = "")
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - GetAll called.");

            try
            {
                if (!String.IsNullOrEmpty(checkName))
                {
                    return await GetByName(checkName, true);
                }

                if (!String.IsNullOrEmpty(name))
                {
                    return await GetByName(name, false);
                }

                if (!String.IsNullOrEmpty(id))
                {
                    return await GetById(id);
                }

                if (!String.IsNullOrEmpty(reference))
                {
                    return await GetByReference(reference);
                }

                var modelList = await _opportunityService.GetAllAsync(page ?? 1, 10, requestId);
                Guard.Against.Null(modelList, nameof(modelList), requestId);

                if (modelList.ItemsList.Count == 0)
                {
                    _logger.LogError($"RequestID:{requestId} - GetAll no user profiles found.");
                    return NotFound($"RequestID:{requestId} - GetAll no user profiles found");
                }

                return Ok(JObject.FromObject(modelList));
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - GetAll error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"GetAll error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        // GET: /Opportunity?id={id}
        [HttpGet("id")]
        public async Task<IActionResult> GetById(string id)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - GetOpportunityById called.");

            try
            {
                if (String.IsNullOrEmpty(id))
                {
                    _logger.LogError($"RequestID:{requestId} - GetOpportunityById id == null.");
                    return NotFound($"RequestID:{requestId} - GetOpportunityById Invalid parameter. ID = null.");
                }
                var thisOpportunity = await _opportunityService.GetItemByIdAsync(id, requestId);
                if (thisOpportunity == null)
                {
                    _logger.LogError($"RequestID:{requestId} - GetOpportunityById id no opportunities found.");
                    return NotFound($"RequestID:{requestId} - GetOpportunityById no opportunities found");
                }

                return Ok(JObject.FromObject(thisOpportunity));
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - GetOpportunityById error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"GetOpportunityById error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        // GET: /Opportunity?name={name}
        [HttpGet("name")]
        public async Task<IActionResult> GetByName(string name, bool isCheckName)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - GetOpportunityByName called.");

            try
            {
                if (String.IsNullOrEmpty(name))
                {
                    _logger.LogError($"RequestID:{requestId} - GetOpportunityByName name == null.");
                    return NotFound($"RequestID:{requestId} - GetOpportunityByName Invalid parameter passed");
                }
                var thisOpportunity = await _opportunityService.GetItemByNameAsync(name, isCheckName, requestId);
                if (thisOpportunity == null)
                {
                    _logger.LogError($"RequestID:{requestId} - GetOpportunityByName no opporunities found.");
                    return NotFound($"RequestID:{requestId} - GetOpportunityByName no opportunities found");
                }

                return Ok(JObject.FromObject(thisOpportunity));
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - GetOpportunityByName error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"GetOpportunityByName error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        // GET: /Opportunity?id={id}
        [HttpGet("reference")]
        public async Task<IActionResult> GetByReference(string reference)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - GeOpportunitytByReference called.");

            try
            {
                if (String.IsNullOrEmpty(reference))
                {
                    _logger.LogError($"RequestID:{requestId} - GeOpportunitytByReference ref == null.");
                    return NotFound($"RequestID:{requestId} - GeOpportunitytByReference Invalid parameter. ref = null.");
                }
                var thisOpportunity = await _opportunityService.GetItemByRefAsync(reference, requestId);
                if (thisOpportunity == null)
                {
                    _logger.LogError($"RequestID:{requestId} - GeOpportunitytByReference no opportunities found.");
                    return NotFound($"RequestID:{requestId} - GeOpportunitytByReference no opportunities found");
                }

                return Ok(JObject.FromObject(thisOpportunity));
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - GeOpportunitytByReference error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"GeOpportunitytByReference error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }
    }
}