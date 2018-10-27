// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ApplicationCore.Entities;


namespace ApplicationCore.Interfaces
{
    public interface IPowerBIService
    {
        Task<String> GenerateTokenAsync(string requestId = "");
    }
}
