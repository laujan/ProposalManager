// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ProposalCreation.Core.Configuration;

namespace ProposalCreation.Core.Providers
{

	public class RootConfigurationProvider : IRootConfigurationProvider
	{

		public RootConfigurationProvider(
			IConventionBasedConfigurationProvider<AzureAdConfiguration> azureAdConfigurationProvider,
			IConventionBasedConfigurationProvider<GeneralConfiguration> generalConfigurationProvider,
			IConventionBasedConfigurationProvider<ProposalManagerConfiguration> proposalManagerConfigurationProvider)
		{
			AzureAdConfiguration = azureAdConfigurationProvider.Configuration;
			GeneralConfiguration = generalConfigurationProvider.Configuration;
			ProposalManagerConfiguration = proposalManagerConfigurationProvider.Configuration;
		}

		public AzureAdConfiguration AzureAdConfiguration { get; }
		public GeneralConfiguration GeneralConfiguration { get; }
		public ProposalManagerConfiguration ProposalManagerConfiguration { get; }
	}

}