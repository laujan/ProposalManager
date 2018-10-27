// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApplicationCore.Interfaces
{
	public interface ISharePointLocationRepository
	{
		string ProposalManagerSiteId { get; }
		Task CreateLocationsForOpportunityAsync(string opportunityId, string opportunityName, IEnumerable<string> locations);
		Task CreateTemporaryLocationForOpportunityAsync(string opportunityId, string opportunityName);
		Task DeleteTemporaryLocationForOpportunityAsync(string opportunityName);
	}
}