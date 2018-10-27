// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System.Net.Http;
using System.Threading.Tasks;

namespace ProposalCreation.Core.Helpers
{

	public interface IDaemonHelper
	{
		Task<HttpClient> GetProposalManagerAuthorizedWebClientAsync();
		Task<string> GetGraphTokenAsync();
	}

}