/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

// Global imports
import React, { Component } from 'react';
import * as microsoftTeams from '@microsoft/teams-js';
import { Route } from 'react-router';
import GraphSdkHelper from './helpers/GraphSdkHelper';
import AuthHelper from './helpers/AuthHelper';
import appSettingsObject from './helpers/AppSettings';
// Teams Add-in imports
import { ThemeStyle, Th } from 'msteams-ui-components-react';

import { Home } from './components-teams/Home';

import { Config } from './components-teams/Config';
import { Privacy } from './components-teams/Privacy';
import { TermsOfUse } from './components-teams/TermsOfUse';

//import { Checklist } from './views-teams/Proposal/Checklist';
import { Checklist } from './components-teams/Checklist';
import { RootTab } from './components-teams/RootTab'; //'./views-teams/Proposal/RootTab';
import { TabAuth } from './components-teams/TabAuth';
import { ProposalStatus } from './components-teams/ProposalStatus';
import { CustomerDecision } from './components-teams/CustomerDecision';
// Components mobile
import { RootTab as RootTabMob } from './components-mobile/RootTab';
import { initializeIcons } from 'office-ui-fabric-react/lib/Icons';

import { Administration } from './components-teams/general/Administration';
import { General } from './components-teams/general/General';
import { Configuration } from './components-teams/general/Configuration';
import { AddDealType } from './components-teams/general/AddDealType';
import { AddDealTypeR } from './components-teams/general/DealType/AddDealTypeR';
import { OpportunityDetails } from './components-teams/general/Opportunity/OpportunityDetails';
import { ChooseTeam } from './components-teams/general/Opportunity/ChooseTeam';

import i18n from './i18n';
import { setTimeout } from 'timers';
//import { clearInterval, setTimeout } from 'timers'; // This component causes a conflict with setTmeout206

var appSettings;

export class AppTeams extends Component {
    displayName = AppTeams.name

    constructor(props) {
        super(props);
        console.log("AppTeams: Contructor");
        initializeIcons();

        // Setting the default values
        appSettings = {
            generalProposalManagementTeam: appSettingsObject.generalProposalManagementTeam,
            teamsAppInstanceId: appSettingsObject.teamsAppInstanceId,
            teamsAppName: appSettingsObject.teamsAppName,
            reportId: appSettingsObject.reportId,
            workspaceId: appSettingsObject.workspaceId
        };

        this.localStorePrefix = appSettingsObject.localStorePrefix;
        if (window.authHelper) {
            console.log("AppTeams: Auth already initialized");
            this.authHelper = window.authHelper;
        } else {
            // Initilize the AuthService and save it in the window object.
            console.log("AppTeams: Initialize auth");
            this.authHelper = new AuthHelper();
            window.authHelper = this.authHelper;
        }

        if (window.sdkHelper) {
            this.sdkHelper = window.sdkHelper;
        } else {
            // Initialize the GraphService and save it in the window object.
            this.sdkHelper = new GraphSdkHelper();
            window.sdkHelper = this.sdkHelper;
        }

        try {
			/* Initialize the Teams library before any other SDK calls.
			 * Initialize throws if called more than once and hence is wrapped in a try-catch to perform a safe initialization.
			 */
            microsoftTeams.initialize();
        }
        catch (err) {
            console.log(err);
        }
        finally {
            this.inTeamsClient = false;
            if (navigator.userAgent.indexOf("Teams") !== -1) {
                this.inTeamsClient = true;
            }

            this._isMounted = false;

            let locale = "en-us";
            if (this.getQueryVariable('locale') !== null && this.getQueryVariable('locale') !== undefined) {
                locale = this.getQueryVariable('locale');
            }
            locale = (navigator.languages && navigator.languages[0]) || navigator.language || navigator.userLanguage;
            console.log("App Teams Locale====" + locale + " ***** getQueryVarLocale **** " + this.getQueryVariable('locale'));

            this.state = {
                isAuthenticated: false,
                theme: ThemeStyle.Light,
                fontSize: 16,
                authUser: "",
                channelName: this.getQueryVariable('channelName'),
                channelId: this.getQueryVariable('channelId'),
                teamName: this.getQueryVariable('teamName'),
                groupId: this.getQueryVariable('groupId'),
                loginHint: this.getQueryVariable('loginHint'),
                locale: locale,
                appSettings: appSettings
            };
        }
    }

    async componentDidMount() {
        console.log("AppTeams_componentDidMount v2 loginHint: " + this.state.loginHint + " window.location.pathname: " + window.location.pathname);

        const tokenGraphAdmin = await this.handleGraphAdminToken();
        console.log("AppTeams_componentDidMount handleGraphAdminToken: " + tokenGraphAdmin);

        const resTeamsContext = await this.getTeamsContext();
        console.log("AppTeams_componentDidMount getTeamsContext resTeamsContext: " + resTeamsContext);

        if (resTeamsContext.includes("AppTeams_getTeamsContext success") || window.location.pathname.includes("/tabmob/")) {
            this.setState({
                authUser: "start"
            });
        }

        if (window.location.pathname !== "/tab/tabauth") {
            if (await this.authHelper.userHasWebApiToken() && appSettings.generalProposalManagementTeam.length === 0) {
                /// adding client settings
                this.getClientSettings()
                    .then(res => {
                        appSettings = {
                            generalProposalManagementTeam: res.GeneralProposalManagementTeam,
                            teamsAppInstanceId: res.TeamsAppInstanceId,
                            teamsAppName: res.ProposalManagerAddInName,
                            reportId: res.PBIReportId,
                            workspaceId: res.PBIWorkSpaceId
                        };
                        console.log("AppTeams_componentDidMount_getClientSettings  ==>", appSettings);
                        //Need to review refresh issue
                        if (this.state.appSettings.reportId.length === 0) {
                            console.log("AppTeams_componentDidMount_getClientSettings inisde setstate:", appSettings);
                            this.setState({ appSettings });
                        }
                        //Need to review refresh issue
                    })
                    .catch(err => {
                        console.log("AppTeams_componentDidMount_getClientSettings error:", err);
                    });
            }
        }
    }

    async componentDidUpdate() {
        console.log("AppTeams_componentDidUpdate window.location.pathname: " + window.location.pathname + " state.isAuthenticated: " + this.state.isAuthenticated);

        const tokenGraphAdmin = await this.handleGraphAdminToken();
        console.log("AppTeams_componentDidUpdate handleGraphAdminToken: " + tokenGraphAdmin);

        if (window.location.pathname !== "/tab/tabauth") {

            if (await this.authHelper.userHasWebApiToken() && appSettings.generalProposalManagementTeam.length === 0) {
                /// adding client settings
                this.getClientSettings()
                    .then(res => {
                        appSettings = {
                            generalProposalManagementTeam: res.GeneralProposalManagementTeam,
                            teamsAppInstanceId: res.TeamsAppInstanceId,
                            teamsAppName: res.ProposalManagerAddInName,
                            reportId: res.PBIReportId,
                            workspaceId: res.PBIWorkSpaceId
                        };
                        console.log("AppTeams_componentDidUpdate_getClientSettings  ==>", res);
                        //Need to review refresh issue
                        if (this.state.appSettings.reportId.length === 0) {
                            console.log("AppTeams_componentDidMount_getClientSettings inisde setstate:", appSettings);
                            this.setState({ appSettings });
                        }
                        //Need to review refresh issue
                    })
                    .catch(err => {
                        console.log("AppTeams_componentDidUpdate_getClientSettings error:", err);
                    });
            }

            if (this.state.authUser === "start") {
                const isAuthentcatedRes = await this.authHelper.userIsAuthenticatedAsync();
                console.log("AppTeams_componentDidUpdate loginHint: " + this.state.loginHint + " isAuthentcatedRes: " + isAuthentcatedRes);

                let loginHint = this.state.loginHint;
                const isAdminCall = await this.isAdminCall();

                console.log("AppTeams_componentDidUpdate isAdminCall: " + isAdminCall);

                if (isAdminCall === "true") {
                    const userHasGraphAdminToken = await this.authHelper.userHasGraphAdminToken();

                    console.log("AppTeams_componentDidUpdate userHasGraphAdminToken: " + userHasGraphAdminToken + " isAdminCall: " + isAdminCall);

                    if (!userHasGraphAdminToken) {
                        loginHint = "";
                    }
                }

                if (isAuthentcatedRes !== loginHint) {
                    console.log("AppTeams_componentDidUpdate isAuthentcatedRes !== loginHint: " + loginHint + " isAuthentcatedRes: " + isAuthentcatedRes);
                    if (window.location.pathname.includes("/tabmob/")) {
                        await this.browserAuthentication();
                    } else {
                        this.teamsAuthentication();
                    }
                } else {
                    this.setState({
                        authUser: "AppTeams_teamsAuthentication_successCallback"
                    });
                }
            } else if (this.state.authUser === "AppTeams_teamsAuthentication_successCallback") {
                //const
                const resCallGetUserProfile = await this.callGetUserProfile();
                console.log("AppTeams_componentDidUpdate callGetUserProfile: " + resCallGetUserProfile);
            } else {
                console.log("AppTeams_componentDidUpdate not ready to continue auth sequence state.authUser: " + this.state.authUser);
            }
        }
    }

    async handleGraphAdminToken() {
        try {
            // Store the original request so we can detect the type of token in TabAuth
            if (window.location.pathname !== "/tab/tabauth") {
                localStorage.setItem(this.localStorePrefix + "appteams.request", window.location.pathname);
                if (window.location.pathname !== "/tab/generalAdministrationTab") {
                    // Clear GraphAdminToken in case user navigates to admin then to another non-admin tab
                    const graphAdminTokenStoreKey = this.localStorePrefix + "AdminGraphToken";
                    localStorage.removeItem(graphAdminTokenStoreKey);
                }
            }

            return true;
        } catch (err) {
            console.log("AppTeams_handleGraphAdminToken error: " + err);
            return false;
        }
    }

    async isAdminCall() {
        const appTeamsRequest = localStorage.getItem(this.localStorePrefix + "appteams.request");

        try {
            if (appTeamsRequest === "/tab/generalAdministrationTab") {
                return "true";
            }
            else {
                return "false";
            }
        } catch (err) {
            console.log("AppTeams_isAdminCall error: " + err);
            return "false";
        }
    }

    teamsAuthentication() {
        let timerAuthWindow = setTimeout(() => {
            microsoftTeams.authentication.authenticate({
                url: window.location.protocol + '//' + window.location.host + '/tab/tabauth' + "?channelName=" + this.state.channelName + "&teamName=" + this.state.teamName + "&channelId=" + this.state.channelId + "&locale=" + this.state.locale + "&loginHint=" + encodeURIComponent(this.state.loginHint),
                height: 5000,
                width: 800,
                successCallback: (message) => {
                    //document.getElementById('messageDisplay').innerHTML = message;
                    console.log("AppTeams_authUserPromise successCallback result:");
                    console.log(message);

                    //localStorage.setItem("AuthUserStatus", "AppTeams_teamsAuthentication_successCallback" + message);
                    this.setState({
                        isAuthenticated: true,
                        authUser: "AppTeams_teamsAuthentication_successCallback"
                    });
                },
                failureCallback: (message) => {
                    //document.getElementById('messageDisplay').innerHTML = message;
                    console.log("AppTeams_authUserPromise failureCallback:");
                    console.log(message);

                    //localStorage.setItem("AuthUserStatus", "AppTeams_authUserPromise_failureCallback" + message);
                }
            });
        }, 2000);
    }

    // This function is used only for tab mobile, teams browser & teams client are handled via the TabAuth component
    async browserAuthentication() {
        let isAuthenticated = this.authHelper.userIsAuthenticated();
        let loginHint = "";
        if (this.props.teamsContext !== null && this.props.teamsContext !== undefined) {
            loginHint = this.props.teamsContext.loginHint;
        }

        console.log("AppTeams_browserAuthentication v3 START authSeq: " + this.authSeq + " loginHint: " + loginHint + " isAuthenticated: " + isAuthenticated);

        if (isAuthenticated.includes("error")) {
            console.log("AppTeams_browserAuthentication isAuthenticated: " + isAuthenticated);

            let extraParameters = "login_hint=" + encodeURIComponent(loginHint);
            console.log("AppTeams_browserAuthentication acquireTokenSilentAsync extraParameters: " + extraParameters);

            const tabAuthSeq1 = await this.authHelper.acquireTokenSilentAsync();

            if (tabAuthSeq1.includes("error")) {
                const tabAuthSeq2 = await this.logonInteractive();
            } else {
                const tabAuthSeq2 = await this.authHelper.acquireWebApiTokenSilentAsync();

                //localStorage.setItem("AppTeamsState", tabAuthSeq2);
                if (!tabAuthSeq2.includes("error")) {
                    this.setState({
                        authUser: "AppTeams_teamsAuthentication_successCallback"
                    });
                }
            }
        }

        return "AppTeams_browserAuthentication_finish";
    }

    // This function is used only for tab mobile, teams browser & teams client are handled via the TabAuth component
    async logonInteractive() {
        window.setTimeout(function () {
            this.authHelper.loginRedirect();
        }, 500);

        return "loginRedirect";
    }

    async callGetUserProfile() {
        console.log("AppTeams_callGetUserProfile start authUser: " + this.state.authUser);
        if (window.location.pathname !== "/tab/config") {
            try {
                const userProfile = await this.authHelper.callGetUserProfile();
                console.log("AppTeams_callGetUserProfile: " + userProfile.userPrincipalName);

                if (userProfile.userPrincipalName.length > 0) {
                    this.setState({
                        userProfile: userProfile,
                        isAuthenticated: true,
                        displayName: `Hello ${userProfile.displayName}!`,
                        authUser: "callGetUserProfile_success"
                    });

                    return "callGetUserProfile_success";
                }
                else {
                    console.log("AppTeams_callGetUserProfile finished authUser=callGetUserProfile userProfile.userPrincipalName empty");
                    this.setState({
                        authUser: "callGetUserProfile_error"
                    });

                    return "AppTeams_callGetUserProfile error finished authUser=callGetUserProfile userProfile.userPrincipalName empty";
                }
            } catch (err) {
                console.log("AppTeams_callGetUserProfile error:");
                console.log(err);

                this.setState({
                    authUser: "callGetUserProfile_error"
                });

                return "AppTeams_callGetUserProfile insideif authUser=callGetUserProfile error:" + err;
            }
        } else {
            return "callGetUserProfile_success";
        }
    }

    //getting client settings
    async getClientSettings() {
        let clientSettings = { "reportId": "", "workspaceId": "", "teamsAppInstanceId": "" };
        try {
            console.log("AppTeams_getClientSettings");
            let requestUrl = 'api/Context/GetClientSettings';

            let data = await fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            });
            let response = await data.json();

            return response;
        } catch (error) {
            console.log("AppTeams_getClientSettings error: ", error);
            return error;
        }
    }

    async getTeamsContext() {
        try {
            if (this.state.loginHint.length > 0) {
                console.log("AppTeams_getTeamsContext values already exists loginHint: " + this.state.loginHint);

                return "AppTeams_getTeamsContext success loginHint: " + this.state.loginHint;
            }
            else {

                let context = await microsoftTeams.getContext();
                console.log("AppTeams_getTeamsContext context ==>", context);

                microsoftTeams.getContext(context => {
                    console.log("AppTeams_getTeamsContext  ==> context", context);
                    if (context) {
                        this.setState({
                            channelName: context.channelName,
                            channelId: context.channelId,
                            teamName: context.teamName,
                            groupId: context.groupId,
                            loginHint: context.loginHint,
                            locale: context.locale,
                            authUser: "start"
                        });
                    }
                });
                console.log("AppTeams_getTeamsContext context Test ==>", context);
                console.log(context);
                return "AppTeams_getTeamsContext started - " + window.location.pathname;
            }
        } catch (err) {
            console.log("AppTeams_getTeamsContext error: " + err);
            return "AppTeams_getTeamsContext error: " + err;
        }
    }

    // Sign the user out of the session.
    logout() {
        this.authHelper.logout()
            .then(() => {
                this.setState({
                    isAuthenticated: false,
                    displayName: ''
                });
            });
    }

    // Grabs the font size in pixels from the HTML element on your page.
    pageFontSize = () => {
        let sizeStr = window.getComputedStyle(document.getElementsByTagName('html')[0]).getPropertyValue('font-size');
        sizeStr = sizeStr.replace('px', '');
        let fontSize = parseInt(sizeStr, 10);
        if (!fontSize) {
            fontSize = 16;
        }
        return fontSize;
    }

    // Sets the correct theme type from the query string parameter.
    updateTheme = (themeStr) => {
        let theme;
        switch (themeStr) {
            case 'dark':
                theme = ThemeStyle.Dark;
                break;
            case 'contrast':
                theme = ThemeStyle.HighContrast;
                break;
            case 'default':
            default:
                theme = ThemeStyle.Light;
        }
        this.setState({ theme });
    }

    // Returns the value of a query variable.
    getQueryVariable = (variable) => {
        const query = window.location.search.substring(1);
        const vars = query.split('&');
        for (const varPairs of vars) {
            const pair = varPairs.split('=');
            if (decodeURIComponent(pair[0]) === variable) {
                return decodeURIComponent(pair[1]);
            }
        }
        return "";
    }

    render() {
        const teamsContext = {
            channelName: this.state.channelName,
            channelId: this.state.channelId,
            teamName: this.state.teamName,
            groupId: this.state.groupId,
            loginHint: this.state.loginHint,
            locale: this.state.locale
        };

        //Setting the locale in Teams
        i18n.init({ lng: this.state.locale }, function (t) {
            i18n.t('key');
        });

        const ConfigView = ({ match }) => {
            return <Config teamsContext={teamsContext} appSettings={this.state.appSettings} />;
        };

        const TabAuthView = ({ match }) => {
            return <TabAuth teamsContext={teamsContext} />;
        };

        const RootTabView = ({ match }) => {
            return <RootTab teamsContext={teamsContext} />;
        };

        const AdministrationView = ({ match }) => {
            return <Administration teamsContext={teamsContext} appSettings={this.state.appSettings} />;
        };

        const ConfigurationView = ({ match }) => {
            return <Configuration teamsContext={teamsContext} />;
        };

        const AddDealTypeView = ({ match }) => {
            return <AddDealType teamsContext={teamsContext} />;
        };
        const AddDealTypeViewR = ({ match }) => {
            return <AddDealTypeR teamsContext={teamsContext} />;
        };

        const GeneralView = ({ match }) => {
            return <General teamsContext={teamsContext} appSettings={this.state.appSettings} />;
        };
        const CustomerDecisionView = ({ match }) => {
            return <CustomerDecision teamsContext={teamsContext} />;
        };

        const ChecklistView = ({ match }) => {
            return <Checklist teamsContext={teamsContext} />;
        };

        const ProposalStatusView = ({ match }) => {
            return <ProposalStatus teamsContext={teamsContext} />;
        };

        // Mobile
        const RootTabMobView = ({ match }) => {
            return <RootTabMob teamsContext={teamsContext} />;
        };

        return (
            <div className="ms-font-m show">
                <Route exact path='/tabmob/rootTab' component={RootTabMobView} />
                <Route exact path='/tabmob/proposalStatusTab' component={ProposalStatusView} />
                <Route exact path='/tabmob/checklistTab' component={ChecklistView} />
                <Route exact path='/tabmob/customerDecisionTab' component={CustomerDecisionView} />
                <Route exact path='/tabmob/generalConfigurationTab' component={ConfigurationView} />
                <Route exact path='/tabmob/generalAdministrationTab' component={AdministrationView} />
                <Route exact path='/tabmob/generalDashboardTab' component={GeneralView} />
                <Route exact path='/tabmob/generalAddDealType' component={AddDealTypeView} />
                <Route exact path='/tabmob/OpportunityDetails' component={OpportunityDetails} />
                <Route exact path='/tabmob/ChooseTeam' component={ChooseTeam} />
                <Route exact path='/tabmob/tabauth' component={TabAuthView} />

                <Route exact path='/tab' component={Home} />
                <Route exact path='/tab/config' component={ConfigView} />
                <Route exact path='/tab/tabauth' component={TabAuthView} />
                <Route exact path='/tab/privacy' component={Privacy} />
                <Route exact path='/tab/termsofuse' component={TermsOfUse} />

                <Route exact path='/tab/proposalStatusTab' component={ProposalStatusView} />
                <Route exact path='/tab/checklistTab' component={ChecklistView} />
                <Route exact path='/tab/rootTab' component={RootTabView} />
                <Route exact path='/tab/customerDecisionTab' component={CustomerDecisionView} />

                <Route exact path='/tab/generalConfigurationTab' component={ConfigurationView} />
                <Route exact path='/tab/generalAdministrationTab' component={AdministrationView} />
                <Route exact path='/tab/generalDashboardTab' component={GeneralView} />
                <Route exact path='/tab/generalAddDealType' component={AddDealTypeView} />
                <Route exact path='/tab/generalAddDealTypeR' component={AddDealTypeViewR} />
                <Route exact path='/tab/OpportunityDetails' component={OpportunityDetails} />
                <Route exact path='/tab/ChooseTeam' component={ChooseTeam} />

            </div>
        );
    }
}