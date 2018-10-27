// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.Extensions.Configuration;

namespace ProposalCreation.Core.Providers
{
	public class ConventionBasedConfigurationProvider<T> : IConventionBasedConfigurationProvider<T> where T : new()
	{
		public ConventionBasedConfigurationProvider(IConfiguration configuration) => PerformBindings(configuration);

		public T Configuration { get; private set; } = new T();

		private void PerformBindings(IConfiguration configuration) => configuration.Bind(
				typeof(T).Name
					.Replace("Configuration", string.Empty)
					.Replace("Options", string.Empty),
				Configuration);
	}

}