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
using System.Linq;
using ApplicationCore.Helpers.Exceptions;

namespace Infrastructure.Services
{
    public class ProcessRepository : BaseRepository<ProcessesType>, IProcessRepository
    {
        private readonly GraphSharePointAppService _graphSharePointAppService;

        public ProcessRepository(
            ILogger<ProcessRepository> logger,
            IOptionsMonitor<AppOptions> appOptions,
            GraphSharePointAppService graphSharePointAppService) : base(logger, appOptions)
        {
            Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));
            _graphSharePointAppService = graphSharePointAppService;
        }

        public async Task<JObject> CreateItemAsync(ProcessesType process, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - ProcessRepository_CreateItemAsync called.");

            try
            {
                Guard.Against.Null(process, nameof(process), requestId);
                Guard.Against.NullOrEmpty(process.ProcessStep, nameof(process.ProcessStep), requestId);

                // Ensure id is blank since it will be set by SharePoint
                process.Id = String.Empty;

                _logger.LogInformation($"RequestId: {requestId} - processRepository_CreateItemAsync creating SharePoint List for process.");
               
                // Create Json object for SharePoint create list item
                dynamic processFieldsJson = new JObject();
                processFieldsJson.ProcessType = process.ProcessType;
                processFieldsJson.ProcessStep = process.ProcessStep;
                processFieldsJson.Channel = process.Channel;
                processFieldsJson.RoleId = process.RoleId;
                processFieldsJson.RoleName = process.RoleName;

                dynamic processJson = new JObject();
                processJson.fields = processFieldsJson;

                var processSiteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.ProcessListId
                };

                var result = await _graphSharePointAppService.CreateListItemAsync(processSiteList, processJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - processRepository_CreateItemAsync finished creating SharePoint List for process.");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - processRepository_CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - processRepository_CreateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - ProcessRepository_DeleteItemAsync called.");

            try
            {
                Guard.Against.Null(id, nameof(id), requestId);

                var processSiteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.ProcessListId
                };

                var json = await _graphSharePointAppService.DeleteListItemAsync(processSiteList, id, requestId);
                Guard.Against.Null(json, nameof(json), requestId);

                return StatusCodes.Status204NoContent;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - ProcessRepository_DeleteItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - ProcessRepository_DeleteItemAsync Service Exception: {ex}");
            }
        }

        public async Task<IList<ProcessesType>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - ProcessRepository_GetAllAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.ProcessListId
                };

                dynamic json = await _graphSharePointAppService.GetListItemsAsync(siteList, "all", requestId);
                var itemsList = new List<ProcessesType>();
                if (json.value.HasValues)
                {
                    foreach (var item in json.value)
                    {
                        var obj = JObject.Parse(item.ToString()).SelectToken("fields");
                        itemsList.Add(new ProcessesType
                        {
                            Id = obj.SelectToken("id")?.ToString(),
                            ProcessType = obj.SelectToken("ProcessType")?.ToString(),
                            ProcessStep = obj.SelectToken("ProcessStep")?.ToString(),
                            Channel = obj.SelectToken("Channel")?.ToString(),
                            RoleId = obj.SelectToken("RoleId") != null? obj.SelectToken("RoleId").ToString() : String.Empty,
                            RoleName = obj.SelectToken("RoleName") != null ? obj.SelectToken("RoleName").ToString() : String.Empty
                        });
                    }
                }

                return itemsList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - ProcessRepo_GetAllAsync error: {ex}");
                throw;
            }
        }

        public async Task<JObject> UpdateItemAsync(ProcessesType process, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - ProcessRepository_UpdateItemAsync called.");

            try
            {
                await DeleteItemAsync(process.Id.ToString(), requestId);
                var result = await CreateItemAsync(process, requestId);

                _logger.LogInformation($"RequestId: {requestId} - ProcessRepository_UpdateItemAsync finished updating SharePoint List for process.");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - ProcessRepository_UpdateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - ProcessRepository_UpdateItemAsync Service Exception: {ex}");
            }
        }
    }
}