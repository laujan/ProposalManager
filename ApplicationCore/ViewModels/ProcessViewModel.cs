using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ApplicationCore.Entities;
using ApplicationCore.ViewModels;
using ApplicationCore.Serialization;

namespace ApplicationCore.ViewModels
{
    public class ProcessViewModel : ProcessTypeViewModel
    {
        [JsonProperty("order", Order = 5)]
        public string Order { get; set; }
        [JsonProperty("daysEstimate", Order = 6)]
        public string DaysEstimate { get; set; }
        [JsonConverter(typeof(StatusConverter))]
        [JsonProperty("status", Order = 7)]
        public ActionStatus Status { get; set; }
        /// <summary>
        /// Represents the empty user profile. This field is read-only.
        /// </summary>
        public static ProcessViewModel Empty
        {
            get => new ProcessViewModel
            {
               Order = string.Empty,
               DaysEstimate = string.Empty,
               Status = ActionStatus.NotStarted
            };
        }
    }
}
