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
        this.utils = new Utils();

        this.localStorePrefix = this.props.teamsContext.localStorePrefix;

        try {
            microsoftTeams.initialize();
        }
        catch (err) {
            console.log("TabAuth error initializing teams:", err);
        }
    }

    componentDidMount() {
        this.authHelper.acquireWebApiTokenSilentAsync()
            .then(token => {
                microsoftTeams.authentication.notifySuccess(token);
            })
            .catch(err => {
                console.log("TabAuth_componentDidMount_acquiringWebApiToken error:", err);
                this.authHelper.loginRedirect()
                    .catch(loginError => {
                        console.log("TabAuth_logonInteractive_loginRedirect:", loginError);
                        microsoftTeams.authentication.notifyFailure(loginError);
                    });
            });
    }

    logout() {
        this.authHelper.logout();
    }

    notifySuccessBtnClick() {
        microsoftTeams.authentication.notifySuccess();
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
