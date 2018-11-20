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
    public class Tasks : BaseEntity<Tasks>
    {
        /// <summary>
        /// Industry display name
        /// </summary>
        [JsonProperty("name", Order = 2)]
        public string Name { get; set; }

        /// <summary>
        /// Represents the empty client. This field is read-only.
        /// </summary>
        public static Tasks Empty
        {
            get => new Tasks
            {
                Id = String.Empty,
                Name = String.Empty
            };
        }
    }
}
