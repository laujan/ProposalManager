// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using ApplicationCore.Entities;
using ApplicationCore.Entities.GraphServices;
using ApplicationCore.Helpers;
using ApplicationCore.Interfaces;
using ApplicationCore.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class MetaDataRepository : BaseRepository<MetaData>, IMetaDataRepository
    {
        private readonly GraphSharePointAppService _graphSharePointAppService;
        private Lazy<IAuthorizationService> _authorizationService;
        private SiteList siteList;

        public MetaDataRepository(
            ILogger<MetaDataRepository> logger,
            IOptionsMonitor<AppOptions> appOptions,
            GraphSharePointAppService graphSharePointAppService,
            IServiceProvider services) : base(logger, appOptions)
        {
            Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));
            _graphSharePointAppService = graphSharePointAppService;
            _authorizationService = new Lazy<IAuthorizationService>(() =>
            services.GetRequiredService<IAuthorizationService>());

            siteList = new SiteList
             {
                 SiteId = _appOptions.ProposalManagementRootSiteId,
                 ListId = _appOptions.OpportunityMetaDataId
             };
        }


        public async Task<StatusCodes> CreateItemAsync(MetaData entity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - MetaDataRepo_CreateItemAsync called.");

            try
            {
                // Create Json object for SharePoint create list item
                dynamic itemFieldsJson = new JObject();
                itemFieldsJson.FieldName = entity.DisplayName;
                itemFieldsJson.FieldType = entity.FieldType.Name.ToString();
                if(entity.FieldType.Name==FieldType.DropDown.Name)
                    itemFieldsJson.FieldValue = JsonConvert.SerializeObject(entity.Values, Formatting.Indented);
                else
                    itemFieldsJson.FieldValue = entity.Values;
                itemFieldsJson.FieldScreen = entity.Screen;

                dynamic itemJson = new JObject();
                itemJson.fields = itemFieldsJson;

                await _graphSharePointAppService.CreateListItemAsync(siteList, itemJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - MetaDataRepo_CreateItemAsync finished creating SharePoint list item.");

                return StatusCodes.Status201Created;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - MetaDataRepo_CreateItemAsync error: {ex}");
                throw;
            }
        }

        public async Task<StatusCodes> UpdateItemAsync(MetaData entity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - MetaDataRepo_UpdateItemAsync called.");

            try
            {
                // Create Json object for SharePoint create list item
                dynamic itemFieldsJson = new JObject();
                itemFieldsJson.FieldName = entity.DisplayName;
                itemFieldsJson.FieldType = entity.FieldType.Name.ToString();
                if (entity.FieldType.Name == FieldType.DropDown.Name)
                    itemFieldsJson.FieldValue = JsonConvert.SerializeObject(entity.Values, Formatting.Indented);
                else
                    itemFieldsJson.FieldValue = entity.Values;
                itemFieldsJson.FieldScreen = entity.Screen;

                await _graphSharePointAppService.UpdateListItemAsync(siteList, entity.Id, itemFieldsJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - MetaDataRepo_UpdateItemAsync finished creating SharePoint list item.");

                return StatusCodes.Status200OK;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - MetaDataRepo_UpdateItemAsync error: {ex}");
                throw;
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - MetaDataRepo_DeleteItemAsync called.");

            try
            {
                Guard.Against.NullOrEmpty(id, "MetaDataRepo_DeleteItemAsync id null or empty", requestId);

                await _graphSharePointAppService.DeleteListItemAsync(siteList, id, requestId);

                _logger.LogInformation($"RequestId: {requestId} - MetaDataRepo_DeleteItemAsync finished creating SharePoint list item.");

                return StatusCodes.Status204NoContent;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - MetaDataRepo_DeleteItemAsync error: {ex}");
                throw;
            }
        }

        public async Task<IList<MetaData>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - MetaDataRepo_GetAllAsync called.");

            try
            {
                //check access
                //await _authorizationService.Value.CheckAdminAccsessAsync(requestId);
                var metaDataList = new List<MetaData>();
                var json = await _graphSharePointAppService.GetListItemsAsync(siteList, "all", requestId);
                var jsonArray = json["value"] as JArray;

                if(jsonArray == null)
                {
                    return metaDataList;
                }

                foreach (var item in jsonArray)
                {
                    var metaData = MetaData.Empty;
                    metaData.Id = item.SelectToken("fields.id")?.ToObject<string>();
                    if (!String.IsNullOrEmpty(metaData.Id))
                    {
                        metaData.FieldType = FieldType.FromName(item["fields"]["FieldType"].ToString());
                        metaData.DisplayName = item.SelectToken("fields.FieldName")?.ToObject<string>();
                        metaData.Screen = item.SelectToken("fields.FieldScreen")?.ToObject<string>();

                        try
                        {
                            if (metaData.FieldType == FieldType.DropDown)
                            {
                                JArray jsonAr = JArray.Parse(item["fields"]["FieldValue"].ToString());
                                
                                metaData.Values = new List<DropDownMetaDataValue>();
                                foreach (var property in jsonAr)
                                {
                                    metaData.Values.Add(property.ToObject<DropDownMetaDataValue>());
                                }
                            }
                            else
                            {
                                metaData.Values = item["fields"]["FieldValue"].ToString() ?? String.Empty;
                            }
                        }
                        catch
                        {
                            metaData.Values =  String.Empty;
                        }
                    }
                    metaDataList.Add(metaData);
                }

                return metaDataList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - MetaDataRepo_GetAllAsync error: {ex}");
                throw;
            }
        }
    }
}
