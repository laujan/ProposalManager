import React, { Component } from 'react';
import {
    Pivot,
    PivotItem,
    PivotLinkFormat,
    PivotLinkSize
} from 'office-ui-fabric-react/lib/Pivot';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import { OpportunitySummary } from './OpportunitySummary';
import { OpportunityNotes } from './OpportunityNotes';
import { getQueryVariable } from '../../../common';
import { Trans } from "react-i18next";


export class OpportunityDetails extends Component {
    displayName = OpportunityDetails.name

    constructor(props) {
        super(props);

        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;
        this.accessGranted = false;
        const oppData = this.props.oppDetails
        console.log ("OpportunityDetails_constructor oppId : ", this.props.teamname)
        this.state = {
            loading: true,
            oppData: oppData,
            userProfile: "",
            isAuthenticated: false
        };
    }

    componentWillMount() {
        console.log("OpportunityDetails_componentWillMount isauth: " + this.authHelper.isAuthenticated());
    }

    componentWillReceiveProps(nextProps) {
        console.log("OpportunityDetails_componentWillReceiveProps : ",nextProps);
        this.setState({ oppData: nextProps.oppDetails});  
    }

    async componentDidMount() {
        console.log("OpportunityDetails_componentWillMount isauth: " + this.authHelper.isAuthenticated() + " this.accessGranted: " + this.accessGranted);

        if (!this.state.isAuthenticated) {
            this.setState({
                isAuthenticated: this.authHelper.isAuthenticated()
            });
        }
        
    }

    async componentDidUpdate() {
        console.log("OpportunityDetails_componentDidUpdate isauth: " + this.authHelper.isAuthenticated() + " this.accessGranted: " + this.accessGranted);
        console.log("OpportunityDetails_componentDidUpdate data 0: " , this.state.oppData);
        try {
            if (this.state.isAuthenticated && this.state.loading && !this.accessGranted) {
                let userProfile = await  this.authHelper.callGetUserProfile();
                this.accessGranted = true;
                let loading =false;
                if(this.state.oppData){
                   let oppData = this.state.oppData;
                   console.log("OpportunityDetails_componentDidUpdate oppdata 1: " , oppData , userProfile);
                   this.setState({userProfile,loading});
                }
                else{
                    let oppData  = await this.getOppDetails(this.props.teamname);
                    console.log("OpportunityDetails_componentDidUpdate oppdata 2: " , oppData , userProfile);
                    this.setState({userProfile,oppData,loading});
                }
            }
        } catch (error) {
            this.accessGranted = false;
            this.state.loading === true ? this.setState({loading:false}): "";
            console.log("OpportunityDetails_componentDidUpdate error :", error);
        }
    }

    async getOppDetails(teamname) {
        let data = "";
        let requestUrl = `api/Opportunity/?name=${teamname}`;
        if(!teamname){
            let oppId = getQueryVariable('opportunityId') ? getQueryVariable('opportunityId') : "";
            requestUrl = `api/Opportunity/?id=${oppId}`;
        }
        
        console.log("OpportunityDetails_getOppDetails teamname :", requestUrl)
        try {
            let token = "";
            token = this.authHelper.getWebApiToken();
            console.log("OpportunityDetails_getOppDetails  token: ", token.length);
            let response = await fetch(requestUrl, {
                        method: "GET",
                        headers: { 'authorization': 'Bearer ' + token }
                    }) ;
            data = await response.json();
            return data;

        }
        catch (err) {
            console.log("OpportunityDetails_getOppDetails err:", err);
            return data;
        }
    }

    render() {
        const OpportunitySummaryView = ({ match }) => {
            return <OpportunitySummary opportunityData={this.state.oppData} userprofile={this.state.userProfile}/>;
        };

        console.log("OpportunityDetails_render oppId and userprofile : ", this.state.oppData, this.state.userProfile , this.state.loading)
        return (
            <div className='ms-Grid'>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12' />
                    {(this.state.loading && this.state.oppData && this.state.userProfile)
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