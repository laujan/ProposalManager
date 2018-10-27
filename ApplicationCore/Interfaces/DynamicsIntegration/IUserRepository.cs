// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore.Helpers;

namespace ApplicationCore.Interfaces
{
	public interface IUserRepository
	{
		UserDataAccessor Users { get; }
	}
}