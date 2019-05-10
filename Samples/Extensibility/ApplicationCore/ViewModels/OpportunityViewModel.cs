// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore.Helpers;
using ApplicationCore.Models;
using ApplicationCore.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using ApplicationCore.Artifacts;

namespace ApplicationCore.ViewModels
{
    public class OpportunityViewModel
    {
        public OpportunityViewModel()
        {
            Id = String.Empty;
            Reference = String.Empty;
            DisplayName = String.Empty;
            OpportunityState = OpportunityStateModel.NoneEmpty;
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

        /// <summary>
        /// Unique identifier of the artifact
        /// </summary>
        [JsonProperty("reference")]
        public string Reference { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        // ContentType not needed
        // Version not needed
        // Uri not needed
        // TypeName not needed

        // Metadata
        [JsonConverter(typeof(OpportunityStateModelConverter))]
        [JsonProperty("opportunityState")]
        public OpportunityStateModel OpportunityState { get; set; }

        [JsonProperty("customer")]
        public CustomerModel Customer { get; set; }

        [JsonProperty("metaDataFields")]
        public IList<OpportunityMetaDataFields> MetaDataFields { get; set; }


        [JsonProperty("opportunityChannelId")]
        public string OpportunityChannelId { get; set; }

        //templateLoaded
        [JsonProperty("templateLoaded")]
        public bool TemplateLoaded { get; set; }

        // Content
        [JsonProperty("teamMembers")]
        public IList<TeamMemberModel> TeamMembers { get; set; }

        [JsonProperty("notes")]
        public IList<NoteModel> Notes { get; set; }

        [JsonProperty("checklists")]
        public IList<ChecklistModel> Checklists { get; set; }

        [JsonProperty("customerFeedback")]
        public CustomerFeedbackModel CustomerFeedback { get; set; }

        [JsonProperty("proposalDocument")]
        public ProposalDocumentModel ProposalDocument { get; set; }

        [JsonProperty("customerDecision")]
        public CustomerDecisionModel CustomerDecision { get; set; }

        // DocumentAttachments
        [JsonProperty("documentAttachments")]
        public IList<DocumentAttachmentModel> DocumentAttachments { get; set; }

        // DealType
        [JsonProperty("template")]
        public TemplateViewModel Template { get; set; }
    }

    public class OpportunityStateModel : SmartEnum<OpportunityStateModel, int>
    {
        public static OpportunityStateModel NoneEmpty = new OpportunityStateModel(nameof(NoneEmpty), 0);
        public static OpportunityStateModel Creating = new OpportunityStateModel(nameof(Creating), 1);
        public static OpportunityStateModel InProgress = new OpportunityStateModel(nameof(InProgress), 2);
        public static OpportunityStateModel Assigned = new OpportunityStateModel(nameof(Assigned), 3);
        public static OpportunityStateModel Draft = new OpportunityStateModel(nameof(Draft), 4);
        public static OpportunityStateModel NotStarted = new OpportunityStateModel(nameof(NotStarted), 5);
        public static OpportunityStateModel InReview = new OpportunityStateModel(nameof(InReview), 6);
        public static OpportunityStateModel Blocked = new OpportunityStateModel(nameof(Blocked), 7);
        public static OpportunityStateModel Completed = new OpportunityStateModel(nameof(Completed), 8);
        public static OpportunityStateModel Submitted = new OpportunityStateModel(nameof(Submitted), 9);
        public static OpportunityStateModel Accepted = new OpportunityStateModel(nameof(Accepted), 10);
		public static OpportunityStateModel Archived = new OpportunityStateModel(nameof(Archived), 11);

		[JsonConstructor]
        protected OpportunityStateModel(string name, int value) : base(name, value)
        {
        }
    }
}

