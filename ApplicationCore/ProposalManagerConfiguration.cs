// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System.Collections.Generic;

namespace ApplicationCore
{
	public class ProposalManagerConfiguration
	{
		public const string ConfigurationName = "ProposalManager";
		public ProposalManagerRole CreatorRole { get; set; }

        public ICollection<string> LeadRoles { get; set; }
    }
	public class ProposalManagerRole
	{
		public string Id { get; set; }
		public string AdGroupName { get; set; }
		public string DisplayName { get; set; }
	}
}