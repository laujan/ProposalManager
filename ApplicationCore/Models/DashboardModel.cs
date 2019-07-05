// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationCore.Models
{
    public class DashboardModel 
    {
        [JsonProperty("id", Order = 1)]
        public string Id { get; set; }
        [JsonProperty("customername", Order = 2)]
        public string CustomerName { get; set; }
        [JsonProperty("opportunityid", Order = 3)]
        public string OpportunityId { get; set; }
        [JsonProperty("status", Order = 4)]
        public string Status { get; set; }
        [JsonProperty("startdate", Order = 5)]
        public DateTimeOffset StartDate { get; set; }
        [JsonProperty("targetcompletiondate", Order = 6)]
        public DateTimeOffset TargetCompletionDate { get; set; }
        [JsonProperty("compliancereviewstartdate", Order = 7)]
        public DateTimeOffset ComplianceReviewStartDate { get; set; }
        [JsonProperty("compliancereviewcomplteiondate", Order = 8)]
        public DateTimeOffset ComplianceReviewComplteionDate { get; set; }
        [JsonProperty("creditcheckstartdate", Order = 9)]
        public DateTimeOffset CreditCheckStartDate { get; set; }
        [JsonProperty("creditcheckcompletiondate", Order = 10)]
        public DateTimeOffset CreditCheckCompletionDate { get; set; }
        [JsonProperty("riskassesmentstartdate", Order = 11)]
        public DateTimeOffset RiskAssesmentStartDate { get; set; }
        [JsonProperty("riskassesmentcompletiondate", Order = 12)]
        public DateTimeOffset RiskAssesmentCompletionDate { get; set; }
        [JsonProperty("formalproposalstartdate", Order = 13)]
        public DateTimeOffset FormalProposalStartDate { get; set; }
        [JsonProperty("formalproposalcompletiondate", Order = 14)]
        public DateTimeOffset FormalProposalCompletionDate { get; set; }
        [JsonProperty("statuschangeddate", Order = 15)]
        public DateTimeOffset StatusChangedDate { get; set; }
        [JsonProperty ("opportunityenddate" ,Order=16)]
        public DateTimeOffset OpportunityEndDate { get; set; }
        [JsonProperty("opportunityname", Order =17)]
        public string OpportunityName { get; set; }
        [JsonProperty("loanofficer", Order = 18)]
        public string LoanOfficer { get; set; }
        [JsonProperty("relationshipmanager", Order = 19)]
        public string RelationshipManager { get; set; }

        //Calculated Columns

        [JsonProperty("TotalNoOfDays", Order = 20)]
        public int TotalNoOfDays { get; set; }
        [JsonProperty("CreditCheckNoOfDays", Order = 21)]
        public int CreditCheckNoOfDays { get; set; }
        [JsonProperty("ComplianceReviewNoOfDays", Order = 22)]
        public int ComplianceReviewNoOfDays { get; set; }
        [JsonProperty("FormalProposalNoOfDays", Order = 23)]
        public int FormalProposalNoOfDays { get; set; }
        [JsonProperty("RiskAssessmentNoOfDays", Order = 24)]
        public int RiskAssessmentNoOfDays { get; set; }
    }
}
