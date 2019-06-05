/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

// Global imports
import React, { Component } from 'react';
import * as microsoftTeams from '@microsoft/teams-js';
import { Route, Switch } from 'react-router-dom';
import Utils from './helpers/Utils';
import { Home } from './components-teams/Home';
import { Config } from './components-teams/Config';
import { Privacy } from './components-teams/Privacy';
import { TermsOfUse } from './components-teams/TermsOfUse';
import { Setup } from './components-teams/Setup';
import { Help } from './components-teams/Help';
import { Checklist } from './components-teams/Checklist';
import { RootTab } from './components-teams/RootTab'; 
import { TabAuth } from './components-teams/TabAuth';
import { ProposalStatus } from './components-teams/ProposalStatus';
import { CustomerDecision } from './components-teams/CustomerDecision';

// Components mobile
import { RootTab as RootTabMob } from './components-mobile/RootTab';
import { initializeIcons } from 'office-ui-fabric-react/lib/Icons';

import { Administration } from './components-teams/general/Administration';
import { General } from './components-teams/general/General';
import { Configuration } from './components-teams/general/Configuration';
import { AddDealTypeR } from './components-teams/general/DealType/AddDealTypeR';
import { OpportunityDetails } from './components-teams/general/Opportunity/OpportunityDetails';
import { ChooseTeam } from './components-teams/general/Opportunity/ChooseTeam';
import { AddTemplate } from './components-teams/general/Templates/AddTemplate';

import i18n from './i18n';

import AuthorizedRoute from './AuthorizedRoute';
import appSettingsObject from './helpers/AppSettings';

export class AppTeams extends Component {
    displayName = AppTeams.name

    constructor(props) {
        super(props);
        console.log("AppTeams: Contructor");
        initializeIcons();

        // Setting the default values
        this.state = {
            teamsContext: {
                channelName: "",
                channelId: "",
                teamName: "",
                groupId: "",
                loginHint: "",
                locale: "",
                localStorePrefix: ""
            },
            contextInit: false
        };

        if (window.utils) {
            this.utils = window.utils;
        } else {
            // Initilize the utils and save it in the window object.
            this.utils = new Utils();
            window.utils = this.utils;
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
    }

    async componentDidMount() {
        microsoftTeams.getContext(context => {
            console.log("AppTeams_getTeamsContext  ==> context", context);
            if (context) {
                let teamsFromContext = {
                    channelName: context.channelName,
                    channelId: context.channelId,
                    teamName: context.teamName,
                    groupId: context.groupId,
                    loginHint: context.loginHint,
                    locale: context.locale,
                    localStorePrefix: appSettingsObject.localStorePrefix
                };

                this.setState({ teamsContext: teamsFromContext, contextInit: true });
            }
        });
    }

    render() {
        const { teamsContext, contextInit } = this.state;
        console.log("AppTeams_render", this.state);

        let isMobile = window.location.pathname.toLocaleLowerCase().includes("tabmob");

        if (!isMobile && !contextInit) {
            return <div>Initializing applications...</div>;
        }
       
        //Setting the locale in Teams
        i18n.init({ lng: teamsContext.locale }, function (t) {
            i18n.t('key');
        });

        const TabAuthView = () => { return <TabAuth teamsContext={teamsContext}/>; };
        const AdministrationView = () => { return <Administration teamsContext={teamsContext}/>; };
        const ConfigurationView = () => { return <Configuration teamsContext={teamsContext}/>; };
        const AddTemplateView = () => { return <AddTemplate teamsContext={teamsContext}/>; };
        const GeneralView = () => { return <General teamsContext={teamsContext}/>; };
        const CustomerDecisionView = () => { return <CustomerDecision teamsContext={teamsContext}/>; };
        const ChecklistView = () => { return <Checklist teamsContext={teamsContext}/>; };
        const ProposalStatusView = () => { return <ProposalStatus teamsContext={teamsContext}/>; };

        // Mobile
        const RootTabMobView = () => { return <RootTabMob teamsContext={teamsContext}/>; };
        const HelpView = () => { return <Help teamsContext={teamsContext}/>; };

        return (
            <div className="ms-font-m show">
                <Switch>
                    <Route exact path='/tabmob/tabauth' component={TabAuthView} />
                    <AuthorizedRoute exact path='/tabmob/rootTab' component={RootTabMobView} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tabmob/proposalStatusTab' component={ProposalStatusView} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tabmob/checklistTab' component={ChecklistView} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tabmob/customerDecisionTab' component={CustomerDecisionView} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tabmob/generalConfigurationTab' component={ConfigurationView} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tabmob/generalAdministrationTab' component={AdministrationView} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tabmob/generalDashboardTab' component={GeneralView} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tabmob/OpportunityDetails' component={OpportunityDetails} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tabmob/ChooseTeam' component={ChooseTeam} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tabmob/generalAddTemplate' component={AddTemplateView} teamsContext={teamsContext} />

                    <Route exact path='/tab' component={Home} />
                    <Route exact path='/tab/privacy' component={Privacy} />
                    <Route exact path='/tab/termsofuse' component={TermsOfUse} />
                    <Route exact path='/tab/helpTab' component={HelpView} />
                    <Route exact path='/tab/tabauth' component={TabAuthView} />
                    <AuthorizedRoute exact path='/tab/config' component={Config} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tab/proposalStatusTab' component={ProposalStatus} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tab/checklistTab' component={Checklist} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tab/rootTab' component={RootTab} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tab/customerDecisionTab' component={CustomerDecision} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tab/generalConfigurationTab' component={Configuration} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tab/generalAdministrationTab' component={Administration} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tab/generalDashboardTab' component={General} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tab/generalAddDealTypeR' component={AddDealTypeR} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tab/OpportunityDetails' component={OpportunityDetails} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tab/ChooseTeam' component={ChooseTeam} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tab/generalAddTemplate' component={AddTemplate} teamsContext={teamsContext} />
                    <AuthorizedRoute exact path='/tab/setupTab' component={Setup} teamsContext={teamsContext} />
                </Switch>
            </div>
        );
    }
}