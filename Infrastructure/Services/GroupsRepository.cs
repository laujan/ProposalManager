// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationCore;
using ApplicationCore.Entities;
using ApplicationCore.Entities.GraphServices;
using ApplicationCore.Helpers;
using ApplicationCore.Interfaces;
using ApplicationCore.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Infrastructure.Services
{
    public class GroupsRepository: BaseRepository<Groups>, IGroupsRepository
    {
        private readonly GraphSharePointAppService _graphSharePointAppService;
        private readonly IMemoryCache _cache;
        private const string GroupCacheKey = "PM_GroupsList";

        public GroupsRepository(
        ILogger<GroupsRepository> logger,
        IOptionsMonitor<AppOptions> appOptions,
        GraphSharePointAppService graphSharePointAppService,
        IMemoryCache memoryCache) : base(logger, appOptions)
        {
            Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));
            _graphSharePointAppService = graphSharePointAppService;
            _cache = memoryCache;
        }

        public void CleanCache()
        {
            _cache.Remove(GroupCacheKey);
        }

        public async Task<StatusCodes> CreateItemAsync(Groups entity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GroupsRepo_CreateItemAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.GroupsListId
                };

                // Create Json object for SharePoint create list item
                dynamic itemFieldsJson = new JObject();
                itemFieldsJson.GroupName = entity.GroupName;
                itemFieldsJson.Title = entity.Id;
                itemFieldsJson.Process = JsonConvert.SerializeObject(entity.Processes,Formatting.Indented);

                dynamic itemJson = new JObject();
                itemJson.fields = itemFieldsJson;

                await _graphSharePointAppService.CreateListItemAsync(siteList, itemJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - GroupsRepo_CreateItemAsync finished creating SharePoint list item.");

                return StatusCodes.Status201Created;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GroupsRepo_CreateItemAsync error: {ex}");
                throw;
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GroupsRepo_DeleteItemAsync called.");

            try
            {
                Guard.Against.NullOrEmpty(id, "GroupsRepo_DeleteItemAsync id null or empty", requestId);

                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.GroupsListId
                };

                await _graphSharePointAppService.DeleteListItemAsync(siteList, id, requestId);

                _logger.LogInformation($"RequestId: {requestId} - GroupsRepo_DeleteItemAsync finished creating SharePoint list item.");

                await CacheTryGetGroupsListAsync(requestId);

                return StatusCodes.Status204NoContent;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GroupsRepo_DeleteItemAsync error: {ex}");
                throw;
            }
        }

        public async Task<IList<Groups>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GroupsRepo_GetAllAsync called.");

            try
            {
                return await CacheTryGetGroupsListAsync(requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GroupsRepo_GetAllAsync error: {ex}");
                throw;
            }
        }

        public async Task<StatusCodes> UpdateItemAsync(Groups entity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GroupsRepo_UpdateItemAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.GroupsListId
                };

                dynamic itemFieldsJson = new JObject();
                dynamic itemJson = new JObject();
                itemFieldsJson.GroupName = entity.GroupName;
                itemFieldsJson.Title = entity.Id;
                itemFieldsJson.Process = JsonConvert.SerializeObject(entity.Processes, Formatting.Indented);
                itemJson.fields = itemFieldsJson;

                 await _graphSharePointAppService.UpdateListItemAsync(siteList, entity.Id, itemJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - GroupsRepo_UpdateItemAsync finished creating SharePoint list item.");

                await CacheTryGetGroupsListAsync(requestId);

                return StatusCodes.Status200OK;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GroupsRepo_UpdateItemAsync error: {ex}");
                throw;
            }
        }

        private async Task<IList<Groups>> CacheTryGetGroupsListAsync(string requestId = "")
        {
            try
            {
                var groupsList = new List<Groups>();
                if (_appOptions.UserProfileCacheExpiration == 0)
                {
                    groupsList = (await GetGroupsListAsync(requestId)).ToList();
                }
                else
                {
                    var isExist = _cache.TryGetValue(GroupCacheKey, out groupsList);

                    if (!isExist)
                    {
                        groupsList = (await GetGroupsListAsync(requestId)).ToList();

                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_appOptions.UserProfileCacheExpiration));

                        _cache.Set(GroupCacheKey, groupsList, cacheEntryOptions);
                    }
                }
                return groupsList;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task<IList<Groups>> GetGroupsListAsync(string requestId)
        {
            _logger.LogInformation($"RequestId: {requestId} - GroupsRepo_GetGroupsListAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.GroupsListId
                };

                dynamic json = await _graphSharePointAppService.GetListItemsAsync(siteList, "all", requestId);
                var itemsList = new List<Groups>();
                foreach (var item in json.value)
                {
                    var obj = JObject.Parse(item.ToString()).SelectToken("fields");
                    itemsList.Add(new Groups
                    {
                        Id = obj.SelectToken("id")?.ToString(),
                        GroupName = obj.SelectToken("GroupName")?.ToString(),
                        Processes = obj.SelectToken("Process") != null ? JsonConvert.DeserializeObject<IList<ProcessesType>>(obj.SelectToken("Process").ToString(), new JsonSerializerSettings
                        {
                            MissingMemberHandling = MissingMemberHandling.Ignore,
                            NullValueHandling = NullValueHandling.Ignore
                        }) : new List<ProcessesType>()
                    });
                }

                return itemsList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GroupsRepo_GetGroupsListAsync error: {ex}");
                throw;
            }
        }
    }
}
