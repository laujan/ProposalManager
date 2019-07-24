// Copyright(c) Microsoft Corporation. 
// All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Interfaces;
using ApplicationCore.Entities;
using ApplicationCore.Services;
using ApplicationCore;
using ApplicationCore.Helpers;
using ApplicationCore.Entities.GraphServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using ApplicationCore.Helpers.Exceptions;

namespace Infrastructure.Services
{
    public class RoleRepository : BaseRepository<Role>, IRoleRepository
    {
        private readonly GraphSharePointAppService _graphSharePointAppService;
        private IMemoryCache _cache;
        private readonly GraphUserAppService _graphUserAppService;
        private readonly GraphTeamsAppService _graphTeamsAppService;

        public RoleRepository(
            ILogger<RoleRepository> logger,
            IOptionsMonitor<AppOptions> appOptions,
            GraphSharePointAppService graphSharePointAppService,
            GraphUserAppService graphUserAppService,
            GraphTeamsAppService graphTeamsAppService,
            IMemoryCache memoryCache) : base(logger, appOptions)
        {
            Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));
            Guard.Against.Null(graphUserAppService, nameof(graphUserAppService));
            Guard.Against.Null(graphTeamsAppService, nameof(graphTeamsAppService));

            _graphSharePointAppService = graphSharePointAppService;
            _cache = memoryCache;
            _graphUserAppService = graphUserAppService;
            _graphTeamsAppService = graphTeamsAppService;
        }

        public async Task<StatusCodes> CreateItemAsync(Role entity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RolesRepo_CreateItemAsync called.");

            try
            {
                if (!(await CheckRoleAdGroupNameExist(entity.AdGroupName.Trim(), requestId)))
                {
                    try
                    {
                        await _graphTeamsAppService.CreateGroupAsync(entity.AdGroupName.Trim(), entity.AdGroupName.Trim() + " Group");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} - SetupService_CreateAdminGroupAsync error: {ex}");
                    }
                }

                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.RoleListId
                };

                // Create Json object for SharePoint create list item
                dynamic itemFieldsJson = new JObject();
                dynamic itemJson = new JObject();
                itemFieldsJson.AdGroupName = entity.AdGroupName.Trim();
                itemFieldsJson.Role = entity.DisplayName.Trim();
                itemFieldsJson.TeamsMembership = entity.TeamsMembership.Name.ToString();
                itemFieldsJson.Permissions = JsonConvert.SerializeObject(entity.Permissions, Formatting.Indented);
                itemJson.fields = itemFieldsJson;

                await _graphSharePointAppService.CreateListItemAsync(siteList, itemJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - RolesRepo_CreateItemAsync finished creating SharePoint list item.");

                await SetCacheAsync(requestId);

                return StatusCodes.Status201Created;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RolesRepo_CreateItemAsync error: {ex}");
                throw;
            }
        }

        private async Task<bool> CheckRoleAdGroupNameExist(string adGroupName, string requestId)
        {
            bool flag = false;
            try
            {
                //bug fix
                if ("aud" == adGroupName.Substring(0, 3).ToLower())
                    flag = true;
                else
                {
                    var options = new List<QueryParam>();
                    //Granular Permission Change :  Start
                    options.Add(new QueryParam("filter", $"startswith(displayName,'{adGroupName}')"));
                    dynamic jsonDyn = await _graphUserAppService.GetGroupAsync(options, "", requestId);
                    if (jsonDyn.value.HasValues)
                    {
                        var id = "";
                        id = jsonDyn.value[0].id.ToString();
                        if (!string.IsNullOrEmpty(id))
                            flag = true;

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RoleMappingRepo_CreateItemAsync error: {ex}");
            }

            return flag;
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RolesRepo_DeleteItemAsync called.");

            try
            {
                Guard.Against.NullOrEmpty(id, "RolesRepo_DeleteItemAsync id null or empty", requestId);

                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.RoleListId
                };

                await _graphSharePointAppService.DeleteListItemAsync(siteList, id, requestId);

                _logger.LogInformation($"RequestId: {requestId} - RolesRepo_DeleteItemAsync finished creating SharePoint list item.");

                await SetCacheAsync(requestId);

                return StatusCodes.Status204NoContent;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RolesRepo_DeleteItemAsync error: {ex}");
                throw;
            }
        }

        public async Task<IList<Role>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RolesRepo_GetAllAsync called.");

            try
            {
                return await CacheTryGetRoleListAsync(requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RolesRepo_GetAllAsync error: {ex}");
                throw;
            }
        }

        public async Task<StatusCodes> UpdateItemAsync(Role entity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RolesRepo_UpdateItemAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.RoleListId
                };

                // Create Json object for SharePoint create list item
                dynamic itemFieldsJson = new JObject();
                itemFieldsJson.AdGroupName = entity.AdGroupName.Trim();
                itemFieldsJson.Role = entity.DisplayName.Trim();
                itemFieldsJson.TeamsMembership = entity.TeamsMembership.Name.ToString();
                itemFieldsJson.Permissions = JsonConvert.SerializeObject(entity.Permissions, Formatting.Indented);


                await _graphSharePointAppService.UpdateListItemAsync(siteList, entity.Id, itemFieldsJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - RolesRepo_UpdateItemAsync finished creating SharePoint list item.");

                await SetCacheAsync(requestId);

                return StatusCodes.Status200OK;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RolesRepo_UpdateItemAsync error: {ex}");
                throw;
            }
        }

        private async Task<IList<Role>> CacheTryGetRoleListAsync(string requestId = "")
        {
            try
            {
                var roleList = new List<Role>();

                if (_appOptions.UserProfileCacheExpiration == 0)
                {
                    roleList = (await GetRoleListAsync(requestId)).ToList();
                }
                else
                {
                    var isExist = _cache.TryGetValue("PM_RoleList", out roleList);

                    if (!isExist)
                    {
                        roleList = (await GetRoleListAsync(requestId)).ToList();

                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_appOptions.UserProfileCacheExpiration));

                        _cache.Set("PM_RoleList", roleList, cacheEntryOptions);
                    }
                }

                return roleList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RolesRepo_CacheTryGetRoleListAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - RolesRepo_CacheTryGetRoleListAsync Service Exception: {ex}");
            }
        }

        private async Task<IList<Role>> GetRoleListAsync(string requestId)
        {
            _logger.LogInformation($"RequestId: {requestId} - RolesRepo_GetRoleListAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.RoleListId
                };

                dynamic json = await _graphSharePointAppService.GetListItemsAsync(siteList, "all", requestId);
                var itemsList = new List<Role>();
                if (json.value.HasValues)
                {
                    foreach (var item in json.value)
                    {
                        var obj = JObject.Parse(item.ToString()).SelectToken("fields");
                        itemsList.Add(new Role
                        {                           
                            Id = obj.SelectToken("id")?.ToString(),
                            AdGroupName = obj.SelectToken("AdGroupName")?.ToString(),
                            DisplayName = obj.SelectToken("Role")?.ToString(),
                            TeamsMembership = obj.SelectToken("TeamsMembership") !=null? TeamsMembership.FromName(obj.SelectToken("TeamsMembership").ToString()): TeamsMembership.None,
                            Permissions = obj.SelectToken("Permissions") != null ? JsonConvert.DeserializeObject<IList<Permission>>(obj.SelectToken("Permissions").ToString(), new JsonSerializerSettings
                            {
                                MissingMemberHandling = MissingMemberHandling.Ignore,
                                NullValueHandling = NullValueHandling.Ignore
                            }) : new List<Permission>()                         
 
                        });
                    }
                }


                return itemsList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RolesRepo_GetRoleListAsync error: {ex}");
                throw;
            }
        }

        private async Task SetCacheAsync(string requestId)
        {
            try
            {
                var roleList = (await GetRoleListAsync(requestId)).ToList();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(_appOptions.UserProfileCacheExpiration));
                _cache.Set("PM_RoleList", roleList, cacheEntryOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Role_SetCahceAsync error: {ex}");
            }
        }
    }
}
