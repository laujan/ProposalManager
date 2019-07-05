// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;

namespace ApplicationCore.Interfaces
{
    public interface IDashboardAnalysis
    {
        int GetDateDifference(DateTimeOffset startDate, DateTimeOffset endDate, DateTimeOffset opportunityStarDate);
    }
}
