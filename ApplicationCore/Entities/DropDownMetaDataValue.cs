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
    public class DropDownMetaDataValue : BaseEntity<DropDownMetaDataValue>
    {
        /// <summary>
        /// dropdown type value
        /// </summary>
        /// 
        [JsonProperty("name")]
        public string Name { get; set; }

        public static DropDownMetaDataValue Empty
        {
            get => new DropDownMetaDataValue
            {
                Name = String.Empty,
                Id = String.Empty
            };
        }
    }
}
