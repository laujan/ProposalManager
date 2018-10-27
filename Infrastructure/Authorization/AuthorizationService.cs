// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Text;
using ApplicationCore.Authorization;
using ApplicationCore;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Interfaces;
using ApplicationCore.Helpers;
using ApplicationCore.Artifacts;
using System.Threading.Tasks;
using ApplicationCore.Helpers.Exceptions;
using ApplicationCore.Entities;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Authorization
{
    public class AuthorizationService : BaseService<AuthorizationService>, IAuthorizationService
    {
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IRoleMappingRepository _roleMappingRepository;
        private readonly IPermissionRepository _permissionRepository;

        private readonly IUserContext _userContext;
        private IMemoryCache _cache;
        private bool _overrdingAccess;
        private readonly string _clientId;
        public AuthorizationService(
            ILogger<AuthorizationService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IUserProfileRepository userProfileRepository,
            IRoleMappingRepository roleMappingRepository,
            IPermissionRepository permissionRepository,
            IMemoryCache cache,
            IConfiguration configuration,
            IUserContext userContext) : base(logger, appOptions)
        {
            Guard.Against.Null(userProfileRepository, nameof(userProfileRepository));
            Guard.Against.Null(roleMappingRepository, nameof(roleMappingRepository));
            _userProfileRepository = userProfileRepository;
            _roleMappingRepository = roleMappingRepository;
            _permissionRepository = permissionRepository;
            _userContext = userContext;
            _cache = cache;
            _overrdingAccess = false;

            var azureOptions = new AzureAdOptions();
            configuration.Bind("AzureAd", azureOptions);
            _clientId = azureOptions.ClientId;
        }
        public async Task<StatusCodes> CheckAdminAccsessAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - AuthorizationService_CheckAdminAccessAsync called.");

            //var currentUserScope = (_userContext.User.Claims).ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/scope")?.Value;

            //if (currentUserScope != "access_as_user")
            //{
            //    var app_permission = false;
            //    app_permission = (await _permissionRepository.GetAllAsync(requestId)).ToList().Any(x => x.Name.ToLower() == currentUserScope.ToString().ToLower());
            //    if (app_permission)
            //        return StatusCodes.Status200OK;
            //    else
            //        return StatusCodes.Status401Unauthorized;
            //}
            //var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;
            //var selectedUserProfile = await _userProfileRepository.GetItemByUpnAsync(currentUser, requestId);

            //var currentUserPermissionList = new List<Permission>();
            //var rolemappinglist = new List<RoleMapping>();
            //rolemappinglist = (await _roleMappingRepository.GetAllAsync(requestId)).ToList();
            //currentUserPermissionList = (from curroles in selectedUserProfile.Fields.UserRoles
            //                             from roles in rolemappinglist
            //                             where curroles.DisplayName == roles.Role.DisplayName
            //                             select roles.Permissions).SelectMany(x => x).ToList();
            var currentUserPermissionList = new List<Permission>();
            var rolemappinglist = new List<RoleMapping>();

            var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;

            if (!(string.IsNullOrEmpty(currentUser)))
            {

                var selectedUserProfile = await _userProfileRepository.GetItemByUpnAsync(currentUser, requestId);
                rolemappinglist = (await _roleMappingRepository.GetAllAsync(requestId)).ToList();
                currentUserPermissionList = (from curroles in selectedUserProfile.Fields.UserRoles
                                             from roles in rolemappinglist
                                             where curroles.DisplayName == roles.Role.DisplayName
                                             select roles.Permissions).SelectMany(x => x).ToList();
            }
            else
            {
                var aud = (_userContext.User.Claims).ToList().Find(x => x.Type == "aud")?.Value;
                var azp = (_userContext.User.Claims).ToList().Find(x => x.Type == "azp")?.Value;

                if (azp == _clientId)
                {
                    rolemappinglist = (await _roleMappingRepository.GetAllAsync(requestId)).
                        Where(x => x.AdGroupName == $"aud_{aud}").ToList();
                    currentUserPermissionList = (from rolemapping in rolemappinglist
                                                 select rolemapping.Permissions).SelectMany(x => x).ToList();
                }else
                    return StatusCodes.Status401Unauthorized;

            }
            bool check = false;
            foreach(var userPermission in currentUserPermissionList)
            {
                if(userPermission.Name == Access.Administrator.ToString())
                    check = true;
            }

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
                var rolemappinglist = new List<RoleMapping>();

                var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;
                if (!(string.IsNullOrEmpty(currentUser)))
                {

                    var selectedUserProfile = await _userProfileRepository.GetItemByUpnAsync(currentUser, requestId);
                    rolemappinglist = (await _roleMappingRepository.GetAllAsync(requestId)).ToList();
                    currentUserPermissionList = (from curroles in selectedUserProfile.Fields.UserRoles
                                                 from roles in rolemappinglist
                                                 where curroles.DisplayName == roles.Role.DisplayName
                                                 select roles.Permissions).SelectMany(x => x).ToList();
                }
                else
                {
                    var aud = (_userContext.User.Claims).ToList().Find(x => x.Type == "aud")?.Value;
                    var azp = (_userContext.User.Claims).ToList().Find(x => x.Type == "azp")?.Value;

                    if (azp == _clientId)
                    {
                        rolemappinglist = (await _roleMappingRepository.GetAllAsync(requestId)).
                            Where(x => x.AdGroupName == $"aud_{aud}").ToList();
                        currentUserPermissionList = (from rolemapping in rolemappinglist
                                                     select rolemapping.Permissions).SelectMany(x => x).ToList();
                    }
                    else
                        return StatusCodes.Status401Unauthorized;

                }

                if (currentUserPermissionList.Any(curnt_per => permissionsRequested.Any(req_per => req_per.Name.ToLower() == curnt_per.Name.ToLower())))
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

        private async Task<List<Permission>> CacheTryCurrentUserPermissiontAsync(string currentUser,string requestId = "")
        {
            try
            {
                var currentUserPermissionList = new List<Permission>();
                var rolemappinglist = new List<RoleMapping>();
                var isExist = _cache.TryGetValue("PM_CurntUserPermissionList", out currentUserPermissionList);

                if (_appOptions.UserProfileCacheExpiration == 0 || !isExist)
                {
                    var selectedUserProfile = await _userProfileRepository.GetItemByUpnAsync(currentUser, requestId);
                    rolemappinglist = (await _roleMappingRepository.GetAllAsync(requestId)).ToList();
                    currentUserPermissionList = (from curroles in selectedUserProfile.Fields.UserRoles
                            from roles in rolemappinglist
                            where curroles.DisplayName == roles.Role.DisplayName
                            select roles.Permissions).SelectMany(x => x).ToList();
                    currentUserPermissionList = currentUserPermissionList.Select(x => new Permission { Id = x.Id, Name = x.Name.ToLower() }).ToList();
                    currentUserPermissionList = currentUserPermissionList.GroupBy(x => x.Name).Select(x => x.First()).ToList();
                    if (_appOptions.UserProfileCacheExpiration>0)
                    {
                        var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(_appOptions.UserProfileCacheExpiration));
                        _cache.Set("PM_CurntUserPermissionList", currentUserPermissionList, cacheEntryOptions);
                    }
                }

                return currentUserPermissionList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - AuthorizationService_CacheTryCurrentUserPermissiontAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - AuthorizationService_CacheTryCurrentUserPermissiontAsync Service Exception: {ex}");
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

                //toLower
                permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().
                    //Where(x => list.Any(x.Name.Contains)).ToList();
                    Where(permissions => list.Any(req_per => req_per.ToLower() == permissions.Name.ToLower())).ToList();
                var result = await CheckAccessAsync(permissionsNeeded, requestId);

                return result;
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
                    var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;
                    if (!(opportunity.Content.TeamMembers).ToList().Any(teamMember => teamMember.Fields.UserPrincipalName == currentUser))
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
 