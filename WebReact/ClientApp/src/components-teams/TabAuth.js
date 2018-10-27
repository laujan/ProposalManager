/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import Utils from '../helpers/Utils';
import * as microsoftTeams from '@microsoft/teams-js';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { Trans } from "react-i18next";

export class TabAuth extends Component {
    displayName = TabAuth.name

    constructor(props) {
        super(props);

        this.authHelper = window.authHelper;
        this.sdkHelper = window.sdkHelper;
        this.utils = new Utils();

        this.localStorePrefix = this.props.teamsContext.localStorePrefix 

        // Check to see if client is teams browser or client app
        this.inTeamsClient = false;
        if (navigator.userAgent.indexOf("Teams") !== -1) {
            this.inTeamsClient = true;
        }

        try {
            microsoftTeams.initialize();
        }
        catch (err) {
            console.log("TabAuth error initializing teams: ");
            console.log(err);
        }
        finally {
            console.log("TabAuth_Constructor navigator.userAgent: " + navigator.userAgent);

            if (navigator.userAgent.indexOf("Teams") !== -1) {
                // Do not set state when in Teams client otherwise it will throw an error when component loaded in authentication context
                console.log("TabAuth_Constructor_finally userAgent = Teams");
            }
            else {
                console.log("TabAuth_Constructor_finally userAgent = Browser");
                this.state = {
                    isAuthenticated: false
                };
            }

            /** Pass the Context interface to the initialize function below */
            //microsoftTeams.getContext(context => this.initialize(context));
        }

    }

    async componentDidMount() {
        let loginHint = this.props.teamsContext.loginHint;
        if (loginHint === null || loginHint === undefined) {
            loginHint = "";
        }

        const isAdminCall = await this.isAdminCall();
        console.log("TabAuth_componentDidMount loginHint: " + loginHint + " isAdminCall: " + isAdminCall);

        const isAuthenticated = await this.authHelper.userIsAuthenticatedAsync();

        console.log("TabAuth_componentDidMount userIsAuthenticated: " + isAuthenticated + " loginHint: " + loginHint);

        if (isAdminCall === "true") {
            const userHasGraphAdminToken = await this.authHelper.userHasGraphAdminToken();

            console.log("TabAuth_componentDidMount_isAdminCall loginHint: " + loginHint + " userHasGraphAdminToken: " + userHasGraphAdminToken);
            if (isAuthenticated === loginHint && userHasGraphAdminToken) {
                this.notifySuccess(true);
            } else {
                loginHint = "";
            }
        }

        if (isAuthenticated === loginHint && !isAdminCall) {
            this.notifySuccess(true);
        } else {
            loginHint = "";
        }

        if (isAuthenticated !== loginHint) {
            const resAquireTokenTeams = await this.acquireTokenTeams();
            console.log("TabAuth_componentDidMount resAquireTokenTeams: " + resAquireTokenTeams);
        }
    }

    async componentDidUpdate() {
        let loginHint = this.props.teamsContext.loginHint;
        if (loginHint === null || loginHint === undefined) {
            loginHint = "";
        }

        console.log("TabAuth_componentDidUpdate loginHint: " + loginHint);
    }

    async acquireTokenTeams() {
        let isAuthenticated = await this.authHelper.userIsAuthenticatedAsync();
        let loginHint = this.props.teamsContext.loginHint;
        if (loginHint === null || loginHint === undefined) {
            loginHint = "";
        }
        const isAdminCall = await this.isAdminCall();
        const userHasGraphAdminToken = await this.authHelper.userHasGraphAdminToken();

        if (isAdminCall && !userHasGraphAdminToken) {
            isAuthenticated = "error no graph admin token";
            console.log("TabAuth_acquireTokenTeams isAuthenticated: " + isAuthenticated);
        }

        console.log("TabAuth_acquireTokenTeams v3 START loginHint: " + loginHint + " isAuthenticated: " + isAuthenticated);

        if (isAuthenticated.includes("error")) {
            console.log("TabAuth_acquireTokenTeams isAuthenticated: " + isAuthenticated);

            let extraParameters = "login_hint=" + encodeURIComponent(loginHint);
            console.log("TabAuth_acquireTokenTeams acquireTokenSilentAsync extraParameters: " + extraParameters);

            if (!await isAdminCall) {
                const tabAuthSeq1 = await this.authHelper.acquireTokenSilentAsync();

                if (tabAuthSeq1.includes("error")) {
                    const tabAuthSeq2 = await this.logonInteractive();
                } else {
                    const tabAuthSeq2 = await this.authHelper.acquireWebApiTokenSilentAsync();

                    //localStorage.setItem("TabAuthState", tabAuthSeq2);
                    if (!tabAuthSeq2.includes("error")) {
                        this.notifySuccess(true);
                    }
                }
            } else {
                const tabAuthSeq1 = await this.authHelper.acquireTokenSilentAdminAsync();

                if (tabAuthSeq1.includes("error")) {
                    const tabAuthSeq2 = await this.logonInteractive();
                } else {
                    const tabAuthSeq2 = await this.authHelper.acquireTokenSilentAsync();

                    if (!tabAuthSeq2.includes("error")) {
                        const tabAuthSeq3 = await this.authHelper.acquireWebApiTokenSilentAsync();

                        //localStorage.setItem("TabAuthState", tabAuthSeq3);
                        if (!tabAuthSeq3.includes("error")) {
                            this.notifySuccess(true);
                        }
                    }
                }
                
            } 
        }

        console.log("TabAuth_acquireTokenTeams FINISH");
    }

    async logonInteractive() {
        if (await this.isAdminCall()) {
            window.setTimeout(function () {
                this.authHelper.loginRedirectAdmin();
            }, 500);

            return "loginRedirect";
        } else {
            window.setTimeout(function () {
                this.authHelper.loginRedirect();
            }, 500);

            return "loginRedirect";
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
            console.log("TabAuth_isAdminCall error: " + err);
            return "false";
        }
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

    // Returns the value of a query variable from href.
    getHrefQueryVariable = (variable) => {
        const query = window.location.href.substring(1);
        const vars = query.split('&');
        for (const varPairs of vars) {
            const pair = varPairs.split('=');
            if (decodeURIComponent(pair[0]) === variable) {
                return decodeURIComponent(pair[1]);
            }
        }
        return "";
    }

    logout() {
        this.authHelper.logout().then(() => {
            this.setState({
                isAuthenticated: false,
                displayName: ''
            });
        });
    }

    notifySuccessBtnClick() {
        microsoftTeams.authentication.notifySuccess();
    }

    notifySuccess(force) {
        microsoftTeams.authentication.notifySuccess("notifySuccess");
    }

    notifyFailure() {
        microsoftTeams.authentication.notifyFailure();
    }

    render() {

        return (
            <div className="BgConfigImage ">
                <h2 className='font-white text-center darkoverlay'><Trans>proposalManager</Trans></h2>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 mt50 mb50 text-center'>
                <div className='TabAuthLoader'>
                    <Spinner size={SpinnerSize.large} label={<Trans>loadingYourExperience</Trans>} ariaLive='assertive' />
                    </div>
                    </div>
                </div>

                <div className='ms-Grid-row mt50'>
                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12  text-center'>
                        <PrimaryButton className='ml10 backbutton ' onClick={this.logout.bind(this)}>
                            <Trans>resetToken</Trans>
                        </PrimaryButton>
              
                        <PrimaryButton className='ml10 backbutton ' onClick={this.notifySuccessBtnClick.bind(this)}>
                            <Trans>forceclose</Trans>
                        </PrimaryButton>
                    </div>
                </div>
            </div>
        );
    }
}
