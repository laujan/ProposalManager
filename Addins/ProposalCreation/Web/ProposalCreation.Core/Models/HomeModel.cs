// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System.Collections.Generic;

namespace ProposalCreation.Core.Models
{
	public class HomeModel
	{
		public IEnumerable<ResourceItem> Resources { get; set; }
    }

	public class ResourceItem
	{
		public string Key { get; set; }
		public string Value { get; set; }
	}
}
