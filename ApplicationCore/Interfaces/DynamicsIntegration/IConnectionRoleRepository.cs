// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System.Collections.Generic;

namespace ApplicationCore.Interfaces
{
	public interface IConnectionRoleRepository
	{
		IReadOnlyDictionary<string, string> ConnectionRoles { get; }
	}
}