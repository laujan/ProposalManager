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
    public class RoleService : BaseService<RoleService>, IRoleService
    {
        private readonly IRoleRepository _rolesRepository;

        public RoleService(
            ILogger<RoleService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IRoleRepository rolesRepository) : base(logger, appOptions)
        {
            Guard.Against.Null(rolesRepository, nameof(rolesRepository));
            _rolesRepository = rolesRepository;
        }
        public async Task<StatusCodes> CreateItemAsync(RoleModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RolesSvc_CreateItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.DisplayName, nameof(modelObject.DisplayName), requestId);
            try
            {
                var entityObject = MapToEntity(modelObject, requestId);

                var result = await _rolesRepository.CreateItemAsync(entityObject, requestId);

                Guard.Against.NotStatus201Created(result, "RolesSvc_CreateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RolesSvc_CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - RolesSvc_CreateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - Roles_DeleteItemAsync called.");
            Guard.Against.NullOrEmpty(id, nameof(id), requestId);

            try
            {
                var result = await _rolesRepository.DeleteItemAsync(id, requestId);

                Guard.Against.NotStatus204NoContent(result, $"Roles_DeleteItemAsync failed for id: {id}", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - Roles_DeleteItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - Roles_DeleteItemAsync Service Exception: {ex}");
            }
        }

        public async Task<IList<RoleModel>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RolesSvc_GetAllAsync called.");

            try
            {
                var modelListItems = (await _rolesRepository.GetAllAsync(requestId)).Select(item => MapToModel(item)).ToList();

                if (modelListItems.Count == 0)
                {
                    _logger.LogWarning($"RequestId: {requestId} - RolesSvc_GetAllAsync no items found");
                    throw new NoItemsFound($"RequestId: {requestId} - Method name: RolesSvc_GetAllAsync - No Items Found");
                }

                return modelListItems;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RolesSvc_GetAllAsync error: " + ex);
                throw;
            }
        }

        public async Task<StatusCodes> UpdateItemAsync(RoleModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RolesSvc_UpdateItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.Id, nameof(modelObject.Id), requestId);

            try
            {
                var entityObject = MapToEntity(modelObject, requestId);

                var result = await _rolesRepository.UpdateItemAsync(entityObject, requestId);

                Guard.Against.NotStatus200OK(result, "CategorySvc_UpdateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RolesSvc_UpdateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - RolesSvc_UpdateItemAsync Service Exception: {ex}");
            }
        }

        private RoleModel MapToModel(Role entity, string requestId = "")
        {

            return new RoleModel
            {
                Id = entity.Id ?? String.Empty,
                AdGroupName = entity.AdGroupName ?? String.Empty,
                DisplayName = entity.DisplayName ?? String.Empty,
                TeamsMembership = entity.TeamsMembership ?? TeamsMembership.None,
                UserPermissions = entity.Permissions.Select(permission => new PermissionModel
                {
                    Id = permission.Id,
                    Name = permission.Name
                }).ToList()
            };

        }

        private Role MapToEntity(RoleModel entity, string requestId = "")
        {
            return new Role
            {
                Id = entity.Id ?? String.Empty,
                AdGroupName = entity.AdGroupName ?? String.Empty,
                DisplayName = entity.DisplayName ?? String.Empty,
                TeamsMembership = entity.TeamsMembership ?? TeamsMembership.None,
                Permissions = entity.UserPermissions.Select(permission => new Permission
                {
                    Id = permission.Id,
                    Name = permission.Name
                }).ToList()
            };
        }
    }
}

