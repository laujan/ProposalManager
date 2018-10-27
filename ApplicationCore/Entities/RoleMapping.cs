// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using ApplicationCore.Authorization;
using ApplicationCore.Serialization;

namespace ApplicationCore.Entities
{
    public class RoleMapping : BaseEntity<RoleMapping>
    {

        /// <summary>
        /// AD Group display name
        /// </summary>
        [JsonProperty("adGroupName", Order = 2)]
        public string AdGroupName { get; set; }

        /// <summary>
        /// Role name 
        /// </summary>
        [JsonProperty("role", Order = 3)]
        public Role Role { get; set; }

        /// <summary>
        /// Permissions 
        /// </summary>
        [JsonProperty("permissions", Order = 4)]
        public IList<Permission> Permissions { get; set; }


        /// <summary>
        /// Represents the empty client. This field is read-only.
        /// </summary>
        public static RoleMapping Empty
        {
            get => new RoleMapping
            {
                Id = String.Empty,
                AdGroupName = String.Empty,
                Role = Role.Empty,
                Permissions = new List<Permission>()
            };
        }
    }
}
