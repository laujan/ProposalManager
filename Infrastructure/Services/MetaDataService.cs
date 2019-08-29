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
using Newtonsoft.Json.Linq;

namespace Infrastructure.Services
{
    public class MetaDataService : BaseService<MetaDataService>, IMetaDataService
    {
        private readonly IMetaDataRepository _metaDataRepository;

        public MetaDataService(
            ILogger<MetaDataService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IMetaDataRepository metaDataRepository) : base(logger, appOptions)
        {
            Guard.Against.Null(metaDataRepository, nameof(metaDataRepository));
            _metaDataRepository = metaDataRepository;
        }

        public async Task<StatusCodes> CreateItemAsync(MetaDataModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - MetaData_CreateItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            try
            {
                var entityObject = MapToEntity(modelObject, requestId);

                var result = await _metaDataRepository.CreateItemAsync(entityObject, requestId);

                Guard.Against.NotStatus201Created(result, "Metadata_CreateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - MetaData_CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - MetaData_CreateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> UpdateItemAsync(MetaDataModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - MetaData_UpdateItemAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.Id, nameof(modelObject.Id), requestId);

            try
            {
                var entityObject = MapToEntity(modelObject, requestId);

                var result = await _metaDataRepository.UpdateItemAsync(entityObject, requestId);

                Guard.Against.NotStatus200OK(result, "MetaData_UpdateItemAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - MetaData_UpdateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - MetaDatapdateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - MetaData_DeleteItemAsync called.");
            Guard.Against.NullOrEmpty(id, nameof(id), requestId);

            try
            {
                var result = await _metaDataRepository.DeleteItemAsync(id, requestId);

                Guard.Against.NotStatus204NoContent(result, $"MetaData_DeleteItemAsync failed for id: {id}", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - MetaData_DeleteItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - MetaData_DeleteItemAsync Service Exception: {ex}");
            }
        }

        public async Task<IList<MetaDataModel>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - MetaDataSvc_GetAllAsync called.");

            try
            {
                var listItems = (await _metaDataRepository.GetAllAsync(requestId)).Select(x => MapToModel(x)).ToList();

                Guard.Against.Null(listItems, nameof(listItems), requestId);

                return listItems;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - MetaDataSvc_GetAllAsync error: " + ex);
                throw;
            }
        }

        private MetaDataModel MapToModel(MetaData entity, string requestId = "")
        {
            // Perform mapping
            var model = new MetaDataModel();

            model.Id = entity.Id ?? String.Empty;
            model.DisplayName = entity.DisplayName ?? String.Empty;
            model.Screen = entity.Screen ?? String.Empty;
            model.FieldType = entity.FieldType ?? FieldType.None;
            model.Required = entity.Required;
            model.UniqueId = entity.UniqueId;
            if (model.FieldType == FieldType.DropDown)
            {
                model.Values = new List<DropDownMetaDataValue>();
                foreach(DropDownMetaDataValue metaValue in entity.Values)
                {
                    model.Values.Add(metaValue);
                }
            }
            else
            {
                model.Values = entity.Values ?? String.Empty;
            }
            return model;
        }

        private MetaData MapToEntity(MetaDataModel model, string requestId = "")
        {
            // Perform mapping
            var entity = MetaData.Empty;

            entity.Id = model.Id ?? String.Empty;
            entity.DisplayName = model.DisplayName ?? String.Empty;
            entity.Screen = model.Screen ?? String.Empty;
            entity.FieldType = model.FieldType ?? FieldType.None;
            entity.Required = model.Required;
            entity.UniqueId = model.UniqueId;
            if (entity.FieldType.Name == FieldType.DropDown.Name)
            {
                JArray jsonArray = JArray.Parse(model.Values.ToString());
                entity.Values = new List<DropDownMetaDataValue>();
                foreach (var metaValue in jsonArray)
                {
                    if(!string.IsNullOrEmpty(metaValue["name"].ToString()))
                        entity.Values.Add(new DropDownMetaDataValue { Id= metaValue["id"].ToString(), Name= metaValue["name"].ToString() });
                }
            }
            else
            {
                entity.Values = model.Values ?? String.Empty;
            }
            return entity;
        }
    }
}
