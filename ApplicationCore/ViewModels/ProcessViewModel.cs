﻿// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ApplicationCore.Entities;
using ApplicationCore.ViewModels;
using ApplicationCore.Serialization;

namespace ApplicationCore.ViewModels
{
    public class ProcessViewModel : ProcessTypeViewModel
    {
        [JsonProperty("order", Order = 5)]
        public string Order { get; set; }
        [JsonProperty("daysEstimate", Order = 6)]
        public string DaysEstimate { get; set; }
        [JsonConverter(typeof(StatusConverter))]
        [JsonProperty("status", Order = 7)]
        public ActionStatus Status { get; set; }
        [JsonProperty("processnumber", Order = 8)]
        public int ProcesNumber { get; set; }
        [JsonProperty("groupnumber", Order = 9)]
        public int GroupNumber { get; set; }
        /// <summary>
        /// Represents the empty user profile. This field is read-only.
        /// </summary>
        public static ProcessViewModel Empty
        {
            get => new ProcessViewModel
            {
               Order = string.Empty,
               DaysEstimate = string.Empty,
               Status = ActionStatus.NotStarted,
               ProcesNumber=0,
               GroupNumber=0,
            };
        }
    }
}
