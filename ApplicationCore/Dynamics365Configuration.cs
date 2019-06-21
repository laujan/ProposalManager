// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System.Collections.Generic;
using System.Linq;

namespace ApplicationCore
{
    public class Dynamics365Configuration
    {
        public const string ConfigurationName = "Dynamics365";
        public string OrganizationUri { get; set; }
        public int ProposalManagerCategoryId { get; set; }
        public string RootDrive { get; set; }
        /// <summary>
        /// The display name of the Deal Type (defined in Proposal Manager) to be assigned to every new opportunity created from the Dynamics Integration.
        /// If this field is completed, the Integration will ignore the msbnk_dealtype property in the Dynamics payload.
        /// </summary>
        public string DefaultDealType { get; set; }

        public OpportunityMappingConfiguration OpportunityMapping { get; set; }
    }

    public class OpportunityMappingConfiguration
    {
        /// <summary>
        /// A string with the internal name of the entity used to represent an opportunity in Dynamics 365. Default is 'opportunity'.
        /// </summary>
        public string EntityName { get; set; }
        /// <summary>
        /// A string with the internal name of the field within the Dynamics 365 entity that holds the proper name of the opportunity instance. Default is 'name'.
        /// </summary>
        public string NameProperty { get; set; }
        /// <summary>
        /// An array where each object defines the mapping from a field in Dynamics 365 to a field in Proposal Manager.
        /// </summary>
        public ICollection<OpportunityMapping> MetadataFields { get; set; }
        /// <summary>
        /// An array where each object defines the mapping from a status number in Dynamics 365 to a status number in Proposal Manager.
        /// </summary>
        public ICollection<OpportunityStatusMapping> Status { get; set; }

        public int MapStatusCode(int statusCode) => Status?.FirstOrDefault(s => s.From == statusCode)?.To ?? statusCode;
    }

    public class OpportunityStatusMapping
    {
        public int From { get; set; }
        public int To { get; set; }
    }

    public class OpportunityMapping
    {
        public string From { get; set; }
        public string To { get; set; }
    }
}