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
    public class DashboardProcessList : BaseEntity<DashboardProcessList>
    {
        [JsonProperty("processName", Order = 2)]
        public string ProcessName { get; set; }
        [JsonProperty("processStartDate", Order = 3)]
        public string ProcessStartDate { get; set; }
        [JsonProperty("processEndDate", Order = 4)]
        public string ProcessEndDate { get; set; }
        [JsonProperty("noOfDays", Order = 5)]
        public int NoOfDays { get; set; }

        public static DashboardProcessList Empty
        {
            get => new DashboardProcessList
            {
                Id = string.Empty,
                ProcessName = string.Empty,
                ProcessStartDate = string.Empty,
                ProcessEndDate = string.Empty,
                NoOfDays = 0
            };
        }
    }
}
