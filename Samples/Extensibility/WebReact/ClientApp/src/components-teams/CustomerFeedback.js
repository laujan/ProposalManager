/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import React, { Component } from 'react';
import { loadTheme } from 'office-ui-fabric-react';

loadTheme({
    palette: {
        themePrimary: '#0078d4',
        themeLighterAlt: '#eff6fc',
        themeLighter: '#deecf9',
        themeLight: '#c7e0f4',
        themeTertiary: '#71afe5',
        themeSecondary: '#2b88d8',
        themeDarkAlt: '#106ebe',
        themeDark: '#005a9e',
        themeDarker: '#004578',
        neutralLighterAlt: '#f8f8f8',
        neutralLighter: '#f4f4f4',
        neutralLight: '#eaeaea',
        neutralQuaternaryAlt: '#dadada',
        neutralQuaternary: '#d0d0d0',
        neutralTertiaryAlt: '#c8c8c8',
        neutralTertiary: '#c2c2c2',
        neutralSecondary: '#858585',
        neutralPrimaryAlt: '#4b4b4b',
        neutralPrimary: '#333333',
        neutralDark: '#272727',
        black: '#1d1d1d',
        white: '#ffffff',
    }
});


export class CustomerFeedback extends Component {
    displayName = CustomerFeedback.name
    constructor(props) {
        super(props);
        this.state = {
            symbol: null
        };
        this.handleChange = this.handleChange.bind(this);
    }

    lookup() {
        fetch(`https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol=${this.state.symbol}&apikey=517ON3Z1FQZ9N67M`)
            .then(data => data.json())
            .then(json => alert(json["Global Quote"]["05. price"]));
    }

    handleChange(value) {
        this.setState({ symbol: value });
    }

    render() {
        return (
            <div>
                <h1 className="ms-font-su">Customer Feedback</h1>
                <form>
                    <TextField label="Symbol" required={true} value={this.state.value} onChanged={this.handleChange} />
                    <PrimaryButton text="Look up" onClick={() => this.lookup()} />
                </form>
            </div>
        );
    }
}