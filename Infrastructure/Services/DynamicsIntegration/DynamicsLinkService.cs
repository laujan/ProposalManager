// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
	public class DynamicsLinkService : IDynamicsLinkService
	{
		private readonly IConnectionRoleRepository connectionRoleRepository;
		private readonly IAccountRepository accountRepository;
        private readonly IContactRepository contactRepository;
        private readonly IUserRepository userRepository;
		private readonly ISharePointLocationRepository sharePointLocationRepository;
		private readonly IOneDriveLinkService oneDriveLinkService;

		public DynamicsLinkService(
			IConnectionRoleRepository connectionRoleRepository,
			IAccountRepository accountRepository,
            IContactRepository contactRepository,
            IUserRepository userRepository,
			ISharePointLocationRepository sharePointLocationRepository,
			IOneDriveLinkService oneDriveLinkService)
		{
			this.connectionRoleRepository = connectionRoleRepository;
			this.accountRepository = accountRepository;
            this.contactRepository = contactRepository;
            this.userRepository = userRepository;
			this.sharePointLocationRepository = sharePointLocationRepository;
			this.oneDriveLinkService = oneDriveLinkService;
		}

		public string GetConnectionRoleName(string id)
		{
			try
			{
				return connectionRoleRepository.ConnectionRoles[id];
			}
			catch (KeyNotFoundException)
			{
				return null;
			}
		}

		public string GetAccountName(string id) => accountRepository.Accounts[id];

        public string GetContactName(string id) => contactRepository.Contacts[id];

        public UserData GetUserData(string id) => userRepository.Users[id];

		public async Task CreateTemporaryLocationForOpportunityAsync(string opportunityId, string opportunityName)
		{
			await oneDriveLinkService.EnsureTempFolderForOpportunityExistsAsync(opportunityName);
			await oneDriveLinkService.SubscribeToTempFolderChangesAsync();
			await sharePointLocationRepository.CreateTemporaryLocationForOpportunityAsync(opportunityId, opportunityName);
		}

		public async Task CreateLocationsForOpportunityAsync(string opportunityId, string opportunityName, IEnumerable<string> locations)
		{
			await oneDriveLinkService.EnsureChannelFoldersForOpportunityExistAsync(opportunityName, locations);
			await sharePointLocationRepository.DeleteTemporaryLocationForOpportunityAsync(opportunityName);
			await sharePointLocationRepository.CreateLocationsForOpportunityAsync(opportunityId, opportunityName, locations);
			await oneDriveLinkService.SubscribeToFormalProposalChangesAsync(opportunityName);
		}
	}
}