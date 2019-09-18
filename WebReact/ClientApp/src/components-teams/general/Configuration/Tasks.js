/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { Component } from 'react';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { IconButton } from 'office-ui-fabric-react/lib/Button';
import { DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react/lib/DetailsList';
import { Link } from 'office-ui-fabric-react/lib/Link';
import Utils from '../../../helpers/Utils';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { Trans } from "react-i18next";

export class Tasks extends Component {
    displayName = Tasks.name;

    constructor(props) {
        super(props);
        this.utils = new Utils();
        this.authHelper = window.authHelper;
        this.apiService = this.props.apiService;
        this.logService = this.props.logService;

        const columns = [
            {
                key: 'column1',
                name: <Trans>Tasks</Trans>,
                headerClassName: 'ms-List-th browsebutton RegionCol',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8 RegionCol',
                fieldName: 'Region',
                minWidth: 150,
                maxWidth: 250,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <TextField
                            id={'txtTasks' + item.id}
                            value={item.name}
                            onBlur={(e) => this.onBlurTasksName(e, item)}
                            required={true}
                        />
                    );
                }
            },
            {
                key: 'column2',
                name: <Trans>action</Trans>,
                headerClassName: 'ms-List-th tasksaction',
                className: 'ms-Grid-col ms-sm12 ms-md12 ms-lg4 tasksaction',
                minWidth: 16,
                maxWidth: 16,
                onRender: (item) => {
                    return (
                        <div>
                            <IconButton iconProps={{ iconName: 'Save' }} onClick={e => this.addTask(item)} />&nbsp;&nbsp;&nbsp;
                            <IconButton iconProps={{ iconName: 'Delete' }} onClick={e => this.deleteRow(item)} />
                        </div>
                    );
                }
            }
        ];

        this.state = {
            items: [],
            columns: columns,
            loading: true,
            isUpdate: false,
            MessagebarText: "",
            MessageBarType: MessageBarType.success,
            isUpdateMsg: false
        };
    }

    async componentDidMount() {
        await this.getTasks();
    }

    onAddRow() {
        let items = this.state.items.slice(0);
        items.push({ id: "", name: "" });
        this.setState({ items });
    }

    checkTasksIsAlreadyPresent(value) {
        let items = this.state.items;
        let tasks = items.filter(x => x.name.toLowerCase() === value.toLowerCase());

        return tasks.length > 0;
    }

    tasksList(columns, isCompactMode, items) {
        return (
            <div className='ms-Grid-row LsitBoxAlign p20ALL'>
                <DetailsList
                    items={items}
                    compact={isCompactMode}
                    columns={columns}
                    selectionMode={SelectionMode.none}
                    selectionPreservedOnEmptyClick='true'
                    setKey='set'
                    layoutMode={DetailsListLayoutMode.justified}
                    enterModalSelectionOnTouch='false'
                />
            </div>
        );
    }

    setMessage(isUpdate, isUpdateMsg, MessageBarType, MessagebarText) {
        //Show message
        this.setState({ isUpdate, isUpdateMsg, MessageBarType, MessagebarText });

        //Schedule message hide
        setTimeout(function () {
            this.setState({ isUpdate: false, isUpdateMsg: false, MessageBarType: "", MessagebarText: "" });
        }.bind(this), 2000);
    }

    async getTasks() {
        this.apiService.callApi('Tasks', 'GET')
            .then(async (response) => {
                let data = await this.utils.handleErrors(response).json();
                let items = [];

                if (typeof data === "string") {
                    this.setMessage(false, true, MessageBarType.info, <Trans>itemsNotFound</Trans>);
                }
                else {
                    items = data.map(tasks => { return { "id": tasks.id, "name": tasks.name }; });
                    this.setState({ items });
                }
            })
            .catch(error => {
                this.setMessage(false, true, MessageBarType.error, error.message);
            })
            .finally(() => {
                this.setState({ loading: false });
            });
    }

    async addTask(item) {
        if (item.name.length === 0) {
            this.setMessage(false, true, MessageBarType.error, <Trans>tasksCannotbeEmpty</Trans>);
            return;
        }

        let items = this.state.items;
        let tasks = items.filter(x => x.name.toLowerCase() === item.name.toLowerCase() && x.id !== item.id);

        if (tasks.length > 0) {
            this.setMessage(false, true, MessageBarType.error, <Trans>tasksExist</Trans>);
            return;
        }

        this.setState({ isUpdate: true });

        let id = item.id;
        let value = item.name;
        let method = item.id.length === 0 ? "POST" : "PATCH";

        this.apiService.callApi('Tasks', method, { body: JSON.stringify({ "id": id, "name": value }) })
            .then(async (response) => {
                response = this.utils.handleErrors(response);

                if (method === "POST") {
                    let newId = response.headers.get("location");
                    item.id = newId;
                }

                this.setMessage(false, true, MessageBarType.success, method === "PATCH" ? <Trans>tasksUpdatedSuccess</Trans> : <Trans>tasksAddedSuccess</Trans>);
            })
            .catch(error => {
                this.setMessage(false, true, MessageBarType.error, <Trans>errorOoccuredPleaseTryAgain</Trans> + " " + error.message);
            });
    }

    onBlurTasksName(e, item) {
        let value = e.target.value;
        item.name = value;
    }

    async deleteRow(tasksItem) {
        let items = this.state.items;
        
        if (tasksItem.id.length > 0) {
            if (!window.confirm("Do you want to delete the the Task?")) {
                return;
            }
            this.setState({ isUpdate: true });

            items = items.filter(p => p.id !== tasksItem.id);
            this.apiService.callApi('Tasks', "DELETE", { id: tasksItem.id })
                .then(async (response) => {
                    response = this.utils.handleErrors(response);
                    this.setMessage(false, true, MessageBarType.success, <Trans>tasksDeletedSuccess</Trans>);
                })
                .catch(error => {
                    this.setMessage(false, true, MessageBarType.error, <Trans>errorOoccuredPleaseTryAgain</Trans> + " " + error.message);
                });
        }
        else {
            items = items.reduce((result, element) => {
                if (element.name !== tasksItem.name) {
                    result.push(element);
                }
                else if (element.id !== tasksItem.id) {
                    result.push(element);
                }

                return result;
            }, []);
        }

        this.setState({ items: items });
    }

    render() {
        const { columns, items } = this.state;
        const tasksList = this.tasksList(columns, false, items);
        if (this.state.loading) {
            return (
                <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                </div>
            );
        } else {
            return (

                <div className='ms-Grid bg-white ibox-content'>

                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 pt10'>
                                <Link href='' className='pull-left' onClick={() => this.onAddRow()} >+ <Trans>addNew</Trans></Link>
                            </div>
                            {tasksList}
                        </div>
                    </div>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                            <div className='ms-BasicSpinnersExample p-10'>
                                {
                                    this.state.isUpdate ?
                                        <div className='overlay on'>
                                            <div className='overlayModal'>
                                                <Spinner size={SpinnerSize.large} className='savingSpinner' label='Saving data'/>
                                            </div>
                                        </div>
                                        : ""
                                }
                                {
                                    this.state.isUpdateMsg ?
                                        <MessageBar
                                            messageBarType={this.state.MessageBarType}
                                            isMultiline={false}
                                        >
                                            {this.state.MessagebarText}
                                        </MessageBar>
                                        : ""
                                }
                            </div>
                        </div>
                    </div>
                </div>
            );
        }
    }
}