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
        private readonly IProcessRepository _processRepository;

        public DashboardService(ILogger<DashboardService> logger, IOptionsMonitor<AppOptions> appOptions,IDashboardRepository dashboardRepo, IProcessRepository processRepository) : base(logger, appOptions)
        {
            Guard.Against.Null(dashboardRepo, nameof(dashboardRepo));
            _dashboardRepository = dashboardRepo;
            _processRepository = processRepository;
        }


        public async Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - CreateDashBoardEntryAsync called.");
            try
            {
                var targetDate = opportunity.Metadata.Fields.ToList().Find(x => x.DisplayName == "Target Date")?.Values;
                var openedDate = opportunity.Metadata.Fields.ToList().Find(x => x.DisplayName == "Opened Date")?.Values;

                if (targetDate != null && openedDate != null)
                {
                    var entity = new Dashboard();
                    entity.CustomerName = opportunity.Metadata.Customer.DisplayName.ToString();
                    entity.Status = opportunity.Metadata.OpportunityState.Name.ToString();
                    entity.StartDate = openedDate ?? String.Empty;
                    entity.OpportunityName = opportunity.DisplayName.ToString();
                    entity.OpportunityId = opportunity.Id;
                    entity.Id = String.Empty;
                    entity.TotalNoOfDays = 0;
                    entity.ProcessList = new List<DashboardProcessList>();
                    entity.ProcessEndDateList = new List<DashboradProcessEndDateList>();
                    entity.ProcessLoanOfficerNames = new List<DashboardLoanOfficers>();

                    var processList = (await _processRepository.GetAllAsync(requestId)).ToList();

                    foreach(var process in processList)
                    {
                        if (process.ProcessType.ToLower()== "checklisttab")
                        {
                            entity.ProcessList.Add(new DashboardProcessList
                            {
                                ProcessName = process.Channel.ToLower(),
                                ProcessEndDate = string.Empty,
                                ProcessStartDate = string.Empty,
                                NoOfDays = 0
                            });

                            entity.ProcessEndDateList.Add(new DashboradProcessEndDateList
                            {
                                Process = process.Channel.ToLower() + "enddate",
                                EndDate = string.Empty
                            });
                        }
                    }


                    var loanOfficerAdgroup = opportunity.Content.TeamMembers.FirstOrDefault(mem => mem.Fields.Permissions.Any(per => per.Name.ToLower() == "opportunity_readwrite_dealtype"));
                    entity.ProcessLoanOfficerNames.Add(new DashboardLoanOfficers
                    {
                        AdGroupName = loanOfficerAdgroup != null ? loanOfficerAdgroup.RoleName.ToString():string.Empty,
                        OfficerName = loanOfficerAdgroup != null ? loanOfficerAdgroup.DisplayName.ToString() : string.Empty
                    });


                    await _dashboardRepository.CreateOpportunityAsync(entity, requestId);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - CreateDashBoardEntryAsync Service Exception: {ex}");
            }

            return opportunity;
        }

        public async Task<Opportunity> UpdateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - UpdateDashBoardEntryAsync called.");
            try
            {
                var dashboard = await _dashboardRepository.GetAsync(opportunity.DisplayName.ToString(), requestId);

                if (dashboard != null){
                    dashboard.OpportunityId = opportunity.Id;
                    var date = DateTimeOffset.Now.Date;

                    if (dashboard.Status.ToLower() != opportunity.Metadata.OpportunityState.Name.ToLower()){
                        dashboard.Status = opportunity.Metadata.OpportunityState.Name.ToString();
                        if (dashboard.Status.ToLower().ToString() == "accepted" || dashboard.Status.ToLower().ToString() == "archived"){
                            dashboard.TargetCompletionDate = date.ToString();
                            dashboard.TotalNoOfDays = GetDateDifference(DateTime.Parse(dashboard.StartDate.ToString()), date);
                        }

                    }

                    var oppCheckLists = opportunity.Content.Checklists.ToList();

                    foreach (var process in opportunity.Content.Template.ProcessList)
                    {
                        if (process.ProcessType.ToLower() == "checklisttab")
                        {
                            var checklistItm = oppCheckLists.Find(x => x.ChecklistChannel.ToLower() == process.Channel.ToLower());
                            if (checklistItm != null)
                            {
                                var dProcess = dashboard.ProcessList.ToList().Find(x => x.ProcessName.ToLower() == process.Channel.ToLower());
                                if (dProcess != null)
                                {
                                    if (checklistItm.ChecklistStatus == ActionStatus.InProgress)
                                    {
                                        dProcess.ProcessStartDate = date.ToString();
                                    }
                                    if (checklistItm.ChecklistStatus == ActionStatus.Completed)
                                    {
                                        dProcess.ProcessEndDate = date.ToString();
                                        dProcess.NoOfDays = GetDateDifference(DateTime.Parse(dProcess.ProcessStartDate), date);
                                    }
                                }
                                else
                                {
                                    dProcess = new DashboardProcessList();
                                    dProcess.ProcessName = checklistItm.ChecklistChannel.ToLower();
                                    dProcess.ProcessStartDate = date.ToString();
                                    dProcess.NoOfDays = 0;
                                    dashboard.ProcessList.Add(dProcess);

                                }

                                var processEndDateObj = dashboard.ProcessEndDateList.ToList().Find(x => x.Process.ToLower() == process.Channel.ToLower() + "enddate");
                                if (processEndDateObj != null)
                                {
                                    if (checklistItm.ChecklistStatus == ActionStatus.Completed)
                                    {
                                        processEndDateObj.EndDate = date.ToString();                                     
                                    }
                                }
                                else
                                {
                                    processEndDateObj = new DashboradProcessEndDateList();
                                    processEndDateObj.Process = checklistItm.ChecklistChannel.ToLower() + "enddate";
                                    processEndDateObj.EndDate = string.Empty;
                                    dashboard.ProcessEndDateList.Add(processEndDateObj);

                                }
                            }
                        }

                    }

                    var loanOfficerAdgroup = opportunity.Content.TeamMembers.FirstOrDefault(mem => mem.Fields.Permissions.Any(per => per.Name.ToLower() == "opportunity_readwrite_dealtype"));
                    if (loanOfficerAdgroup !=null)
                    {
                        var obj = dashboard.ProcessLoanOfficerNames.ToList().Find(x => x.AdGroupName.ToLower() == loanOfficerAdgroup.RoleName.ToLower());
                        if (obj != null) obj.OfficerName = loanOfficerAdgroup.DisplayName.ToString();
                    }

                    await _dashboardRepository.UpdateOpportunityAsync(dashboard, requestId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - UpdateDashBoardEntryAsync Service Exception: {ex}");
            }

            return opportunity;
        }

        public async Task<StatusCodes> DeleteOpportunityAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DeleteOpportunityAsync called.");
            Guard.Against.Null(id, nameof(id));

            var result = await _dashboardRepository.DeleteOpportunityAsync(id, requestId);

            Guard.Against.NotStatus204NoContent(result, "DeleteOpportunityAsync", requestId);

            return result;
        }

        private int GetDateDifference(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            //=VALUE(IF(ISBLANK(OpportunityEndDate),0,DATEDIF(StartDate,OpportunityEndDate,"d")))
            //=IF(ISBLANK(CreditCheckCompletionDate),0,DATEDIF(CreditCheckStartDate,CreditCheckCompletionDate,"d"))
            //=IF(ISBLANK(ComplianceRewiewCompletionDate),0,DATEDIF(ComplianceRewiewStartDate,ComplianceRewiewCompletionDate,"d"))
            //=IF(ISBLANK(FormalProposalEndDateDate), 0, DATEDIF(FormalProposalStartDate, FormalProposalEndDateDate, "d"))
            //=IF(ISBLANK(RiskAssesmentCompletionDate),0,DATEDIF(RiskAssesmentStartDate,RiskAssesmentCompletionDate,"d"))
            int datediff = 0;
            try
            {
                if (endDate != null && endDate != DateTimeOffset.MinValue)
                {
                    if (startDate != null && startDate != DateTimeOffset.MinValue)
                    {
                        if (startDate.Date == endDate.Date) datediff = 1;
                        else datediff = Convert.ToInt32((endDate - startDate).TotalDays);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"DashBoardAnalysis_GetDateDifference Service Exception: {ex}");
            }
            return datediff;
        }

    }
}
