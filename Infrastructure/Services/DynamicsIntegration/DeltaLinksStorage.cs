// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore.Interfaces;
using System.Collections.Generic;

namespace Infrastructure.Services
{
	public class DeltaLinksStorage : IDeltaLinksStorage
	{
		public IDictionary<string, string> OpportunityDeltaLinks { get; set; } = new Dictionary<string, string>();
		public string ProposalManagerDeltaLink { get; set; }
	}
}
