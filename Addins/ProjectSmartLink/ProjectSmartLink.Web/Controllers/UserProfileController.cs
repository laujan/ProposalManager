// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectSmartLink.Service;

namespace ProjectSmartLink.Web.Controllers
{
	[Authorize]
    public class UserProfileController : Controller
    {
        private readonly IUserProfileService _userProfileService;
        public UserProfileController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        [HttpGet]
        [Route("api/UserProfile")]
        public IActionResult GetUserProfile()
        {
            var retValue = _userProfileService.GetCurrentUser();
            return Ok(retValue);
        }
    }
}