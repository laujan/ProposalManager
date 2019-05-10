// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ApplicationCore.Artifacts;
using ApplicationCore.Entities;
using ApplicationCore.Authorization;
using Newtonsoft.Json.Linq;

namespace ApplicationCore.Interfaces
{
    public interface IOpportunityFactory : IArtifactFactory<Opportunity>
    {
    
        Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "");

        Task<Opportunity> UpdateWorkflowAsync(Opportunity opportunity, string requestId = "");

        Task<Opportunity> MoveTempFileToTeamAsync(Opportunity opportunity, string requestId = "");
    }
}
