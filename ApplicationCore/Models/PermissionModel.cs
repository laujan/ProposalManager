﻿// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace ApplicationCore.Models
{
    public class PermissionModel
    {
        /// <summary>
        /// Roles identifier
        /// </summary>
        /// <value>Unique ID to identify the model data</value>
        [JsonProperty("id", Order = 1)]
        public string Id { get; set; }

        /// <summary>
        /// Category display name
        /// </summary>
        [JsonProperty("name", Order = 2)]
        public string Name { get; set; }
        public static PermissionModel Empty
        {
            get => new PermissionModel
            {
                Id = string.Empty,
                Name = string.Empty
            };
        }
    }
}
