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
	public class UserRepository : IUserRepository
	{
		private readonly Dynamics365Configuration dynamicsConfiguration;

		public UserRepository(
			IConfiguration configuration,
			IDynamicsClientFactory dynamicsClientFactory)
		{
			dynamicsConfiguration = new Dynamics365Configuration();
			configuration.Bind(Dynamics365Configuration.ConfigurationName, dynamicsConfiguration);
			Users = new UserDataAccessor(dynamicsConfiguration, dynamicsClientFactory);
		}

		public UserDataAccessor Users { get; private set; }
	}
}