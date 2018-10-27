// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApplicationCore.Interfaces
{
	public interface IOneDriveLinkService
	{
		Task EnsureTempFolderForOpportunityExistsAsync(string opportunityName);
		Task SubscribeToFormalProposalChangesAsync(string opportunityName);
		Task SubscribeToTempFolderChangesAsync();
		Task RegisterOpportunityDeltaLinkAsync(string opportunityName, string resource);
		Task ProcessAttachmentChangesAsync(string resource);
		Task ProcessFormalProposalChangesAsync(string opportunityName, string resource);
		Task EnsureChannelFoldersForOpportunityExistAsync(string opportunityName, IEnumerable<string> locations);
		Task RenewAllSubscriptionsAsync();
	}
}