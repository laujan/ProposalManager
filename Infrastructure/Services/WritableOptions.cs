// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using ApplicationCore.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        private readonly IHostingEnvironment _environment;
        private readonly IOptionsMonitor<T> _options;
        private readonly string _section;
        private readonly string _file;

        public WritableOptions(
            IHostingEnvironment environment,
            IOptionsMonitor<T> options,
            string section,
            string file)
        {
            _environment = environment;
            _options = options;
            _section = section;
            _file = file;
        }

        public T Value => _options.CurrentValue;
        public T Get(string name) => _options.Get(name);

        public StatusCodes UpdateAsync(string key, string value, string requestId = "")
        {
            var fileProvider = _environment.ContentRootFileProvider;
            var fileInfo = fileProvider.GetFileInfo(_file);
            var physicalPath = fileInfo.PhysicalPath;

            var jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(physicalPath));
            if(jObject.TryGetValue(_section, out JToken section))
            {
                var sectionObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(section.ToString());
                sectionObject[key] = value;
                jObject[_section] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
                File.WriteAllText(physicalPath, JsonConvert.SerializeObject(jObject, Formatting.Indented));
                return StatusCodes.Status200OK;
            }
            else
            {

                return StatusCodes.Status400BadRequest;
            }
            
        }
    }
}
