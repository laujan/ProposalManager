using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ApplicationCore.Entities;
using ApplicationCore.ViewModels;

namespace ApplicationCore.ViewModels
{
    public class TemplateViewModel
    {
        public TemplateViewModel()
        {
            TemplateName = string.Empty;
            Description = string.Empty;
            LastUsed = new DateTimeOffset();
            CreatedBy = new UserProfileViewModel();
            ProcessList = new List<ProcessViewModel>();
        }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("templateName", Order = 2)]
        public string TemplateName { get; set; }
        [JsonProperty("description", Order = 3)]
        public string Description { get; set; }
        [JsonProperty("lastUsed", Order = 4)]
        public DateTimeOffset LastUsed { get; set; }
        [JsonProperty("createdBy", Order = 5)]
        public UserProfileViewModel CreatedBy { get; set; }
        [JsonProperty("processes", Order = 6)]
        public IList<ProcessViewModel> ProcessList { get; set; }
    }
}
