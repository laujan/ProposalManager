/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import * as microsoftTeams from '@microsoft/teams-js';
import { appUri } from '../helpers/AppSettings';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { Trans } from "react-i18next";

export class Config extends Component {
	displayName = Config.name

	constructor(props) {
		super(props);

        this.apiService = this.props.apiService;
        this.logService = this.props.logService;

        this.logService.log("Config: Contructor", props);
        try
        {
			microsoftTeams.initialize();
		}
        catch (err)
        {
            this.logService.log("Config_constructor error initializing teams: " + JSON.stringify(err));
		}
	}

    componentDidMount() {
        this.logService.log("Config_componentDidMount appSettings: ", this.props.appSettings);

        if (this.props.appSettings.generalProposalManagementTeam.length > 0)
        {
            this.setChannelConfig();
        }
        else
        {
            microsoftTeams.settings.setValidityState(false);
        }
	}

	//Get Opportunitydata by TeamName
    async getOpportunityByName()
    {
        let teamName = this.props.teamsContext.teamName;

        try {
            let response = await this.apiService.callApi('Opportunity', 'GET', { query: `name=${teamName}` });
            if (response.ok) {
                let data = await response.json();
                this.logService.log("Config_getOpportunityByName userRoleList data length: ", data.length, data);

                let processList = data.template.processes;
                let oppChannels = processList.filter(x => x.channel.toLowerCase() !== "none");
                this.logService.log("Config_getOpportunityByName userRoleList lenght: ", oppChannels.length);
                return { userRoleList: oppChannels };
            }
        }
        catch (err) {
            throw new Error("Error retrieving getOpportunityByName: ", err);
        }
    }

    async setChannelConfig() {
        let tabName = "";
        let teamName = this.props.teamsContext.teamName;
        let channelId = this.props.teamsContext.channelId;
        let channelName = this.props.teamsContext.channelName;
        let loginHint = this.props.teamsContext.loginHint;
        let locale = this.props.teamsContext.locale;

        if (teamName !== null && teamName !== undefined)
        {
            this.logService.log("Config_setChannelConfig generalSharePointSite: ", this.props.teamsContext.teamsAppName);

            if (teamName === this.props.appSettings.generalProposalManagementTeam)
            {
                switch (channelName) {
                    case "General":
                        tabName = "generalDashboardTab";
                        break;
                    case "Configuration":
                        tabName = "generalConfigurationTab";
                        break;
                    case "Administration":
                        tabName = "generalAdministrationTab";
                        break;
                    case "Setup":
                        tabName = "setupTab";
                        break;
                    case "Help":
                        tabName = "helpTab";
                        break;
                    default:
                        tabName = "generalDashboardTab";
                }
                this.logService.log("Config_setChannelConfig generalSharePointSite tabName: ", tabName);
            }
            else
            {
                try {
                    let res = await this.getOpportunityByName();

                    let channelMapping = res.userRoleList.filter(x => x.channel.toLowerCase() === channelName.toLowerCase());
                    this.logService.log("Config_setChannelConfig channelMapping.length:", channelMapping);

                    if (channelName === "General") {
                        tabName = "rootTab";
                    }
                    else if (channelMapping.length > 0) {
                        if (channelMapping.processType !== "Base" && channelMapping.processType !== "Administration") {
                            this.logService.log("Config_setChannelConfig channelMapping.lenght >0: " + channelMapping[0].processType);
                            tabName = channelMapping[0].processType;
                        }
                    }
                    this.logService.log("Config_setChannelConfig tabName: ", tabName, " ChannelName: ", channelName);
                }
                catch (err) {
                    this.logService.log("Config_getOpportunityByName error: ", err);
                    microsoftTeams.settings.setValidityState(false);
                }
            }

            if (tabName !== "")
            {
                let self = this;
                microsoftTeams.settings.registerOnSaveHandler(function (saveEvent) {
                    microsoftTeams.settings.setSettings({
                        entityId: "PM" + channelName,
                        contentUrl: appUri + "/tab/" + tabName + "?channelName=" + channelName + "&teamName=" + teamName + "&channelId=" + channelId + "&locale=" + locale + "&loginHint=" + encodeURIComponent(loginHint),
                        suggestedDisplayName: self.props.appSettings.generalProposalManagementTeam,
                        websiteUrl: appUri + "/tabMob/" + tabName + "?channelName=" + channelName + "&teamName=" + teamName + "&channelId=" + channelId + "&locale=" + locale + "&loginHint=" + encodeURIComponent(loginHint)

                    });
                    self.logService.log("Config_setChannelConfig microsoftTeams.settings: ", microsoftTeams.settings);
                    saveEvent.notifySuccess();
                });

                microsoftTeams.settings.setValidityState(true);
            }
        }
        else
        {
            microsoftTeams.settings.setValidityState(false);
        }
    }

    async logout() {
        await this.authHelper.logout(true);
    }

	refresh() {
		window.location.reload();
	}
    
	getQueryVariable = (variable) => {
		const query = window.location.search.substring(1);
		const vars = query.split('&');
		for (const varPairs of vars) {
			const pair = varPairs.split('=');
			if (decodeURIComponent(pair[0]) === variable) {
				return decodeURIComponent(pair[1]);
			}
		}
		return null;
	}

    render() {
        const userUpn = this.props.teamsContext.loginHint;
                // TODO: Add a text field for localStorePrefix
                // TODO: If you change this value, you must reload the tab by clicking the refresh button
		return (
			<div className="BgConfigImage">

				<br /><br /><br /><br /><br /><br /><br />	<br />
                <p className="WhiteFont"><Trans>hello</Trans> {userUpn ? userUpn : <Trans>welcome</Trans>}</p>

                <PrimaryButton className='pull-right refreshbutton' onClick={this.logout.bind(this)}>
                    <Trans>resetToken</Trans>
                </PrimaryButton>
                <br /><br />
				<PrimaryButton className='pull-right refreshbutton' onClick={this.refresh.bind(this)}>
					<Trans>refresh</Trans>
				</PrimaryButton>
			</div>
		);
	}
}