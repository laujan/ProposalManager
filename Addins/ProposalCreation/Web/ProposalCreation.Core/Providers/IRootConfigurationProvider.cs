// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ProposalCreation.Core.Configuration;

namespace ProposalCreation.Core.Providers
{
	public interface IRootConfigurationProvider
	{
		AzureAdConfiguration AzureAdConfiguration { get; }
		GeneralConfiguration GeneralConfiguration { get; }
		ProposalManagerConfiguration ProposalManagerConfiguration { get; }
	}
}