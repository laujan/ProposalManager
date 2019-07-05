// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationCore.Entities
{
    public class Dashboard : BaseEntity<Dashboard>
    {
        [JsonProperty("customername", Order = 2)]
        public string CustomerName { get; set; }
        [JsonProperty("opportunityid", Order = 3)]
        public string OpportunityId { get; set; }
        [JsonProperty("status", Order = 4)]
        public string Status { get; set; }
        [JsonProperty("startdate", Order = 5)]
        public string StartDate { get; set; }
        [JsonProperty("targetcompletiondate", Order = 6)]
        public string TargetCompletionDate { get; set; }
        [JsonProperty("opportunityName", Order = 7)]
        public string OpportunityName { get; set; }
        [JsonProperty("totalNoOfDays", Order = 8)]
        public int TotalNoOfDays { get; set; }
        [JsonProperty("processList", Order = 9)]
        public IList<DashboardProcessList> ProcessList { get; set; }
        [JsonProperty("processEndDateList", Order = 10)]
        public IList<DashboradProcessEndDateList> ProcessEndDateList { get; set; }
        [JsonProperty("processLoanOfficerNames", Order = 11)]
        public IList<DashboardLoanOfficers> ProcessLoanOfficerNames { get; set; }

        public static Dashboard Empty
        {
            get => new Dashboard
            {
                Id = String.Empty,
                CustomerName = string.Empty,
                OpportunityId = string.Empty,
                Status = string.Empty,
                StartDate = string.Empty,
                OpportunityName = string.Empty,
                TotalNoOfDays=0,
                ProcessList =new  List<DashboardProcessList>(),
                ProcessEndDateList = new List<DashboradProcessEndDateList>(),
                ProcessLoanOfficerNames = new List<DashboardLoanOfficers>()
            };
        }
    }
}
