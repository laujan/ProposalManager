// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Helpers
{
    public class SharePointListsSchemaHelper
    {
        public static string CategoriesJsonSchema(string displayName)
        {
           string json = @"{
              'displayName': '" + displayName + @"',
              'columns': [
                {
                  'name': 'Name',
                  'text': {}
                }
              ]
            }";
            return json;
        }
        public static string TasksJsonSchema(string displayName)
        {
            string json = @"{
              'displayName': '" + displayName + @"',
              'columns': [
                {
                  'name': 'Name',
                  'text': {}
                }
              ]
            }";
            return json;
        }
        public static string IndustryJsonSchema(string displayName)
        {
            string json = @"{
              'displayName': '" + displayName + @"',
              'columns': [
                {
                  'name': 'Name',
                  'text': {}
                }
              ]
            }";
            return json;
        }
        public static string RegionsJsonSchema(string displayName)
        {
            string json = @"{
              'displayName': '" + displayName + @"',
              'columns': [
                {
                  'name': 'Name',
                  'text': {}
                }
              ]
            }";
            return json;
        }
        public static string OpportunitiesJsonSchema(string displayName)
        {
            string json = @"{
              'displayName': '" + displayName + @"',
              'columns': [
                {
                  'name': 'OpportunityId',
                  'text': {},
                  'indexed': true
                },
                {
                  'name': 'Name',
                  'text': {},
                  'indexed': true
                },
                {
                  'name': 'OpportunityState',
                  'text': {}
                },
                {
                  'name': 'OpportunityObject',
                  'text': {'allowMultipleLines': true}
                },
                {
                  'name': 'TemplateLoaded',
                  'text': {},
                },
                {
                  'name': 'Reference',
                  'text': {},
                  'indexed': true
                }
              ]
            }";
            return json;
        }

        public static string PermissionJsonSchema(string displayName)
        {
            string json = @"{
              'displayName': '" + displayName + @"',
              'columns': [
                {
                  'name': 'Name',
                  'text': {}
                }
              ]
            }";
            return json;
        }
        public static string RoleJsonSchema(string displayName)
        {
            string json = @"{
              'displayName': '" + displayName + @"',
              'columns': [
                {
                  'name': 'AdGroupName',
                  'text': {}
                },
                {
                  'name': 'Role',
                  'text': {}
                },
                {
                  'name': 'TeamsMembership',
                  'text': {}
                },
                {
                  'name': 'Permissions',
                  'text': {'allowMultipleLines': true}
                }
              ]
            }";
            return json;
        }
        public static string GroupJsonSchema(string displayName)
        {
            string json = @"{
              'displayName': '" + displayName + @"',
              'columns': [
                {
                  'name': 'GroupName',
                  'text': {}
                },
                {
                  'name': 'Process',
                  'text': {'allowMultipleLines': true}
                }
              ]
            }";
            return json;
        }
        public static string TemplatesJsonSchema(string displayName)
        {
            string json = @"{
              'displayName': '" + displayName + @"',
              'columns': [
                {
                  'name': 'TemplateName',
                  'text': {}
                },
                {
                  'name': 'Description',
                  'text': {}
                },
                {
                  'name': 'LastUsed',
                  'dateTime': {'format': 'dateOnly'}
                },
                {
                  'name': 'CreatedBy',
                  'text': {'allowMultipleLines': true}
                },
                {
                  'name': 'ProcessList',
                  'text': {'allowMultipleLines': true}
                },
                {
                  'name': 'DefaultTemplate',
                  'text': {}
                }
              ]
            }";
            return json;
        }
        public static string WorkFlowItemsJsonSchema(string displayName)
        {
            string json = @"{
              'displayName': '" + displayName + @"',
              'columns': [
                {
                  'name': 'ProcessStep',
                  'text': {}
                },
                {
                  'name': 'Channel',
                  'text': {}
                },
                {
                  'name': 'ProcessType',
                  'text': {}
                },
                {
                  'name': 'RoleId',
                  'text': {}
                },
                {
                  'name': 'RoleName',
                  'text': {}
                }
              ]
            }";
            return json;
        }
        public static string DashboardJsonSchema(string displayName)
        {
            string json = @"{
              'displayName': '" + displayName + @"',
              'columns': [
                {
                  'name': 'CustomerName',
                  'text': {}
                },
                {
                  'name': 'OpportunityID',
                  'text': {}
                },
                {
                  'name': 'Status',
                  'text': {}
                },
                {
                  'name': 'StartDate',
                  'dateTime': {}
                },
                {
                  'name': 'TargetCompletionDate',
                  'dateTime': {}
                },           
                {
                  'name': 'OpportunityName',
                  'text': {},
                  'indexed': true
                },
                {
                  'name': 'TotalNoOfDays',
                  'number': {},
                  'defaultValue': { 'value': '0' }
                },
                {
                  'name': 'ProcessNoOfDays',
                  'text': {'allowMultipleLines': true}
                },
                {
                  'name': 'ProcessEndDates',
                  'text': {'allowMultipleLines': true}
                },
                {
                  'name': 'ProcessLoanOfficers',
                  'text': {'allowMultipleLines': true}
                }
              ]
            }";
            return json;
        }
        public static string OpportunityMetaDataJsonSchema(string displayName)
        {
            string json = @"{
              'displayName': '" + displayName + @"',
              'columns': [
                {
                  'name': 'FieldName',
                  'text': {}
                },
                {
                  'name': 'FieldType',
                  'text': {}
                },
                {
                  'name': 'FieldScreen',
                  'text': {}
                },
                {
                  'name': 'FieldValue',
                  'text': {'allowMultipleLines': true}
                }
              ]
            }";
            return json;
        }

        public static string AuditJsonSchema(string displayName)
        {
            string json = @"{
              'displayName': '" + displayName + @"',
              'columns': [
                {
                  'name': 'Log',
                  'text': {'allowMultipleLines': true}
                },
                {
                  'name': 'User',
                  'text': {}
                },
                {
                  'name': 'Action',
                  'text': {}
                },
                {
                  'name': 'Controller',
                  'text': {}
                },
                {
                  'name': 'Method',
                  'text': {},
                }
              ]
            }";
            return json;
        }
    }

    public enum ListSchema
    {
        OpportunitiesListId,
        ProcessListId,
        RoleListId,
        GroupsListId,
        TemplateListId,
        Permissions,
        DashboardListId,
        OpportunityMetaDataId,
        TasksListId,
        AuditListId
    }
}
