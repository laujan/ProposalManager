// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace ApplicationCore.Entities
{
    public class Role : BaseEntity<Role>
    {

        /// <summary>
        /// Role display name
        /// </summary>
        [JsonProperty("displayName", Order = 2)]
        public string DisplayName { get; set; }
        /// <summary>
        /// Role AdgroupName
        /// </summary>
        /// 
        [JsonProperty("adGroupName", Order = 3)]
        public string AdGroupName { get; set; }
        //WAVE-4 GENERIC ACCELERATOR Change : start
        /// <summary>
        /// List of permissions
        /// </summary>
        /// 
        [JsonProperty("permissions", Order = 4)]
        public IList<Permission> Permissions { get; set; }

        /// <summary>
        /// Team membership property
        /// </summary>
        /// 
        [JsonProperty("teamsMembership", Order = 5)]
        public TeamsMembership TeamsMembership { get; set; }

        //WAVE-4 GENERIC ACCELERATOR Change : end

        /// <summary>
        /// Represents the empty client. This field is read-only.
        /// </summary>
        /// 
        public static Role Empty
        {
            get => new Role
            {
                Id = String.Empty,
                DisplayName = String.Empty,
                AdGroupName = String.Empty,
                TeamsMembership = TeamsMembership.None,
                Permissions = new List<Permission>()
            };
        }
    }
}
