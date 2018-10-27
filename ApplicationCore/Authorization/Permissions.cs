// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationCore.Authorization
{
    /// <summary>
    /// Defines the permissions on the system
    /// Note: This type must use a custom Json serializer as in other similar types: [JsonConverter(typeof(PermissionsConverter))]
    /// </summary>
    public enum PermissionNeededTo
    {
        Admin,
        Create,
        Read,
        ReadAll,
        Write,
        WriteAll,
        DealTypeWrite,
        TeamWrite,
        ReadPartial,
        WritePartial
    }

    public enum Access
    {
        Opportunity_Create,
        Opportunities_Read_All,
        Opportunities_ReadWrite_All,
        Opportunity_ReadWrite_All,
        Opportunity_Read_All,
        Administrator,
        Opportunity_ReadWrite_Dealtype,
        Opportunity_ReadWrite_Team,
        Opportunity_ReadWrite_Partial,
        Opportunity_Read_Partial
    }
}
