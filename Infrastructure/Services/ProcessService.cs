// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using ApplicationCore.Entities;
using ApplicationCore.Helpers;
using ApplicationCore.Helpers.Exceptions;
using ApplicationCore.Interfaces;
using ApplicationCore.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace Infrastructure.Services
{
    public class ProcessService : BaseService<ProcessService>, IProcessService
    {
        private readonly IProcessRepository processRepository;
        private readonly IPermissionRepository permissionRepository;
        private readonly IRoleRepository roleRepository;
        public ProcessService(
        ILogger<ProcessService> logger,
        IOptionsMonitor<AppOptions> appOptions,
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IProcessRepository processRepository) : base(logger, appOptions)
        {
            Guard.Against.Null(processRepository, nameof(processRepository));
            this.processRepository = processRepository;
            this.permissionRepository = permissionRepository;
            this.roleRepository = roleRepository;
        }

        public async Task<JObject> CreateItemAsync(ProcessTypeViewModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - Process_CreateItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.ProcessType, nameof(modelObject.ProcessType), requestId);
            try
            {
                var entityObject = MapToProcessEntity(modelObject, requestId);

                var result = await processRepository.CreateItemAsync(entityObject, requestId);

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
                    await permissionRepository.CreateItemAsync(permissionReadObj, requestId);
                    await permissionRepository.CreateItemAsync(permissionReadWriteObj, requestId);

                    permissionRepository.CleanCache();
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
                    await roleRepository.CreateItemAsync(roleObj, requestId);
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

        public async Task<StatusCodes> DeleteItemAsync(ProcessTypeViewModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DeleteItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.Id, nameof(modelObject.Id), requestId);

            var entityObject = MapToProcessEntity(modelObject, requestId);

            // Remove associated permissions
            try
            {
                var channelName = modelObject.Channel.Replace(" ", "").ToString();

                var permissionReadObj = $"{channelName}_Read";

                var permissionReadWriteObj = $"{channelName}_ReadWrite";

                var permissions = await permissionRepository.GetAllAsync();

                var permissionReadId = permissions.FirstOrDefault(x => x.Name.Equals(permissionReadObj, StringComparison.OrdinalIgnoreCase))?.Id;
                var permissionReadWriteId = permissions.FirstOrDefault(x => x.Name.Equals(permissionReadWriteObj, StringComparison.OrdinalIgnoreCase))?.Id;

                await permissionRepository.DeleteItemAsync(permissionReadId, requestId);
                await permissionRepository.DeleteItemAsync(permissionReadWriteId, requestId);

                permissionRepository.CleanCache();
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Process_DeleteItemAsync Service Exception, error while deleting associated permissions: {ex}");
                throw new ResponseException($"RequestId: {requestId} - DeleteItemAsync Service Exception: {ex}");
            }

            try {
                var result = await processRepository.DeleteItemAsync(modelObject.Id, requestId);

                Guard.Against.NotStatus204NoContent(result, "DeleteItemAsync", requestId);

                return result;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Process_DeleteItemAsync Service Exception, error while deleting process: {ex}");
                throw new ResponseException($"RequestId: {requestId} - DeleteItemAsync Service Exception: {ex}");
            }
        }           

        public async Task<ProcessTypeListViewModel> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GetAllAsync called.");

            try
            {

                var processTypeListViewModel = new ProcessTypeListViewModel();
                processTypeListViewModel.ItemsList = (await processRepository.GetAllAsync(requestId)).Select(item => MapToProcessViewModel(item)).ToList();

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

        public async Task<JObject> UpdateItemAsync(ProcessTypeViewModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - Process_UpdateAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.ProcessStep, nameof(modelObject.ProcessStep), requestId);
            try
            {
                try
                {
                    var processes = await processRepository.GetAllAsync();

                    var oldProcess = processes.FirstOrDefault(x => x.Id == modelObject.Id);

                    var channelName = oldProcess.Channel.Replace(" ", "").ToString();

                    var permissionReadObj = $"{channelName}_Read";

                    var permissionReadWriteObj = $"{channelName}_ReadWrite";

                    var permissions = await permissionRepository.GetAllAsync();

                    var permissionReadId = permissions.FirstOrDefault(x => x.Name.Equals(permissionReadObj, StringComparison.OrdinalIgnoreCase))?.Id;
                    var permissionReadWriteId = permissions.FirstOrDefault(x => x.Name.Equals(permissionReadWriteObj, StringComparison.OrdinalIgnoreCase))?.Id;

                    await permissionRepository.DeleteItemAsync(permissionReadId, requestId);
                    await permissionRepository.DeleteItemAsync(permissionReadWriteId, requestId);

                    permissionRepository.CleanCache();
                }
                catch (ArgumentException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"RequestId: {requestId} - Process_DeleteItemAsync Service Exception, error while deleting associated permissions: {ex}");
                }

                await processRepository.DeleteItemAsync(modelObject.Id, requestId);

                return await CreateItemAsync(modelObject, requestId);
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
                return new ProcessTypeViewModel
                {
                    Id = entity.Id ?? string.Empty,
                    ProcessStep = entity.ProcessStep ?? string.Empty,
                    ProcessType = entity.ProcessType ?? string.Empty,
                    Channel = entity.Channel ?? string.Empty,
                    RoleId = entity.RoleId ?? String.Empty,
                    RoleName = entity.RoleName ?? String.Empty
                };

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
                return new ProcessesType
                {
                    Id = model.Id ?? string.Empty,
                    ProcessStep = model.ProcessStep ?? string.Empty,
                    ProcessType = model.ProcessType ?? string.Empty,
                    Channel = model.Channel ?? string.Empty,
                    RoleId = model.RoleId ?? String.Empty,
                    RoleName = model.RoleName ?? String.Empty
                };

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - ToEntity Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - ToEntity Service Exception: {ex}");
            }
        }
    }

}
