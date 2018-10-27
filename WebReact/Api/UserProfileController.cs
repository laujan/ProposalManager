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

namespace WebReact.Api
{
    [Authorize(AuthenticationSchemes = "AzureAdBearer")]
    public class UserProfileController : BaseApiController<UserProfileController>
    {
        private readonly IUserProfileService _userProfileService;
        private readonly IUserContext _userContext;

        public UserProfileController(
            ILogger<UserProfileController> logger, 
            IOptions<AppOptions> appOptions,
            IUserProfileService userProfileService,
            IUserContext userContext) : base(logger, appOptions)
        {
            Guard.Against.Null(userProfileService, nameof(userProfileService));
            Guard.Against.Null(userContext, nameof(userContext));

            _userProfileService = userProfileService;
            _userContext = userContext;
        }

        // GET: /UserProfile/me
        [HttpGet("me")]
        public async Task<IActionResult> GetById()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation("UserProfileController_GetById called.");

            try
            {
                var userUpn = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;

                if (String.IsNullOrEmpty(userUpn))
                {
                    _logger.LogError($"UPN:{requestId} - UserProfileController_GetById name == null.");
                    return NotFound($"UPN:{requestId} - UserProfileController_GetById Invalid parameter passed");
                }

                var userProfile = await _userProfileService.GetItemByUpnAsync(userUpn);
                if (userProfile == null)
                {
                    _logger.LogError($"UPN:{requestId} - UserProfileController_GetById no user found.");
                    return NotFound($"UPN:{requestId} - UserProfileController_GetById no user found");
                }

                var responseJObject = JObject.FromObject(userProfile);

                return Ok(responseJObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"UPN:{requestId} - UserProfileController_GetById error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"UserProfileController_GetById error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        // GET: /UserProfile
        [HttpGet]
        public async Task<IActionResult> GetAll(int? page, [FromQuery] string upn)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - UserProfileController_GetAll called.");

            try
            {
                if (!String.IsNullOrEmpty(upn))
                {
					return await GetByUpn(upn);
					
				}

                var itemsPage = 10;
                var modelList = await _userProfileService.GetAllAsync(page ?? 1, itemsPage, requestId);
                Guard.Against.Null(modelList, nameof(modelList), requestId);

                if (modelList.ItemsList.Count == 0)
                {
                    _logger.LogError($"RequestID:{requestId} - UserProfileController_GetAll no items found.");
                    return NotFound($"RequestID:{requestId} - UserProfileController_GetAll no items found");
                }

                var responseJson = JObject.FromObject(modelList);

                return Ok(responseJson);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - UserProfileController_GetAll error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"UserProfileController_GetAll error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }

        // GET: /UserProfile?upn={name}
        [HttpGet("name")]
        public async Task<IActionResult> GetByUpn(string upn)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"UPN:{upn} - UserProfileController_GetByUPN called.");

            try
            {
				if (String.IsNullOrEmpty(upn))
                {
                    _logger.LogError($"UPN:{requestId} - UserProfileController_GetByUPN name == null.");
                    return NotFound($"UPN:{requestId} - UserProfileController_GetByUPN Invalid parameter passed");
                }
                var userProfile = await _userProfileService.GetItemByUpnAsync(upn);
                if (userProfile == null)
                {
                    _logger.LogError($"UPN:{requestId} - UserProfileController_GetByUPN no user found.");
                    return NotFound($"UPN:{requestId} - UserProfileController_GetByUPN no user found");
                }

                var responseJObject = JObject.FromObject(userProfile);

                return Ok(responseJObject);
            }
            catch (Exception ex)
            {
                _logger.LogError($"UPN:{requestId} - UserProfileController_GetByUPN error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"UserProfileController_GetByUPN error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }
    }
}
