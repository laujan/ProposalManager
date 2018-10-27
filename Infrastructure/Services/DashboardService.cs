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
    public class DashboardService : BaseService<DashboardService>, IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        public DashboardService(ILogger<DashboardService> logger, IOptionsMonitor<AppOptions> appOptions,IDashboardRepository dashboardRepo) : base(logger, appOptions)
        {
            Guard.Against.Null(dashboardRepo, nameof(dashboardRepo));
            _dashboardRepository = dashboardRepo;
        }
        public async Task<StatusCodes> CreateOpportunityAsync(DashboardModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DashboardSvc_CreateOpportunityAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.CustomerName, nameof(modelObject.CustomerName), requestId);
            try
            {
                var entityObject = MapToEntity(modelObject, requestId);

                var result = await _dashboardRepository.CreateOpportunityAsync(entityObject, requestId);

                Guard.Against.NotStatus200OK(result, "DashboardSvc_CreateOpportunityAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - DashboardSvc_CreateOpportunityAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - DashboardSvc_CreateOpportunityAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> DeleteOpportunityAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DeleteOpportunityAsync called.");
            Guard.Against.Null(id, nameof(id));

            var result = await _dashboardRepository.DeleteOpportunityAsync(id, requestId);

            Guard.Against.NotStatus204NoContent(result, "DeleteOpportunityAsync", requestId);

            return result;
        }

        public async Task<IList<DashboardModel>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DashboardSvc_GetAllAsync called.");

            try
            {
                var listItems = (await _dashboardRepository.GetAllAsync(requestId)).ToList();
                Guard.Against.Null(listItems, nameof(listItems), requestId);

                var modelListItems = new List<DashboardModel>();
                foreach (var item in listItems)
                {
                    modelListItems.Add(MapToModel(item));
                }

                if (modelListItems.Count == 0)
                {
                    _logger.LogWarning($"RequestId: {requestId} - DashboardSvc_GetAllAsync no items found");
                    throw new NoItemsFound($"RequestId: {requestId} - Method name: DashboardSvc_GetAllAsync - No Items Found");
                }

                return modelListItems;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - CategorySvc_GetAllAsync error: " + ex);
                throw;
            }
        }

        private DashboardModel MapToModel(Dashboard entity)
        {
            var model = new DashboardModel();

            model.Id = entity.Id;
            model.CustomerName = entity.CustomerName ?? String.Empty;
            model.OpportunityId = entity.OpportunityId ?? String.Empty;
            model.Status = entity.Status ?? String.Empty;
            model.OpportunityName = entity.OpportunityName ?? String.Empty;
            model.LoanOfficer = entity.LoanOfficer ?? String.Empty;
            model.RelationshipManager = entity.RelationshipManager ?? String.Empty;
            if (entity.StartDate != null) model.StartDate = entity.StartDate;
            if (entity.TargetCompletionDate != null) model.TargetCompletionDate = entity.TargetCompletionDate;
            if (entity.StatusChangedDate != null) model.StatusChangedDate = entity.StatusChangedDate;
            if (entity.OpportunityEndDate != null) model.OpportunityEndDate = entity.OpportunityEndDate;

            if (entity.RiskAssesmentCompletionDate != null) model.RiskAssesmentCompletionDate = entity.RiskAssesmentCompletionDate;
            if (entity.RiskAssesmentStartDate != null) model.RiskAssesmentStartDate = entity.RiskAssesmentStartDate;
            if (entity.CreditCheckCompletionDate != null) model.CreditCheckCompletionDate = entity.CreditCheckCompletionDate;
            if (entity.CreditCheckStartDate != null) model.CreditCheckStartDate = entity.CreditCheckStartDate;
            if (entity.ComplianceReviewComplteionDate != null) model.ComplianceReviewComplteionDate = entity.ComplianceReviewComplteionDate;
            if (entity.ComplianceReviewStartDate != null) model.ComplianceReviewStartDate = entity.ComplianceReviewStartDate;
            if (entity.FormalProposalCompletionDate != null) model.FormalProposalCompletionDate = entity.FormalProposalCompletionDate;
            if (entity.FormalProposalStartDate != null) model.FormalProposalStartDate = entity.FormalProposalStartDate;

            //days mapping
            if (entity.TotalNoOfDays != 0) model.TotalNoOfDays = entity.TotalNoOfDays;
            if (entity.CreditCheckNoOfDays != 0) model.CreditCheckNoOfDays = entity.CreditCheckNoOfDays;
            if (entity.ComplianceReviewNoOfDays != 0) model.ComplianceReviewNoOfDays = entity.ComplianceReviewNoOfDays;
            if (entity.FormalProposalNoOfDays != 0) model.FormalProposalNoOfDays = entity.FormalProposalNoOfDays;
            if (entity.RiskAssessmentNoOfDays != 0) model.RiskAssessmentNoOfDays = entity.RiskAssessmentNoOfDays;

            return model;
        }

        public async Task<StatusCodes> UpdateOpportunityAsync(DashboardModel modelObject, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DashboardSvc_UpdateOpportunityAsync called.");

            Guard.Against.Null(modelObject, nameof(modelObject), requestId);
            Guard.Against.NullOrEmpty(modelObject.OpportunityId, nameof(modelObject.OpportunityId), requestId);
            try
            {
                var entityObject = MapToEntity(modelObject, requestId);

                var result = await _dashboardRepository.UpdateOpportunityAsync(entityObject, requestId);

                Guard.Against.NotStatus200OK(result, "DashboardSvc_CreateOpportunityAsync", requestId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - DashboardSvc_CreateOpportunityAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - DashboardSvc_CreateOpportunityAsync Service Exception: {ex}");
            }
        }

        private Dashboard MapToEntity(DashboardModel model, string requestId = "")
        {
            // Perform mapping
            var entity = new Dashboard();

            entity.Id = model.Id ?? String.Empty;
            entity.CustomerName = model.CustomerName ?? String.Empty;
            entity.OpportunityId = model.OpportunityId ?? String.Empty;
            entity.Status = model.Status ?? String.Empty;
            entity.OpportunityName = model.OpportunityName ?? String.Empty;
            entity.LoanOfficer = model.LoanOfficer ?? String.Empty;
            entity.RelationshipManager = model.RelationshipManager ?? String.Empty;
            if (model.StartDate != null) entity.StartDate = model.StartDate;
            if (model.TargetCompletionDate != null) entity.TargetCompletionDate = model.TargetCompletionDate;
            if (model.StatusChangedDate != null) entity.StatusChangedDate = model.StatusChangedDate;
            if (model.OpportunityEndDate != null) entity.OpportunityEndDate = model.OpportunityEndDate;

            if (model.RiskAssesmentCompletionDate != null) entity.RiskAssesmentCompletionDate = model.RiskAssesmentCompletionDate;
            if (model.RiskAssesmentStartDate != null) entity.RiskAssesmentStartDate = model.RiskAssesmentStartDate;
            if (model.CreditCheckCompletionDate != null) entity.CreditCheckCompletionDate = model.CreditCheckCompletionDate;
            if (model.CreditCheckStartDate != null) entity.CreditCheckStartDate = model.CreditCheckStartDate;
            if (model.ComplianceReviewComplteionDate != null) entity.ComplianceReviewComplteionDate = model.ComplianceReviewComplteionDate;
            if (model.ComplianceReviewStartDate != null) entity.ComplianceReviewStartDate = model.ComplianceReviewStartDate;
            if (model.FormalProposalCompletionDate != null) entity.FormalProposalCompletionDate = model.FormalProposalCompletionDate;
            if (model.FormalProposalStartDate != null) entity.FormalProposalStartDate = model.FormalProposalStartDate;


            //days mapping
            if (model.TotalNoOfDays != 0) entity.TotalNoOfDays = model.TotalNoOfDays;
            if (model.CreditCheckNoOfDays != 0) entity.CreditCheckNoOfDays = model.CreditCheckNoOfDays;
            if (model.ComplianceReviewNoOfDays != 0) entity.ComplianceReviewNoOfDays = model.ComplianceReviewNoOfDays;
            if (model.FormalProposalNoOfDays != 0) entity.FormalProposalNoOfDays = model.FormalProposalNoOfDays;
            if (model.RiskAssessmentNoOfDays != 0) entity.RiskAssessmentNoOfDays = model.RiskAssessmentNoOfDays;
            return entity;
        }

        public async Task<DashboardModel> GetAsync(string Id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DashboardSvc_GetAsync called.");

            try
            {
                var dashboard = await _dashboardRepository.GetAsync(Id, requestId);
                Guard.Against.Null(dashboard, nameof(dashboard), requestId);

                var dashboardmodel = MapToModel(dashboard);

                return dashboardmodel;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - CategorySvc_GetAsync error: " + ex);
                throw;
            }
        }
    }
}
