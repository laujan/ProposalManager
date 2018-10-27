// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

namespace ApplicationCore.Interfaces
{

	public interface ISharePointLocationsCache
	{
		string ProposalManagerSiteId { get; set; }
		string ProposalManagerBaseSiteId { get; set; }
		string RootDriveLocationId { get; set; }
		string TempFolderLocationId { get; set; }
	}

}