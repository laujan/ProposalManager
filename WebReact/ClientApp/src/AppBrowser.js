/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

// Global imports
import React, { Component } from 'react';
import AuthHelper from './helpers/AuthHelper';
import GraphSdkHelper from './helpers/GraphSdkHelper';
import Utils from './helpers/Utils';
import { CommandBar } from 'office-ui-fabric-react/lib/CommandBar';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { Link } from 'office-ui-fabric-react/lib/Link';
import appSettingsObject from './helpers/AppSettings';
import { Col, Grid, Row } from 'react-bootstrap';
import { Trans } from "react-i18next";

var appSettings;

export class AppBrowser extends Component {
    displayName = AppBrowser.name

    constructor(props) {
        super(props);

        if (window.authHelper) {
            this.authHelper = window.authHelper;
        } else {
            // Initilize the AuthService and save it in the window object.
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

        if (window.utils) {
            this.utils = window.utils;
        } else {
            // Initilize the utils and save it in the window object.
            this.utils = new Utils();
            window.utils = this.utils;
        }


        // Setting the default values
        appSettings = {
            generalProposalManagementTeam: appSettingsObject.generalProposalManagementTeam,
            teamsAppInstanceId: appSettingsObject.teamsAppInstanceId,
            teamsAppName: appSettingsObject.teamsAppName,
            reportId: appSettingsObject.reportId,
            workspaceId: appSettingsObject.workspaceId
        };

        const userProfile = { id: "", displayName: "", mail: "", phone: "", picture: "", userPrincipalName: "", roles: [] };

        this.state = {
            isAuthenticated: false,
            userProfile: userProfile,
            isLoading: false
        };
    }

    async componentDidMount() {
        console.log("AppBrowser_componentDidMount v1 window.location.pathname: " + window.location.pathname);

        await this.handleGraphAdminToken();

        if (window.location.pathname.toLowerCase() !== "/setup") {
            const isAuthenticated = await this.authHelper.userIsAuthenticatedAsync();

            console.log("AppBrowser_componentDidMount userIsAuthenticated: ");
            console.log(isAuthenticated);

            if (!isAuthenticated.includes("error") && window.location.pathname.toLowerCase() !== "/setup") {
                this.setState({
                    isAuthenticated: true
                });
            }
        }
    }

    async componentDidUpdate() {
        console.log("AppBrowser_componentDidUpdate window.location.pathname: " + window.location.pathname + " state.isAuthenticated: " + this.state.isAuthenticated);

        const isAuthenticated = await this.authHelper.userIsAuthenticatedAsync();

        if (window.location.pathname.toLowerCase() !== "/setup") {
            console.log("AppBrowser_componentDidMount userIsAuthenticated: " + isAuthenticated);

            if (isAuthenticated.includes("error")) {
                const resAquireToken = await this.acquireToken();
                console.log("AppBrowser_componentDidUpdate resAquireToken: " + resAquireToken);
            }

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
                        console.log("AppTeams_componentDidMount_getClientSettings  ==>", res);
                    })
                    .catch(err => {
                        console.log("AppTeams_componentDidMount_getClientSettings error:", err);
                    });
            }
        }
    }

    async acquireToken() {
        const isAuthenticated = await this.authHelper.userIsAuthenticatedAsync();
        const isAdminCall = await this.isAdminCall();

        console.log("AppBrowser_acquireTokenTeams START isAuthenticated: " + isAuthenticated + " isAdminCall: " + isAdminCall);

        if (isAuthenticated.includes("error")) {
            if (isAdminCall === "false") {
                const tabAuthSeq1 = await this.authHelper.acquireTokenSilentAsync();

                if (tabAuthSeq1.includes("error")) {
                    const tabAuthSeq2 = await this.authHelper.loginPopupAsync();

                    if (!tabAuthSeq2.includes("error")) {
                        const tabAuthSeq3 = await this.authHelper.acquireTokenSilentAsync();

                        if (!tabAuthSeq3.includes("error")) {
                            const tabAuthSeq4 = await this.authHelper.acquireWebApiTokenSilentAsync();

                            if (!tabAuthSeq4.includes("error")) {
                                localStorage.setItem("AppBrowserState", "callGetUserProfile");

                                if (window.location.pathname.toLowerCase() !== "/setup") {
                                    localStorage.setItem("AppBrowserState", "");
                                    const userProfile = await this.authHelper.callGetUserProfile();

                                    if (userProfile !== null && userProfile !== undefined) {
                                        console.log("AppBrowser_acquireTokenTeams callGetUserProfile success");
                                        this.setState({
                                            userProfile: userProfile,
                                            isAuthenticated: true,
                                            displayName: `${userProfile.displayName}!`
                                        });

                                        //Granular Access Start:
                                        //Trial calling,will remove this
                                        this.authHelper.callCheckAccess(["administrator", "opportunities_read_all"]).then(data => console.log("Granular AppBrowser: ", data));
                                        //Granular Access end:
                                        console.log("AppBrowser_acquireTokenTeams callGetUserProfile finish");
                                    } else {
                                        console.log("AppBrowser_acquireTokenTeams callGetUserProfile error");
                                        localStorage.setItem("AppBrowserState", "");
                                        this.setState({
                                            isAuthenticated: false
                                        });
                                    }
                                } else {
                                    localStorage.setItem("AppBrowserState", "");
                                    const getUser = await this.authHelper.getUserAsync();
                                    console.log("AppBrowser_acquireTokenTeams in /setup getUserAsync: ");
                                    console.log(getUser);
                                    const userProfile = { id: getUser.displayableId, displayName: getUser.displayableId, mail: getUser.displayableId, phone: "", picture: "", userPrincipalName: "", roles: [] };
                                    this.setState({
                                        userProfile: userProfile,
                                        isAuthenticated: true,
                                        displayName: `${getUser.displayableId}!`
                                    });
                                }
                            }
                        }
                    }
                } else {
                    const tabAuthSeq1 = await this.authHelper.acquireWebApiTokenSilentAsync();

                    if (!tabAuthSeq1.includes("error")) {
                        localStorage.setItem("AppBrowserState", "callGetUserProfile");
                        this.setState({
                            isAuthenticated: true
                        });
                    }
                }
            } else { // IsAdmin = true
                console.log("AppBrowser_acquireTokenTeams IsAdmin = true");
                const tabAuthSeq1 = await this.authHelper.acquireTokenSilentAdminAsync();

                if (tabAuthSeq1.includes("error")) {
                    const tabAuthSeq2 = await this.authHelper.loginPopupAdminAsync();

                    if (!tabAuthSeq2.includes("error")) {
                        const tabAuthSeq3 = await this.authHelper.acquireTokenSilentAdminAsync();

                        if (!tabAuthSeq3.includes("error")) {
                            const tabAuthSeq4 = await this.authHelper.acquireTokenSilentAsync();

                            if (!tabAuthSeq4.includes("error")) {
                                const tabAuthSeq5 = await this.authHelper.acquireWebApiTokenSilentAsync();

                                if (!tabAuthSeq5.includes("error")) {
                                    localStorage.setItem("AppBrowserState", "callGetUserProfile");

                                    if (window.location.pathname.toLowerCase() !== "/setup") {
                                        localStorage.setItem("AppBrowserState", "");
                                        const userProfile = await this.authHelper.callGetUserProfile();
                                        if (userProfile !== null && userProfile !== undefined) {
                                            console.log("AppBrowser_acquireTokenTeams callGetUserProfile success");
                                            this.setState({
                                                userProfile: userProfile,
                                                isAuthenticated: true,
                                                displayName: `${userProfile.displayName}!`
                                            });

                                            //Granular Access Start:
                                            //Trial calling,will remove this
                                            this.authHelper.callCheckAccess(["administrator", "opportunities_read_all"]).then(data => console.log("Granular AppBrowser: ", data));
                                            //Granular Access end:
                                            console.log("AppBrowser_acquireTokenTeams callGetUserProfile finish");
                                        } else {
                                            console.log("AppBrowser_acquireTokenTeams callGetUserProfile error");
                                            localStorage.setItem("AppBrowserState", "");
                                            this.setState({
                                                isAuthenticated: false
                                            });
                                        }
                                    } else {
                                        localStorage.setItem("AppBrowserState", "");
                                        const getUser = await this.authHelper.getUserAsync();
                                        console.log("AppBrowser_acquireTokenTeams in /setup getUserAsync: ");
                                        console.log(getUser);
                                        const userProfile = { id: getUser.displayableId, displayName: getUser.displayableId, mail: getUser.displayableId, phone: "", picture: "", userPrincipalName: "", roles: [] };
                                        this.setState({
                                            userProfile: userProfile,
                                            isAuthenticated: true,
                                            displayName: `${getUser.displayableId}!`
                                        });
                                    }
                                }
                            }
                        }
                    }
                } else {
                    const tabAuthSeq2 = await this.authHelper.acquireTokenSilentAsync();

                    if (!tabAuthSeq2.includes("error")) {
                        const tabAuthSeq3 = await this.authHelper.acquireWebApiTokenSilentAsync();

                        if (!tabAuthSeq3.includes("error")) {
                            localStorage.setItem("AppBrowserState", "callGetUserProfile");
                            this.setState({
                                isAuthenticated: true
                            });
                        }
                    }
                }

            }
        }

        console.log("AppBrowser_acquireTokenTeams FINISH");
        return "AppBrowser_acquireTokenTeams FINISH";
    }

    async isAdminCall() {
        try {
            if (window.location.pathname.includes("/Administration") || window.location.pathname.includes("/Setup")) {
                return "true";
            }
            else {
                return "false";
            }
        } catch (err) {
            console.log("AppBrowser_isAdminCall error: " + err);
            return "false";
        }
    }

    async handleGraphAdminToken() {
        try {
            const isAdminCall = await this.isAdminCall();

            // Store the original request so we can detect the type of token in TabAuth
            localStorage.setItem(appSettings.localStorePrefix + "appbrowser.request", window.location.pathname);
            if (isAdminCall !== "true") {
                // Clear GraphAdminToken in case user navigates to admin then to another non-admin tab
                const graphAdminTokenStoreKey = appSettings.localStorePrefix + "AdminGraphToken";
                localStorage.removeItem(graphAdminTokenStoreKey);
            }

            return true;
        } catch (err) {
            console.log("AppBrowser_handleGraphAdminToken error: " + err);
            return false;
        }

    }

    //getting client settings
    async getClientSettings() {
        try {
            console.log("AppBrowser_getClientSettings");
            let requestUrl = 'api/Context/GetClientSettings';

            let data = await fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            });
            return await data.json();
        } catch (error) {
            console.log("AppBrowser_getClientSettings error: ", error);
            return error;
        }
    }

    async login() {
        const resAquireToken = await this.acquireToken();
        console.log("AppBrowser_login resAquireToken: ");
        console.log(resAquireToken);
        return resAquireToken;
    }

    // Sign the user out of the session.
    logout() {
        localStorage.removeItem("AppBrowserState");
        this.authHelper.logout().then(() => {
            this.setState({
                isAuthenticated: false,
                displayName: ''
            });
        });
    }


    render() {
        const userDisplayName = this.state.displayName;
        const isAuthenticated = this.state.isAuthenticated;

        const isLoading = this.state.isLoading;


        console.log("App browswer : render isAuthenticated", isAuthenticated);

        return (
            <div>
                <CommandBar farItems={
                    [

                        {
                            key: 'display-hello',
                            name: this.state.isAuthenticated ? <Trans>hello</Trans> : ""
                        },
                        {

                            key: 'display-name',
                            name: userDisplayName
                        },
                        {
                            key: 'log-in-out=button',
                            name: this.state.isAuthenticated ? <Trans>signout</Trans> : <Trans>signin</Trans>,
                            onClick: this.state.isAuthenticated ? this.logout.bind(this) : this.login.bind(this)
                        }
                    ]
                }
                />

                <div className="ms-font-m show">
                    {
                        isAuthenticated ?
                            <div>
                                <Grid fluid>
                                    <Row>
                                        <Col sm={2}/>
                                        <Col sm={8} className='p20ALL'><br />
                                            <h2 className="ms-textAlignCenter">Welcome to Proposal Manager</h2>                                            
                                            <div className="wrapper">
                                                <p>As of now installation and basic configuration of proposal manager is complete.  You have given the required consent for the application. There are few more steps you need to complete before making proposal manager available to everyone in the organization</p>
                                                <p>
                                                    1. <b>Login</b> to Microsoft Teams as O365 Global Administrator (same credential used during installation) using the Teams link shown in the figure.
                                                </p>
                                                <p>
                                                   2. After login,
                                                </p>
                                                <Link href="https://teams.microsoft.com" target="_blank">
                                                    <img src={require('./Images/teamsLogin.png')} alt="" className="teamsLogo" />
                                                </Link>
                                                <p>
                                                    <ul>
                                                        <li>
                                                            <b>Upload the Proposal Manager Teams Add-in package</b> in the Teams app store. Use "upload a custom app" option and select the proposal manager add-in zip file generated in the Setup folder.
                                                        </li>
                                                        <li>
                                                            Go to the "proposal manager" team created in the MS Teams. There will be a <b>Setup channel</b>. <b>Add a Tab &#x2192; </b> search for "Proposal Manager" app.
                                                        </li>
                                                        <li>
                                                            Now in "Proposal manager" tab app you can <b>complete the setup</b> for SharePoint site and add an administrator for proposal manager.  Other features can be configured later.<br />
                                                            <span>Refer to <b>Help channel</b> or <b>Getting Started Guide</b> for guidance on above setup.</span>
                                                        </li>
                                                    </ul>
                                                </p>
                                                <p>
                                                    <b>Proposal Manager is now ready.</b><br />
                                                    You need to <b>restart the Proposal Manager Azure App </b>after setup changes.<br /><br/>
                                                    Next you could login with the credentials of the proposal manager administrator you have added during the setup process.  After login, you can configure templates, and associate users and their roles. Refer to <b>Help channel</b> or <b>Getting Started Guide</b> for guidance on configuration.

                                                </p>


                                            </div>
                                            
                                        </Col>
                                        <Col sm={2} />
                                    </Row>
                                </Grid>
                            </div>

                            :
                            <div className="BgImage">
                                <div className="Caption">
                                    <h3> <span> <Trans>empowerBanking</Trans> </span></h3>
                                    <h2> <Trans>proposalManager</Trans></h2>
                                </div>
                                {
                                    isLoading &&
                                    <div className='Loading-spinner'>
                                        <Spinner className="Homelaoder Homespinnner" size={SpinnerSize.medium} label={<Trans>loadingYourExperience</Trans>} ariaLive='assertive' />
                                    </div>
                                }
                            </div>
                    }
                </div>
            </div>
        );
    }
}
