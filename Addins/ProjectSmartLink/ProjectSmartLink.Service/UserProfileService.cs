// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ProjectSmartLink.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace ProjectSmartLink.Service
{
    public class UserProfileService : IUserProfileService
    {
		private readonly IHttpContextAccessor httpContextAccessor;
		public UserProfileService(IHttpContextAccessor httpContextAccessor)
		{
			this.httpContextAccessor = httpContextAccessor;
		}

        public UserProfile GetCurrentUser()
        {
            var currentUserProfile = new UserProfile();

            if (httpContextAccessor.HttpContext != null)
            {
				var nameClaim = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(o => o.Type == "name");
                if (nameClaim != null)
                {
                    currentUserProfile.Username = nameClaim.Value;
                }
            }
            return currentUserProfile;
        }
    }
}
