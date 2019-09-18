﻿// Copyright(c) Microsoft Corporation. 
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
using Newtonsoft.Json.Linq;

namespace Infrastructure.Services
{
    public class RoleService : BaseService<RoleService>, IRoleService
    {
        private readonly IRoleRepository _rolesRepository;
        private readonly IUserProfileRepository userProfileRepository;

        public RoleService(
            ILogger<RoleService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IRoleRepository rolesRepository,
            IUserProfileRepository userProfileRepository) : base(logger, appOptions)
        {
            Guard.Against.Null(rolesRepository, nameof(rolesRepository));
            _rolesRepository = rolesRepository;
            this.userProfileRepository = userProfileRepository;
        }
        public async Task<JObject> CreateItemAsync(RoleModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - RolesSvc_CreateItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.DisplayName, nameof(modelObject.DisplayName), requestId);
            try
            {
                var entityObject = MapToEntity(modelObject);

                var result = await _rolesRepository.CreateItemAsync(entityObject, requestId);

                _logger.LogInformation($"Permission created: {result}");

                userProfileRepository.CleanCache();
                _logger.LogInformation("Cleaned user profile cache");

                _rolesRepository.CleanCache();
                _logger.LogInformation("Cleaned role cache");

                _logger.LogInformation($"Permission created: {modelObject}");

                userProfileRepository.CleanCache();
                _logger.LogInformation("Cleaned user profile cache");

                _rolesRepository.CleanCache();
                _logger.LogInformation("Cleaned role cache");

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

                _logger.LogInformation($"Permission deleted: {id}");

                userProfileRepository.CleanCache();
                _logger.LogInformation("Cleaned user profile cache");

                _rolesRepository.CleanCache();
                _logger.LogInformation("Cleaned role cache");

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
                var entityObject = MapToEntity(modelObject);

                var result = await _rolesRepository.UpdateItemAsync(entityObject, requestId);

                Guard.Against.NotStatus200OK(result, "CategorySvc_UpdateItemAsync", requestId);

                _logger.LogInformation($"Permission updated: {modelObject}");

                userProfileRepository.CleanCache();
                _logger.LogInformation("Cleaned user profile cache");

                _rolesRepository.CleanCache();
                _logger.LogInformation("Cleaned role cache");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RolesSvc_UpdateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - RolesSvc_UpdateItemAsync Service Exception: {ex}");
            }
        }

        private RoleModel MapToModel(Role entity)
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

        private Role MapToEntity(RoleModel entity)
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

