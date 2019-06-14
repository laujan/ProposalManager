/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

// General settings
export const appUri = '<APP_URI>';
// This Section is Required to be updated before the initial publish to Azure.
export const clientId = '<CLIENT_ID>'; //Registered Application Id from apps.dev.microsoft.com.
export const redirectUri = appUri + "/"; //Redircet Url used at authentication.
export const instanceId = 'https://login.microsoftonline.com/';
export const graphScopes = ["offline_access", "profile", "User.ReadBasic.All", "mail.send"]; //User scopes defined at app registration.
export const webApiScopes = ["api://<CLIENT_ID>/access_as_user"];// web Api scope generated at app registration from apps.dev.microsoft.com.
export const authority = "https://login.microsoftonline.com/<TENANT_ID>"; // Null for login as common (multi-tenant also) eg. https://login.microsoftonline.com/common/oauth2/v2.0/authorize

//No need  to update anything bellow these are placeholders
const generalProposalManagementTeam = ""; //The Proposal Manager general team name that contains all the administration functionality.
const teamsAppInstanceId = ""; //Id of the Proposal Manager application instaled in teams.
const localStorePrefix = "env1_"; //Local Store Prefix.
const teamsAppName = ""; //The short Name specified in the appllication manifest file.
const reportId = ""; //PowerBI Report Id.
const workspaceId = ""; //PowerBI WorkSpace Id.
const logEnabled = false; //Client-side logging.

export const appSettingsObject = {
    generalProposalManagementTeam,
    teamsAppInstanceId,
    localStorePrefix,
    teamsAppName,
    reportId,
    workspaceId,
    logEnabled
};

export default appSettingsObject;

// Template tab enabled based on this flag in Configuration page
export const isTemplateEnabled = false;