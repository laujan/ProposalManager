// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore.Entities;
using System.Collections.Generic;
using System.IO;

namespace ApplicationCore.Interfaces
{
    public interface IWordParser
    {
        IList<DocumentSection> RetrieveTOC(Stream fileStream, string requestId = "");
    }
}