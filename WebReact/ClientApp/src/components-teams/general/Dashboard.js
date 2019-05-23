/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import React, { Component } from 'react';
import '../teams.css';
import { Trans } from "react-i18next";
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import Utils from '../../helpers/Utils';
import * as pbi from 'powerbi-client';
import Accessdenied from '../../helpers/AccessDenied';

export class Dashboard extends Component {

	displayName = Dashboard.name
	
	constructor(props) {
		super(props);
		this.authHelper = window.authHelper;
		this.sdkHelper = window.sdkHelper;
        this.utils = new Utils();
		this.accessGranted = false;
		const reportId = this.props.appSettings.reportId;
		const workspaceId = this.props.appSettings.workspaceId;
        console.log("Dashboard_render: appSettings ", reportId, workspaceId);
		this.state = {
			loading: true,
			aadToken: "",
			reportId : reportId,
			workspaceId:workspaceId,
			embedConfig: {
                embedUrl: `https://app.powerbi.com/reportEmbed?reportId=${reportId}&groupId=${workspaceId}`,
                accessToken: ""
            },
            isAuthenticated: false
		};
	}

    async componentDidMount() {
        console.log("Dashboard_componentDidMount isauth: " + this.authHelper.isAuthenticated() + " this.accessGranted: " + this.accessGranted);

        if (!this.state.isAuthenticated) {
            this.setState({
                isAuthenticated: this.authHelper.isAuthenticated()
            });
		}
		
		if(!this.state.reportId){

			let clientSettings = await  this.getClientSettings();
			console.log("Dashboard_componentDidMount clientSettings: " , clientSettings);
			
			let reportId = clientSettings.PBIReportId;
			let workspaceId = clientSettings.PBIWorkSpaceId;

			if(reportId && workspaceId){
				let embedConfig =  {
					embedUrl: `https://app.powerbi.com/reportEmbed?reportId=${reportId}&groupId=${workspaceId}`,
					accessToken: ""
				};

                this.setState({ embedConfig, workspaceId, reportId });
			}
			
		}
    }

    async componentDidUpdate() {
        console.log("Dashboard_componentDidUpdate isauth: " + this.authHelper.isAuthenticated() + " this.accessGranted: " + this.accessGranted);

        try {
            if (this.state.isAuthenticated && !this.accessGranted && this.state.loading) {
                if (await this.setAccessGranted()) {
                    if (this.state.aadToken === "") {
                        await this.getDataForDashboard();
                    }
                }
            }
        } catch (error) {
            this.accessGranted = false;
            console.log("Dashboard_componentDidUpdate error_callCheckAccess:");
        }
    }

	async setAccessGranted(){
		try{
			console.log("Dashboard_setAccessGranted isauth: " + this.authHelper.isAuthenticated() + " this.accessGranted: " + this.accessGranted);
            let access = await this.authHelper.callCheckAccess(["Administrator", "Opportunities_ReadWrite_All", "Opportunity_ReadWrite_All"]);
			if(typeof access ==='boolean' && access===true){
				this.accessGranted = true;
				return true;
			}
			return false;
		}
		catch(error){
			this.accessGranted = false;
			let self = this;
			setTimeout(function(){
				self.setState({loading:false});
			},1000);
			console.log("Dashboard_setAccessGranted error: " ,error);
			return false;
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

	async getDataForDashboard() {
		let requestUrl = "api/PowerBI";
		console.log("Dashboard_from_PBI_Controller : ", this.authHelper.getWebApiToken());
		try {
			let response  = await fetch(requestUrl, {method: "GET", headers: { 'authorization': `Bearer ${this.authHelper.getWebApiToken()}`}});
			let data = await response.json();
			console.log("Dashboard_from_PBI_Controller data : ", this.state.reportId);
			console.log("Dashboard_from_PBI_Controller data : ", this.state.embedConfig);

			this.setState({
				aadToken: data,
				loading: false
			});
			var config = {
				type: 'report',
				tokenType: pbi.models.TokenType.Aad,
                accessToken: data,
				embedUrl: this.state.embedConfig.embedUrl,
				id: this.state.reportId,
				permissions: pbi.permissions,
				height: "800px !important",
				settings: {
					filterPaneEnabled: true,
					navContentPaneEnabled: true,
					layoutType: pbi.models.LayoutType.Custom,
					customLayout: {
						pageSize: {
							type: pbi.models.PageSizeType.Custom,
							width: 1000,
							height: 1200
						}
					}
				}
			};

			let powerbi = new pbi.service.Service(pbi.factories.hpmFactory, pbi.factories.wpmpFactory, pbi.factories.routerFactory);

			// Embed the report and display it within the div container.
			var reportContainer = this.refs.reportContainerRef;//document.getElementById('reportContainer');

			console.log("Dashboard_getDataForDashboard reportContainer: " + reportContainer);
			powerbi.embed(reportContainer, config); //TODO: Do we need this?
		} catch (error) {
			console.log("Dashboard_getDataForDashboard error_fetch: ", error);
		}
	}

	render() {
        const isLoading = this.state.loading;
		console.log("Dashboard_render: isLoading " ,isLoading );
		return (
			<div className='ms-Grid'>
				<div className='ms-Grid-row bg-white'>
                    {
                        isLoading ?
                            <div>
                                <br /><br />
                                <Spinner size={SpinnerSize.medium} label={<Trans>loading</Trans>} ariaLive='assertive' />
                                <br /><br />
                            </div>
                            :
                            <div>
                                {
                                    this.accessGranted ?
                                        <div ref="reportContainerRef" id="reportContainer" className='ms-Grid-col ms-sm6 ms-md8 ms-lg12'/>
                                        :
                                        <Accessdenied />
                                }
                            </div>
                    }
				</div>
			</div>
		
		);
	}
}
