/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import React, { Component } from 'react';
import '../teams.css';
import { Trans } from "react-i18next";
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import * as pbi from 'powerbi-client';
import Accessdenied from '../../helpers/AccessDenied';

export class Audit extends Component {

    displayName = Audit.name

    constructor(props) {
        super(props);
        this.apiService = this.props.apiService;
        this.logService = this.props.logService;
        this.authHelper = window.authHelper;
        const reportId = this.props.appSettings.auditReportId;
        const workspaceId = this.props.appSettings.auditWorkspaceId;
        this.logService.log("Audit_render: appSettings ", reportId, workspaceId);
        this.state = {
            loading: true,
            reportId: reportId,
            workspaceId: workspaceId,
            embedConfig: {
                embedUrl: `https://app.powerbi.com/reportEmbed?reportId=${reportId}&groupId=${workspaceId}`,
                accessToken: "" 
            },
            accessGranted: false
        };
    }

    async componentDidMount() {
        this.logService.log("Audit_componentDidMount");

        try {
            await this.setAccessGranted();
        } catch (error) {
            this.logService.log("Audit_componentDidUpdate error_callCheckAccess: ", error);
            this.setState({ accessGranted: false });
        }
    }

    async setAccessGranted() {
        try {
            let res = await this.authHelper.callCheckAccess(["Administrator", "Opportunities_ReadWrite_All", "Opportunity_ReadWrite_All"]);
            if (res) {
                await this.getDataForDashboard();
            } else {
                this.setState({ loading: false, accessGranted: false });
                this.logService.log("Audit_setAccessGranted error: " + res);
            }
        }
        catch (error) {
            this.logService.log("Audit_setAccessGranted error: ", error);
            this.setState({ loading: false, accessGranted: false });
        }
    }

    async getDataForDashboard() {
        this.apiService.callApi('PowerBI', 'GET')
            .then(response => {
                return response.json();
            })
            .then(data => {
                this.setState({ loading: false, accessGranted: true });

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
                var reportContainer = this.refs.reportContainerRef;

                this.logService.log("Audit_getDataForDashboard reportContainer: " + reportContainer);
                powerbi.embed(reportContainer, config); 
            })
            .catch(error => {
                this.logService.log("Audit_getDataForDashboard error_fetch: ", error);
                this.setState({ loading: false, accessGranted: false });
            });
    }

    render() {
        const { loading, accessGranted } = this.state;
        this.logService.log("Audit_render: isLoading ", loading);
        return (
            <div className='ms-Grid'>
                <div className='ms-Grid-row bg-white'>
                    {
                        loading ?
                            <div>
                                <br /><br />
                                <Spinner size={SpinnerSize.medium} label={<Trans>loading</Trans>} ariaLive='assertive' />
                                <br /><br />
                            </div>
                            :
                            <div>
                                {
                                    accessGranted ?
                                        <div ref="reportContainerRef" id="reportContainer" className='ms-Grid-col ms-sm6 ms-md8 ms-lg12' />
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
