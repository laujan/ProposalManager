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
    public class ProcessService : BaseService<ProcessService>, IProcessService
    {
        private readonly IProcessRepository _processRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRoleRepository _roleRepository;
        public ProcessService(
        ILogger<ProcessService> logger,
        IOptionsMonitor<AppOptions> appOptions,
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IProcessRepository processRepository) : base(logger, appOptions)
        {
            Guard.Against.Null(processRepository, nameof(processRepository));
            _processRepository = processRepository;
            _permissionRepository = permissionRepository;
            _roleRepository = roleRepository;
        }

        public async Task<StatusCodes> CreateItemAsync(ProcessTypeViewModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - Process_CreateItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.ProcessType, nameof(modelObject.ProcessType), requestId);
            try
            {
                var entityObject = MapToProcessEntity(modelObject, requestId);

                var result = await _processRepository.CreateItemAsync(entityObject, requestId);

                Guard.Against.NotStatus201Created(result, "Process_CreateItemAsync", requestId);
                //Granular Access Start
                try
                {
                    var channelName = modelObject.Channel.Replace(" ", "").ToString();

                    var permissionReadObj = new Permission()
                    {
                        Id = string.Empty,
                        Name = $"{channelName}_Read"
                    };
                    var permissionReadWriteObj = new Permission()
                    {
                        Id = string.Empty,
                        Name = $"{channelName}_ReadWrite"
                    };
                    var read = await _permissionRepository.CreateItemAsync(permissionReadObj, requestId);
                    var readWrite = await _permissionRepository.CreateItemAsync(permissionReadWriteObj, requestId);
                }
                catch(Exception ex)
                {
                    _logger.LogError($"RequestId: {requestId} - Process_CreateItemAsync Service Exception, error while creating permissions: {ex}");
                }
                //Granular Access End


                //Adding new role
                try
                {
                    var roleObj = new Role()
                    {
                        Id = string.Empty,
                        DisplayName = modelObject.ProcessStep
                    };
                    var read = await _roleRepository.CreateItemAsync(roleObj, requestId);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"RequestId: {requestId} - Process_CreateItemAsync Service Exception, error while creating permissions: {ex}");
                }
                //Adding new role

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Process_CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - Process_CreateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DeleteItemAsync called.");
            Guard.Against.Null(id, nameof(id));

            var result = await _processRepository.DeleteItemAsync(id, requestId);

            Guard.Against.NotStatus204NoContent(result, "DeleteItemAsync", requestId);

            return result;
        }

        public async Task<ProcessTypeListViewModel> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GetAllAsync called.");

            try
            {
                var listItems = (await _processRepository.GetAllAsync(requestId)).ToList();
                Guard.Against.Null(listItems, nameof(listItems), requestId);

                var processTypeListViewModel = new ProcessTypeListViewModel();
                foreach (var item in listItems)
                {
                    processTypeListViewModel.ItemsList.Add(MapToProcessViewModel(item));
                }

                if (processTypeListViewModel.ItemsList.Count == 0)
                {
                    _logger.LogWarning($"RequestId: {requestId} - GetAllAsync no items found");
                    throw new NoItemsFound($"RequestId: {requestId} - Method name: GetAllAsync - No Items Found");
                }

                return processTypeListViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetAllAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetAllAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> UpdateItemAsync(ProcessTypeViewModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - Process_UpdateAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.ProcessStep, nameof(modelObject.ProcessStep), requestId);
            try
            {
                var entityObject = MapToProcessEntity(modelObject, requestId);

                var result = await _processRepository.UpdateItemAsync(entityObject, requestId);

                Guard.Against.NotStatus201Created(result, "Process_CreateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Process_CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - Process_CreateItemAsync Service Exception: {ex}");
            }
        }

        private ProcessTypeViewModel MapToProcessViewModel(ProcessesType entity, string requestId = "")
        {

            try
            {
                var model = new ProcessTypeViewModel();
                model.Id = entity.Id ?? string.Empty;
                model.ProcessStep = entity.ProcessStep ?? string.Empty;
                model.ProcessType = entity.ProcessType ?? string.Empty;
                model.Channel = entity.Channel ?? string.Empty;

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - ToViewModel Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - ToViewModel Service Exception: {ex}");
            }
        }
        private ProcessesType MapToProcessEntity(ProcessTypeViewModel model, string requestId = "")
        {

            try
            {
                var entity = new ProcessesType();
                entity.Id = model.Id ?? string.Empty;
                entity.ProcessStep = model.ProcessStep ?? string.Empty;
                entity.ProcessType = model.ProcessType ?? string.Empty;
                entity.Channel = model.Channel ?? string.Empty;

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - ToEntity Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - ToEntity Service Exception: {ex}");
            }
        }
    }

}
