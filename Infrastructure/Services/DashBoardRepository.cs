using System;
using System.Collections.Generic;
using System.Text;
using ApplicationCore.Interfaces;
using ApplicationCore.Entities;
using ApplicationCore.Entities.GraphServices;
using ApplicationCore.Services;
using ApplicationCore;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ApplicationCore.Helpers.Exceptions;
using System.Net;

namespace Infrastructure.Services
{
    public class DashBoardRepository : BaseRepository<Dashboard>, IDashboardRepository
    {
        private readonly GraphSharePointAppService _graphSharePointAppService;

        public DashBoardRepository(
            ILogger<DashBoardRepository> logger,
            IOptionsMonitor<AppOptions> appOptions,
            GraphSharePointAppService graphSharePointAppService) : base(logger, appOptions)
        {
            Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));
            _graphSharePointAppService = graphSharePointAppService;

        }

        public async Task<StatusCodes> CreateOpportunityAsync(Dashboard entity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DashboradRepository_CreateItemAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.DashboardListId
                };

                // Create Json object for SharePoint create list item
                dynamic itemFieldsJson = new JObject();
                dynamic itemJson = new JObject();

                itemFieldsJson.Title = entity.Id;
                itemFieldsJson.CustomerName = entity.CustomerName;
                itemFieldsJson.Status = entity.Status;
                itemFieldsJson.StartDate = entity.StartDate;
                itemFieldsJson.OpportunityName = entity.OpportunityName;

                itemFieldsJson.TotalNoOfDays = entity.TotalNoOfDays;

                itemFieldsJson.ProcessNoOfDays = JsonConvert.SerializeObject(entity.ProcessList, Formatting.Indented);

                itemFieldsJson.ProcessEndDates = JsonConvert.SerializeObject(entity.ProcessEndDateList, Formatting.Indented);

                itemFieldsJson.ProcessLoanOfficers = JsonConvert.SerializeObject(entity.ProcessLoanOfficerNames, Formatting.Indented);

                itemJson.fields = itemFieldsJson;

                var result = await _graphSharePointAppService.CreateListItemAsync(siteList, itemJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - DashboradRepository_CreateItemAsync finished creating SharePoint list item.");

                return StatusCodes.Status200OK;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - DashboradRepository_CreateItemAsync error: {ex}");
                throw new ResponseException($"RequestId: {requestId} - DashboradRepository_CreateItemAsync Service Exception: {ex}");
            }

        }

        public async Task<StatusCodes> DeleteOpportunityAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DashboardRepository_DeleteOpportunityAsync called.");
            Guard.Against.Null(id, nameof(id), requestId);
            var sitelist = new SiteList
            {
                SiteId = _appOptions.ProposalManagementRootSiteId,
                ListId = _appOptions.DashboardListId
            };
            var json = await _graphSharePointAppService.DeleteListItemAsync(sitelist, id, requestId);
            return StatusCodes.Status204NoContent;
        }

        public async Task<IList<Dashboard>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DashboardRepository_GetAllAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.DashboardListId
                };

                var json = await _graphSharePointAppService.GetListItemsAsync(siteList, "all", requestId);
                var itemsList = new List<Dashboard>();
                JArray jsonArray = JArray.Parse(json["value"].ToString());

                foreach (var item in jsonArray)
                {
                    itemsList.Add(JsonConvert.DeserializeObject<Dashboard>(item["fields"].ToString(), new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    }));
                }

                return itemsList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - DashboardRepository_GetAllAsync error: {ex}");
                throw;
            }
        }

        public async Task<Dashboard> GetAsync(string Name, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DashboardRepository_GetAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.DashboardListId
                };
                var name = WebUtility.UrlEncode(Name);
                var options = new List<QueryParam>();
                options.Add(new QueryParam("filter", $"startswith(fields/OpportunityName,'{name}')"));

                var json = await _graphSharePointAppService.GetListItemsAsync(siteList, options, "all", requestId);

                var obj = JObject.Parse(json["value"][0].ToString()).SelectToken("fields");
                var dashboard = new Dashboard();

                dashboard.Id = obj.SelectToken("id")?.ToString();
                dashboard.OpportunityName = obj.SelectToken("OpportunityName")?.ToString();
                dashboard.CustomerName = obj.SelectToken("CustomerName")?.ToString();
                dashboard.OpportunityId = obj.SelectToken("OpportunityID")?.ToString();
                dashboard.Status = obj.SelectToken("Status")?.ToString();
                dashboard.StartDate = obj.SelectToken("StartDate").ToString();
                dashboard.TotalNoOfDays = obj.SelectToken("TotalNoOfDays") != null ? Int32.Parse(obj.SelectToken("TotalNoOfDays").ToString()) : 0;
                dashboard.TargetCompletionDate = obj.SelectToken("TargetCompletionDate")?.ToString();

                dashboard.ProcessList = obj.SelectToken("ProcessNoOfDays") != null ? JsonConvert.DeserializeObject<IList<DashboardProcessList>>(obj.SelectToken("ProcessNoOfDays").ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                }) : new List<DashboardProcessList>();

                dashboard.ProcessEndDateList = obj.SelectToken("ProcessEndDates") != null ? JsonConvert.DeserializeObject<IList<DashboradProcessEndDateList>>(obj.SelectToken("ProcessEndDates").ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                }) : new List<DashboradProcessEndDateList>();

                dashboard.ProcessLoanOfficerNames = obj.SelectToken("ProcessLoanOfficers") != null ? JsonConvert.DeserializeObject<IList<DashboardLoanOfficers>>(obj.SelectToken("ProcessLoanOfficers").ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                }) : new List<DashboardLoanOfficers>();


                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - DashboardRepository_GetAllAsync error: {ex}");
                throw;
            }
        }

        public async Task<StatusCodes> UpdateOpportunityAsync(Dashboard dashboard, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DashBoard_UpdateOpportunityAsync called.");
            Guard.Against.Null(dashboard, nameof(dashboard), requestId);
            Guard.Against.NullOrEmpty(dashboard.OpportunityId, nameof(dashboard.OpportunityId), requestId);

            try
            {
                _logger.LogInformation($"RequestId: {requestId} - DashBoard_UpdateOpportunityAsync SharePoint List for dashboard.");

                dynamic dashboardJson = new JObject();


                dashboardJson.Status = dashboard.Status;
                dashboardJson.StartDate = dashboard.StartDate;
                dashboardJson.OpportunityName = dashboard.OpportunityName;
                dashboardJson.TargetCompletionDate = dashboard.TargetCompletionDate??String.Empty;
                dashboardJson.OpportunityID = dashboard.OpportunityId;
                dashboardJson.TotalNoOfDays = dashboard.TotalNoOfDays;

                dashboardJson.ProcessNoOfDays = JsonConvert.SerializeObject(dashboard.ProcessList, Formatting.Indented);

                dashboardJson.ProcessEndDates = JsonConvert.SerializeObject(dashboard.ProcessEndDateList, Formatting.Indented);

                dashboardJson.ProcessLoanOfficers = JsonConvert.SerializeObject(dashboard.ProcessLoanOfficerNames, Formatting.Indented);

                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.DashboardListId
                };

                var result = await _graphSharePointAppService.UpdateListItemAsync(siteList, dashboard.Id, dashboardJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - DashBoard_UpdateOpportunityAsync finished SharePoint List for dashboard.");
                //For DashBoard---
                return StatusCodes.Status200OK;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - DashBoard_UpdateOpportunityAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - DashBoard_UpdateOpportunityAsync Service Exception: {ex}");
            }
        }
    }
}
