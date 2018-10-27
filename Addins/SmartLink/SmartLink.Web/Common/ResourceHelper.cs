// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using SmartLink.Web.ViewModel;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading;

namespace SmartLink.Web.Common
{
    public class ResourceHelper
    {
        public static IEnumerable<ResourceItem> GetResourceItems()
        {
            var rm = new ResourceManager("SmartLink.Web.Resources.Resource", typeof(ResourceHelper).Assembly);

            var resourceSet = rm.GetResourceSet(Thread.CurrentThread.CurrentCulture, true, false);

            if (resourceSet == null)
            {
                resourceSet = rm.GetResourceSet(CultureInfo.InvariantCulture, true, false);
            }

            var items = resourceSet.OfType<DictionaryEntry>();

            return items.Select(x => new ResourceItem() { Key = x.Key.ToString(), Value = System.Web.HttpUtility.JavaScriptStringEncode(x.Value.ToString()) });
        }
    }
}