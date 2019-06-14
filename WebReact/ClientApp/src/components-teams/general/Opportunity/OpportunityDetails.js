import React, { Component } from 'react';
import { Pivot, PivotItem, PivotLinkFormat, PivotLinkSize } from 'office-ui-fabric-react/lib/Pivot';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { OpportunitySummary } from './OpportunitySummary';
import { getQueryVariable } from '../../../common';
import { Trans } from "react-i18next";
import Accessdenied from '../../../helpers/AccessDenied';

export class OpportunityDetails extends Component {
    displayName = OpportunityDetails.name

    constructor(props) {
        super(props);

        this.apiService = this.props.apiService;
        this.logService = this.props.logService;
        this.authHelper = window.authHelper;
        const oppData = this.props.oppDetails;
        this.state = {
            loading: true,
            oppData: oppData
        };

        this.checkReadWrite = ["Administrator", "Opportunity_ReadWrite_All", "Opportunity_ReadWrite_Partial"];
    }

    async componentDidMount() {
        this.logService.log("OpportunityDetails_componentDidMount");
        let oppData = this.state.oppData;
        try {
            let data = await this.authHelper.callCheckAccess(this.checkReadWrite);
            
            if (data) {
                if (!oppData) {
                    oppData = await this.getOppDetails(this.props.teamname);
                    this.logService.log("OpportunityDetails_componentDidUpdate oppdata: ", oppData);
                }
            }
            this.setState({ hasAccess: data, oppData, loading: false });
        }
        catch (e) {
            this.logService.log("OpportunityDetails_componentDidUpdate error: ", e);
            this.setState({ hasAccess: false, loading: false, oppData });
        }
    }

    async getOppDetails(teamname) {
        try {
            let args = `name=${teamname}`;

            if (!teamname) {
                let oppId = getQueryVariable('opportunityId') ? getQueryVariable('opportunityId') : "";
                args = `id=${oppId}`;
            }

            let response = await this.apiService.callApi('Opportunity', 'GET', { query: args });
            let data = await response.json();

            if (response.ok) {
                return data;
            }
            else {
                throw new Error(data.error.message);
            }
        } catch (err) {
            let errorMessage = `OpportunityDetails_getOppDetails error retrieving: ${err}`;
            this.logService.log(errorMessage);
            throw new Error(errorMessage);
        }
    }

    render() {
        const OpportunitySummaryView = () => {
            return <OpportunitySummary opportunityData={this.state.oppData} userProfile={this.props.userProfile} apiService={this.props.apiService} logService={this.props.logService} />;
        };

        return (
            <div className='ms-Grid'>
                {this.state.hasAccess 
                ?
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12' />
                    {this.state.loading && this.state.oppData && this.props.userProfile
                        ?
                        <div className='ms-BasicSpinnersExample'>
                            <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                        </div>
                        :
                        <Pivot className='tabcontrols pt35' linkFormat={PivotLinkFormat.tabs} linkSize={PivotLinkSize.large} selectedKey={this.state.selectedTabName}>
                            <PivotItem linkText={<Trans>summary</Trans>} width='100%' itemKey="Summary" >
                                <div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12' >
                                    <OpportunitySummaryView  />
                                </div>
                            </PivotItem>
                        </Pivot>
                    }
                    </div>
                    :
                    <Accessdenied />
                }
            </div>
        );
    }
}