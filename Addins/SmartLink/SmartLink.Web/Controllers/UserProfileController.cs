// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using SmartLink.Service;
using System.Web.Http;

namespace SmartLink.Web.Controllers
{
    [APIAuthorize]
    public class UserProfileController : ApiController
    {
        protected readonly IUserProfileService _userProfileService;
        public UserProfileController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        [HttpGet]
        [Route("api/UserProfile")]
        public IHttpActionResult GetUserProfile()
        {
            var retValue = _userProfileService.GetCurrentUser();
            return Ok(retValue);
        }
    }
}