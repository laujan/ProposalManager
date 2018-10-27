// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using SmartLink.Entity;

namespace SmartLink.Service
{
    public interface IUserProfileService
    {
        UserProfile GetCurrentUser();
    }
}
