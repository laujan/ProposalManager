/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { NormalPeoplePicker } from 'office-ui-fabric-react/lib/Pickers';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { PersonaPresence } from 'office-ui-fabric-react/lib/Persona';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';

export class PeoplePickerTeamMembers extends Component {
    displayName = PeoplePickerTeamMembers.name

    constructor(props) {
        super(props);
        this.apiService = this.props.apiService;
        this.logService = this.props.logService;
        // Set the initial state for the picker data source.
        // The people list is populated in the _onFilterChanged function.
        this._peopleList = [];
        this._searchResults = [];

        this.apiService = this.props.apiService;
        this._showError = this._showError.bind(this);

        let filteredList = this.props.teamMembers;
        let isDisableTextBox = this.props.isDisableTextBox;

        this.state = {
            teamMembers: filteredList,
            defaultSelectedItems: this.props.defaultSelectedUsers.length > 0 ? this.props.defaultSelectedUsers : [],
            isLoadingPeople: false,
            isLoadingPics: false,
            isDisableTextBox: isDisableTextBox
        };
    }

    componentDidMount() {
        if (this.props.defaultSelectedUsers && this.props.defaultSelectedUsers.length > 0 && this.props.defaultSelectedUsers[0].displayName.length > 0) {
            this.mapDefaultSelectedItems();
        }
    }

    errorHandler(err, referenceCall) {
        this.logService.log("PeoplePickerTeamMembers Ref: ", referenceCall, " error: " + JSON.stringify(err));
    }

    getUserProfilesSearch(searchText, callback) {
        this.apiService.callApi('UserProfile', 'GET')
            .then(response => {
                if (response.ok) {
                    return response.json();
                } else {
                    this.errorHandler(response, "getUserProfilesSearch");
                    let err = "Error in fetch get users";
                    callback(err, []);
                }
            })
            .then(data => {
                let itemslist = [];

                if (data.ItemsList.length > 0) {
                    for (let i = 0; i < data.ItemsList.length; i++) {

                        let item = data.ItemsList[i];

                        let newItem = {};

                        newItem.id = item.id;
                        newItem.displayName = item.displayName;
                        newItem.mail = item.mail;
                        newItem.userPrincipalName = item.userPrincipalName;
                        newItem.userRoles = item.userRoles;

                        itemslist.push(newItem);
                    }
                }

                let filteredList = itemslist.filter(itm => itm.userRole === 1);

                this.setState({
                    teamMembers: filteredList,
                    isLoadingPeople: false
                });

                callback(null, filteredList ? filteredList : []);
            })
            .catch(err => {
                this._showError(err);
                callback(err, []);
            });

    }

    // Create the persona for the defaultSelectedItems using user ID
    mapDefaultSelectedItems() {
        this.setState({
            defaultSelectedItems: this._mapUsersToPersonas(this.props.defaultSelectedUsers, false)
        });
    }

    // Map user properties to persona properties.
    _mapUsersToPersonas(users, useMailProp) {
        return users.map((p) => {

            // The email property is returned differently from the /users and /people endpoints.
            let email = p.mail ? p.mail : p.userPrincipalName;
            this.logService.log("ChooseTeams_Log _mapUsersToPersonas : p", p);
            let persona = {
                id: p.id,
                text: p.displayName ? p.displayName : "USER NAME",
                text2: p.hasOwnProperty("adGroupName") ? p.adGroupName : "",
                secondaryText: p.userPrincipalName,
                presence: PersonaPresence.none,
                imageInitials: p.displayName.substring(0, 2),
                initialsColor: Math.floor(Math.random() * 15) + 0,
                mail: email,
                userPrincipalName: p.userPrincipalName,
                userRoles: p.userRoles,
                status: 0
            };

            return persona;
        });
    }

    // Gets the profile photo for each user.
    _getPics(personas) {

        // Make suggestions available before retrieving profile pics.
        this.setState({
            isLoadingPics: false
        });
    }

    // Remove currently selected people from the suggestions list.
    _listContainsPersona(persona, items) {
        if (!items || !items.length || items.length === 0) {
            return false;
        }
        return items.filter(item => item.text === persona.text).length > 0;
    }

    // Handler for when text is entered into the picker control.
    // Populate the people list.
    _onFilterChanged(filterText, items) {
        if (this._peopleList) {
            return filterText ? this._peopleList.concat(this._searchResults)
                .filter(item => item.text.toLowerCase().indexOf(filterText.toLowerCase()) === 0)
                .filter(item => !this._listContainsPersona(item, items)) : [];
        }
    }

    // Handler for when the Search button is clicked.
    // This method returns the first 20 matches as suggestions.
    _onGetMoreResults(searchText) {
        this.setState({
            isLoadingPeople: true,
            isLoadingPics: false
        });
        return new Promise((resolve) => {
            this.getUserProfilesSearch(searchText.toLowerCase(), (err, people) => {
                if (!err) {
                    this._searchResults = this._mapUsersToPersonas(people, true);
                    this.setState({
                        isLoadingPeople: false
                    });
                    this._getPics(this._searchResults);
                    resolve(this._searchResults);
                }
            });
        });
    }

    // Handler for when the picker gets focus
    onEmptyInputFocusHandler() {
        return new Promise((resolve) => {
            this._peopleList = this._mapUsersToPersonas(this.state.teamMembers, true);

            this._getPics(this._peopleList);
            resolve(this._peopleList);
        });
    }

    // Show the results of the `/me/people` query.
    // For sample purposes only.
    _showPeopleResults() {
        let message = 'Query loading. Please try again.';
        if (!this.state.isLoadingPeople) {
            const people = this._peopleList.map((p) => {
                return `\n${p.text}`;
            });
            message = people.toString();
        }
        alert(message);
    }

    // Configure the error message.
    _showError(err) {
        this.setState({
            result: {
                type: MessageBarType.error,
                text: `Error ${err.statusCode}: ${err.code} - ${err.message}`
            }
        });
    }

    // Renders the people picker using the NormalPeoplePicker template.
    render() {
        return (
            <div>
                {
                    this.state.isLoadingPeople ?
                        <div>
                            <Spinner size={SpinnerSize.xSmall} label='Loading loan officers list ...' ariaLive='assertive' /><br />
                        </div>
                        :
                        <div />
                }
                <NormalPeoplePicker
                    onResolveSuggestions={this._onFilterChanged.bind(this)}
                    pickerSuggestionsProps={{
                        suggestionsHeaderText: 'Team Members',
                        noResultsFoundText: 'No results found',
                        loadingText: 'Loading pictures...',
                        isLoading: this.state.isLoadingPics
                    }}
                    getTextFromItem={(persona) => persona.text}
                    onEmptyInputFocus={this.onEmptyInputFocusHandler.bind(this)}
                    onChange={this.props.onChange}
                    className='ms-PeoplePicker normalPicker'
                    key='normal-people-picker'
                    itemLimit={this.props.itemLimit ? this.props.itemLimit : '1'}
                    defaultSelectedItems={this.state.defaultSelectedItems ? this.state.defaultSelectedItems : []}
                    disabled={this.state.isLoadingPeople || this.state.isDisableTextBox}
                />
                <br />
                {
                    this.state.result &&
                    <MessageBar messageBarType={this.state.result.type}>
                        {this.state.result.text}
                    </MessageBar>
                }
            </div>
        );
    }
}
