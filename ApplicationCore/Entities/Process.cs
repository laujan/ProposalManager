using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using ApplicationCore.Serialization;

namespace ApplicationCore.Entities
{
    public class Process : ProcessesType
    {
        [JsonProperty("order", Order = 5)]
        public string Order { get; set; }
        [JsonProperty("daysEstimate", Order = 6)]
        public string DaysEstimate { get; set; }
        [JsonConverter(typeof(StatusConverter))]
        [JsonProperty("status", Order = 7)]
        public ActionStatus Status { get; set; }
        [JsonProperty("processnumber", Order = 8)]
        public int ProcessNumber { get; set; }
        [JsonProperty("groupnumber", Order = 9)]
        public int GroupNumber { get; set; }
        public new static Process Empty
        {
            get => new Process
            {
                Id = string.Empty,
                ProcessStep = String.Empty,
                Channel = string.Empty,
                ProcessType = string.Empty,
                Order = string.Empty,
                DaysEstimate = string.Empty,
                Status = ActionStatus.NotStarted,
                ProcessNumber=0,
                GroupNumber=0
            };
        }
    }
}
