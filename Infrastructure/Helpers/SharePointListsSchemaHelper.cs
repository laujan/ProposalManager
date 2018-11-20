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
                  'name': 'LoanOfficer',
                  'text': {},
                  'indexed': true
                },
                {
                  'name': 'RelationshipManager',
                  'text': {},
                  'indexed': true
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
        public static string RoleMappingsJsonSchema(string displayName)
        {
            string json = @"{
              'displayName': '" + displayName + @"',
              'columns': [
                {
                  'name': 'ADGroupName',
                  'text': {}
                },
                {
                  'name': 'Role',
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
        public static string RoleJsonSchema(string displayName)
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
                  'text': {},
                  'indexed': true
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
                  'name': 'ComplianceRewiewStartDate',
                  'dateTime': {}
                },
                {
                  'name': 'ComplianceRewiewCompletionDate',
                  'dateTime': {}
                },
                {
                  'name': 'CreditCheckStartDate',
                  'dateTime': {}
                },
                {
                  'name': 'CreditCheckCompletionDate',
                  'dateTime': {}
                },
                {
                  'name': 'RiskAssesmentStartDate',
                  'dateTime': {}
                },
                {
                  'name': 'RiskAssesmentCompletionDate',
                  'dateTime': {}
                },
                {
                  'name': 'FormalProposalStartDate',
                  'dateTime': {}
                },
                {
                  'name': 'FormalProposalEndDate',
                  'dateTime': {}
                },
                {
                  'name': 'StatusChangedDate',
                  'dateTime': {}
                },
                {
                  'name': 'OpportunityEndDate',
                  'dateTime': {}
                },
                {
                  'name': 'OpportunityName',
                  'text': {}
                },
                {
                  'name': 'LoanOfficer',
                  'text': {}
                },
                {
                  'name': 'RelationshipManager',
                  'text': {}
                },
                {
                  'name': 'TotalNoOfDays',
                  'number': {},
                  'defaultValue': { 'value': '1' }
                },
                {
                  'name': 'CreditCheckNoOfDays',
                  'number': {},
                  'defaultValue': { 'value': '0' }
                },
                {
                  'name': 'ComplianceReviewNoOfDays',
                  'number': {},
                  'defaultValue': { 'value': '0' }
                },
                {
                  'name': 'FormalProposalNoOfDays',
                  'number': {},
                  'defaultValue': { 'value': '0' }
                },
                {
                  'name': 'RiskAssessmentNoOfDays',
                  'number': {},
                  'defaultValue': { 'value': '0' }
                }
              ]
            }";
            return json;
        }
    }

    public enum ListSchema
    {
        CategoriesListId,
        IndustryListId,
        OpportunitiesListId,
        ProcessListId,
        RegionsListId,
        RoleListId,
        RoleMappingsListId,
        TemplateListId,
        Permissions,
        DashboardListId,
        TasksListId
    }
}
