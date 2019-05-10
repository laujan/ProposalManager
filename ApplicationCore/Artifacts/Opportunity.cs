// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Newtonsoft.Json;
using ApplicationCore.Entities;
using ApplicationCore.Helpers;
using ApplicationCore.Serialization;
using ApplicationCore;

namespace ApplicationCore.Artifacts
{
    public class Opportunity : BaseArtifact<Opportunity>
    {
        public Opportunity()
        {
            ContentType = ContentType.Opportunity;
            Version = "2.0";
   
        }

        /// <summary>
        /// Content type of the opportunity
        /// </summary>
        [JsonProperty("contentType")]
        public new ContentType ContentType { get; private set; }

        /// <summary>
        /// Metadata of the opportunity
        /// </summary>
        [JsonProperty("metadata")]
        public new OpportunityMetadata Metadata { get; set; }

        /// <summary>
        /// Content of the opportunity
        /// </summary> // may have document, workflow etc
        [JsonProperty("content")]
        public new OpportunityContent Content { get; set; }

        /// <summary>
        /// Artifacts bag
        /// </summary>
        [JsonProperty("documentAttachments")]
        public IList<DocumentAttachment> DocumentAttachments { get; set; }

        ///<Summary>
        ///Initail template loaded
        ///</Summary>
        [JsonProperty("templateLoaded")]
        public bool TemplateLoaded { get; set; }
        /// <summary>
        /// Represents the empty opportunity. This field is read-only.
        /// </summary>
        /// 
        public static Opportunity Empty
        {
            get => new Opportunity
            {
                Id = String.Empty,
                DisplayName = String.Empty,
                Reference = String.Empty,
                ContentType = ContentType.Opportunity,
                Version = "2.0",
                Metadata = OpportunityMetadata.Empty,
                Content = OpportunityContent.Empty,
                DocumentAttachments = new List<DocumentAttachment>(),
                TemplateLoaded = false
            };
        }   
    }

    public class OpportunityContent
    {
        [JsonProperty("teamMembers")]
        public IList<TeamMember> TeamMembers { get; set; }

        [JsonProperty("notes")]
        public IList<Note> Notes { get; set; }

        [JsonProperty("checklists")]
        public IList<Checklist> Checklists { get; set; }

        [JsonProperty("proposalDocument")]
        public ProposalDocument ProposalDocument { get; set; }

        [JsonProperty("customerDecision")]
        public CustomerDecision CustomerDecision { get; set; }
        // DealType
        [JsonProperty("template")]
        public Template Template { get; set; }

        /// <summary>
        /// Represents the empty opportunity. This field is read-only.
        /// </summary>
        public static OpportunityContent Empty
        {
            get => new OpportunityContent
            {
                TeamMembers = new List<TeamMember>(),
                Notes = new List<Note>(),
                Checklists = new List<Checklist>(),
                ProposalDocument = ProposalDocument.Empty,
                Template = new Template()
            };
        }
    }

    //WAVE-4 GENERIC ACCELERATOR Change : start
    public class OpportunityMetadata
    {
        [JsonConverter(typeof(OpportunityStateConverter))]
        [JsonProperty("opportunityState")]
        public OpportunityState OpportunityState { get; set; }
        [JsonProperty("customer")]
        public Customer Customer { get; set; }
        [JsonProperty("metadatafields")]
        public IList<OpportunityMetaDataFields> Fields { get; set; }
        [JsonProperty("opportunityChannelId")]
        public string OpportunityChannelId { get; set; }
        public static OpportunityMetadata Empty
        {
            get => new OpportunityMetadata
            {
                OpportunityState = OpportunityState.NoneEmpty,
                Customer = Customer.Empty,
                Fields = new List<OpportunityMetaDataFields>()
            };
        }
    }
    //WAVE-4 GENERIC ACCELERATOR Change : end

}
