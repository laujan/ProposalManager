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
using ApplicationCore.Services;
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
                var listItems = (await _rolesRepository.GetAllAsync(requestId)).ToList();
                Guard.Against.Null(listItems, nameof(listItems), requestId);

                var modelListItems = new List<RoleModel>();
                foreach (var item in listItems)
                {
                    modelListItems.Add(MapToModel(item));
                }

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
            // Perform mapping
            var model = new RoleModel();

            model.Id = entity.Id ?? String.Empty;
            model.DisplayName = entity.DisplayName ?? String.Empty;

            return model;
        }

        private Role MapToEntity(RoleModel model, string requestId = "")
        {
            // Perform mapping
            var entity = Role.Empty;

            entity.Id = model.Id ?? String.Empty;
            entity.DisplayName = model.DisplayName ?? String.Empty;

            return entity;
        }
    }
}

