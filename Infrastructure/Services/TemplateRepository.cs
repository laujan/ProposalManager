// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Interfaces;
using ApplicationCore.Entities;
using ApplicationCore.Services;
using ApplicationCore;
using ApplicationCore.Helpers;
using ApplicationCore.Entities.GraphServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ApplicationCore.Helpers.Exceptions;

namespace Infrastructure.Services
{
    public class TemplateRepository : BaseRepository<Template>, ITemplateRepository
    {
        private readonly GraphSharePointAppService _graphSharePointAppService;

        public TemplateRepository(
            ILogger<TemplateRepository> logger,
            IOptionsMonitor<AppOptions> appOptions,
            GraphSharePointAppService graphSharePointAppService) : base(logger, appOptions)
        {
            Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));
            _graphSharePointAppService = graphSharePointAppService;
        }

        public async Task<StatusCodes> CreateItemAsync(Template template, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - TemplateRepository_CreateItemAsync called.");

            try
            {
                Guard.Against.Null(template, nameof(template), requestId);
                Guard.Against.NullOrEmpty(template.TemplateName, nameof(template.TemplateName), requestId);

                // TODO Check access

                // Ensure id is blank since it will be set by SharePoint
                template.Id = String.Empty;

                _logger.LogInformation($"RequestId: {requestId} - TemplateRepository_CreateItemAsync creating SharePoint List for template.");
                // Create Json object for SharePoint create list item
                dynamic templateFieldsJson = new JObject();
                templateFieldsJson.TemplateName = template.TemplateName;
                templateFieldsJson.Description = template.Description;
                templateFieldsJson.CreatedBy = JsonConvert.SerializeObject(template.CreatedBy, Formatting.Indented);
                //set todays date as the last used date
                templateFieldsJson.LastUsed = DateTimeOffset.Now.Date;
                templateFieldsJson.ProcessList = JsonConvert.SerializeObject(template.ProcessList, Formatting.Indented);
                templateFieldsJson.DefaultTemplate = template.DefaultTemplate.ToString();

                dynamic templateJson = new JObject();
                templateJson.fields = templateFieldsJson;

                var templateSiteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.TemplateListId
                };

                await _graphSharePointAppService.CreateListItemAsync(templateSiteList, templateJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - TemplateRepository_CreateItemAsync finished creating SharePoint List for template.");
                // END TODO

                return StatusCodes.Status201Created;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TemplateRepository_CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - TemplateRepository_CreateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - TemplateRepository_DeleteItemAsync called.");

            try
            {
                Guard.Against.Null(id, nameof(id), requestId);

                var templateSiteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.TemplateListId
                };

                var json = await _graphSharePointAppService.DeleteListItemAsync(templateSiteList, id, requestId);
                Guard.Against.Null(json, nameof(json), requestId);

                return StatusCodes.Status204NoContent;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TemplateRepository_DeleteItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - TemplateRepository_DeleteItemAsync Service Exception: {ex}");
            }
        }

        public async Task<IList<Template>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - TemplateRepository_GetAllAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.TemplateListId
                };

                dynamic jsonDyn = await _graphSharePointAppService.GetListItemsAsync(siteList, "all", requestId); 
                var itemsList = new List<Template>();

                if (jsonDyn.value.HasValues)
                {
                    foreach (var item in jsonDyn.value)
                    {
                        var obj = JObject.Parse(item.ToString()).SelectToken("fields");
                        itemsList.Add(new Template
                        {
                            Id = obj.SelectToken("id")?.ToString(),
                            TemplateName = obj.SelectToken("TemplateName")?.ToString(),
                            Description = obj.SelectToken("Description")?.ToString(),
                            LastUsed = Convert.ToDateTime(obj.SelectToken("LastUsed")?.ToString()),
                            DefaultTemplate = obj.SelectToken("DefaultTemplate") != null ? obj.SelectToken("DefaultTemplate").ToString() == "True" : false,
                            CreatedBy = obj.SelectToken("CreatedBy") != null ? JsonConvert.DeserializeObject<UserProfile>(obj.SelectToken("CreatedBy").ToString(), new JsonSerializerSettings
                            {
                                MissingMemberHandling = MissingMemberHandling.Ignore,
                                NullValueHandling = NullValueHandling.Ignore
                            }) : UserProfile.Empty,
                            ProcessList = obj.SelectToken("ProcessList") != null ? JsonConvert.DeserializeObject<IList<Process>>(obj.SelectToken("ProcessList").ToString(), new JsonSerializerSettings
                            {
                                MissingMemberHandling = MissingMemberHandling.Ignore,
                                NullValueHandling = NullValueHandling.Ignore
                            }) : new List<Process>()
                        });
                    }
                }

                return itemsList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TemplateRepo_GetAllAsync error: {ex}");
                throw;
            }
        }

        public Task<Template> GetItemByIdAsync(string id, string requestId = "")
        {
            throw new NotImplementedException();
        }

        public async Task<StatusCodes> UpdateItemAsync(Template template, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - TemplateRepository_UpdateItemAsync called.");

            try
            {
                await DeleteItemAsync(template.Id.ToString(), requestId);
                await CreateItemAsync(template, requestId);

                _logger.LogInformation($"RequestId: {requestId} - TemplateRepository_UpdateItemAsync finished updating SharePoint List for template.");

                return StatusCodes.Status201Created;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TemplateRepository_UpdateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - TemplateRepository_UpdateItemAsync Service Exception: {ex}");
            }
        }
    }
}
