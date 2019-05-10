// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationCore.Artifacts;
using ApplicationCore.Helpers;
using ApplicationCore.Serialization;
using Newtonsoft.Json;
using ApplicationCore.ViewModels;

namespace ApplicationCore.Models
{
    public class OpportunityIndexModel
    {
        public OpportunityIndexModel()
        {
            Id = String.Empty;
            DisplayName = String.Empty;
            OpportunityState = OpportunityStateModel.NoneEmpty;
            Customer = new CustomerModel();
            Template = new TemplateViewModel();
            Dealsize = String.Empty;
            OpenedDate = String.Empty;
        }

        /// <summary>
        /// Unique identifier of the artifact
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Unique identifier of the artifact
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }


        // Metadata
        [JsonConverter(typeof(OpportunityStateModelConverter))]
        [JsonProperty("opportunityState")]
        public OpportunityStateModel OpportunityState { get; set; }

        [JsonProperty("customer")]
        public CustomerModel Customer { get; set; }
        [JsonProperty("template")]
        public TemplateViewModel Template { get; set; }

        [JsonProperty("dealsize")]
        public string Dealsize { get; set; }

        [JsonProperty("openedDate")]
        public string OpenedDate { get; set; }
    }
}
