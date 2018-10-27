// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore.Authorization;
using ApplicationCore.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationCore.Models
{
    public class RoleMappingModel
    {
        /// <summary>
        /// Role mapping identifier
        /// </summary>
        /// <value>Unique ID to identify the model data</value>
        [JsonProperty("id", Order = 1)]
        public string Id { get; set; }

        /// <summary>
        /// AD Group display name
        /// </summary>
        [JsonProperty("adGroupName", Order = 2)]
        public string AdGroupName { get; set; }

        /// <summary>
        /// Role name 
        /// </summary>
        [JsonProperty("role", Order = 3)]
        public RoleModel Role { get; set; }

        /// <summary>
        /// Permissions 
        /// </summary>
        //[JsonConverter(typeof(PermissionsConverter))]
        [JsonProperty("permissions", Order = 4)]
        public IList<PermissionModel> Permissions { get; set; }

        /// <summary>
        /// Represents the empty client. This field is read-only.
        /// </summary>
        public static RoleMappingModel Empty
        {
            get => new RoleMappingModel
            {
                Id = String.Empty,
                AdGroupName = String.Empty,
                Role = RoleModel.Empty,
                Permissions = new List<PermissionModel>()
            };
        }
    }
}
