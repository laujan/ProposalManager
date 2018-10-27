using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProposalCreation.Core.Models
{

	public class OpportunityViewModel
	{

		public string Id { get; set; }

		[Required]
		public string DisplayName { get; set; }

		public CustomerModel Customer { get; set; }

		[Required]
		public string Reference { get; set; }

		public string Version { get; set; }

		public int? OpportunityState { get; set; }

		public double DealSize { get; set; }

		public double AnnualRevenue { get; set; }

		public DateTimeOffset OpenedDate { get; set; }

		public double Margin { get; set; }

		public double Rate { get; set; }

		public double DebtRatio { get; set; }

		public string Purpose { get; set; }

		public string DisbursementSchedule { get; set; }

		public double CollateralAmount { get; set; }

		public string Guarantees { get; set; }

		public int RiskRating { get; set; }

		public string OpportunityChannelId { get; set; }

		public IList<TeamMemberModel> TeamMembers { get; set; }

		[Required]
		public IList<ChecklistModel> Checklists { get; set; }

	}

	public class CustomerModel
	{
		public string Id { get; set; }

		public string DisplayName { get; set; }

		public string ReferenceId { get; set; }
	}

	public class TeamMemberModel
	{

		public string Id { get; set; }

		public string DisplayName { get; set; }

		public int Status { get; set; }

		public string Mail { get; set; }

		public string UserPrincipalName { get; set; }

		public string Title { get; set; }

		public RoleModel AssignedRole { get; set; }

	}

	public class RoleModel
	{

		public string Id { get; set; }

		public string DisplayName { get; set; }

		public string AdGroupName { get; set; }

	}

	public class ChecklistModel
	{

		public string Id { get; set; }

		public string ChecklistChannel { get; set; }

	}

}