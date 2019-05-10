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
		public OpportunityMappingConfiguration OpportunityMapping { get; set; }
	}

	public class OpportunityMappingConfiguration
	{
        public ICollection<OpportunityMapping> MetadataFields { get; set; }
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