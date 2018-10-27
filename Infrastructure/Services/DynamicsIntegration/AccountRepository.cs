// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore;
using ApplicationCore.Helpers;
using ApplicationCore.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services
{
	public class AccountRepository : IAccountRepository
	{
		private readonly Dynamics365Configuration dynamicsConfiguration;

		public AccountRepository(IConfiguration configuration,
			IDynamicsClientFactory dynamicsClientFactory)
		{
			dynamicsConfiguration = new Dynamics365Configuration();
			configuration.Bind(Dynamics365Configuration.ConfigurationName, dynamicsConfiguration);

			Accounts = new AccountNameAccessor(dynamicsConfiguration, dynamicsClientFactory);
		}

		public AccountNameAccessor Accounts { get; private set; }
	}
}