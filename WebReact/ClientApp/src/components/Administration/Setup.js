/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/
import React, { Component } from 'react';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { I18n, Trans } from "react-i18next";
import { Label } from 'office-ui-fabric-react/lib/Label';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
//import  appSettingsObject  from './helpers/AppSettings';

export class Setup extends Component {
    displayName = Setup.name

    constructor(props) {
        super(props);
        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;
        console.log("Setup : Constructor");
        this.state = {
            loading: true,
            isUpdateOpp: false,
            isUpdateOppMsg: false,
            updateMessageBarType: MessageBarType.success,
            updateOppMessagebarText: "nil",
            PMAddinName: "",
            appId: "",
            PMTeamName: "",
            ADGroupName: "",
            iSComponentDidMount: false,
            ProposalManagement_Misc: {
                "UserProfileCacheExpiration": 0,
                "GraphRequestUrl": "https://graph.microsoft.com/v1.0/",
                "GraphBetaRequestUrl": "https://graph.microsoft.com/beta/",
                "SetupPage": "",
                "SharePointListsPrefix": "e3_"
            },
            ProposalManagement_Team: {
                "GeneralProposalManagementTeam": "",
                "ProposalManagerAddInName": "",
                "TeamsAppInstanceId": "",
                "ProposalManagerGroupID": ""
            },
            ProposalManagement_Sharepoint: {
                "SharePointHostName": "",
                "ProposalManagementRootSiteId": "",
                "SharePointSiteRelativeName": "",
                "CategoriesListId": "",
                "TemplateListId": "",
                "RoleListId": "",
                "Permissions": "",
                "ProcessListId": "",
                "IndustryListId": "",
                "RegionsListId": "",
                "DashboardListId": "",
                "RoleMappingsListId": "",
                "OpportunitiesListId": ""
            },
            ProposalManagement_bot: {
                "BotServiceUrl": "https://smba.trafficmanager.net/amer-client-ss.msg/",
                "BotName": "Proposal Manager <tenant>",
                "BotId": "",
                "MicrosoftAppId": "",
                "MicrosoftAppPassword": "",
                "AllowedTenants": ""
            },
            ProposalManagement_BI: {
                "PBIUserName": "",
                "PBIUserPassword": "",
                "PBIApplicationId": "",
                "PBIWorkSpaceId": "",
                "PBIReportId": "",
                "PBITenantId": ""
            },
            DocumentIdActivator: {
                "WebhookAddress": "",
                "WebhookUsername": "",
                "WebhookPassword": ""
            },
            renderStep_1: false,
            renderStep_2: false,
            renderStep_3: false,
            sharepoint: false,
            misc: false,
            bot: false,
            powerbi: false,
            documentid: false,
            finish: false
        };

        this.CreateProposalManagerTeam = this.CreateProposalManagerTeam.bind(this);
        this.SetAppSetting_JsonKeys = this.SetAppSetting_JsonKeys.bind(this);
        this.CreateAdminPermissions = this.CreateAdminPermissions.bind(this);
        this.onBlurOnAettingKeys = this.onBlurOnAettingKeys.bind(this);
        this.onBlurSetPM = this.onBlurSetPM.bind(this);
        this.onBlurOnBotSettings = this.onBlurOnBotSettings.bind(this);
        this.onBlurOnBISettings = this.onBlurOnBISettings.bind(this);
        this.onBlurOnDocumentIdActivatorSettings = this.onBlurOnDocumentIdActivatorSettings.bind(this);
        this.loadDataForPermision_Process_Roles = this.loadDataForPermision_Process_Roles.bind(this);
        this.onFinish = this.onFinish.bind(this);
        this.ConfigureAppIDAndGroupID = this.ConfigureAppIDAndGroupID.bind(this);

        this.setClientSettings().then();
    }



    async componentDidMount() {
        if (this.props.userProfile.displayName.length > 0 && this.state.loading) {
            this.setState({
                loading: false
            });
            console.log("SetUp_componentDidMount State : ", this.state);
        }
    }


    async componentDidUpdate() {
        if (this.props.userProfile.displayName.length > 0 && this.state.loading) {
            this.setState({
                loading: false
            });
        }
    }

    async setClientSettings() {
        let ProposalManagement_Sharepoint = { ...this.state.ProposalManagement_Sharepoint };
        let ProposalManagement_BI = { ...this.state.ProposalManagement_BI };
        let DocumentIdActivator = { ...this.state.DocumentIdActivator };
        let ProposalManagement_bot = { ...this.state.ProposalManagement_bot };
        let ProposalManagement_Misc = { ...this.state.ProposalManagement_Misc };
        let ProposalManagement_Team = { ...this.state.ProposalManagement_Team };
        let SharepointObj = await this.getClientSettings();

        for (const key of Object.keys(SharepointObj)) {
            let value = SharepointObj[key] || this.defaultValue(key);
            if (ProposalManagement_Sharepoint.hasOwnProperty(key)) {
                ProposalManagement_Sharepoint[key] = value;
            }
            if (ProposalManagement_BI.hasOwnProperty(key)) {
                ProposalManagement_BI[key] = value;
            }
            if (DocumentIdActivator.hasOwnProperty(key)) {
                DocumentIdActivator[key] = value;
            }
            if (ProposalManagement_bot.hasOwnProperty(key)) {
                ProposalManagement_bot[key] = value;
            }
            if (ProposalManagement_Misc.hasOwnProperty(key)) {
                ProposalManagement_Misc[key] = value;
            }
            if (ProposalManagement_Team.hasOwnProperty(key)) {
                ProposalManagement_Team[key] = value;
            }
        }
        console.log("SetUp_componentDidMount : ",
            ProposalManagement_Misc, ProposalManagement_Sharepoint, ProposalManagement_bot, ProposalManagement_BI, ProposalManagement_Team, DocumentIdActivator);
        this.setState({
            ProposalManagement_Misc, ProposalManagement_Sharepoint, ProposalManagement_bot, ProposalManagement_BI, ProposalManagement_Team, DocumentIdActivator
            , loading: false
        });
    }

    delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    async getClientSettings() {
        let clientSettings = { "setupPage": "" };
        try {
            console.log("Setup_getClientSettings");
            let requestUrl = 'api/Context/GetClientSettings';
            let token = this.authHelper.getWebApiToken();
            console.log("Setup_getClientSettings token==> ", token.length);
            let data = await fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + token }
            });
            let response = await data.json();
            return response;
        } catch (error) {
            console.log("Setup_getClientSettings error: ", error.message);
            return clientSettings;
        }
    }

    showSpinnerAndMessage() {
        return (
            <div className='ms-Grid-row'>
                <div className="ms-Grid-col">
                    {
                        this.state.isUpdateOpp ?
                            <div className='ms-BasicSpinnersExample'>
                                <Spinner size={SpinnerSize.large} label={<Trans>Updating</Trans>} ariaLive='assertive' />
                            </div>
                            : ""
                    }
                </div>
                <div>
                    {
                        this.state.isUpdateOppMsg ?
                            <MessageBar messageBarType={this.state.updateMessageBarType}>
                                {this.state.updateOppMessagebarText}
                            </MessageBar>
                            : ""
                    }
                </div>
            </div>
        );
    }

    async hideMessagebar() {
        await this.delay(3500);
        this.setState({ isUpdateOpp: false, isUpdateOppMsg: false, updateOppMessagebarText: "", updateMessageBarType: MessageBarType.info });
    }

    placeholderForProposalManager() {
        let obj = {
            "SharePointHostName": "onterawe.sharepoint.com",
            "SharePointSiteRelativeName": "Give Sharepoint relative web address (eg: proposalmanager)",
            "SharePointListsPrefix": "e3_",
            "CategoriesListId": "Categories",
            "TemplateListId": "Templates",
            "RoleListId": "Roles",
            "Permissions": "Permission",
            "ProcessListId": "WorkFlow Items",
            "IndustryListId": "Industry",
            "RegionsListId": "Regions",
            "DashboardListId": "DashBoard",
            "RoleMappingsListId": "RoleMappings",
            "OpportunitiesListId": "Opportunities",
            "BotName": "Proposal Manager Bot",
            "BotId": "GUID",
            "PBIUserName": "Power BI user name",
            "PBIUserPassword": "Power BI user password",
            "PBIApplicationId": "Power BI App ID",
            "PBIWorkSpaceId": "Power BI Workspace ID",
            "PBIReportId": "Power BI Report ID",
            "PBITenantId": "Your Azure tenant ID",
            "UserProfileCacheExpiration": 0,
            "GraphRequestUrl": "https://graph.microsoft.com/v1.0/",
            "GraphBetaRequestUrl": "https://graph.microsoft.com/beta/",
            "BotServiceUrl": "https://smba.trafficmanager.net/amer-client-ss.msg/",
            "WebhookAddress": "https://<app_name>.scm.azurewebsites.net/api/triggeredwebjobs/DocumentIdActivator/run",
            "WebhookUsername": "The username to run the webjob",
            "WebhookPassword": "The username to run the webjob"
        };
        return function (key) {
            return obj[key];
        };
    }

    defaultValue(key) {
        let obj = {
            "SharePointListsPrefix": "e3_",
            "CategoriesListId": "Categories",
            "TemplateListId": "Templates",
            "RoleListId": "Roles",
            "Permissions": "Permission",
            "ProcessListId": "WorkFlow Items",
            "IndustryListId": "Industry",
            "RegionsListId": "Regions",
            "DashboardListId": "DashBoard",
            "RoleMappingsListId": "RoleMappings",
            "OpportunitiesListId": "Opportunities",
            "BotName": "Proposal Manager <tenant>",
            "UserProfileCacheExpiration": 30,
            "SharePointSiteRelativeName": "proposalmanager",
            "SharePointHostName": "<tenant>.sharepoint.com"
        };

        return obj[key] || "";

    }

    setSpinnerAndMsg(isUpdateOpp, isUpdateOppMsg, updateOppMessagebarText, updateMessageBarType = MessageBarType.info) {
        this.setState({ isUpdateOpp, isUpdateOppMsg, updateOppMessagebarText, updateMessageBarType });
    }

    async downloadJsonObject() {
        this.setState({ finish: true });
        this.setSpinnerAndMsg(true, false, "");

        try {
            let SharepointObj = await this.getClientSettings();
            let obj = { "ProposalManagement": SharepointObj };
            var data = "data:text/json;charset=utf-8," + encodeURIComponent(JSON.stringify(obj));
            let dlAnchorElem = document.getElementById('downloadFile');
            dlAnchorElem.setAttribute("href", data);
            dlAnchorElem.setAttribute("download", "appsettings_ProposalManagement.json");
            dlAnchorElem.click();
        } catch (error) {
            console.log("Setup_downloadJsonObject error: ", error.message);
        }

        this.setSpinnerAndMsg(false, false, "");
        this.setState({ finish: false });
    }

    async onFinish() {
        this.setState({ finish: true });
        this.setSpinnerAndMsg(true, false, "");
        let token = this.authHelper.getWebApiToken();
        await this.UpdateAppSettings("SetupPage", "disabled", token);
        this.setSpinnerAndMsg(false, false, "");
        this.setState({ finish: false });
    }

    async UpdateAppSettings(key, value, token) {
        try {
            console.log("SetUp_updateAppSettings");
            let requestUrl = `api/Setup/${key}/${value}`;
            let options = {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'authorization': `Bearer ${token}`
                }
            };
            let data = await fetch(requestUrl, options);
            console.log("SetUp_updateAppSettings response: ", data);
            return true;
        } catch (error) {
            console.log("SetUp_updateAppSettings error: ", error.message);
            return false;
        }
    }

    async UpdateDocumentIdActivatorSettings(key, value, token) {
        try {
            console.log("SetUp_updateDocumentIdActivatorSettings");
            let requestUrl = "api/Setup/documentid";
            let postData = {
                key: key,
                value: value
            };
        
            let options = {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'authorization': `Bearer ${token}`
                },
                body: JSON.stringify(postData)
            };
            let data = await fetch(requestUrl, options);
            console.log("SetUp_updateDocumentIdActivatorSettings response: ", data);
            return true;
        } catch (error) {
            console.log("SetUp_updateDocumentIdActivatorSettings error: ", error.message);
            return false;
        }
    }

    //Setp 1
    async CreateProposalManagerTeam() {
        // CreateProposalManagerTeam() this will be used to create the team that contains the Configuration/DashBoard/Administration.
        this.setState({ renderStep_1: true });
        this.setSpinnerAndMsg(true, false, "");

        let PMTeamName = this.state.PMTeamName || this.state.ProposalManagement_Team.GeneralProposalManagementTeam;
        let token = this.authHelper.getWebApiToken();
        try {
            console.log("Setup_CreateProposalManagerTeam", PMTeamName);
            if (PMTeamName) {
                let requestUrl = `api/Setup/CreateProposalManagerTeam/${PMTeamName}`;
                let options = {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'authorization': `Bearer ${token}`
                    }
                };
                let data = await fetch(requestUrl, options);
                if (data) {
                    console.log("Setup_CreateProposalManagerTeam response: ", data);
                    this.setSpinnerAndMsg(false, true, "Updated", MessageBarType.success);
                    await this.UpdateAppSettings("GeneralProposalManagementTeam", PMTeamName, token);
                    //setting GeneralProposalManagementTeam name in appsettings.
                    let ProposalManagement_Team = { ...this.state.ProposalManagement_Team };
                    ProposalManagement_Team.GeneralProposalManagementTeam = PMTeamName;
                    this.setState({ ProposalManagement_Team });
                } else
                    throw new Error("App creation failer, bad request");

            } else
                throw new Error("PMTeamName cannot be empty");

        } catch (error) {
            this.setSpinnerAndMsg(false, true, error.message, MessageBarType.error);
            console.log("Setup_CreateProposalManagerTeam error : ", error.message);
        }
        await this.hideMessagebar();
        this.setState({ renderStep_1: false });
    }

    //Setp 2
    async ConfigureAppIDAndGroupID() {
        // GetAppId() this is used to get the id of the application uploaded manually to the Proposal Manager Group.
        //store get app instance id..
        this.setState({ renderStep_2: true });
        this.setSpinnerAndMsg(true, false, "");

        let PMTeamName = this.state.ProposalManagement_Team.GeneralProposalManagementTeam || this.state.PMTeamName;
        let PMAddinName = this.state.PMAddinName || this.state.ProposalManagement_Team.ProposalManagerAddInName;
        let ProposalManagement_Team = { ...this.state.ProposalManagement_Team };
        ProposalManagement_Team.GeneralProposalManagementTeam = PMTeamName;
        ProposalManagement_Team.ProposalManagerAddInName = PMAddinName;
        this.setState({ ProposalManagement_Team });
        try {
            if (!PMTeamName) throw new Error("Proposal Manager Team is empty");
            console.log("Setup_ConfigureAppIDAndGroupID PMTeamName :", PMTeamName);
            let Team = await this.sdkHelper.getTeamByName(PMTeamName);
            console.log("Setup_ConfigureAppIDAndGroupID Team :", Team);
            if (Team && Team["value"].length > 0) {
                let teamId = Team["value"][0].id.toString();
                console.log("Setup_ConfigureAppIDAndGroupID Team :", teamId);
                let Apps = await this.sdkHelper.getApps(teamId);
                console.log("Setup_ConfigureAppIDAndGroupID Team :", Apps);
                if (Apps && Apps["value"].length > 0) {
                    let AppID = "";
                    for (const app of Apps["value"]) {
                        console.log("Setup_ConfigureAppIDAndGroupID Team :", PMAddinName, app.name.toString());
                        if (app.name.toString() === PMAddinName && app.distributionMethod.toString() === "sideloaded") {
                            AppID = app.id;
                            break;
                        }
                    }
                    let token = await this.authHelper.getWebApiToken();
                    await this.UpdateAppSettings("ProposalManagerAddInName", PMAddinName, token);
                    if (AppID) {
                        await this.UpdateAppSettings("TeamsAppInstanceId", AppID, token);
                        await this.UpdateAppSettings("ProposalManagerGroupID", teamId, token);

                        ProposalManagement_Team.TeamsAppInstanceId = AppID;
                        ProposalManagement_Team.ProposalManagerGroupID = teamId;
                        this.setState({ ProposalManagement_Team, appId: AppID });
                        this.setSpinnerAndMsg(false, true, "Updated", MessageBarType.success);
                    } else
                        throw new Error("app id is empty");
                } else
                    throw new Error("teamId is empty ", teamId);
            } else
                throw new Error("Provided team name is not present, check the manifest file for the exact name");
        } catch (error) {
            this.setSpinnerAndMsg(false, true, error.message, MessageBarType.error);
            console.log("Setup_ConfigureAppIDAndGroupID error : ", error.message);
        }
        this.setState({ renderStep_2: false });
        await this.hideMessagebar();
    }

    //Setp 3 & //Setp 4
    async SetAppSetting_JsonKeys(ProposalManagement, group, key = false) {
        console.log("SetAppSetting_JsonKeys   : ", ProposalManagement);
        console.log("SetAppSetting_JsonKeys   : ", ProposalManagement.constructor.name);
        this.spinnerOff(group, true);
        let token = this.authHelper.getWebApiToken();

        this.setSpinnerAndMsg(true, false, "");
        let SharePointHostName = ProposalManagement.SharePointHostName;
        let ProposalManagementRootSiteId = ProposalManagement.ProposalManagementRootSiteId;
        let SharePointSiteRelativeName = ProposalManagement.SharePointSiteRelativeName;
        try {

            for (const Objkey of Object.keys(ProposalManagement)) {
                try {
                    if (Objkey !== "ProposalManagementRootSiteId") {
                        const contents = await this.UpdateAppSettings(Objkey, ProposalManagement[Objkey], token);
                        console.log(`SetAppSetting_JsonKeys_${Objkey} : `, contents);
                    }
                } catch (error) {
                    console.log(`SetAppSetting_JsonKeys_${Objkey}_err : `, error.message);
                }
            }

            let rootID = "";
            if (SharePointHostName !== "" && SharePointSiteRelativeName !== "" && key) {
                console.log("SetAppSetting_JsonKeys ProposalManagementRootSiteId ", SharePointHostName, SharePointSiteRelativeName, ProposalManagementRootSiteId);
                let ProposalManagement_Sharepoint = { ...this.state.ProposalManagement_Sharepoint };
                let rootIdObj = await this.sdkHelper.getSharepointRootId(SharePointHostName, SharePointSiteRelativeName);
                console.log("ProposalManagementRootSiteId 1: ", rootIdObj);
                if (rootIdObj) {
                    rootID = rootIdObj.id;
                    ProposalManagement_Sharepoint["ProposalManagementRootSiteId"] = rootID;
                    this.setState({ ProposalManagement_Sharepoint });
                    console.log("ProposalManagementRootSiteId 2: ", rootID);
                    const contents = await this.UpdateAppSettings("ProposalManagementRootSiteId", rootID, token);
                }
            } else if (ProposalManagementRootSiteId) {
                rootID = ProposalManagementRootSiteId;
            }

            this.setSpinnerAndMsg(false, true, "Updated", MessageBarType.success);

        } catch (error) {
            this.setSpinnerAndMsg(false, true, error.message, MessageBarType.error);
            console.log(`SetAppSetting_JsonKeys_err : `, error.message);
        }
        this.hideMessagebar();
        this.spinnerOff(group, false);
    }

    async SetDocumentIdActivatorSetting_JsonKeys(DocumentIdActivator, group, key = false) {
        console.log("SetDocumentIdActivatorSetting_JsonKeys   : ", DocumentIdActivator);
        console.log("SetDocumentIdActivatorSetting_JsonKeys   : ", DocumentIdActivator.constructor.name);
        this.spinnerOff(group, true);
        let token = this.authHelper.getWebApiToken();

        this.setSpinnerAndMsg(true, false, "");
        try {

            for (const Objkey of Object.keys(DocumentIdActivator)) {
                try {
                    const contents = await this.UpdateDocumentIdActivatorSettings(Objkey, DocumentIdActivator[Objkey], token);
                    console.log(`SetDocumentIdActivatorSetting_JsonKeys_${Objkey} : `, contents);
                } catch (error) {
                    console.log(`SetDocumentIdActivatorSetting_JsonKeys_${Objkey}_err : `, error.message);
                }
            }

            this.setSpinnerAndMsg(false, true, "Updated", MessageBarType.success);

        } catch (error) {
            this.setSpinnerAndMsg(false, true, error.message, MessageBarType.error);
            console.log(`SetDocumentIdActivatorSetting_JsonKeys_err : `, error.message);
        }
        this.hideMessagebar();
        this.spinnerOff(group, false);
    }

    async CreateAllLists(rootID, token) {
        try {
            console.log("Setup_createAllLists");
            let requestUrl = `api/Setup/CreateAllLists/${rootID}`;
            let options = {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'authorization': `Bearer ${token}`
                }
            };
            let data = await fetch(requestUrl, options);
            console.log("Setup_createAllLists response: ", data);
            return true;
        } catch (error) {
            console.log("Setup_createAllLists error: ", error.message);
            return false;
        }
    }

    //Setp 5
    async CreateAdminPermissions() {
        this.setState({ renderStep_5: true });
        let AdGroupName = this.state.ADGroupName;
        let token = this.authHelper.getWebApiToken();
        this.setState({ renderStep_3: true });
        this.setSpinnerAndMsg(true, false, "");

        try {

            try {
                if (this.state.ProposalManagement_Sharepoint.ProposalManagementRootSiteId) {
                    let rootID = this.state.ProposalManagement_Sharepoint.ProposalManagementRootSiteId;
                    console.log("ProposalManagementRootSiteId 3: ", rootID);
                    let createList = await this.CreateAllLists(rootID, token);
                }
            } catch (error) {
                throw new Error("CreateAllLists: ", error.message);
            }

            try {
                await this.CreateProposalManagerAdminGroup(AdGroupName);
            } catch (error) {
                throw new Error("CreateProposalManagerAdminGroup: ", error.message);
            }

            console.log("Setup_CreateAdminPermissions");
            if (AdGroupName) {
                let requestUrl = `api/Setup/CreateAdminPermissions/${AdGroupName}`;
                let options = {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'authorization': `Bearer ${token}`
                    }
                };
                let data = await fetch(requestUrl, options);
                console.log("Setup_CreateAdminPermissions response: ", data);
                this.setSpinnerAndMsg(false, true, "Updated", MessageBarType.success);

                //loading
                await this.loadDataForPermision_Process_Roles();

                return true;
            } else
                throw new Error("AdGroupName cannot be empty");
        } catch (error) {
            this.setSpinnerAndMsg(false, true, error.message, MessageBarType.error);
            console.log("Setup_CreateAdminPermissions error : ", error.message);
        }

        await this.hideMessagebar();
        this.setState({ renderStep_3: true });
    }

    //step 5
    async loadDataForPermision_Process_Roles() {
        // CreateSitePermissions() to add all the known permissions to the permission list.
        // CreateSiteProcesses() to add all the known processes to the workflow items list.
        // CreateSiteRoles() to add all the known roles to the roles list.
        this.setSpinnerAndMsg(true, false, "");
        let token = this.authHelper.getWebApiToken();
        let requestUriArray = ["CreateSiteRoles", "CreateSiteProcesses", "CreateSitePermissions"];
        try {
            console.log("Setup_loadDataForPermision_Process_Roles");
            for (const uri of requestUriArray) {
                try {
                    let requestUrl = `api/Setup/${uri}`;
                    let options = {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'authorization': `Bearer ${token}`
                        }
                    };
                    let data = await fetch(requestUrl, options);
                    console.log("Setup_loadDataForPermision_Process_Roles response: ", data);
                } catch (error) {
                    console.log(error.message);
                }
            }
            this.setSpinnerAndMsg(false, true, "Loaded all", MessageBarType.success);
            await this.hideMessagebar();
            return true;
        } catch (error) {
            console.log("Setup_loadDataForPermision_Process_Roles: ", error.message);
            this.setSpinnerAndMsg(false, true, error.message, MessageBarType.error);
            await this.hideMessagebar();
            return false;
        }
    }

    //step 1
    async CreateProposalManagerAdminGroup(AdGroupName) {
        // CreateAdministrationGroup() this is used to create the proposal manager admin group.
        //Admin rolemaping group
        // Error handling: if fails because you can’t find the teams addin, display an error that says that the specified Teams Add-In has not been found, 
        // please make sure the add-in was sideloaded and that the name form the manifest file matches the name entered.

        let token = this.authHelper.getWebApiToken();
        try {
            console.log("Setup_CreateProposalManagerAdminGroup", AdGroupName);
            if (AdGroupName) {
                let requestUrl = `api/Setup/CreateProposalManagerAdminGroup/${AdGroupName}`;
                let options = {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'authorization': `Bearer ${token}`
                    }
                };
                let data = await fetch(requestUrl, options);
                console.log("Setup_CreateProposalManagerAdminGroup response: ", data);
                return true;
            } else
                throw new Error("PMAddinName cannot be empty");
        } catch (error) {
            console.log("Setup_CreateProposalManagerAdminGroup error : ", error.message);
        }
    }

    async spinnerOff(group, flag) {
        switch (group) {
            case "sharepoint":
                this.setState({ sharepoint: flag });
                break;
            case "powerbi":
                this.setState({ powerbi: flag });
                break;
            case "bot":
                this.setState({ bot: flag });
                break;
            case "misc":
                this.setState({ misc: flag });
                break;
            case "DocumentIdActivator":
                this.setState({ documentid: flag });
            default:
                break;
        }
    }

    async onBlurSetPM(e, key) {
        let value = e.target.value;
        const ProposalManagement_Team = { ...this.state.ProposalManagement_Team };

        if (value) {
            value = value.trim();
            switch (key) {
                case "PMAddinName":
                    ProposalManagement_Team.ProposalManagerAddInName = value;
                    this.setState({ PMAddinName: value });
                    break;
                case "PMTeamName":
                    ProposalManagement_Team.GeneralProposalManagementTeam = value;
                    this.setState({ PMTeamName: value });
                    break;
                case "APPID":
                    ProposalManagement_Team.TeamsAppInstanceId = value;
                    this.setState({ appId: value });
                    break;
                case "ADGroupName":
                    this.setState({ ADGroupName: value });
                    break;
                default:
                    break;
            }
        }
        this.setState({ ProposalManagement_Team });
    }

    async onBlurOnAettingKeys(e, key, defaultValue = "") {
        let value = e.target.value || defaultValue;
        console.log("onBlurOnAettingKeys : ", key, e.target.value, defaultValue);
        let obj = {};
        const ProposalManagement_Sharepoint = { ...this.state.ProposalManagement_Sharepoint };
        if (value) {
            ProposalManagement_Sharepoint[key] = value;
            obj[key] = value;
            this.setState({ ProposalManagement_Sharepoint });
        }
    }

    async onBlurOnBotSettings(e, key) {
        let value = e.target.value;
        let obj = {};
        const ProposalManagement_bot = { ...this.state.ProposalManagement_bot };
        if (ProposalManagement_bot.hasOwnProperty(key) && value) {
            ProposalManagement_bot[key] = value;
            obj[key] = value;
            this.setState({ ProposalManagement_bot });
        }
    }

    async onBlurOnDocumentIdActivatorSettings(e, key) {
        let value = e.target.value;
        let obj = {};
        const DocumentIdActivator = { ...this.state.DocumentIdActivator };
        if (DocumentIdActivator.hasOwnProperty(key) && value) {
            DocumentIdActivator[key] = value;
            obj[key] = value;
            this.setState({ DocumentIdActivator });
        }
    }

    async onBlurOnBISettings(e, key) {
        let value = e.target.value;
        let obj = {};
        const ProposalManagement_BI = { ...this.state.ProposalManagement_BI };
        if (ProposalManagement_BI.hasOwnProperty(key) && value) {
            ProposalManagement_BI[key] = value;
            obj[key] = value;
            this.setState({ ProposalManagement_BI });
        }
    }

    async onBlurOnWebhookAddressSettings(e, key) {
        let value = e.target.value;
        let obj = {};
        const ProposalManagement_WebhookAddress = { ...this.state.ProposalManagement_WebhookAddress };
        if (ProposalManagement_WebhookAddress.hasOwnProperty(key) && value) {
            ProposalManagement_WebhookAddress[key] = value;
            obj[key] = value;
            this.setState({ ProposalManagement_WebhookAddress });
        }
    }

    renderStep_9() {
        let margin = { margin: '10px' };
        let bold = { 'fontWeight': 'bold' };
        let disabled = Object.keys(this.state.DocumentIdActivator).every(key => this.state.DocumentIdActivator[key]);
        let placeholders = this.placeholderForProposalManager();
        let TextBoxViewList = Object.keys(this.state.DocumentIdActivator).map(key => {
            return (
                <TextField
                    key={key}
                    label={<Trans>{key}</Trans>}
                    onBlur={(e) => this.onBlurOnDocumentIdActivatorSettings(e, key)}
                    required={true}
                    disabled={this.state.isUpdateOpp}
                    placeholder={`eg : <${placeholders(key)}>`}
                    value={this.state.DocumentIdActivator[key] ? this.state.DocumentIdActivator[key] : ""}
                />
            );
        });

        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold} className="pageheading"><Trans>step9</Trans></h4>
                <span>
                    <Trans>ste9Label</Trans>
                </span>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                        {TextBoxViewList}
                    </div>
                </div>
                <div className='ms-Grid-col ms-sm2 ms-md4 ms-lg4'>
                    <PrimaryButton
                        style={margin}
                        className='pull-right' onClick={(e) => this.SetDocumentIdActivatorSetting_JsonKeys(this.state.DocumentIdActivator, "DocumentIdActivator")}
                        disabled={this.state.isUpdateOpp}
                    >{<Trans>Configure</Trans>}</PrimaryButton>
                </div>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                        <span><Trans>step9Complete</Trans></span>
                    </div>
                </div>
                {this.state.documentid ? this.showSpinnerAndMessage() : null}
            </div>);
    }

    renderStep_8() {
        const margin = { margin: '10px' };
        const bold = { 'fontWeight': 'bold' };
        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold} className="pageheading"><Trans>step8</Trans></h4>
                <h5 style={bold}><Trans>step8label</Trans></h5>
                <h5 style={bold}><Trans>step8label_1</Trans></h5>
                <PrimaryButton
                    style={margin} disabled={this.state.isUpdateOpp}
                    className='pull-right' onClick={(e) => this.onFinish()}
                >{<Trans>finish</Trans>}</PrimaryButton>
                <PrimaryButton
                    style={margin} disabled={this.state.isUpdateOpp}
                    className='pull-right'
                    onClick={(e) => this.downloadJsonObject()}
                >{<Trans>downlaod</Trans>}</PrimaryButton>
                <a id="downloadFile" />
                {this.state.finish ? this.showSpinnerAndMessage(true) : null}
            </div>);
    }

    renderStep_7() {
        let margin = { margin: '10px' };
        let bold = { 'fontWeight': 'bold' };
        let disabled = Object.keys(this.state.ProposalManagement_BI).every(key => this.state.ProposalManagement_BI[key]);
        let placeholders = this.placeholderForProposalManager();
        let TextBoxViewList = Object.keys(this.state.ProposalManagement_BI).map(key => {
            return (
                <TextField
                    key={key}
                    label={<Trans>{key}</Trans>}
                    onBlur={(e) => this.onBlurOnBISettings(e, key)}
                    required={true}
                    disabled={this.state.isUpdateOpp}
                    placeholder={`eg : <${placeholders(key)}>`}
                    value={this.state.ProposalManagement_BI[key] ? this.state.ProposalManagement_BI[key] : ""}
                />
            );
        });

        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold} className="pageheading"><Trans>step7</Trans></h4>
                <span>
                    <Trans>step7label</Trans>
                </span>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                        {TextBoxViewList}
                    </div>
                </div>
                <div className='ms-Grid-col ms-sm2 ms-md4 ms-lg4'>
                    <PrimaryButton
                        style={margin}
                        className='pull-right' onClick={(e) => this.SetAppSetting_JsonKeys(this.state.ProposalManagement_BI, "powerbi")}
                        disabled={this.state.isUpdateOpp}
                    >{<Trans>Configure</Trans>}</PrimaryButton>
                </div>
                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                    <span><Trans>step7Complete</Trans></span>
                </div>
                {this.state.powerbi ? this.showSpinnerAndMessage(true) : null}
            </div>);
    }

    renderStep_6() {
        let margin = { margin: '10px' };
        let bold = { 'fontWeight': 'bold' };
        let disabled = Object.keys(this.state.ProposalManagement_bot).every(key => this.state.ProposalManagement_bot[key]);
        let placeholders = this.placeholderForProposalManager();
        let TextBoxViewList = Object.keys(this.state.ProposalManagement_bot).map(key => {
            if (key !== "BotServiceUrl")
                return (
                    <TextField
                        key={key}
                        label={<Trans>{key}</Trans>}
                        onBlur={(e) => this.onBlurOnBotSettings(e, key)}
                        required={true}
                        disabled={this.state.isUpdateOpp}
                        placeholder={`eg : <${placeholders(key)}>`}
                        value={this.state.ProposalManagement_bot[key] ? this.state.ProposalManagement_bot[key] : ""}
                    />
                );
        });

        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold} className="pageheading"><Trans>step6</Trans></h4>
                <span>
                    <Trans>step6label</Trans>
                </span>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                        {TextBoxViewList}
                    </div>
                </div>
                <div className='ms-Grid-col ms-sm2 ms-md4 ms-lg4'>
                    <PrimaryButton
                        style={margin}
                        className='pull-right' onClick={(e) => this.SetAppSetting_JsonKeys(this.state.ProposalManagement_bot, "bot")}
                        disabled={this.state.isUpdateOpp}
                    >{<Trans>Configure</Trans>}</PrimaryButton>
                </div>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                        <span><Trans>step6Complete</Trans></span>
                    </div>
                </div>
                {this.state.bot ? this.showSpinnerAndMessage() : null}
            </div>);
    }

    renderStep_5() {
        const margin = { margin: '10px' };
        const bold = { 'fontWeight': 'bold' };
        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold} className="pageheading"><Trans>step5</Trans></h4>
                <span><Trans>step5Note1</Trans></span>
                <span><Trans>step5note</Trans></span>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                        <TextField
                            id='appKey'
                            label={<Trans>step5label</Trans>}
                            onBlur={(e) => this.onBlurSetPM(e, "ADGroupName")}
                            required={true}
                            disabled={this.state.isUpdateOpp}
                            placeholder={`eg : < Proposal Manager >`}
                        />
                    </div>
                </div>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                        <span><Trans>Step5complete</Trans></span>
                    </div>
                </div>
                <div className='ms-Grid-row '>
                    <PrimaryButton style={margin} className='pull left' disabled={this.state.isUpdateOpp}
                        onClick={(e) => this.CreateAdminPermissions()}
                    >{<Trans>Step5bttn</Trans>}</PrimaryButton>
                </div>
                {this.state.renderStep_3 ? this.showSpinnerAndMessage() : null}
            </div>);
    }

    renderStep_4() {
        let margin = { margin: '10px' };
        let bold = { 'fontWeight': 'bold' };
        let disabled = Object.keys(this.state.ProposalManagement_Misc).every(key => this.state.ProposalManagement_Misc[key]);
        let placeholders = this.placeholderForProposalManager();
        let TextBoxViewList = Object.keys(this.state.ProposalManagement_Misc).map(key => {
            if (key !== "SetupPage" && key !== "GraphRequestUrl" && key !== "GraphBetaRequestUrl") {
                return (
                    <TextField
                        key={key}
                        label={<Trans>{key}</Trans>}
                        onBlur={(e) => this.onBlurOnAettingKeys(e, key)}
                        required={true}
                        value={this.state.ProposalManagement_Misc[key] ? this.state.ProposalManagement_Misc[key] : this.defaultValue(key)}
                        disabled={this.state.isUpdateOpp}
                        placeholder={`eg : <${placeholders(key)}>`}
                    />
                );
            }
        });

        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold} className="pageheading"><Trans>step4</Trans></h4>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                        {TextBoxViewList}
                    </div>
                </div>
                <div className='ms-Grid-col ms-sm2 ms-md4 ms-lg4'>
                    <PrimaryButton
                        style={margin}
                        className='pull-right' onClick={(e) => this.SetAppSetting_JsonKeys(this.state.ProposalManagement_Misc, "misc")}
                        disabled={this.state.isUpdateOpp}
                    >{<Trans>Set</Trans>}</PrimaryButton>
                </div>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                        <span><Trans>step4Complete</Trans></span>
                    </div>
                </div>
                {this.state.misc ? this.showSpinnerAndMessage() : null}
            </div>);
    }

    renderStep_3() {
        let margin = { margin: '10px' };
        let bold = { 'fontWeight': 'bold' };
        let disabled = Object.keys(this.state.ProposalManagement_Sharepoint).every(key => this.state.ProposalManagement_Sharepoint[key]);
        let placeholders = this.placeholderForProposalManager();
        let TextBoxViewList_1 = Object.keys(this.state.ProposalManagement_Sharepoint).map(key => {
            if (key ==="SharePointHostName" || key ==="SharePointSiteRelativeName") {
                return (
                    <TextField
                        key={key}
                        label={<Trans>{key}</Trans>}
                        onBlur={(e) => this.onBlurOnAettingKeys(e, key, this.defaultValue(key))}
                        required={true}
                        disabled={this.state.isUpdateOpp}
                        placeholder={`eg : <${placeholders(key)}>`}
                        value={this.state.ProposalManagement_Sharepoint[key] ? this.state.ProposalManagement_Sharepoint[key] : this.defaultValue(key)}
                    />
                );
            }
        });
        let TextBoxViewList_2 = Object.keys(this.state.ProposalManagement_Sharepoint).map(key => {
            if (key !== "ProposalManagementRootSiteId" && key !=="SharePointHostName" && key !=="SharePointSiteRelativeName") {
                return (
                    <TextField
                        key={key}
                        label={<Trans>{key}</Trans>}
                        onBlur={(e) => this.onBlurOnAettingKeys(e, key, this.defaultValue(key))}
                        required={true}
                        disabled={this.state.isUpdateOpp}
                        placeholder={`eg : <${placeholders(key)}>`}
                        value={this.state.ProposalManagement_Sharepoint[key] ? this.state.ProposalManagement_Sharepoint[key] : this.defaultValue(key)}
                    />
                );
            }
        });
        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold} className="pageheading"><Trans>Step3</Trans></h4>
                <span><Trans>step3label</Trans></span>
                <br/>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                        {TextBoxViewList_1}
                    </div>
                </div>
                <br/>
                <span>
                    <Trans>step3label_1</Trans>
                </span>
                <br/>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                        {TextBoxViewList_2}
                    </div>
                </div>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm2 ms-md4 ms-lg4'>
                        <PrimaryButton
                            style={margin}
                            className='pull-right' onClick={(e) => this.SetAppSetting_JsonKeys(this.state.ProposalManagement_Sharepoint, "sharepoint", true)}
                            disabled={this.state.isUpdateOpp}
                        >{<Trans>step4button</Trans>}</PrimaryButton>
                    </div>
                </div>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                        <span><Trans>step3labelComplete</Trans></span>
                    </div>
                </div>
                {this.state.sharepoint ? this.showSpinnerAndMessage() : null}
            </div>);
    }

    renderStep_2() {
        const margin = { margin: '10px' };
        const bold = { 'fontWeight': 'bold' };
        const normal = { 'fontWeight': normal };
        const hide = {
            display: 'none'
        };
        return (
            <div>
                <div className='ms-Grid-row ms-Grid bg-white ibox-content p-10'>
                    <h4 style={bold} className="pageheading"><Trans>Step2</Trans></h4>
                    <h4 style={bold}><Trans>step2.1</Trans></h4>
                    <span>
                        <Trans style={normal}>labelforstep2</Trans> <br />
                    </span>
                </div>
                <div className="ms-Grid-row ms-Grid bg-white ibox-content p-10">
                    <h4 style={bold}><Trans>step2.2</Trans></h4>
                    <span>
                        <Trans style={normal}>labelforstep2_1</Trans> <br />
                    </span>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                        <TextField
                            id='appKey'
                            label={<Trans>setupProposalManager</Trans>}
                            onBlur={(e) => this.onBlurSetPM(e, "PMAddinName")}
                            required={true}
                            value={
                                (this.state.ProposalManagement_Team.ProposalManagerAddInName) ?
                                    this.state.ProposalManagement_Team.ProposalManagerAddInName : this.state.PMAddinName}
                            placeholder={`eg : < Proposal Manager >`}
                            disabled={this.state.isUpdateOpp}
                        />
                    </div>
                    <div className='ms-Grid-col ms-sm12'>
                        <PrimaryButton style={margin}
                            onClick={(e) => this.ConfigureAppIDAndGroupID()}
                            disabled={this.state.isUpdateOpp}
                        >{<Trans>addinbutton</Trans>}</PrimaryButton>
                    </div>
                    {this.state.renderStep_2 ? this.showSpinnerAndMessage(true) : null}
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                        <TextField
                            id='appKey'
                            label={<Trans>step2AfterSuccessfullConfigMsg</Trans>}
                            onBlur={(e) => this.onBlurSetPM(e, "APPID")}
                        
                            value={this.state.ProposalManagement_Team.TeamsAppInstanceId ?
                                this.state.ProposalManagement_Team.TeamsAppInstanceId : this.state.appId}
                            disabled={true}
                        />
                    </div>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                        <span><Trans>step2AfterSuccessfullConfigMsg1</Trans></span>
                    </div>
                </div>
            </div>);
    }

    renderStep_1() {
        const margin = { margin: '10px' };
        const bold = { 'fontWeight': 'bold' };
        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold} className="pageheading"><Trans>Step1</Trans></h4>
                <span><Trans>Step1Label</Trans></span>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'>
                        <TextField
                            id='appKey'
                            label={<Trans>provideappteam</Trans>}
                            onBlur={(e) => this.onBlurSetPM(e, "PMTeamName")}
                            value={this.state.ProposalManagement_Team.GeneralProposalManagementTeam ?
                                this.state.ProposalManagement_Team.GeneralProposalManagementTeam : this.state.PMTeamName}
                            required={true}
                            disabled={this.state.isUpdateOpp}
                        />
                    </div>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6'><br />
                        <PrimaryButton style={margin}
                            onClick={(e) => this.CreateProposalManagerTeam()}
                            disabled={this.state.isUpdateOpp}
                        >{<Trans>Create</Trans>}</PrimaryButton>
                    </div>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                        <span><Trans>Step1Complete</Trans></span>
                    </div>
                    {this.state.renderStep_1 ? this.showSpinnerAndMessage(true) : null}
                </div>
            </div>);
    }

    renderAppPrerequisites() {
        const bold = { fontWeight: 'bold' };
        return (
            <div className='ms-Grid bg-white ibox-content p-10'>
                <h4 style={bold}><Trans>PMPrerequesties</Trans></h4>
                <I18n>
                    {
                        t => {
                            return (
                                <div>
                                    <h5>{t('prerequiste')}</h5>
                                    <hr className="prereqLine" />
                                    <ul>
                                        <li>{t('prereq1')}</li>
                                        <li>{t('prereq2')}</li>
                                    </ul>
                                    {t('prereq5')}
                                    <br /><br />
                                    {t('prerequiste1')}
                                    <ul>
                                        <li>{t('prereq6')}</li>
                                        <li>{t('prereq7')}</li>
                                        <li>{t('prereq8')}</li>
                                    </ul>
                                </div>
                            );
                        }
                    }
                </I18n>
            </div>
        );
    }

    render() {
        const disabled = true;
        const disabledClass = {
            'pointerEvents': 'none',
            'opacity': 0.4
        };
        if (this.state.loading) {
            return (
                <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                </div>
            );
        } else {
            return (
                <div style={this.state.ProposalManagement_Misc.SetupPage === "disabled" ? disabledClass : null}>
                    <div className="ms-Grid-row">
                        <div className="ms-Grid-col ms-sm6 ms-md4 ms-lg12"><h3><Trans>setupPage</Trans></h3></div>
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderAppPrerequisites()}
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderStep_1()}
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderStep_2()}
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderStep_3()}
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderStep_4()}
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderStep_5()}
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderStep_6()}
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderStep_7()}
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderStep_9()}
                    </div>
                    <div className='ms-Grid bg-white ibox-content'>
                        {this.renderStep_8()}
                    </div>
                </div>
            );
        }
    }
}