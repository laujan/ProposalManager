using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ApplicationCore.Entities;
using ApplicationCore.ViewModels;

namespace ApplicationCore.ViewModels
{
    public class ProcessTypeViewModel
    {
        public ProcessTypeViewModel()
        {
            ProcessStep = string.Empty;
            ProcessType = string.Empty;
            Channel = string.Empty;
        }

        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("processStep", Order = 2)]
        public string ProcessStep { get; set; }
        [JsonProperty("channel", Order = 3)]
        public string Channel { get; set; }
        [JsonProperty("processType", Order = 4)]
        public string ProcessType { get; set; }
    }
}
