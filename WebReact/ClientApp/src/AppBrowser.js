/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

// Global imports
import React, { Component } from 'react';
import AuthHelper from './helpers/AuthHelper';
import { CommandBar } from 'office-ui-fabric-react/lib/CommandBar';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Col, Grid, Row } from 'react-bootstrap';
import { Trans } from "react-i18next";
import LoggingService from './helpers/LoggingService';

export class AppBrowser extends Component {
    displayName = AppBrowser.name

    constructor(props) {
        super(props);
        this.logService = new LoggingService();
        if (window.authHelper) {
            this.authHelper = window.authHelper;
        } else {
            // Initilize the AuthService and save it in the window object.
            this.authHelper = new AuthHelper();
            window.authHelper = this.authHelper;
        }

        this.state = {
            isAuthenticated: false,
            displayName: ""
        };
    }

    async login() {
        this.authHelper.loginPopupAsync()
            .then(res => {
                this.logService.log("AppBrowser_acquireTokenSilentAsync acquired token", res);
                this.authHelper.acquireWebApiTokenSilentAsync()
                    .then(res => {
                        const user = this.authHelper.getUser();
                        this.setState({ isAuthenticated: true, userDisplayName: user.name });
                    })
                    .catch(err => {
                        this.logService.log(`TabAuth_acquireTokenSilenAsync error: ${err}`);
                        this.setState({ isAuthenticated: false });
                    });
            })
            .catch(errPopup => {
                this.logService.log("errorPopup", errPopup);
            });
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
        const { userDisplayName, isAuthenticated } = this.state;

        this.logService.log("App browswer : render isAuthenticated", isAuthenticated);

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
                            </div>
                    }
                </div>
            </div>
        );
    }
}
