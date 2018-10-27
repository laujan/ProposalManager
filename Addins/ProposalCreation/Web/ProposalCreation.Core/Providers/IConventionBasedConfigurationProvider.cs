// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

namespace ProposalCreation.Core.Providers
{

	public interface IConventionBasedConfigurationProvider<T> where T : new()
	{
		T Configuration { get; }
	}

}