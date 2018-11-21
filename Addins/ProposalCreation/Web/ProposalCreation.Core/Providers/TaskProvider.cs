using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProposalCreation.Core.Helpers;
using ProposalCreation.Core.Interfaces;
using ProposalCreation.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProposalCreation.Core.Providers
{
    public class TaskProvider : ITaskProvider
    {

        private readonly IGraphSdkHelper httpHelper;

        public TaskProvider(IGraphSdkHelper httpHelper, IRootConfigurationProvider rootConfigurationProvider)
        {
            this.httpHelper = httpHelper;
            var appOptions = rootConfigurationProvider.GeneralConfiguration;

            ProposalManagerApiUrl = appOptions.ProposalManagerApiUrl;
        }

        public string ProposalManagerApiUrl { get; }

        public async Task<IEnumerable<string>> GetTasksAsync()
        {
            try
            {
                var uri = $"{ProposalManagerApiUrl}/api/Tasks";
                var client = await httpHelper.GetProposalManagerWebClientAsync();
                var response = await client.GetAsync(uri);
                return from t in JsonConvert.DeserializeObject<JArray>(await response.Content.ReadAsStringAsync())
                       select t["name"].ToString();
            }
            catch(Exception e)
            {
                throw e;
            }
        }
    }
}