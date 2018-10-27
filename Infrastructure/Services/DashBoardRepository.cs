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
                itemFieldsJson.OpportunityID = entity.OpportunityId;
                itemFieldsJson.StartDate = entity.StartDate;
                itemFieldsJson.StatusChangedDate = entity.StatusChangedDate;
                itemFieldsJson.TargetCompletionDate = entity.TargetCompletionDate;

                itemFieldsJson.OpportunityName = entity.OpportunityName;
                itemFieldsJson.LoanOfficer = entity.LoanOfficer;
                itemFieldsJson.RelationshipManager = entity.RelationshipManager;
                //itemFieldsJson.OpportunityStartDate = entity.StartDate;

                //Giving days fields a 0 value @ inception
                itemFieldsJson.TotalNoOfDays = entity.TotalNoOfDays;
                itemFieldsJson.CreditCheckNoOfDays = entity.CreditCheckNoOfDays;
                itemFieldsJson.ComplianceReviewNoOfDays = entity.ComplianceReviewNoOfDays;
                itemFieldsJson.FormalProposalNoOfDays = entity.FormalProposalNoOfDays;
                itemFieldsJson.RiskAssessmentNoOfDays = entity.RiskAssessmentNoOfDays;

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

        public async Task<Dashboard> GetAsync(string Id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DashboardRepository_GetAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.DashboardListId
                };
                var id = WebUtility.UrlEncode(Id);
                var options = new List<QueryParam>();
                options.Add(new QueryParam("filter", $"startswith(fields/OpportunityID,'{id}')"));

                var json = await _graphSharePointAppService.GetListItemsAsync(siteList, options, "all", requestId);
                var dashboardObject = json["value"][0]["fields"].ToString();

                var dashboard = JsonConvert.DeserializeObject<Dashboard>(dashboardObject, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

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

                dashboardJson.Title = dashboard.Id;
                dashboardJson.Status = dashboard.Status;
                dashboardJson.StatusChangedDate = dashboard.StatusChangedDate;

                if(dashboard.TargetCompletionDate != DateTimeOffset.MinValue) dashboardJson.TargetCompletionDate = dashboard.TargetCompletionDate;
                if(dashboard.RiskAssesmentCompletionDate != DateTimeOffset.MinValue) dashboardJson.RiskAssesmentCompletionDate = dashboard.RiskAssesmentCompletionDate;
                if(dashboard.RiskAssesmentStartDate != DateTimeOffset.MinValue) dashboardJson.RiskAssesmentStartDate = dashboard.RiskAssesmentStartDate;
                if(dashboard.CreditCheckCompletionDate != DateTimeOffset.MinValue) dashboardJson.CreditCheckCompletionDate = dashboard.CreditCheckCompletionDate;
                if(dashboard.CreditCheckStartDate != DateTimeOffset.MinValue) dashboardJson.CreditCheckStartDate = dashboard.CreditCheckStartDate;
                if(dashboard.ComplianceReviewComplteionDate != DateTimeOffset.MinValue) dashboardJson.ComplianceRewiewCompletionDate = dashboard.ComplianceReviewComplteionDate;
                if(dashboard.ComplianceReviewStartDate != DateTimeOffset.MinValue) dashboardJson.ComplianceRewiewStartDate = dashboard.ComplianceReviewStartDate;
                if(dashboard.FormalProposalCompletionDate != DateTimeOffset.MinValue) dashboardJson.FormalProposalEndDateDate = dashboard.FormalProposalCompletionDate;
                if(dashboard.FormalProposalStartDate != DateTimeOffset.MinValue) dashboardJson.FormalProposalStartDate = dashboard.FormalProposalStartDate;
                if(dashboard.OpportunityEndDate != DateTimeOffset.MinValue) dashboardJson.OpportunityEndDate = dashboard.OpportunityEndDate;

                if (!string.IsNullOrEmpty(dashboard.LoanOfficer)) dashboardJson.LoanOfficer = dashboard.LoanOfficer;
                if (!string.IsNullOrEmpty(dashboard.RelationshipManager)) dashboardJson.RelationshipManager = dashboard.RelationshipManager;

                dashboardJson.TotalNoOfDays = dashboard.TotalNoOfDays;
                dashboardJson.CreditCheckNoOfDays = dashboard.CreditCheckNoOfDays;
                dashboardJson.ComplianceReviewNoOfDays = dashboard.ComplianceReviewNoOfDays;
                dashboardJson.FormalProposalNoOfDays = dashboard.FormalProposalNoOfDays;
                dashboardJson.RiskAssessmentNoOfDays = dashboard.RiskAssessmentNoOfDays;

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
