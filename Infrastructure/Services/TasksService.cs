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
using ApplicationCore.Models;
using ApplicationCore.Entities;
using ApplicationCore.Helpers.Exceptions;

namespace Infrastructure.Services
{
    public class TasksService : BaseService<TasksService>, ITasksService
    {
        private readonly ITasksRepository _tasksRepository;

        public TasksService(
            ILogger<TasksService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            ITasksRepository tasksRepository) : base(logger, appOptions)
        {
            Guard.Against.Null(tasksRepository, nameof(tasksRepository));
            _tasksRepository = tasksRepository;
        }

        public async Task<StatusCodes> CreateItemAsync(TasksModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - Tasks_CreateItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.Name, nameof(modelObject.Name), requestId);
            try
            {
                var entityObject = MapToEntity(modelObject, requestId);

                var result = await _tasksRepository.CreateItemAsync(entityObject, requestId);

                Guard.Against.NotStatus201Created(result, "Tasks_CreateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Tasks_CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - Tasks_CreateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> UpdateItemAsync(TasksModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - Tasks_UpdateItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.Id, nameof(modelObject.Id), requestId);

            try
            {
                var entityObject = MapToEntity(modelObject, requestId);

                var result = await _tasksRepository.UpdateItemAsync(entityObject, requestId);

                Guard.Against.NotStatus200OK(result, "Tasks_UpdateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Tasks_UpdateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - Tasks_UpdateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - Tasks_DeleteItemAsync called.");
            Guard.Against.NullOrEmpty(id, nameof(id), requestId);

            try
            {
                var result = await _tasksRepository.DeleteItemAsync(id, requestId);

                Guard.Against.NotStatus204NoContent(result, $"Tasks_DeleteItemAsync failed for id: {id}", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Tasks_DeleteItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - Tasks_DeleteItemAsync Service Exception: {ex}");
            }
        }

        public async Task<IList<TasksModel>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GetAllAsync called.");

            try
            {
                var listItems = (await _tasksRepository.GetAllAsync(requestId)).ToList();
                Guard.Against.Null(listItems, nameof(listItems), requestId);

                var modelListItems = new List<TasksModel>();
                foreach (var item in listItems)
                {
                    modelListItems.Add(MapToModel(item));
                }

                return modelListItems;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetAllAsync error: " + ex);
                throw;
            }
        }

        private TasksModel MapToModel(Tasks entity, string requestId = "")
        {
            // Perform mapping
            var model = new TasksModel();

            model.Id = entity.Id ?? String.Empty;
            model.Name = entity.Name ?? String.Empty;

            return model;
        }

        private Tasks MapToEntity(TasksModel model, string requestId = "")
        {
            // Perform mapping
            var entity = Tasks.Empty;

            entity.Id = model.Id ?? String.Empty;
            entity.Name = model.Name ?? String.Empty;

            return entity;
        }
    }
}
