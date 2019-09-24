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
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using ApplicationCore.Helpers.Exceptions;

namespace Infrastructure.Services
{
    public class PermissionRepository: BaseRepository<Permission>, IPermissionRepository
    {
        private readonly GraphSharePointAppService _graphSharePointAppService;
        private IMemoryCache _cache;
        private const string PermissionCacheKey = "PM_PermissionList";

        public PermissionRepository(
        ILogger<PermissionRepository> logger,
        IOptionsMonitor<AppOptions> appOptions,
        GraphSharePointAppService graphSharePointAppService,
        IMemoryCache memoryCache) : base(logger, appOptions)
        {
            Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));
            _graphSharePointAppService = graphSharePointAppService;
            _cache = memoryCache;
        }

        public async Task<JObject> CreateItemAsync(Permission entity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - PermissionRepo_CreateItemAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.Permissions
                };

                // Create Json object for SharePoint create list item
                dynamic itemFieldsJson = new JObject();
                itemFieldsJson.Name = entity.Name;
                itemFieldsJson.Title = entity.Id;

                dynamic itemJson = new JObject();
                itemJson.fields = itemFieldsJson;

                var result = await _graphSharePointAppService.CreateListItemAsync(siteList, itemJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - PermissionRepo_CreateItemAsync finished creating SharePoint list item.");

                await SetCacheAsync(requestId);

                return result;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - PermissionRepo_CreateItemAsync error: {ex}");
                throw;
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - PermissionRepo_DeleteItemAsync called.");

            try
            {
                Guard.Against.NullOrEmpty(id, "PermissionRepo_DeleteItemAsync id null or empty", requestId);

                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.Permissions
                };

                await _graphSharePointAppService.DeleteListItemAsync(siteList, id, requestId);

                _logger.LogInformation($"RequestId: {requestId} - PermissionRepoo_DeleteItemAsync finished creating SharePoint list item.");

                await SetCacheAsync(requestId);

                return StatusCodes.Status204NoContent;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - PermissionRepo_DeleteItemAsync error: {ex}");
                throw;
            }
        }

        public async Task<IList<Permission>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - PermissionRepo_GetAllAsync called.");

            try
            {
                return await CacheTryGetPermissionListAsync(requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - PermissionRepo_GetAllAsync error: {ex}");
                throw;
            }
        }

        private async Task<IList<Permission>> CacheTryGetPermissionListAsync(string requestId = "")
        {
            try
            {
                var roleList = new List<Permission>();

                if (_appOptions.UserProfileCacheExpiration == 0)
                {
                    roleList = (await GetPermissionListAsync(requestId)).ToList();
                }
                else
                {
                    var isExist = _cache.TryGetValue(PermissionCacheKey, out roleList);

                    if (!isExist)
                    {
                        roleList = (await GetPermissionListAsync(requestId)).ToList();

                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_appOptions.UserProfileCacheExpiration));

                        _cache.Set(PermissionCacheKey, roleList, cacheEntryOptions);
                    }
                }

                return roleList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - PermissionRepo_CacheTryGetPermissionListAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - PermissionRepo_CacheTryGetPermissionListAsync Service Exception: {ex}");
            }
        }

        private async Task<IList<Permission>> GetPermissionListAsync(string requestId)
        {
            _logger.LogInformation($"RequestId: {requestId} - PermissionRepo_GetPermissionListAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.Permissions
                };

                dynamic json = await _graphSharePointAppService.GetListItemsAsync(siteList, "all", requestId);
                var itemsList = new List<Permission>();
                foreach (var item in json.value)
                {
                    itemsList.Add(JsonConvert.DeserializeObject<Permission>(item["fields"].ToString(), new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    }));
                }

                return itemsList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - PermissionRepo_GetPermissionListAsync error: {ex}");
                throw;
            }
        }

        private async Task SetCacheAsync(string requestId)
        {
            try
            {
                var permissionList = (await GetPermissionListAsync(requestId)).ToList();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(_appOptions.UserProfileCacheExpiration));
                _cache.Set(PermissionCacheKey, permissionList, cacheEntryOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Role_SetCahceAsync error: {ex}");
            }
        }

        public void CleanCache()
        {
            _cache.Remove(PermissionCacheKey);
        }
    }
}
