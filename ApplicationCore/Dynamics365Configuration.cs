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
		public string DisplayName { get; set; }
		public string DealSize { get; set; }
		public string AnnualRevenue { get; set; }
		public string OpenedDate { get; set; }
		public string Margin { get; set; }
		public string Rate { get; set; }
		public string DebtRatio { get; set; }
		public string Purpose { get; set; }
		public string DisbursementSchedule { get; set; }
		public string CollateralAmount { get; set; }
		public string Guarantees { get; set; }
		public string RiskRating { get; set; }
		public ICollection<OpportunityStatusMapping> Status { get; set; }
		public int MapStatusCode(int statusCode) => Status.First(s => s.From == statusCode).To;
	}

	public class OpportunityStatusMapping
	{
		public int From { get; set; }
		public int To { get; set; }
	}
}