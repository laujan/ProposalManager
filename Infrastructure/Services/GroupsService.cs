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
using ApplicationCore.Helpers;
using ApplicationCore.Helpers.Exceptions;
using ApplicationCore.Interfaces;
using ApplicationCore.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Infrastructure.Services
{
    public class GroupsService : BaseService<GroupsService>, IGroupsService
    {
        private readonly IGroupsRepository _groupsRepository;

        public GroupsService(
        ILogger<GroupsService> logger,
        IOptionsMonitor<AppOptions> appOptions,
        IGroupsRepository groupsRepository) : base(logger, appOptions)
        {
            Guard.Against.Null(groupsRepository, nameof(groupsRepository));
            _groupsRepository = groupsRepository;
        }

        public async Task<StatusCodes> CreateItemAsync(GroupModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - Groups_CreateItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.GroupName, nameof(modelObject.GroupName), requestId);
            try
            {
                var entityObject = MapToEntity(modelObject, requestId);

                var result = await _groupsRepository.CreateItemAsync(entityObject, requestId);

                Guard.Against.NotStatus201Created(result, "Groups_CreateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Groups_CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - Groups_CreateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - Groups_DeleteItemAsync called.");
            Guard.Against.NullOrEmpty(id, nameof(id), requestId);

            try
            {
                var result = await _groupsRepository.DeleteItemAsync(id, requestId);

                Guard.Against.NotStatus204NoContent(result, $"Groups_DeleteItemAsync failed for id: {id}", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Groups_DeleteItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - Groups_DeleteItemAsync Service Exception: {ex}");
            }
        }

        public async Task<IList<GroupModel>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GetAllAsync called.");

            try
            {
                var modelListItems = (await _groupsRepository.GetAllAsync(requestId)).Select(item => MapToModel(item)).ToList();

                if (modelListItems.Count == 0)
                {
                    _logger.LogWarning($"RequestId: {requestId} - GetAllAsync no items found");
                    throw new NoItemsFound($"RequestId: {requestId} - Method name: GetAllAsync - No Items Found");
                }

                return modelListItems;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetAllAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetAllAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> UpdateItemAsync(GroupModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - Groups_UpdateAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.GroupName, nameof(modelObject.GroupName), requestId);
            try
            {
                var entityObject = MapToEntity(modelObject, requestId);

                var result = await _groupsRepository.UpdateItemAsync(entityObject, requestId);

                Guard.Against.NotStatus201Created(result, "Groups_CreateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Groups_CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - Groups_CreateItemAsync Service Exception: {ex}");
            }
        }

        private GroupModel MapToModel(Groups entity, string requestId = "")
        {
            return new GroupModel
            {
                Id= entity.Id ?? String.Empty,
                GroupName = entity.GroupName ?? String.Empty,
                Processes = entity.Processes.Select(item=>new ProcessesType
                {
                    Id = item.Id,
                    ProcessStep = item.ProcessStep,
                    ProcessType = item.ProcessType,
                    Channel = item.Channel
                }).ToList()
            };
        }

        private Groups MapToEntity(GroupModel entity, string requestId = "")
        {
            // Perform mapping
            return new Groups
            {
                Id = entity.Id ?? String.Empty,
                GroupName = entity.GroupName ?? String.Empty,
                Processes = entity.Processes.Select(item => new ProcessesType
                {
                    Id = item.Id,
                    ProcessStep = item.ProcessStep,
                    ProcessType = item.ProcessType,
                    Channel = item.Channel
                }).ToList()
            };
        }
    }
}
