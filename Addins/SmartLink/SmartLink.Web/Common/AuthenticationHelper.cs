// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System.Threading.Tasks;

namespace SmartLink.Web.Common
{
    public class AuthenticationHelper
    {
        public static string token;

        public static string consentUrl;

        public static string sharePointToken;

        public static async Task<string> AcquireTokenAsync()
        {
            return token;
        }

        public static async Task<string> AcquireSharePointTokenAsync()
        {
            return sharePointToken;
        }
    }
}