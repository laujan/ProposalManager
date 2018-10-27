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
using Microsoft.AspNetCore.Authorization;
using System.IO;

namespace WebReact.Api
{
    [Authorize(AuthenticationSchemes = "AzureAdBearer")]
    public class SetupController : BaseApiController<SetupController>
    {
        private readonly ISetupService _setupService;
        private readonly IWebApiAuthProvider _authProvider;


        public SetupController(
            ILogger<SetupController> logger,
            IOptions<AppOptions> appOptions,
            ISetupService setupService,
            IWebApiAuthProvider authProvider) : base(logger, appOptions)
        {
            Guard.Against.Null(setupService, nameof(setupService));
            Guard.Against.Null(authProvider, nameof(authProvider));
            _setupService = setupService;
            _authProvider = authProvider;
        }

        [HttpPost("{key}/{value}")]
        public async Task<IActionResult> UpdateAppSettings(string key, string value)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} SetupController_UpdateAppSettings called.");

            // Check to see if setup is enabled and if not respond with bad request
            var checkSetupState = await CheckSetupState(requestId);
            if (checkSetupState != null) return BadRequest(checkSetupState);

            try
            {
                var result = await _setupService.UpdateAppOpptionsAsync(key, value, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} SetupController_UpdateAppSettings error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"UpdateAppSettings error: {ex.Message}", requestId);
                return BadRequest(errorResponse);
            }
            return NoContent();
        }

        [HttpPost("documentid")]
        [Consumes("application/json")]
        public async Task<IActionResult> UpdateDocumentIdSettings([FromBody] JObject data)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} SetupController_UpdateDocumentIdSettings called.");

            // Check to see if setup is enabled and if not respond with bad request
            var checkSetupState = await CheckSetupState(requestId);
            if (checkSetupState != null) return BadRequest(checkSetupState);
            
            try
            {
                var result = await _setupService.UpdateDocumentIdActivatorOptionsAsync(data["key"].ToString(), data["value"].ToString(), requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} SetupController_UpdateDocumentIdSettings error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"UpdateDocumentIdSettings error: {ex.Message}", requestId);
                return BadRequest(errorResponse);
            }
            return NoContent();
        }

        [HttpPost("CreateAllLists/{siteRootId}", Name = "CreateAllLists")]
        public async Task<IActionResult> CreateAllLists(string siteRootId)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} SetupController_CreateAllLists called.");

            // Check to see if setup is enabled and if not respond with bad request
            var checkSetupState = await CheckSetupState();
            if (checkSetupState != null) return BadRequest(checkSetupState);

            try
            {
                await _setupService.CreateAllListsAsync(siteRootId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} SetupController_CreateAllLists error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"CreateAllLists error: {ex.Message}", requestId);
                return BadRequest(errorResponse);
            }
            return NoContent();
        }

        [HttpPost("CreateSitePermissions", Name = "CreateSitePermissions")]
        public async Task<IActionResult> CreateSitePermissions()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} SetupController_CreateSitePermissions called.");

            // Check to see if setup is enabled and if not respond with bad request
            var checkSetupState = await CheckSetupState();
            if (checkSetupState != null) return BadRequest(checkSetupState);

            try
            {
                await _setupService.CreateSitePermissionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} SetupController_CreateSitePermissions error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"CreateSitePermissions error: {ex.Message}", requestId);
                return BadRequest(errorResponse);
            }
            return NoContent();
        }

        [HttpPost("CreateSiteProcesses", Name = "CreateSiteProcesses")]
        public async Task<IActionResult> CreateSiteProcesses()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} SetupController_CreateSiteProcesses called.");

            // Check to see if setup is enabled and if not respond with bad request
            var checkSetupState = await CheckSetupState();
            if (checkSetupState != null) return BadRequest(checkSetupState);

            try
            {
                await _setupService.CreateSiteProcessesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} SetupController_CreateSiteProcesses error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"CreateSiteProcesses error: {ex.Message}", requestId);
                return BadRequest(errorResponse);
            }
            return NoContent();
        }

        [HttpPost("CreateAdminPermissions/{adGroup}", Name = "CreateAdminPermissions")]
        public async Task<IActionResult> CreateAdminPermissions(string adGroup)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} SetupController_CreateAdminPermissions called.");

            // Check to see if setup is enabled and if not respond with bad request
            var checkSetupState = await CheckSetupState();
            if (checkSetupState != null) return BadRequest(checkSetupState);

            try
            {
                await _setupService.CreateSiteAdminPermissionsAsync(adGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} SetupController_CreateAdminPermissions error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"CreateAdminPermissions error: {ex.Message}", requestId);
                return BadRequest(errorResponse);
            }
            return NoContent();
        }

        [HttpPost("CreateSiteRoles", Name = "CreateSiteRoles")]
        public async Task<IActionResult> CreateSiteRoles()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} SetupController_CreateSiteRoles called.");

            // Check to see if setup is enabled and if not respond with bad request
            var checkSetupState = await CheckSetupState();
            if (checkSetupState != null) return BadRequest(checkSetupState);

            try
            {
                await _setupService.CreateSiteRolesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} SetupController_CreateSiteRoles error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"CreateSiteRoles error: {ex.Message}", requestId);
                return BadRequest(errorResponse);
            }
            return NoContent();
        }

        [HttpPost("CreateProposalManagerTeam/{name}", Name = "CreateProposalManagerTeam")]
        public async Task<IActionResult> CreateProposalManagerTeam(string name)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} SetupController_CreateSiteRoles called.");

            // Check to see if setup is enabled and if not respond with bad request
            var checkSetupState = await CheckSetupState();
            if (checkSetupState != null) return BadRequest(checkSetupState);

            try
            {
                await _setupService.CreateProposalManagerTeamAsync(name, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} SetupController_CreateSiteRoles error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"CreateSiteRoles error: {ex.Message}", requestId);
                return BadRequest(errorResponse);
            }
            return NoContent();
        }

        [HttpPost("CreateProposalManagerAdminGroup/{name}", Name = "CreateProposalManagerAdminGroup")]
        public async Task<IActionResult> CreateProposalManagerAdminGroup(string name)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} SetupController_CreateProposalManagerAdminGroup called.");

            // Check to see if setup is enabled and if not respond with bad request
            var checkSetupState = await CheckSetupState();
            if (checkSetupState != null) return BadRequest(checkSetupState);

            try
            {
                await _setupService.CreateAdminGroupAsync(name, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} SetupController_CreateProposalManagerAdminGroup error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"CreateProposalManagerAdminGroup error: {ex.Message}", requestId);
                return BadRequest(errorResponse);
            }
            return NoContent();
        }

        [HttpGet("GetAppId/{name}", Name = "GetAppId")]
        public async Task<IActionResult> GetAppId(string name)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} SetupController_CreateProposalManagerAdminGroup called.");

            // Check to see if setup is enabled and if not respond with bad request
            var checkSetupState = await CheckSetupState();
            if (checkSetupState != null) return BadRequest(checkSetupState);

            try
            {
                await _setupService.GetAppId(name);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} SetupController_CreateProposalManagerAdminGroup error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"CreateProposalManagerAdminGroup error: {ex.Message}", requestId);
                return BadRequest(errorResponse);
            }
            return NoContent();
        }

        // Get: /Setup/GetTestToken TODO: Test method that needs to be removed befoe release
        [AllowAnonymous]
        [HttpGet("GetTestToken", Name = "GetTestToken")]
        public async Task<IActionResult> GetTestToken()
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} SetupController_GetTestToken called.");

            // Check to see if setup is enabled and if not respond with bad request
            var checkSetupState = await CheckSetupState();
            if (checkSetupState != null) return BadRequest(checkSetupState);

            try
            {
                var accessToken = await _authProvider.GetAppAccessTokenAsync();

                return Ok(accessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} SetupController_GetTestToken error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"RequestID:{requestId} SetupController_GetTestToken error: {ex.Message}", requestId);

                return BadRequest(errorResponse);
            }
        }

        // Private methods
        private Task<JObject> CheckSetupState(string requestId = "")
        {
            var response = new JObject();

            if (_appOptions.SetupPage.ToLower() != "enabled")
            {
                _logger.LogError($"RequestID:{requestId} - SetupController_CheckSetupState error: Setup is not enabled");
                response = JsonErrorResponse.BadRequest($"SetupController_CheckSetupState error: Setup is not enabled", requestId);
            }
            else
            {
                response = null;
            }

            return Task.FromResult(response);
        }
    }
}
