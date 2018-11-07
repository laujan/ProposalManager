// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationCore;
using ApplicationCore.Artifacts;
using ApplicationCore.Entities;
using ApplicationCore.Authorization;

namespace ApplicationCore.Interfaces
{
    public interface IAuthorizationService
    {
        Task<StatusCodes> CheckAccessAsync(List<Permission> permissionsRequested, string requestId = "");
        Task<StatusCodes> CheckAdminAccsessAsync(string requestId = "");
        Task<StatusCodes> CheckAccessFactoryAsync(PermissionNeededTo action, string requestId = "");
        Task<bool> CheckAccessInOpportunityAsync(Opportunity opportunity, PermissionNeededTo access, string requestId = "");
        void SetGranularAccessOverride(bool v);
        bool GetGranularAccessOverride();
    }
}