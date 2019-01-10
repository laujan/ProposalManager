// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore.Artifacts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Interfaces
{
    public interface IAddInHelper
    {
        Task<StatusCodes> CallAddInWebhookAsync(Opportunity opportunity, string requestId = "");
        Task<StatusCodes> ActivateDocumentId(Opportunity opportunity, string requestId = "");
    }
}
