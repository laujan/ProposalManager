// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.ViewModels;
using ApplicationCore.Interfaces;
using ApplicationCore;
using ApplicationCore.Artifacts;
using Infrastructure.Services;
using ApplicationCore.Helpers;
using ApplicationCore.Entities;
using ApplicationCore.Helpers.Exceptions;

namespace Infrastructure.Services
{
    public class TemplateService : BaseService<TemplateService>, ITemplateService
    {
        private readonly ITemplateRepository _templateRepository;
        private readonly TemplateHelpers _templateHelpers;

        public TemplateService(
            ILogger<TemplateService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            ITemplateRepository templateRepository,
            TemplateHelpers templateHelpers) : base(logger, appOptions)
        {
            Guard.Against.Null(templateRepository, nameof(templateRepository));
            _templateRepository = templateRepository;
            _templateHelpers = templateHelpers;
        }
        public async Task<bool> ProcessCheckAsync(IList<ProcessViewModel> processList, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - Template_ProcessCheck called.");

            bool check = true;
            var processType = new List<string>();
            foreach(var process in processList)
            {
                if(!processType.Contains(process.ProcessStep))
                {
                    processType.Add(process.ProcessStep);
                }
                else
                {
                    check = false;
                }
                    
            }

            return check;
        }
        public async Task<StatusCodes> CreateItemAsync(TemplateViewModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - Template_CreateItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.TemplateName, nameof(modelObject.TemplateName), requestId);
            try
            {
                var entityObject = _templateHelpers.MapToEntity(modelObject, requestId);

                var result = await _templateRepository.CreateItemAsync(entityObject.Result, requestId);

                Guard.Against.NotStatus201Created(result, "Template_CreateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Template_CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - Template_CreateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DeleteItemAsync called.");
            Guard.Against.Null(id, nameof(id));

            var result = await _templateRepository.DeleteItemAsync(id, requestId);

            Guard.Against.NotStatus204NoContent(result, "DeleteItemAsync", requestId);

            return result;
        }

        public async Task<TemplateListViewModel> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GetAllAsync called.");

            try
            {
                var listItems = (await _templateRepository.GetAllAsync(requestId)).ToList();
                Guard.Against.Null(listItems, nameof(listItems), requestId);

                var templateistViewModel = new TemplateListViewModel();
                foreach (var item in listItems)
                {
                    templateistViewModel.ItemsList.Add(await _templateHelpers.MapToViewModel(item));
                }

                if (templateistViewModel.ItemsList.Count == 0)
                {
                    _logger.LogWarning($"RequestId: {requestId} - GetAllAsync no items found");
                    throw new NoItemsFound($"RequestId: {requestId} - Method name: GetAllAsync - No Items Found");
                }

                return templateistViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetAllAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetAllAsync Service Exception: {ex}");
            }
        }

        public Task<TemplateViewModel> GetItemByIdAsync(string id, string requestId = "")
        {
            throw new NotImplementedException();
        }

        public async Task<StatusCodes> UpdateItemAsync(TemplateViewModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - Template_UpdateAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.TemplateName, nameof(modelObject.TemplateName), requestId);
            try
            {
                var entityObject = _templateHelpers.MapToEntity(modelObject, requestId);

                var result = await _templateRepository.UpdateItemAsync(entityObject.Result, requestId);

                Guard.Against.NotStatus201Created(result, "Template_CreateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Template_CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - Template_CreateItemAsync Service Exception: {ex}");
            }
        }
    }
}
