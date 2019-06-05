import React, { Component } from 'react';
import { Pivot, PivotItem, PivotLinkFormat, PivotLinkSize } from 'office-ui-fabric-react/lib/Pivot';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { OpportunitySummary } from './OpportunitySummary';
import { getQueryVariable } from '../../../common';
import { Trans } from "react-i18next";

export class OpportunityDetails extends Component {
    displayName = OpportunityDetails.name

    constructor(props) {
        super(props);

        this.apiService = this.props.apiService;
        const oppData = this.props.oppDetails;
        this.state = {
            loading: true,
            oppData: oppData
        };
    }

    async componentDidMount() {
        console.log("OpportunityDetails_componentDidMount isauth");
        try
        {
            let oppData = this.state.oppData;
            if (!oppData) {
                oppData = await this.getOppDetails(this.props.teamname);
                console.log("OpportunityDetails_componentDidUpdate oppdata: ", oppData);
            }

            this.setState({ oppData, loading: false });
        } catch (error) {
            if (this.state.loading) {
                this.setState({ loading: false });
            }
            console.log("OpportunityDetails_componentDidUpdate error :", error);
        }
    }

    async getOppDetails(teamname) {
        let data = "";

        try {
            let args = `name=${teamname}`;

            if (!teamname) {
                let oppId = getQueryVariable('opportunityId') ? getQueryVariable('opportunityId') : "";
                args = `id=${oppId}`;
            }

            let response = await this.apiService.callApi('Opportunity', 'GET', { query: args });

            if (response.ok) {
                data = await response.json();
            }
            else {
                console.log("OpportunityDetails_getOppDetails error retrieving:", response.statusText);
            }
        } catch (err) {
            console.log("OpportunityDetails_getOppDetails err:", err);
        }
        finally {
            return data;
        }
    }

    render() {
        const OpportunitySummaryView = () => {
            return <OpportunitySummary opportunityData={this.state.oppData} userProfile={this.props.userProfile} apiService={this.props.apiService}/>;
        };

        return (
            <div className='ms-Grid'>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12' />
                    {this.state.loading && this.state.oppData && this.state.userProfile
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
            </div>
        );
    }
}