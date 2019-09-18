// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using ApplicationCore.Artifacts;
using ApplicationCore.Authorization;
using ApplicationCore.Entities;
using ApplicationCore.Helpers;
using ApplicationCore.Helpers.Exceptions;
using ApplicationCore.Interfaces;
using Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Authorization
{
    public class AuthorizationService : BaseService<AuthorizationService>, IAuthorizationService
    {
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;

        private readonly IUserContext _userContext;
        private bool _overrdingAccess;
        private readonly string _clientId;
        public AuthorizationService(
            ILogger<AuthorizationService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IUserProfileRepository userProfileRepository,
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IMemoryCache cache,
            IConfiguration configuration,
            IUserContext userContext) : base(logger, appOptions)
        {
            Guard.Against.Null(userProfileRepository, nameof(userProfileRepository));
            Guard.Against.Null(roleRepository, nameof(roleRepository));
            _userProfileRepository = userProfileRepository;
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _userContext = userContext;
            _overrdingAccess = false;

            var azureOptions = new AzureAdOptions();
            configuration.Bind("AzureAd", azureOptions);
            _clientId = azureOptions.ClientId;
        }
        public async Task<StatusCodes> CheckAdminAccsessAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - AuthorizationService_CheckAdminAccessAsync called.");

            var currentUserPermissionList = new List<Permission>();
            var roleList = new List<Role>();

            var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type.Equals("preferred_username", StringComparison.OrdinalIgnoreCase))?.Value;

            if (!(string.IsNullOrEmpty(currentUser)))
            {

                var selectedUserProfile = await _userProfileRepository.GetItemByUpnAsync(currentUser, requestId);

                roleList = (await _roleRepository.GetAllAsync(requestId)).ToList();

                currentUserPermissionList = selectedUserProfile.Fields.UserRoles
                       .Join(roleList, current => current.DisplayName, role => role.DisplayName, (current, role) => role)
                       .SelectMany(x => x.Permissions).ToList();
            }
            else
            {
                var aud = (_userContext.User.Claims).SingleOrDefault(x => x.Type.Equals("aud", StringComparison.OrdinalIgnoreCase))?.Value;
                var azp = (_userContext.User.Claims).SingleOrDefault(x => x.Type.Equals("azp", StringComparison.OrdinalIgnoreCase))?.Value;

                if (azp == _clientId)
                {
                    currentUserPermissionList = (await _roleRepository.GetAllAsync(requestId))
                        .Where(x => x.AdGroupName.Equals($"aud_{aud}", StringComparison.OrdinalIgnoreCase)).SelectMany(x => x.Permissions).ToList();
                }else
                    return StatusCodes.Status401Unauthorized;

            }

            bool check = currentUserPermissionList.Any(x => x.Name.Equals(Access.Administrator.ToString(), StringComparison.OrdinalIgnoreCase));

            //throw an exception if the user doesnt have admin access.
            if (!check)
            {
                _logger.LogInformation($"RequestId: {requestId} - AuthorizationService_CheckAdminAccessAsync admin access exception.");
                throw new AccessDeniedException("Admin Access Required");
            }
            else
            {
                return StatusCodes.Status200OK;
            }
        }
        public async Task<StatusCodes> CheckAccessAsync(List<Permission> permissionsRequested, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - AuthorizationService_CheckAccessAsync called.");

            try
            {
                if (string.IsNullOrEmpty(requestId))
                {
                    if (requestId.StartsWith("bot"))
                    {
                        // TODO: Temp check for bot calls while bot sends token (currently is not)
                        return StatusCodes.Status200OK;
                    }
                }
 
                var currentUserPermissionList = new List<Permission>();
                var roleList = new List<Role>();

                var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;
                if (!(string.IsNullOrEmpty(currentUser)))
                {

                    var selectedUserProfile = await _userProfileRepository.GetItemByUpnAsync(currentUser, requestId);
                    roleList = (await _roleRepository.GetAllAsync(requestId)).ToList();
                    currentUserPermissionList = selectedUserProfile.Fields.UserRoles
                        .Join(roleList, current => current.DisplayName, role => role.DisplayName, (current, role) => role)
                        .SelectMany(x => x.Permissions).ToList();
                }
                else
                {
                    var aud = (_userContext.User.Claims).ToList().Find(x => x.Type == "aud")?.Value;
                    var azp = (_userContext.User.Claims).ToList().Find(x => x.Type == "azp")?.Value;

                    if (azp == _clientId)
                    {
                        currentUserPermissionList = (await _roleRepository.GetAllAsync(requestId))
                            .Where(x => x.AdGroupName.Equals($"aud_{aud}", StringComparison.OrdinalIgnoreCase))
                            .SelectMany(x => x.Permissions).ToList();
                    }
                    else
                        return StatusCodes.Status401Unauthorized;

                }

                if (currentUserPermissionList.Any(curnt_per => permissionsRequested.Any(req_per => req_per.Name.Equals(curnt_per.Name, StringComparison.OrdinalIgnoreCase))))
                    return StatusCodes.Status200OK;
                else
                    return StatusCodes.Status401Unauthorized;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - AuthorizationService_CheckAccessAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - AuthorizationService_CheckAccessAsync Service Exception: {ex}");
            }
        }

        //Granular Access Start
        public async Task<StatusCodes> CheckAccessFactoryAsync(PermissionNeededTo action, string requestId = "")
        {
            try
            {
                var permissionsNeeded = new List<ApplicationCore.Entities.Permission>();
                List<string> list = new List<string>();

                //TODO:Enum would be better
                switch (action)
                {
                    case PermissionNeededTo.Create:
                        list.AddRange(new List<string> { Access.Opportunity_Create.ToString()});
                        break;
                    case PermissionNeededTo.ReadAll:
                        list.AddRange(new List<string> {
                            Access.Opportunities_Read_All.ToString(),
                            Access.Opportunities_ReadWrite_All.ToString()});
                        break;
                    case PermissionNeededTo.Read:
                        list.AddRange(new List<string> {
                            Access.Opportunity_Read_All.ToString(),
                            Access.Opportunity_ReadWrite_All.ToString(),
                       });
                        break;
                    case PermissionNeededTo.ReadPartial:
                        list.AddRange(new List<string> {
                            Access.Opportunity_ReadWrite_Partial.ToString(),
                            Access.Opportunity_Read_Partial.ToString()
                       });
                        break;
                    case PermissionNeededTo.WriteAll:
                        list.AddRange(new List<string> { Access.Opportunities_ReadWrite_All.ToString() });
                        break;
                    case PermissionNeededTo.Write:
                        list.AddRange(new List<string> { Access.Opportunity_ReadWrite_All.ToString()});
                        break;
                    case PermissionNeededTo.WritePartial:
                        list.AddRange(new List<string> { Access.Opportunity_ReadWrite_Partial.ToString() });
                        break;
                    case PermissionNeededTo.Admin:
                        list.AddRange(new List<string> { Access.Administrator.ToString()});
                        break;
                    case PermissionNeededTo.DealTypeWrite:
                        list.AddRange(new List<string> {
                            Access.Opportunity_ReadWrite_Dealtype.ToString(),
                            Access.Opportunities_ReadWrite_All.ToString()});
                        break;
                    case PermissionNeededTo.TeamWrite:
                        list.AddRange(new List<string> {
                            Access.Opportunity_ReadWrite_Team.ToString(),
                            Access.Opportunities_ReadWrite_All.ToString()});
                        break;
                }

                permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId))
                    .Where(permissions => list.Any(req_per => req_per.Equals(permissions.Name, StringComparison.OrdinalIgnoreCase))).ToList();

                return await CheckAccessAsync(permissionsNeeded, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - OpportunityFactory_CheckAccess Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - OpportunityFactory_CheckAccess Service Exception: {ex}");
            }
        }
        //Granular Access End

        public async Task<bool> CheckAccessInOpportunityAsync(Opportunity opportunity, PermissionNeededTo access, string requestId = "")
        {
            try
            {
                bool value = true;

                if (StatusCodes.Status200OK == await CheckAccessFactoryAsync(access, requestId))
                {
                    var currentUser = (_userContext.User.Claims).SingleOrDefault(x => x.Type.Equals("preferred_username", StringComparison.OrdinalIgnoreCase))?.Value;
                    if (!(opportunity.Content.TeamMembers).ToList().Any(teamMember => teamMember.Fields.UserPrincipalName.Equals(currentUser, StringComparison.OrdinalIgnoreCase)))
                    {
                        // This user is not having any write permissions, so he won't be able to update
                        _logger.LogError($"RequestId: {requestId} - CheckAccessInOpportunityAsync current user: {currentUser} AccessDeniedException");
                        value = false;
                    }
                }
                else
                    value = false;

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - CheckAccessInOpportunityAsync Service Exception: {ex}");
                return false;
            }
        }

        public void SetGranularAccessOverride(bool v){
            this._overrdingAccess = v;
        }

        public bool GetGranularAccessOverride()
        {
            return this._overrdingAccess;
        }
    }
}
 