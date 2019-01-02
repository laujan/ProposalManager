// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using SourcePointCreator.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SourcePointCreator
{
    public partial class frm : Form
    {
        private HttpClient httpClient = new HttpClient();
        private string userName = string.Empty;
        public frm()
        {
            InitializeComponent();
            grdPoints.AutoGenerateColumns = false;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            this.Enabled = false;

            // Initialize the http client
            await InitializeHttpClient();

            // Load Source Catalogs 
            await LoadCatalogs();

            this.Enabled = true;
        }

        private async Task InitializeHttpClient()
        {
            var appSettingsReader = new AppSettingsReader();
            var authority = (string)appSettingsReader.GetValue("Authority", typeof(string));
            var clientId = (string)appSettingsReader.GetValue("ClientId", typeof(string));
            var redirectUri = (string)appSettingsReader.GetValue("RedirectUri", typeof(string));
            var apiUrl = (string)appSettingsReader.GetValue("ApiUrl", typeof(string));

            var authContext = new AuthenticationContext(authority);
            
            var result = await authContext.AcquireTokenAsync(clientId, clientId, new Uri(redirectUri), new PlatformParameters(PromptBehavior.Always));

            var token = result.AccessToken;
            userName = result.UserInfo.DisplayableId;

            httpClient.BaseAddress = new Uri(apiUrl);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        private async Task LoadCatalogs()
        {
            var response = await httpClient.GetAsync("SourcePointCatalogs?external=true");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var catalogs = JsonConvert.DeserializeObject<IEnumerable<SourceCatalog>>(content);

                cboCatalog.DataSource = catalogs;
            }
            else
            {
                MessageBox.Show($"Error loading catalogs: {response.ReasonPhrase}");
                Application.Exit();
            }
        }

        private async Task LoadPoints()
        {
            var selectedCatalog = (SourceCatalog)cboCatalog.SelectedItem;

            if (selectedCatalog == null) {
                return;
            }

            var response = await httpClient.GetAsync($"SourcePointCatalog?documentId={selectedCatalog.DocumentId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var catalog = JsonConvert.DeserializeObject<SourceCatalog>(content);

                if (catalog != null)
                {
                    grdPoints.DataSource = catalog.SourcePoints;
                }
                else
                {
                    grdPoints.DataSource = null;
                }
            }
            else
            {
                MessageBox.Show($"Error loading points: {response.ReasonPhrase}");
                Application.Exit();
            }
        }

        private async void btnCreate_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtPointName.Text) &&
                !string.IsNullOrWhiteSpace(txtValue.Text))
            {
                var selectedCatalog = (SourceCatalog)cboCatalog.SelectedItem;
                var sourceType = SourceTypes.Point;
                var value = txtValue.Text;

                if (radTable.Checked)
                {
                    sourceType = SourceTypes.Table;

                    var table = JsonConvert.DeserializeObject<Table>(value);

                    var sb = new StringBuilder();

                    sb.Append("[");
                    foreach (var item in table.Header)
                    {
                        sb.Append($"\"{item}\",");
                    }
                    sb = sb.Remove(sb.Length - 1, 1);
                    sb.Append("],");

                    foreach (var item in table.Values)
                    {
                        sb.Append("[");

                        foreach (var i in item)
                        {
                            sb.Append($"\"{i}\",");
                        }
                        sb = sb.Remove(sb.Length - 1, 1);
                        sb.Append("],");
                    }
                    sb = sb.Remove(sb.Length - 1, 1);

                    var template = "{ \"image\": \"\", \"table\": {\"values\":[ " + sb.ToString() +"]}}";
                    value = template;
                } else if (radImage.Checked)
                {
                    sourceType = SourceTypes.Image;
                }

                var point = new Dictionary<string, string>
                {
                    { "Name", txtPointName.Text },
                    { "CatalogName", selectedCatalog.Name },
                    { "DocumentId", selectedCatalog.DocumentId },
                    { "RangeId", "" },
                    { "Position", "" },
                    { "NameRangeId", "" },
                    { "NamePosition", "" },
                    { "SourceType", ((int)sourceType).ToString() },
                    { "Value", value },
                    { "Created", DateTime.Now.ToShortTimeString() },
                    { "Creator", userName }
                };

                var content = new FormUrlEncodedContent(point);

                var response = await httpClient.PostAsync("SourcePoint", content);

                if (response.IsSuccessStatusCode)
                {
                    txtPointName.Text = string.Empty;
                    txtValue.Text = string.Empty;
                    await LoadPoints();
                }
                else
                {
                    MessageBox.Show($"Error creating point: {response.ReasonPhrase}");
                }
            }
        }

        private void btnCreateCatalog_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtCatalog.Text))
            {
                var newCatalog = new SourceCatalog()
                {
                    DocumentId = Guid.NewGuid().ToString(),
                    Name = txtCatalog.Text
                };

                var catalogs = (List<SourceCatalog>)cboCatalog.DataSource;
                catalogs.Add(newCatalog);

                cboCatalog.DataSource = null;
                cboCatalog.DataSource = catalogs;
                cboCatalog.DisplayMember = "Name";
                txtCatalog.Text = string.Empty;

                cboCatalog.SelectedItem = newCatalog;
            }
        }

        private async void cboCatalog_SelectedIndexChanged(object sender, EventArgs e)
        {
            await LoadPoints();
        }

        private async void grdPoints_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.ColumnIndex == 3)
            {
                var points = grdPoints.DataSource as List<SourcePoint>;
                var selectedPoint = points[e.RowIndex];
                var result = MessageBox.Show($"Do you want to delete point {selectedPoint.Name}?", "Delete Point", MessageBoxButtons.OKCancel);

                if(result == DialogResult.OK)
                {
                    await DeletePoint(selectedPoint.Id);
                  
                }
            }
        }

        private async Task DeletePoint(Guid id)
        {
            var response = await httpClient.DeleteAsync($"SourcePoint?id={id}");

            if(response.IsSuccessStatusCode)
            {
                await LoadPoints();
            }
            else
            {
                MessageBox.Show($"Error deleting point: {response.ReasonPhrase}");
            }
        }
    }
}