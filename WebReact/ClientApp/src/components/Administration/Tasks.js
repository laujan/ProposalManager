/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { Component } from 'react';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { IconButton } from 'office-ui-fabric-react/lib/Button';
import { DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react/lib/DetailsList';
import { Link } from 'office-ui-fabric-react/lib/Link';
import Utils from '../../helpers/Utils';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { Trans } from "react-i18next";

export class Tasks extends Component {
    displayName = Tasks.name

    constructor(props) {
        super(props);
        this.utils = new Utils();
        this.authHelper = window.authHelper;
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
        items.push({ id: items.length + 1, name: "" });
        this.setState({ items });
    }

    checkTasksIsAlreadyPresent(value) {
        let flag = false;
        let items = this.state.items.slice(0);
        let index = items.findIndex(tasks => tasks.name.toLowerCase() === value.toLowerCase());
        if (index !== -1) {
            this.setState({
                isUpdate: false,
                isUpdateMsg: true,
                MessagebarText: <Trans>tasksExist</Trans>,
                MessageBarType: MessageBarType.error
            });
            setTimeout(function () {
                this.setMessage(false, false, "", "");
                this.setState({ items });
            }.bind(this), 2000);
            flag = true;
        }
        return flag;
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
        this.setState({ isUpdate, isUpdateMsg, MessageBarType, MessagebarText });
    }

    async getTasks() {
        let items = [], loading = false;
        try {
            let requestUrl = 'api/Tasks';
            let response = await fetch(requestUrl, {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            });
            let data = await this.utils.handleErrors(response).json();
            items = data.map(tasks => { return { "id": tasks.id, "name": tasks.name }; });
        } catch (error) {
            this.setMessage(false, true, MessageBarType.error, error.message);
        } finally {
            this.setState({ items, loading });
            setTimeout(function () { this.setMessage(false, false, "", ""); }.bind(this), 2000);
        }
    }

    async onBlurTasksName(e, item) {
        this.setState({ isUpdate: true });
        let id = item.id;
        let value = e.target.value;
        let requestUpdUrl = 'api/Tasks';
        let method = item.name.length === 0 ? "POST" : "PATCH";

        try {
            // Checking item value is null
            if (parseInt(value.length) === 0) {
                this.setMessage(false, true, MessageBarType.error, <Trans>tasksCannotbeEmpty</Trans>);
                return;
            }
            //Checking item is already present
            if (this.checkTasksIsAlreadyPresent(value)) return;

            let response = await fetch(requestUpdUrl, {
                method: method,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'authorization': 'Bearer    ' + window.authHelper.getWebApiToken()
                },
                body: JSON.stringify({ "id": id, "name": value })
            });
            response = this.utils.handleErrors(response);
            this.setMessage(false, true, MessageBarType.success, method === "PATCH" ? <Trans>tasksUpdatedSuccess</Trans> : <Trans>tasksAddedSuccess</Trans>);
        } catch (error) {
            this.setMessage(false, true, MessageBarType.error, <Trans>errorOoccuredPleaseTryAgain</Trans> + " " + error.message);
        } finally {
            setTimeout(function () { this.setMessage(false, false, "", ""); }.bind(this), 2000);
            await this.getTasks();
        }
    }

    async deleteRow(tasksItem) {
        this.setState({ isUpdate: true });
        let requestUpdUrl = 'api/Tasks/' + tasksItem.id;
        try {
            let response = await fetch(requestUpdUrl, {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                method: "DELETE",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            });
            response = this.utils.handleErrors(response);
            this.setMessage(false, true, MessageBarType.success, <Trans>tasksDeletedSuccess</Trans>);
        } catch (error) {
            this.setMessage(false, true, MessageBarType.error, <Trans>errorOoccuredPleaseTryAgain</Trans> + " " + error.message);
        } finally {
            setTimeout(function () { this.setMessage(false, false, "", ""); }.bind(this), 2000);
            await this.getTasks();
        }
        return;
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
                                        <Spinner size={SpinnerSize.large} ariaLive='assertive' />
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