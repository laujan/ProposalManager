// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Artifacts;
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

        public RoleRepository(
            ILogger<CategoryRepository> logger,
            IOptionsMonitor<AppOptions> appOptions,
            GraphSharePointAppService graphSharePointAppService,
            IMemoryCache memoryCache) : base(logger, appOptions)
        {
            Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));
            _graphSharePointAppService = graphSharePointAppService;
            _cache = memoryCache;
        }

        public async Task<StatusCodes> CreateItemAsync(Role entity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RolesRepo_CreateItemAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.RoleListId
                };

                // Create Json object for SharePoint create list item
                dynamic itemFieldsJson = new JObject();
                itemFieldsJson.Name = entity.DisplayName;
                itemFieldsJson.Title = entity.Id;

                dynamic itemJson = new JObject();
                itemJson.fields = itemFieldsJson;

                var result = await _graphSharePointAppService.CreateListItemAsync(siteList, itemJson.ToString(), requestId);

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

                var result = await _graphSharePointAppService.DeleteListItemAsync(siteList, id, requestId);

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
                dynamic itemJson = new JObject();
                itemJson.Title = entity.Id;
                itemJson.Name = entity.DisplayName;

                var result = await _graphSharePointAppService.UpdateListItemAsync(siteList, entity.Id, itemJson.ToString(), requestId);

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

                var json = await _graphSharePointAppService.GetListItemsAsync(siteList, "all", requestId);
                JArray jsonArray = JArray.Parse(json["value"].ToString());

                var itemsList = new List<Role>();
                foreach (var item in jsonArray)
                {
                    var role = Role.Empty;
                    role.Id = item["fields"]["id"].ToString() ?? String.Empty;
                    role.DisplayName = item["fields"]["Name"].ToString() ?? String.Empty;

                    itemsList.Add(role);
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
                var roleList = new List<Role>();
                roleList = (await GetRoleListAsync(requestId)).ToList();

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
