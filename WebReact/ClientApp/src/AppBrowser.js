/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

// Global imports
import React, { Component } from 'react';
import Promise from 'promise';
import AuthHelper from './helpers/AuthHelper';
import GraphSdkHelper from './helpers/GraphSdkHelper';
import Utils from './helpers/Utils';
import { Route } from 'react-router';
import { CommandBar } from 'office-ui-fabric-react/lib/CommandBar';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { Image } from 'office-ui-fabric-react/lib/Image';

import  appSettingsObject  from './helpers/AppSettings';
import { Layout } from './components/Layout';
import { Opportunities } from './components/Opportunities';
//import { Notifications } from './components/Notifications';
import { Administration } from './components/Administration/Administration';
import { Settings } from './components/Administration/Settings';
import { Setup } from './components/Administration/Setup';
import { OpportunityDetails } from './components/Opportunity/OpportunityDetails';

import { OpportunityChooseTeam } from './components/OpportunityChooseTeam';

// compoents-mobile
import { getQueryVariable } from './common';

import i18n from './i18n';
import {  Trans } from "react-i18next";

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

        // Setting the default values
        appSettings = {
            generalProposalManagementTeam: appSettingsObject.generalProposalManagementTeam,
            teamsAppInstanceId: appSettingsObject.teamsAppInstanceId,
            teamsAppName: appSettingsObject.teamsAppName,
            reportId: appSettingsObject.reportId,
            workspaceId: appSettingsObject.workspaceId
        };

		this.utils = new Utils();

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
        let clientSettings = { "reportId": "", "workspaceId": "", "teamsAppInstanceId": "" };
        try {
            console.log("AppBrowser_getClientSettings");
            let requestUrl = 'api/Context/GetClientSettings';

            let data = await fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            });
            let response = await data.json();

            return response;
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
		const userProfileData = this.state.userProfile;
        const userDisplayName = this.state.displayName;
		const isAuthenticated = this.state.isAuthenticated;

		const isLoading = this.state.isLoading;

		//get opportunity details
		const oppId = getQueryVariable('opportunityId') ? getQueryVariable('opportunityId') : "";

		//Inject props to components
		const OpportunitiesView = ({ match }) => {
			return <Opportunities userProfile={userProfileData} />;
		};

		const AdministrationView = ({ match }) => {
			return <Administration userProfile={userProfileData} />;
        };

        const SettingsView = ({ match }) => {
            return <Settings userProfile={userProfileData} />;
        };

        const SetupView = ({ match }) => {
            return <Setup userProfile={userProfileData} />;
        };

		const OppDetails = ({ match }) => {
			return <OpportunityDetails userProfile={userProfileData} opportunityId={oppId} />;
		};

		const ChooseTeam = ({ match }) => {
			return <OpportunityChooseTeam opportunityId={oppId} />;
		};

		// Route formatting:
		// <Route path="/greeting/:name" render={(props) => <Greeting text="Hello, " {...props} />} />
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
                            <Layout userProfile={userProfileData}>
								<Route exact path='/' component={OpportunitiesView} />
								
                                <Route exact path='/Administration' component={AdministrationView} />
                                <Route exact path='/Settings' component={SettingsView} />
                                <Route exact path='/Setup' component={SetupView} />

								<Route exact path='/OpportunityDetails' component={OppDetails} />
								
								<Route exact path='/OpportunityChooseTeam' component={ChooseTeam} />
							</Layout>
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
