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
using ApplicationCore.Authorization;

namespace Infrastructure.Services
{
    public class RoleMappingService : BaseService<RoleMappingService>, IRoleMappingService
    {
        private readonly IRoleMappingRepository _roleMappingRepository;

        public RoleMappingService(
            ILogger<RoleMappingService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IRoleMappingRepository roleMappingRepository) : base(logger, appOptions)
        {
            Guard.Against.Null(roleMappingRepository, nameof(roleMappingRepository));
            _roleMappingRepository = roleMappingRepository;
        }


        public async Task<StatusCodes> CreateItemAsync(RoleMappingModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RoleMapping_CreateItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.AdGroupName, nameof(modelObject.AdGroupName), requestId);
            try
            {
                var entityObject = MapToEntity(modelObject, requestId);

                var result = await _roleMappingRepository.CreateItemAsync(entityObject, requestId);

                Guard.Against.NotStatus201Created(result, "RoleMapping_CreateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RoleMapping_CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - RoleMapping_CreateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> UpdateItemAsync(RoleMappingModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RoleMapping_UpdateItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);

            try
            {
                var entityObject = MapToEntity(modelObject, requestId);

                var result = await _roleMappingRepository.UpdateItemAsync(entityObject, requestId);

                Guard.Against.NotStatus200OK(result, "RoleMapping_UpdateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RoleMapping_UpdateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - RoleMapping_UpdateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RoleMapping_DeleteItemAsync called.");
            Guard.Against.NullOrEmpty(id, nameof(id), requestId);

            try
            {
                var result = await _roleMappingRepository.DeleteItemAsync(id, requestId);

                Guard.Against.NotStatus204NoContent(result, $"RoleMapping_DeleteItemAsync failed for id: {id}", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RoleMapping_DeleteItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - RoleMapping_DeleteItemAsync Service Exception: {ex}");
            }
        }

        public async Task<IList<RoleMappingModel>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RoleMappingSvc_GetAllAsync called.");

            try
            {
                var listItems = (await _roleMappingRepository.GetAllAsync(requestId)).ToList();
                Guard.Against.Null(listItems, nameof(listItems), requestId);

                var modelListItems = new List<RoleMappingModel>();
                foreach (var item in listItems)
                {
                    modelListItems.Add(MapToModel(item));
                }

                if (modelListItems.Count == 0)
                {
                    _logger.LogWarning($"RequestId: {requestId} - RoleMappingSvc_GetAllAsync no items found");
                    throw new NoItemsFound($"RequestId: {requestId} - Method name: RoleMappingSvc_GetAllAsync - No Items Found");
                }

                return modelListItems;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RoleMappingSvc_GetAllAsync error: " + ex);
                throw;
            }
        }

        private RoleMappingModel MapToModel(RoleMapping entity, string requestId = "")
        {
            // Perform mapping
            var model = RoleMappingModel.Empty;

            model.Id = entity.Id ?? String.Empty;
            model.AdGroupName = entity.AdGroupName ?? String.Empty;
            model.Role = new RoleModel
            {
                Id = entity.Role.Id,
                DisplayName = entity.Role.DisplayName
            };
            model.Permissions = new List<PermissionModel>();
            foreach(var item in entity.Permissions)
            {
                var permissionmodel = new PermissionModel()
                {
                    Id = item.Id,
                    Name = item.Name
                };
                model.Permissions.Add(permissionmodel);
            }

            return model;
        }

        private RoleMapping MapToEntity(RoleMappingModel model, string requestId = "")
        {
            // Perform mapping
            var entity = RoleMapping.Empty;

            entity.Id = model.Id ?? String.Empty;
            entity.AdGroupName = model.AdGroupName ?? String.Empty;
            entity.Role = new Role
            {
                Id = model.Role.Id,
                DisplayName = model.Role.DisplayName
            };
            entity.Permissions = new List<Permission>();
            foreach (var item in model.Permissions)
            {
                var permission = new Permission()
                {
                    Id = item.Id,
                    Name = item.Name
                };
                entity.Permissions.Add(permission);
            }

            return entity;
        }
    }
}
