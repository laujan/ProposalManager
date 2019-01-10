// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System.Threading.Tasks;

namespace ApplicationCore.Interfaces.SmartLink
{
    public interface IDocumentIdService
    {
        Task ActivateForSite(string site);
    }
}