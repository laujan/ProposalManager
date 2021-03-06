﻿// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApplicationCore.Models
{
    public class RoleModel
    {
        public RoleModel()
        {
            Id = String.Empty;
            DisplayName = String.Empty;
            AdGroupName = String.Empty;
        }

        /// <summary>
        /// Role identifier
        /// </summary>
        /// <value>Unique ID to identify the model data</value>
        [JsonProperty("id", Order = 1)]
        public string Id { get; set; }

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

        /// <summary>
        /// Role permissions
        /// </summary>
        //WAVE-4 GENERIC ACCELERATOR Change : start
        [JsonProperty("permissions", Order = 4)]
        public IList<PermissionModel> UserPermissions { get; set; }

        /// <summary>
        /// Role teamsMembership
        /// </summary>
        [JsonProperty("teamsMembership", Order = 5)]
        public TeamsMembership TeamsMembership { get; set; }
        //WAVE-4 GENERIC ACCELERATOR Change : end

        /// <summary>
        /// Role empty object
        /// </summary>
        public static RoleModel Empty
        {
            get => new RoleModel
            {
                Id = String.Empty,
                DisplayName = String.Empty,
                AdGroupName = String.Empty,
                TeamsMembership = TeamsMembership.None,
                UserPermissions = new List<PermissionModel>()
            };
        }
    }
}
