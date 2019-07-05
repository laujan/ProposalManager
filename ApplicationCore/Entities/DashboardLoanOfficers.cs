// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace ApplicationCore.Entities
{
    public class DashboardLoanOfficers : BaseEntity<DashboardLoanOfficers>
    {
        [JsonProperty("AdGroupName", Order = 2)]
        public string AdGroupName { get; set; }
        [JsonProperty("OfficerName", Order = 3)]
        public string OfficerName { get; set; }

        public static DashboardLoanOfficers Empty
        {
            get => new DashboardLoanOfficers
            {
                Id = string.Empty,
                AdGroupName = string.Empty,
                OfficerName = string.Empty 
            };
        }
    }
}
