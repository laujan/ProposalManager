using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace ApplicationCore.Entities
{
    public class ProcessesType : BaseEntity<ProcessesType>
    {
        [JsonProperty("processStep", Order = 2)]
        public string ProcessStep { get; set; }
        [JsonProperty("channel", Order = 3)]
        public string Channel { get; set; }
        [JsonProperty("processType", Order = 4)]
        public string ProcessType { get; set; }

        [JsonProperty("roleName", Order = 5)]
        public string RoleName { get; set; }
        [JsonProperty("roleId", Order = 6)]
        public string RoleId { get; set; }

        public static ProcessesType Empty
        {
            get => new ProcessesType
            {
                Id = string.Empty,
                ProcessStep = string.Empty,
                Channel = string.Empty,
                ProcessType = string.Empty,
                RoleName = string.Empty,
                RoleId = string.Empty,
            };
        }
    }
}
