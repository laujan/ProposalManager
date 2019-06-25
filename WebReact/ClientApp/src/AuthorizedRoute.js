/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import * as microsoftTeams from '@microsoft/teams-js';
import React from 'react';
import { Route } from 'react-router-dom';
import AuthHelper from './helpers/AuthHelper';
import ApiService from './helpers/ApiService';
import Accessdenied from './helpers/AccessDenied';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import Utils from './helpers/Utils';
import LoggingService from './helpers/LoggingService';

/**
 * Responsible for handling the authentication for protected routes and injects the required component services as props.
 * */
export class AuthorizedRoute extends Route {
    constructor(props) {
        super(props);
        this.logService = new LoggingService();
        this.logService.log("AuthorizedRoute ctor", props);

        this.state = { isAuthorized: false, appSettings: {}, userProfile: {}, refreshRequired: false };

        if (window.authHelper) {
            this.authHelper = window.authHelper;
        } else {
            // Initilize the AuthService and save it in the window object.
            this.authHelper = new AuthHelper();
            window.authHelper = this.authHelper;
        }

        this.utils = new Utils();

        try {
			/* Initialize the Teams library before any other SDK calls.
			 * Initialize throws if called more than once and hence is wrapped in a try-catch to perform a safe initialization.
			 */
            microsoftTeams.initialize();
        }
        catch (err) {
            this.logService.log(err);
        }
    }

    //getting client settings
    async getClientSettings(apiToken) {
        try {
            let requestUrl = 'api/Context/GetClientSettings';

            return await fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + apiToken }
            });
        } catch (error) {
            this.logService.log("AppTeams_getClientSettings error: ", error);
            return error;
        }
    }

    mapAppSettings(appSettingsObject) {
        return {
            generalProposalManagementTeam: appSettingsObject.GeneralProposalManagementTeam,
            teamsAppInstanceId: appSettingsObject.TeamsAppInstanceId,
            teamsAppName: appSettingsObject.TeamsAppName,
            reportId: appSettingsObject.ReportId,
            workspaceId: appSettingsObject.WorkspaceId
        };
    }

    async componentDidMount() {
        const { teamsContext } = this.props;

        let user = this.authHelper.getUser();
        let loginHint = this.props.teamsContext.loginHint;

        let isMobile = window.location.pathname.toLocaleLowerCase().includes("tabmob");
        if (isMobile) {
            this.handleMobileAuth(user);
            return;
        }

        if (user && user.displayableId === loginHint) {
            this.logService.log("AuthorizedRoute check user", user.displayableId, loginHint);
            let token = this.authHelper.getWebApiToken();
            this.authorizeUser(token);
        }
        else {
            // The users are different then clear cache
            if (user && user.displayableId !== loginHint) {
                this.logService.log("clearing msal cache");
                this.authHelper.clearCache();
            }

            this.logService.log("AuthorizedRoute_componentDidMount teamsContext", teamsContext);
            microsoftTeams.authentication.authenticate({
                url: window.location.protocol + '//' + window.location.host + '/tab/tabauth' + "?channelName=" + teamsContext.channelName + "&teamName=" + teamsContext.teamName + "&channelId=" + teamsContext.channelId + "&locale=" + teamsContext.locale + "&loginHint=" + encodeURIComponent(teamsContext.loginHint),
                height: 5000,
                width: 800,
                successCallback: (token) => {
                    this.logService.log("microsoftTeams.authentication success", token);
                    this.authorizeUser(token);
                },
                failureCallback: (message) => {
                    this.logService.log("microsoftTeams.authentication failureCallback:", message);
                    this.setState({ isAuthorized: false });
                }
            });
        }
    }

    async authorizeUser(token) {
        this.getClientSettings(token)
            .then(async response => {
                this.logService.log("getClientSettings", response);

                if (response.status === 401) {
                    this.setState({ refreshRequired: true });
                }

                let data = await response.json();
                let appSettings = this.mapAppSettings(data);
                let channelName = this.utils.getQueryVariable("channelName");

                if (channelName && channelName.toLocaleLowerCase() !== "setup") {
                    let userProfileResponse = await this.authHelper.callGetUserProfile();
                    this.setState({ isAuthorized: true, appSettings: appSettings, userProfile: { ...userProfileResponse }, apiService: new ApiService(token), logService: new LoggingService() });
                }
                else {
                    this.setState({ isAuthorized: true, appSettings: appSettings, apiService: new ApiService(token), logService: new LoggingService() });
                }
            })
            .catch(err => {
                this.logService.log("Error retrieving client settings:", err);
                this.setState({ isAuthorized: false });
            });
    }

    handleMobileAuth(user) {
        if (user) {
            let token = this.authHelper.getWebApiToken();
            this.authorizeUser(token);
        }
        else {
            this.authHelper.loginPopupAsync()
                .then(idToken => {
                    this.logService.log(`AuthorizedRoute_handleMobile idToken: ${idToken}`);
                    this.authHelper.acquireWebApiTokenSilentAsync()
                        .then(accessToken => {
                            this.logService.log(`AuthorizedRoute_handleMobile acquireWebApiTokenSilentAsync: ${accessToken}`);
                            this.authorizeUser(accessToken);
                        })
                        .catch(err => {
                            this.logService.log("AuthorizedRoute_handleMobile acquireWebApiTokenSilentAsync err:", err);
                            this.setState({ isAuthorized: false });
                        });
                    
                })
                .catch(errPopup => {
                    this.logService.log("AuthorizedRoute_handleMobile", errPopup);
                    this.setState({ isAuthorized: false });
                });
        }
    }

    render() {
        const { isAuthorized, appSettings, apiService, userProfile, refreshRequired, logService } = this.state;

        const { component: Component, ...rest } = this.props;

        const renderApp = () => {
            if (refreshRequired) {
                return (
                    <div className='ms-Grid'>
                        <div className='ms-Grid-row bg-white p-10'>
                            Your session has expired, please sign in again. <br />
                            <PrimaryButton text="Sign in" onClick={() => { this.authHelper.clearCache(); window.location.reload(); }} />
                        </div>
                    </div>
                );
            }

            if (isAuthorized) {
                this.logService.log("AuthorizedRoute authorized", appSettings, userProfile, this.props, this.state);
                return <Component {...this.props} appSettings={appSettings} apiService={apiService} userProfile={userProfile} logService={logService} />;
            }
            else {
                return Accessdenied();
            }
        };

        return (
            <Route {...rest}>
                {renderApp()}
            </Route>
        );
    }
}

export default AuthorizedRoute;