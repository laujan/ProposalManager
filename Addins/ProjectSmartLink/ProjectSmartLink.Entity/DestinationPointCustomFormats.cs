// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectSmartLink.Entity
{
    public class DestinationPointCustomFormats
    {
        [Key]
        public Guid CustomFormatId { get; set; }
        [Key]
        public Guid DestinationPointId { get; set; }
        [NotMapped]
        [JsonIgnore]
        public DestinationPoint DestinationPoint { get; set; }
        [NotMapped]
        public CustomFormat CustomFormat { get; set; }
    }
}
