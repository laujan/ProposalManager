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

        public async Task<StatusCodes> CreateItemAsync(ProcessesType process, string requestId = "")
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

                dynamic processJson = new JObject();
                processJson.fields = processFieldsJson;

                var processSiteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.ProcessListId
                };

                var result = await _graphSharePointAppService.CreateListItemAsync(processSiteList, processJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - processRepository_CreateItemAsync finished creating SharePoint List for process.");

                return StatusCodes.Status201Created;
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

                var json = await _graphSharePointAppService.GetListItemsAsync(siteList, "all", requestId);
                dynamic jsonDyn = json;
                var itemsList = new List<ProcessesType>();
                if (jsonDyn.value.HasValues)
                {
                    foreach (var item in jsonDyn.value)
                    {
                        var process = ProcessesType.Empty;
                        process.Id = item.fields.id.ToString();
                        var x = item.fields;
                        process.ProcessType = item.fields.ProcessType.ToString();
                        process.ProcessStep = item.fields.ProcessStep.ToString();
                        process.Channel = item.fields.Channel.ToString();

                        itemsList.Add(process);
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

        public async Task<StatusCodes> UpdateItemAsync(ProcessesType process, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - ProcessRepository_UpdateItemAsync called.");

            try
            {
                await DeleteItemAsync(process.Id.ToString(), requestId);
                await CreateItemAsync(process, requestId);

                _logger.LogInformation($"RequestId: {requestId} - ProcessRepository_UpdateItemAsync finished updating SharePoint List for process.");

                return StatusCodes.Status201Created;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - ProcessRepository_UpdateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - ProcessRepository_UpdateItemAsync Service Exception: {ex}");
            }
        }
    }
}