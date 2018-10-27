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
using ApplicationCore.Helpers;
using ApplicationCore.Entities.GraphServices;
using ApplicationCore.Services;
using ApplicationCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using ApplicationCore.Helpers.Exceptions;
using ApplicationCore.Authorization;

namespace Infrastructure.Services
{
    public class RoleMappingRepository : BaseRepository<RoleMapping>, IRoleMappingRepository
    {
        private readonly GraphSharePointAppService _graphSharePointAppService;
        private IMemoryCache _cache;
        private readonly GraphUserAppService _graphUserAppService;

        public RoleMappingRepository(
            ILogger<RoleMappingRepository> logger,
            IOptionsMonitor<AppOptions> appOptions,
            GraphSharePointAppService graphSharePointAppService,
            GraphUserAppService graphUserAppService,
            IMemoryCache memoryCache) : base(logger, appOptions)
        {
            Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));
            Guard.Against.Null(graphUserAppService, nameof(graphUserAppService));
            _graphSharePointAppService = graphSharePointAppService;
            _cache = memoryCache;
            _graphUserAppService = graphUserAppService;
        }

        public async Task<StatusCodes> CreateItemAsync(RoleMapping entity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RoleMappingRepo_CreateItemAsync called.");

            try
            {
                if (!(await checkADGroupNameinAADAsync(entity.AdGroupName.Trim()))) return StatusCodes.Status400BadRequest;

                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.RoleMappingsListId
                };

                // Create Json object for SharePoint create list item
                dynamic itemFieldsJson = new JObject();
                dynamic itemJson = new JObject();
                itemFieldsJson.Title = entity.Id;
                itemFieldsJson.ADGroupName = entity.AdGroupName.Trim();
                itemFieldsJson.Role = entity.Role.DisplayName;
                itemFieldsJson.Permissions = JsonConvert.SerializeObject(entity.Permissions, Formatting.Indented);
                itemJson.fields = itemFieldsJson;

                var result = await _graphSharePointAppService.CreateListItemAsync(siteList, itemJson.ToString(), requestId);

                await SetUpdatedRoleMappingListInCacheAsync(requestId);

                _logger.LogInformation($"RequestId: {requestId} - RoleMappingRepo_CreateItemAsync finished creating SharePoint list item.");

                return StatusCodes.Status201Created;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RoleMappingRepo_CreateItemAsync error: {ex}");
                throw;
            }
        }

        private async Task<bool> checkADGroupNameinAADAsync(string adGroupName, string requestId="")
        {
            bool flag = false;
            try
            {
                var options = new List<QueryParam>();
                //Granular Permission Change :  Start
                options.Add(new QueryParam("filter", $"startswith(displayName,'{adGroupName}')"));
                var groupIdJson = await _graphUserAppService.GetGroupAsync(options, "", requestId);
                dynamic jsonDyn = groupIdJson;
                if (jsonDyn.value.HasValues)
                {
                    var id = "";
                    id = jsonDyn.value[0].id.ToString();
                    if (!string.IsNullOrEmpty(id))
                        flag = true;

                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RoleMappingRepo_CreateItemAsync error: {ex}");
            }

            return flag;
        }

        private async Task SetUpdatedRoleMappingListInCacheAsync(string requestId)
        {
            try
            {
                var roleMappingList = new List<RoleMapping>();
                roleMappingList = (await GetRoleMappingListAsync(requestId)).ToList();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(_appOptions.UserProfileCacheExpiration));
                _cache.Set("PM_RoleMappingList", roleMappingList, cacheEntryOptions);
            }
            catch(Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RoleMappingRepo_CreateItemAsync error: {ex}");
            }
        }

        public async Task<StatusCodes> UpdateItemAsync(RoleMapping entity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RoleMappingRepo_UpdateItemAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.RoleMappingsListId
                };

                // Create Json object for SharePoint create list item
                dynamic itemJson = new JObject();
                itemJson.Title = entity.Id;
                itemJson.ADGroupName = entity.AdGroupName;
                itemJson.Role = entity.Role.DisplayName;
                itemJson.Permissions = JsonConvert.SerializeObject(entity.Permissions, Formatting.Indented);

                var result = await _graphSharePointAppService.UpdateListItemAsync(siteList, entity.Id, itemJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - RoleMappingRepo_UpdateItemAsync finished creating SharePoint list item.");

                await SetUpdatedRoleMappingListInCacheAsync(requestId);
                return StatusCodes.Status200OK;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RoleMappingRepo_UpdateItemAsync error: {ex}");
                throw;
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RoleMappingRepo_DeleteItemAsync called.");

            try
            {
                Guard.Against.NullOrEmpty(id, "RoleMappingRepo_DeleteItemAsync id null or empty", requestId);

                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.RoleMappingsListId
                };

                var result = await _graphSharePointAppService.DeleteListItemAsync(siteList, id, requestId);

                _logger.LogInformation($"RequestId: {requestId} - RoleMappingRepo_DeleteItemAsync finished creating SharePoint list item.");

                await SetUpdatedRoleMappingListInCacheAsync(requestId);

                return StatusCodes.Status204NoContent;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RoleMappingRepo_DeleteItemAsync error: {ex}");
                throw;
            }
        }

        public async Task<IList<RoleMapping>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RoleMappingRepo_GetAllAsync called.");

            try
            {
                return await CacheTryGetRoleMappingListAsync(requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RoleMappingRepo_GetAllAsync error: {ex}");
                throw;
            }
        }

        private async Task<IList<RoleMapping>> CacheTryGetRoleMappingListAsync(string requestId = "")
        {
            try
            {
                var roleMappingList = new List<RoleMapping>();

                if (_appOptions.UserProfileCacheExpiration == 0)
                {
                    roleMappingList = (await GetRoleMappingListAsync(requestId)).ToList();
                }
                else
                {
                    var isExist = _cache.TryGetValue("PM_RoleMappingList", out roleMappingList);

                    if (!isExist)
                    {
                        roleMappingList = (await GetRoleMappingListAsync(requestId)).ToList();

                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_appOptions.UserProfileCacheExpiration));

                        _cache.Set("PM_RoleMappingList", roleMappingList, cacheEntryOptions);
                    }
                }

                return roleMappingList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RoleMappingRepo_CacheTryGetRoleMappingListAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - RoleMappingRepo_CacheTryGetRoleMappingListAsync Service Exception: {ex}");
            }
        }

        private async Task<IList<RoleMapping>> GetRoleMappingListAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RoleMappingRepo_GetRoleMappingListAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.RoleMappingsListId
                };

                var json = await _graphSharePointAppService.GetListItemsAsync(siteList, "all", requestId);
                JArray jsonArray = JArray.Parse(json["value"].ToString());
                var itemsList = new List<RoleMapping>();
                foreach (var item in jsonArray)
                {
                    var rolemapping = RoleMapping.Empty;
                    rolemapping.Id = item["fields"]["id"].ToString() ?? String.Empty;
                    //Bug Fix
                    if(item["fields"]["ADGroupName"] != null)
                    {
                        rolemapping.AdGroupName = item["fields"]["ADGroupName"].ToString();
                        rolemapping.Role.DisplayName = item["fields"]["Role"].ToString();
                        JArray jsonAr = JArray.Parse(item["fields"]["Permissions"].ToString());
                        foreach (var p in jsonAr)
                        {
                            rolemapping.Permissions.Add(JsonConvert.DeserializeObject<Permission>(p.ToString()));
                        }
                        itemsList.Add(rolemapping);
                    }
                }

                return itemsList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RoleMappingRepo_GetRoleMappingListAsync error: {ex}");
                throw;
            }
        }
    }
}
