// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore.Interfaces;

namespace Infrastructure.Services
{
	public class SharePointLocationsCache : ISharePointLocationsCache
	{
		public string ProposalManagerSiteId { get; set; }
		public string ProposalManagerBaseSiteId { get; set; }
		public string RootDriveLocationId { get; set; }
		public string TempFolderLocationId { get; set; }
	}
}