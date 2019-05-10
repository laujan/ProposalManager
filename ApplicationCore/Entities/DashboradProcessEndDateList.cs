using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace ApplicationCore.Entities
{
    public class DashboradProcessEndDateList : BaseEntity<DashboradProcessEndDateList>
    {
        [JsonProperty("process", Order = 2)]
        public string Process { get; set; }

        [JsonProperty("endate", Order = 4)]
        public string EndDate { get; set; }

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
