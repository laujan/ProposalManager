/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import * as microsoftTeams from '@microsoft/teams-js';
import AuthHelper from '../helpers/AuthHelper';
import GraphSdkHelper from '../helpers/GraphSdkHelper';
import { appUri } from '../helpers/AppSettings';
import Utils from '../helpers/Utils';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { Trans } from "react-i18next";

export class Config extends Component {
	displayName = Config.name

	constructor(props) {
		super(props);

        console.log("Config: Contructor");
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
			// Initilize the AuthService and save it in the window object.
			this.sdkHelper = new GraphSdkHelper();
			window.sdkHelper = this.sdkHelper;
		}

		this.utils = new Utils();

		this.authInProgress = false;
		try {
			microsoftTeams.initialize();
		}
		catch (err) {
			console.log("ProposalManagement_ConfigTAB error initializing teams: " + JSON.stringify(err));
		}
		finally {
			this.state = {
                isAuthenticated: this.authHelper.isAuthenticated(),
                validityState: false,
                userRoleList: []
            };
		}
	}


    componentDidMount() {
        console.log("Config_componentDidMount appSettings: ");
        console.log(this.props.appSettings);

        if (this.props.appSettings.generalProposalManagementTeam.length > 0) {
            this.setChannelConfig();
        }
	}

    componentDidUpdate() {
        microsoftTeams.settings.setValidityState(this.state.validityState);
	}

	getUserRoles() {
		// call to API fetch data
		return new Promise((resolve, reject) => {
			console.log("Config_getUserRoles userRoleList fetch");
			//WAVE-4 : Changing RoleMappong to Roles:
			let requestUrl = 'api/Roles';
			fetch(requestUrl, {
				method: "GET",
				headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
			})
				.then(response => response.json())
				.then(data => {
					try {
						console.log("Config_getUserRoles userRoleList data lenght: " + data.length);

						let userRoleList = [];
						//console.log(data);
						for (let i = 0; i < data.length; i++) {
							let userRole = {};
							userRole.id = data[i].id;
							userRole.adGroupName = data[i].adGroupName;
							userRole.roleName = data[i].roleName;
							userRole.processStep = data[i].processStep;
							userRole.channel = data[i].channel;
							userRole.adGroupId = data[i].adGroupId;
							userRole.processType = data[i].processType;
							userRoleList.push(userRole);
						}
						this.setState({ userRoleList: userRoleList });
						console.log("Config_getUserRoles userRoleList lenght: " + userRoleList.length);
						resolve(true);
					}
					catch (err) {
						reject(err);
					}

				});
		});
    }

    //Get Opportunitydata by TeamName
    getOpportunityByName() {
        return new Promise((resolve, reject) => {
            let teamName = this.props.teamsContext.teamName;

            console.log(`Config_getUserRoles userRoleList fetch ---api/Opportunity?name=${teamName}`);
			
			//let requestUrl = "api/Opportunity?name='" + teamName + "'";
			//changing to template string
            let requestUrl = `api/Opportunity?name=${teamName}`;

            console.log("Config_getOpportunityByName requestUrl: " + requestUrl);

            fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            })
                .then(response => response.json())
                .then(data => {
                    try {
                        console.log("Config_getOpportunityByName userRoleList data length: " + data.length);
                        console.log(data);
                        
                        let processList = data.template.processes;
                        let oppChannels = processList.filter(x => x.channel.toLowerCase() !== "none");
                        if (oppChannels.length > 0) {
                            console.log(oppChannels);
                            this.setState({ userRoleList: oppChannels });
                        }
                        console.log("Config_getOpportunityByName userRoleList lenght: " + oppChannels.length);
                        resolve(true);
                    }
                    catch (err) {
                        reject(err);
                    }

                });
        });
    }

    setChannelConfig() {
        let tabName = "";
        let teamName = this.props.teamsContext.teamName;
        let channelId = this.props.teamsContext.channelId;
        let channelName = this.props.teamsContext.channelName;
        let loginHint = this.props.teamsContext.loginHint;
        let locale = this.props.teamsContext.locale;

        if (teamName !== null && teamName !== undefined && !this.state.validityState) {
            console.log("Config_setChannelConfig generalSharePointSite: " + this.props.teamsContext.teamsAppName);

            if (teamName === this.props.appSettings.generalProposalManagementTeam) {
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
                console.log("Config_setChannelConfig generalSharePointSite tabName: " + tabName);
                let self =this;
                if (tabName !== "") {
                    microsoftTeams.settings.registerOnSaveHandler(function (saveEvent) {
                        microsoftTeams.settings.setSettings({
                            entityId: "PM" + channelName,
                            contentUrl: appUri + "/tab/" + tabName + "?channelName=" + channelName + "&teamName=" + teamName + "&channelId=" + channelId + "&locale=" + locale + "&loginHint=" + encodeURIComponent(loginHint),
                            suggestedDisplayName: self.props.appSettings.generalProposalManagementTeam,
                            websiteUrl: appUri + "/tabMob/" + tabName + "?channelName=" + channelName + "&teamName=" + teamName + "&channelId=" + channelId + "&locale=" + locale + "&loginHint=" + encodeURIComponent(loginHint)

                        });
                        saveEvent.notifySuccess();
                    });

                    this.setState({
                        validityState: true
                    });
                }

            } else {
                this.getOpportunityByName()
                    .then(res => {
                        let channelMapping = this.state.userRoleList.filter(x => x.channel.toLowerCase() === channelName.toLowerCase());
                        console.log("Config_setChannelConfig channelMapping.length:");
                        console.log(channelMapping);

                        if (channelName === "General") {
                            tabName = "rootTab";
                        } else if (channelMapping.length > 0) {
                            if (channelMapping.processType !== "Base" && channelMapping.processType !== "Administration") {
                                console.log("Config_setChannelConfig channelMapping.lenght >0: " + channelMapping[0].processType);
                                tabName = channelMapping[0].processType;
                            }
                        }

                        console.log("Config_setChannelConfig tabName: " + tabName + " ChannelName: " + channelName);

                        if (tabName !== "") {
                            microsoftTeams.settings.registerOnSaveHandler(function (saveEvent) {
                                microsoftTeams.settings.setSettings({
                                    entityId: "PM" + channelName,
                                    contentUrl: appUri + "/tab/" + tabName + "?channelName=" + channelName + "&teamName=" + teamName + "&channelId=" + channelId + "&locale=" + locale + "&loginHint=" + encodeURIComponent(loginHint),
                                    suggestedDisplayName: "Proposal Manager",
                                    websiteUrl: appUri + "/tabMob/" + tabName + "?channelName=" + channelName + "&teamName=" + teamName + "&channelId=" + channelId + "&locale=" + locale + "&loginHint=" + encodeURIComponent(loginHint)

                                });
                                console.log("Config_setChannelConfig microsoftTeams.settings:");
                                console.log(microsoftTeams.settings);
                                saveEvent.notifySuccess();
                            });

                            this.setState({
                                validityState: true
                            });
                        }

                    })
                    .catch(err => {
                        console.log("Config_getOpportunityByName error: ");
                        console.log(err);
                    });
            }
        }
	}

    logout() {
        this.authHelper.logout(true)
            .then(() => {
                this.setState({
                    isAuthenticated: false,
                    displayName: ''
                });
            });
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
