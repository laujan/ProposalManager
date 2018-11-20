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
using ApplicationCore.Helpers;
using ApplicationCore.Entities.GraphServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ApplicationCore;
using Infrastructure.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services
{
    public class TasksRepository : BaseRepository<Tasks>, ITasksRepository
    {
        private readonly GraphSharePointAppService _graphSharePointAppService;
        private Lazy<IAuthorizationService> _authorizationService;

        public TasksRepository(
            ILogger<TasksRepository> logger,
            IOptionsMonitor<AppOptions> appOptions,
            GraphSharePointAppService graphSharePointAppService,
            IServiceProvider services) : base(logger, appOptions)
        {
            Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));
            _graphSharePointAppService = graphSharePointAppService;
            _authorizationService = new Lazy<IAuthorizationService>(() =>
            services.GetRequiredService<IAuthorizationService>());
        }

        public async Task<StatusCodes> CreateItemAsync(Tasks entity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - TasksRepo_CreateItemAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.TasksListId
                };

                // Create Json object for SharePoint create list item
                dynamic itemFieldsJson = new JObject();
                itemFieldsJson.Title = entity.Id;
                itemFieldsJson.Name = entity.Name;

                dynamic itemJson = new JObject();
                itemJson.fields = itemFieldsJson;

                var result = await _graphSharePointAppService.CreateListItemAsync(siteList, itemJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - TasksRepo_CreateItemAsync finished creating SharePoint list item.");

                return StatusCodes.Status201Created;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TasksRepo_CreateItemAsync error: {ex}");
                throw;
            }
        }

        public async Task<StatusCodes> UpdateItemAsync(Tasks entity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - TasksRepo_UpdateItemAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.TasksListId
                };

                // Create Json object for SharePoint create list item
                dynamic itemJson = new JObject();
                itemJson.Title = entity.Id;
                itemJson.Name = entity.Name;

                var result = await _graphSharePointAppService.UpdateListItemAsync(siteList, entity.Id, itemJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - TasksRepo_UpdateItemAsync finished creating SharePoint list item.");

                return StatusCodes.Status200OK;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TasksRepo_UpdateItemAsync error: {ex}");
                throw;
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - TasksRepo_DeleteItemAsync called.");

            try
            {
                Guard.Against.NullOrEmpty(id, "TasksRepo_DeleteItemAsync id null or empty", requestId);

                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.TasksListId
                };

                var result = await _graphSharePointAppService.DeleteListItemAsync(siteList, id, requestId);

                _logger.LogInformation($"RequestId: {requestId} - TasksRepo_DeleteItemAsync finished creating SharePoint list item.");

                return StatusCodes.Status204NoContent;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TasksRepo_DeleteItemAsync error: {ex}");
                throw;
            }
        }

        public async Task<IList<Tasks>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - TasksRepo_GetAllAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.TasksListId
                };

                var json = await _graphSharePointAppService.GetListItemsAsync(siteList, "all", requestId);
                JArray jsonArray = JArray.Parse(json["value"].ToString());

                var itemsList = new List<Tasks>();
                foreach (var item in jsonArray)
                {
                    itemsList.Add(JsonConvert.DeserializeObject<Tasks>(item["fields"].ToString(), new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    }));
                }

                return itemsList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TasksRepo_GetAllAsync error: {ex}");
                throw;
            }
        }
    }
}
