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
using ApplicationCore.Interfaces;
using ApplicationCore;
using ApplicationCore.Helpers;
using ApplicationCore.Models;
using ApplicationCore.Entities;
using ApplicationCore.Helpers.Exceptions;


namespace Infrastructure.Services
{
    public class PermissionService : BaseService<PermissionService>, IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;

        public PermissionService(
            ILogger<PermissionService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IPermissionRepository permissionRepository) : base(logger, appOptions)
        {
            Guard.Against.Null(permissionRepository, nameof(permissionRepository));
            _permissionRepository = permissionRepository;
        }
        public async Task<StatusCodes> CreateItemAsync(PermissionModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - PermissionSVC_CreateItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.Name, nameof(modelObject.Name), requestId);
            try
            {
                var entityObject = MapToEntity(modelObject, requestId);

                var result = await _permissionRepository.CreateItemAsync(entityObject, requestId);

                Guard.Against.NotStatus201Created(result, "PermissionSvc_CreateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - PermissionSvc_CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - PermissionSvc_CreateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - PermissionSvc_DeleteItemAsync called.");
            Guard.Against.NullOrEmpty(id, nameof(id), requestId);

            try
            {
                var result = await _permissionRepository.DeleteItemAsync(id, requestId);

                Guard.Against.NotStatus204NoContent(result, $"PermissionSvc_DeleteItemAsync failed for id: {id}", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - PermissionSvc_DeleteItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - PermissionSvc_DeleteItemAsync Service Exception: {ex}");
            }
        }

        public async Task<IList<PermissionModel>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - PermissionSvc_GetAllAsync called.");

            try
            {
                var modelListItems = (await _permissionRepository.GetAllAsync(requestId)).Select(item => MapToModel(item)).ToList();

                if (modelListItems.Count == 0)
                {
                    _logger.LogWarning($"RequestId: {requestId} - PermissionSvc_GetAllAsync no items found");
                    throw new NoItemsFound($"RequestId: {requestId} - Method name: PermissionSvc_GetAllAsync - No Items Found");
                }

                return modelListItems;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - PermissionSvc_GetAllAsync error: " + ex);
                throw;
            }
        }

        private PermissionModel MapToModel(Permission entity, string requestId = "")
        {
            // Perform mapping
            return new PermissionModel
            {
                Id = entity.Id ?? String.Empty,
                Name = entity.Name ?? String.Empty
            };

        }

        private Permission MapToEntity(PermissionModel entity, string requestId = "")
        {
            // Perform mapping
            return new Permission
            {
                Id = entity.Id ?? String.Empty,
                Name = entity.Name ?? String.Empty
            };
        }
    }
}
