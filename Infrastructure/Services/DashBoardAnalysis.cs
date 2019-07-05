// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Interfaces;
using ApplicationCore;

namespace Infrastructure.Services
{
    public class DashBoardAnalysis : BaseService<DashBoardAnalysis>, IDashboardAnalysis
    {
        public DashBoardAnalysis(ILogger<DashBoardAnalysis> logger, IOptionsMonitor<AppOptions> appOptions) : base(logger, appOptions)
        {
        }

        public int GetDateDifference(DateTimeOffset startDate, DateTimeOffset endDate, DateTimeOffset opportunityStartDate)
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
                        datediff = Convert.ToInt32((endDate - startDate).TotalDays);
                    }
                    else
                    {
                        datediff = Convert.ToInt32((endDate - opportunityStartDate).TotalDays);
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"DashBoardAnalysis_GetDateDifference Service Exception: {ex}");
            }
            return datediff;
        }
    }
}

