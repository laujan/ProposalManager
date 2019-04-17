using Microsoft.Crm.Sdk.Messages;
using Microsoft.Uii.Common.Entities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Microsoft.Xrm.Tooling.PackageDeployment.CrmPackageExtentionBase;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.Web.Security.Membership;

namespace ProposalManager
{
    /// <summary>
    /// Import package starter frame. 
    /// </summary>
    [Export(typeof(IImportExtensions))]
    public class PackageTemplate : ImportExtension
    {

        #region Constants

        private const string ProposalManagerApplicationName = "Proposal Manager Application";

        // The following values have been retrieved from the documentation:
        // https://docs.microsoft.com/en-us/dynamics365/customer-engagement/developer/entities/serviceendpoint
        private const int WebhookContractEnumValue = 8;
        private const int WebhookKeyAuthTypeEnumValue = 4;
        // Enum extracted from 
        //https://docs.microsoft.com/en-us/dynamics365/customer-engagement/developer/entities/sdkmessageprocessingstep#BKMK_Mode
        private enum ExecutionMode
        {
            Synchronous = 0,
            Asynchronous = 1
        }

        private const int OpportunityObjectTypeCode = 3;
        private const int ConnectionObjectTypeCode = 3234;

        private const int PostOperationStage = 40;

        #endregion

        #region Properties

        private Lazy<string> TenantDomain { get; }
        private Lazy<string> BusinessUnit { get; }
        private Lazy<Guid> BusinessUnitId { get; }
        private Lazy<Guid> CreateMessageId { get; }
        private Lazy<Guid> ProposalManagerApplicationId { get; }
        private Lazy<string> SharePointDomain { get; }
        private Lazy<string> ProposalManagerSharePointSiteName { get; }
        private Lazy<string> DriveName { get; }
        private Lazy<string> ProposalManagerApplicationUrl { get; }

        private IDictionary<string, PrivilegeDepth> RequiredPrivileges => new Dictionary<string, PrivilegeDepth>
        {
            { "prvReadAccount", PrivilegeDepth.Global },
            { "prvReadConnectionRole", PrivilegeDepth.Global },
            { "prvCreateSharePointDocumentLocation", PrivilegeDepth.Global },
            { "prvReadSharePointDocumentLocation", PrivilegeDepth.Global },
            { "prvCreateSharePointSite", PrivilegeDepth.Global },
            { "prvReadSharePointSite", PrivilegeDepth.Global },
            { "prvReadUser", PrivilegeDepth.Global }
        };

        private Dictionary<string, string> ValuesToReplace { get; } = new Dictionary<string, string>();

        #endregion

        #region Initialization

        public PackageTemplate() : base()
        {
            TenantDomain = new Lazy<string>(() => (string)RuntimeSettings["TenantDomain"]);
            BusinessUnit = new Lazy<string>(() => (string)RuntimeSettings["BusinessUnit"]);
            BusinessUnitId = new Lazy<Guid>(() => GetIdByName("businessunit", BusinessUnit.Value));
            CreateMessageId = new Lazy<Guid>(() => GetIdByName("sdkmessage", "Create"));
            ProposalManagerApplicationId = new Lazy<Guid>(() => Guid.Parse((string)RuntimeSettings["ProposalManagerApplicationId"]));
            SharePointDomain = new Lazy<string>(() => (string)RuntimeSettings["SharePointDomain"]);
            ProposalManagerSharePointSiteName = new Lazy<string>(() => (string)RuntimeSettings["ProposalManagerSharePointSiteName"]);
            DriveName = new Lazy<string>(() => (string)RuntimeSettings["DriveName"]);
            ProposalManagerApplicationUrl = new Lazy<string>(() => (string)RuntimeSettings["ProposalManagerApplicationUrl"]);
        }

        private Guid GetIdByName(string entityName, string recordName) => (Guid)CrmSvc.GetEntityDataBySearchParams(entityName, new List<CrmServiceClient.CrmSearchFilter>
            {
                new CrmServiceClient.CrmSearchFilter
                {
                    SearchConditions = new List<CrmServiceClient.CrmFilterConditionItem>
                    {
                        new CrmServiceClient.CrmFilterConditionItem
                        {
                            FieldName = "name",
                            FieldOperator = ConditionOperator.Equal,
                            FieldValue = recordName
                        }
                    }
                }
            }, CrmServiceClient.LogicalSearchOperator.None, new List<string> { $"{entityName}id" }).Single().Value[$"{entityName}id"];

        private Guid GetBusinessUnitId(string businessUnitName) => GetIdByName("businessunit", businessUnitName);

        #endregion

        #region Unused hooks

        public override void InitializeCustomExtension() { }

        public override bool BeforeImportStage() => true;

        public override ApplicationRecord BeforeApplicationRecordImport(ApplicationRecord app) => app;

        public override void RunSolutionUpgradeMigrationStep(string solutionName, string oldVersion, string newVersion, Guid oldSolutionId, Guid newSolutionId) => base.RunSolutionUpgradeMigrationStep(solutionName, oldVersion, newVersion, oldSolutionId, newSolutionId);

        #endregion

        public override bool AfterPrimaryImport()
        {
            try
            {
                ValuesToReplace.Add("organizationApiUri", new Uri(CrmSvc.CrmConnectOrgUriActual.ToString()).GetLeftPart(UriPartial.Authority));
                ValuesToReplace.Add("oneDriveWebhookSecret", GenerateSecret());
                SetUpPermissions();
                SetUpSharePointIntegration();
                SetUpWebhooks();
                PersistConfiguration();
                return true;
            }
            catch (Exception e)
            {
                throw new Exception($@"Message: {e.Message}
StackTrace: {e.StackTrace}");
            }
        }

        #region Permissions

        private Guid? GetExistingUser()
        {
            var users = CrmSvc.GetEntityDataBySearchParams("systemuser", new List<CrmServiceClient.CrmSearchFilter>
            {
                new CrmServiceClient.CrmSearchFilter
                {
                    SearchConditions = new List<CrmServiceClient.CrmFilterConditionItem>
                    {
                        new CrmServiceClient.CrmFilterConditionItem
                        {
                            FieldName = "applicationid",
                            FieldOperator = ConditionOperator.Equal,
                            FieldValue = ProposalManagerApplicationId.Value
                        }
                    }
                }
            }, CrmServiceClient.LogicalSearchOperator.None, new List<string> { "systemuserid" });

            if (users != null)
            {
                if (users.Any())
                {
                    PackageLog.Log("PM user already created. Id: " + users.First().Value["systemuserid"].ToString());
                    return (Guid?)users.First().Value["systemuserid"];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private Guid? GetExistingRoleId()
        {
            var roles = CrmSvc.GetEntityDataBySearchParams("role", new List<CrmServiceClient.CrmSearchFilter>
            {
                new CrmServiceClient.CrmSearchFilter
                {
                    SearchConditions = new List<CrmServiceClient.CrmFilterConditionItem>
                    {
                        new CrmServiceClient.CrmFilterConditionItem
                        {
                            FieldName = "name",
                            FieldOperator = ConditionOperator.Equal,
                            FieldValue = ProposalManagerApplicationName
                        }
                    }
                }
            }, CrmServiceClient.LogicalSearchOperator.None, new List<string> { "roleid" });

            if (roles != null)
            {
                return roles.Any() ? (Guid?)roles.First().Value["roleid"] : null;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>The created role id.</returns>
        private Guid CreateRole() => CrmSvc.CreateNewRecord("role", new Dictionary<string, CrmDataTypeWrapper>
            {
                { "name", new CrmDataTypeWrapper(ProposalManagerApplicationName, CrmFieldType.String) },
                { "businessunitid", new CrmDataTypeWrapper(BusinessUnitId.Value, CrmFieldType.Lookup, "businessunit") }
            });

        /// <summary>
        /// 
        /// </summary>
        /// <returns>The created user id.</returns>
        private Guid CreateProposalManagerUser() => CrmSvc.CreateNewRecord("systemuser", new Dictionary<string, CrmDataTypeWrapper>
            {
                { "firstname", new CrmDataTypeWrapper(ProposalManagerApplicationName, CrmFieldType.String) },
                { "lastname", new CrmDataTypeWrapper("Application", CrmFieldType.String) },
                { "businessunitid", new CrmDataTypeWrapper(BusinessUnitId.Value, CrmFieldType.Lookup, "businessunit") },
                { "applicationid", new CrmDataTypeWrapper(ProposalManagerApplicationId.Value, CrmFieldType.UniqueIdentifier) },
                { "internalemailaddress", new CrmDataTypeWrapper($"{Guid.NewGuid()}@{TenantDomain.Value}", CrmFieldType.String) }
            });

        private Guid GetRoleId() => GetExistingRoleId() ?? CreateRole();

        private Dictionary<string, Dictionary<string, object>> GetPrivilegeRecords() =>
            CrmSvc.GetEntityDataBySearchParams("privilege", new List<CrmServiceClient.CrmSearchFilter>
            {
                new CrmServiceClient.CrmSearchFilter
                {
                    FilterOperator = LogicalOperator.Or,
                    SearchConditions =
                    (
                        from rp in RequiredPrivileges
                        select new CrmServiceClient.CrmFilterConditionItem
                        {
                            FieldName = "name",
                            FieldOperator = ConditionOperator.Equal,
                            FieldValue = rp.Key
                        }
                    ).ToList()
                }
            }, CrmServiceClient.LogicalSearchOperator.None, new List<string> { "privilegeid", "name" });

        private void SetUpPermissions()
        {
            if (GetExistingUser() == null)
            {
                var roleId = GetRoleId();
                var privileges = GetPrivilegeRecords();

                var addPrivilegesRoleRequest = new AddPrivilegesRoleRequest
                {
                    RoleId = roleId,
                    Privileges =
                    (
                        from p in privileges
                        select new RolePrivilege(RequiredPrivileges[(string)p.Value["name"]], (Guid)p.Value["privilegeid"], BusinessUnitId.Value)
                    ).ToArray()
                };
                CrmSvc.ExecuteCrmOrganizationRequest(addPrivilegesRoleRequest);

                var user = CreateProposalManagerUser();
                PackageLog.Log("User created...");
                PackageLog.Log(user.ToString());

                CrmSvc.Associate(
                    "systemuser",
                    user,
                    new Relationship("systemuserroles_association"),
                    new EntityReferenceCollection {
                new EntityReference("role", roleId)
                    });
            }
        }

        #endregion

        #region SharePoint

        private void SetUpSharePointIntegration()
        {
            CreateSharePointLinks();
            ValuesToReplace.Add("driveName", DriveName.Value);
        }

        private void CreateSharePointLinks()
        {
            var defaultSiteId = CrmSvc.CreateNewRecord("sharepointsite", new Dictionary<string, CrmDataTypeWrapper>
            {
                { "name", new CrmDataTypeWrapper("Default Site", CrmFieldType.String) },
                { "absoluteurl", new CrmDataTypeWrapper($"https://{SharePointDomain.Value}", CrmFieldType.String) }
            });
            var proposalManagerSiteId = CrmSvc.CreateNewRecord("sharepointsite", new Dictionary<string, CrmDataTypeWrapper>
            {
                { "name", new CrmDataTypeWrapper("Proposal Manager Site", CrmFieldType.String) },
                { "parentsite", new CrmDataTypeWrapper(defaultSiteId, CrmFieldType.Lookup, "sharepointsite") },
                { "relativeurl", new CrmDataTypeWrapper($"sites/{ProposalManagerSharePointSiteName.Value}", CrmFieldType.String) }
            });
            var proposalManagerSiteDriveId = CrmSvc.CreateNewRecord("sharepointdocumentlocation", new Dictionary<string, CrmDataTypeWrapper>
            {
                { "name", new CrmDataTypeWrapper("Proposal Manager Site Drive", CrmFieldType.String) },
                { "parentsiteorlocation", new CrmDataTypeWrapper(proposalManagerSiteId, CrmFieldType.Lookup, "sharepointsite") },
                { "relativeurl", new CrmDataTypeWrapper(DriveName.Value, CrmFieldType.String) }
            });
            CrmSvc.CreateNewRecord("sharepointdocumentlocation", new Dictionary<string, CrmDataTypeWrapper>
            {
                { "name", new CrmDataTypeWrapper("Proposal Manager Temporary Folder", CrmFieldType.String) },
                { "parentsiteorlocation", new CrmDataTypeWrapper(proposalManagerSiteDriveId, CrmFieldType.Lookup, "sharepointdocumentlocation") },
                { "relativeurl", new CrmDataTypeWrapper("TempFolder", CrmFieldType.String) }
            });
        }

        #endregion

        #region WebHooks

        /// <summary>
        /// 
        /// </summary>
        /// <param name="otc">Object Type Code of the entity.</param>
        /// <returns></returns>
        private Guid GetCreateFilterIdByOtc(int otc) => (Guid)CrmSvc.GetEntityDataBySearchParams("sdkmessagefilter", new List<CrmServiceClient.CrmSearchFilter>
            {
                new CrmServiceClient.CrmSearchFilter
                {
                    FilterOperator = LogicalOperator.And,
                    SearchConditions = new List<CrmServiceClient.CrmFilterConditionItem>
                    {
                        new CrmServiceClient.CrmFilterConditionItem
                        {
                            FieldName = "sdkmessageid",
                            FieldOperator = ConditionOperator.Equal,
                            FieldValue = CreateMessageId.Value
                        },
                        new CrmServiceClient.CrmFilterConditionItem
                        {
                            FieldName = "primaryobjecttypecode",
                            FieldOperator = ConditionOperator.Equal,
                            FieldValue = otc
                        },
                        new CrmServiceClient.CrmFilterConditionItem
                        {
                            FieldName = "secondaryobjecttypecode",
                            FieldOperator = ConditionOperator.Equal,
                            FieldValue = "none"
                        }
                    }
                }
            }, CrmServiceClient.LogicalSearchOperator.None, new List<string> { "sdkmessagefilterid" }).Single().Value["sdkmessagefilterid"];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="entityOtc"></param>
        /// <param name="executionMode"></param>
        /// <returns>The secret used to secure the webhook.</returns>
        private string SetUpWebhook(string entityName, int entityOtc, ExecutionMode executionMode)
        {
            var secret = GenerateSecret();
            var opportunitiesServiceEndpointId = CrmSvc.CreateNewRecord("serviceendpoint", new Dictionary<string, CrmDataTypeWrapper>
            {
                { "name", new CrmDataTypeWrapper($"Proposal Manager {entityName}", CrmFieldType.String) },
                { "url", new CrmDataTypeWrapper($"{ProposalManagerApplicationUrl.Value}/api/webhooks/incoming/dynamicscrm/{entityName}", CrmFieldType.String) },
                { "contract", new CrmDataTypeWrapper(WebhookContractEnumValue, CrmFieldType.Picklist) },
                { "authtype", new CrmDataTypeWrapper(WebhookKeyAuthTypeEnumValue, CrmFieldType.Picklist) },
                { "authvalue", new CrmDataTypeWrapper(secret, CrmFieldType.String) }
            });
            /*var filterId = GetCreateFilterIdByOtc(entityOtc);
            CrmSvc.CreateNewRecord("sdkmessageprocessingstep", new Dictionary<string, CrmDataTypeWrapper>
            {
                { "sdkmessageid", new CrmDataTypeWrapper(CreateMessageId.Value, CrmFieldType.Lookup, "sdkmessage") },
                { "sdkmessagefilterid", new CrmDataTypeWrapper(filterId, CrmFieldType.Lookup, "sdkmessagefilter") },
                { "plugintypeid", new CrmDataTypeWrapper(filterId, CrmFieldType.Lookup, "sdkmessagefilter") },
                { "mode", new CrmDataTypeWrapper(executionMode, CrmFieldType.Picklist) },
                { "stage", new CrmDataTypeWrapper(PostOperationStage, CrmFieldType.Picklist) }
            });
            //throw new Exception(CrmSvc.LastCrmError);*/
            return secret;
        }

        private void SetUpWebhooks()
        {
            ValuesToReplace.Add("opportunitiesSecret", SetUpWebhook("opportunity", OpportunityObjectTypeCode, ExecutionMode.Asynchronous));
            ValuesToReplace.Add("connectionsSecret", SetUpWebhook("connection", ConnectionObjectTypeCode, ExecutionMode.Synchronous));
        }

        #endregion

        private void PersistConfiguration()
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            var configuration = File.ReadAllText("appsettings.json");
            var resultingConfiguration = ValuesToReplace.Aggregate(configuration, (a, v) => a.Replace($"<{v.Key}>", v.Value));
            File.WriteAllText($"appsettings-{Guid.NewGuid()}.json", resultingConfiguration);
        }

        #region Helpers

        private string GenerateSecret() => GeneratePassword(32, 0);

        #endregion

        #region Package Properties

        public override string GetNameOfImport(bool plural) => "Proposal Manager";

        public override string GetImportPackageDataFolderName => "ProposalManager";

        public override string GetImportPackageDescriptionText => "Proposal Manager Integration";

        public override string GetLongNameOfImport => "Proposal Manager Integration";

        #endregion

    }
}