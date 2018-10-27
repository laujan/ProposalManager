// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApplicationCore.Interfaces
{
	public interface IDynamicsLinkService
	{
		string GetConnectionRoleName(string id);
		string GetAccountName(string id);
		UserData GetUserData(string id);
		Task CreateTemporaryLocationForOpportunityAsync(string opportunityId, string opportunityName);
		Task CreateLocationsForOpportunityAsync(string opportunityId, string opportunityName, IEnumerable<string> locations);
	}
}