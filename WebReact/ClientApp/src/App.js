/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

// Global imports
import React, { Component } from 'react';
import GraphSdkHelper from './helpers/GraphSdkHelper';
import AuthHelper from './helpers/AuthHelper';
import Utils from './helpers/Utils';
import { AppBrowser } from './AppBrowser';
import { AppTeams } from './AppTeams';
import { I18nextProvider } from "react-i18next";
import i18n from './i18n';

export default class App extends Component {
    displayName = App.name

    constructor(props) {
        super(props);
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

        if (window.utils) {
            this.utils = window.utils;
        } else {
            // Initilize the utils and save it in the window object.
            this.utils = new Utils();
            window.utils = this.utils;
        }
    }

    render() {
        const inTeams = window.location.pathname.substring(0, 4) === "/tab";
        // Get Browser supported language
        const language = (navigator.languages && navigator.languages[0]) || navigator.language || navigator.userLanguage;

        const renderApp = () => {
            if (inTeams) {
                return <AppTeams/>;
            }
            else {
                return <AppBrowser />;
            }
        };

        //Set Language
        i18n.init({ lng: language }, function (t) {
            i18n.t('key');
        });

        return (
            <I18nextProvider i18n={i18n}>
                {renderApp()}
            </I18nextProvider>
        );
    }
}