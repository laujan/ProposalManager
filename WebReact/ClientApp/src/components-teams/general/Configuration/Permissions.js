/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { Component } from 'react';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { IconButton, PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react/lib/DetailsList';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { Trans } from "react-i18next";
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';

export class Permissions extends Component {
    displayName = Permissions.name

    constructor(props) {
        super(props);

        this.logService = this.props.logService;
        this.authHelper = window.authHelper;
        this.apiService = this.props.apiService;

        const columns = [
            {
                key: 'column1',
                name: <Trans>adGroupName</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'adGroupName',
                minWidth: 150,
                maxWidth: 250,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <TextField
                            id={'txtAdGroupName' + item.id}
                            value={item.adGroupName}
                            onBlur={(e) => this.onChangeProperty(e, item, "adGroupName")}
                            required={true}
                        />
                    );
                }
            },
            {
                key: 'column2',
                name: <Trans>role</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'role',
                minWidth: 150,
                maxWidth: 200,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <TextField
                            id={'txtRole' + item.id}
                            value={item.displayName}
                            onBlur={(e) => this.onChangeProperty(e, item, "displayName")}
                            required={true}
                        />
                    );
                }
            },
            {
                key: 'column3',
                name: <Trans>permissions</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'permissions',
                minWidth: 150,
                maxWidth: 300,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <div className="docs-DropdownExample">
                            <Dropdown
                                id={'txtPermissions' + item.id}
                                ariaLabel='Permissions'
                                multiSelect
                                options={this.state.permissionTypes}
                                defaultSelectedKeys={item.selPermissions}
                                onChanged={(e) => this.onChangeProperty(e, item, "permissions")}
                            />
                        </div>
                    );
                }
            },
            {
                key: 'column4',
                name: <Trans>Type</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'type',
                minWidth: 150,
                maxWidth: 150,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <div className="docs-DropdownExample">
                            <Dropdown
                                id={'txtType' + item.id}
                                ariaLabel='Type'
                                options={[
                                    { key: 0, text: 'Owner' },
                                    { key: 1, text: 'Member' }
                                ]}
                                defaultSelectedKey={item.teamsMembership.value}
                                onChanged={(e) => this.onChangeProperty(e, item, "type")}
                            />
                        </div>
                    );
                }
            },
            {
                key: 'column5',
                name: <Trans>actions</Trans>,
                headerClassName: 'ms-List-th',
                className: 'ms-Grid-col ms-sm12 ms-md12 ms-lg4',
                minWidth: 100,
                maxWidth: 100,
                onRender: (item) => {
                    return (
                        <div>
                            <IconButton iconProps={{ iconName: 'Save' }} onClick={e => this.addRow(e, item)} />&nbsp;&nbsp;&nbsp;
                            <IconButton iconProps={{ iconName: 'Delete' }} onClick={e => this.deleteRow(item)} />
                        </div>
                    );
                }
            },
            {
                key: 'column6',
                name: "",
                headerClassName: 'ms-List-th',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg3',
                minWidth: 10,
                maxWidth: 10,
                onRender: (item) => {
                    return (
                        <div />
                    );
                }
            }
        ];

        this.schema = {
            adGroupName: "",
            displayName: "",
            id: "",
            permissions: [],
            teamsMembership: { name: "Member", value: 0 }
        };

        this.state = {
            items: [],
            rowItemCounter: 0,
            columns: columns,
            isCompactMode: false,
            loading: true,
            isUpdate: false,
            updatedItems: [],
            MessagebarText: "",
            MessageBarType: MessageBarType.success,
            isUpdateMsg: false,
            roles: [],
            permissionTypes: [],
            permissionItem: this.schema,
            isAdGroupNameError: false
        };
    }

    async componentDidMount() {
        await this.loadAllPermissionData();
    }

    async loadAllPermissionData() {
        this.setState({ loading: true });

        try {
            let values = await Promise.all([this.getAllRoles(), this.getAllPemissionTypes(), this.getAllPermissionsList()]);
            let rolesList = values[0];
            let permissionsList = values[1];
            let data = values[2];
            this.setState({ items: data.items, rowItemCounter: data.rowItemCounter, permissionTypes: permissionsList, roles: rolesList });
        }
        catch (err) {
            this.logService.log("Permissions_loadAllPermissionData error: ", err);
        }
        finally {
            this.setState({ loading: false });
        }
    }

    async getAllRoles() {
        try {
            let response = await this.apiService.callApi('Roles', "GET");
            let data = await response.json();

            if (response.ok) {
                let roles = data.map(role => { return { "key": role.displayName, "text": role.displayName }; });
                return roles;
            }
            else {
                throw new Error(data.error.message);
            }
        } catch (error) {
            this.logService.log("getAllRoles: ", error);
            throw error;
        }
    }

    async getAllPemissionTypes() {
        try {
            let response = await this.apiService.callApi('Permissions', "GET");
            let data = await response.json();

            if (response.ok) {
                let permissionsList = data.map(permission => { return { "key": permission.name, "text": permission.name }; });
                return permissionsList;
            }
            else {
                throw new Error(data.error.message);
            }
        }
        catch (error) {
            this.logService.log("getAllPemissionTypes : ", error);
            throw new Error(error);
        }
    }

    async getAllPermissionsList() {
        try {
            let response = await this.apiService.callApi('Roles', "GET");
            let data = await response.json();

            if (response.ok) {
                for (let p = 0; p < data.length; p++) {
                    let permissionsList = [];
                    for (let i = 0; i < data[p].permissions.length; i++) {
                        permissionsList.push(data[p].permissions[i].name);
                    }
                    data[p].selPermissions = permissionsList;
                }

                return { items: data, rowItemCounter: data.length };
            }
            else {
                throw new Error(data.error.message);
            }
        }
        catch (error) {
            this.logService.log("getAllPermissionsList : ", error);
            throw new Error(error);
        }
    }

    setMessage(isUpdate, isUpdateMsg, MessageBarType, MessagebarText) {
        //Show message
        this.setState({ isUpdate, isUpdateMsg, MessageBarType, MessagebarText });

        //Schedule message hide
        setTimeout(function () {
            this.setState({ isUpdate: false, isUpdateMsg: false, MessageBarType: "", MessagebarText: "" });
        }.bind(this), 2000);
    }

    async addRow(e, item) {
        if (this.state.isAdGroupNameError) {
            this.setMessage(false, true, MessageBarType.error, <Trans>adGroupNameExist</Trans>);
        }

        if (item.adGroupName.length === 0) {
            this.setMessage(false, true, MessageBarType.error, "You must enter a AD Group Name.");
            return;
        }

        if (item.displayName.length === 0) {
            this.setMessage(false, true, MessageBarType.error, "You must enter a Display Name.");
            return;
        }

        if (item.permissions.length === 0) {
            this.setMessage(false, true, MessageBarType.error, "You must select a Permission.");
            return;
        }

        if (item.id.length === 0) {
            await this.addOrUpdatePermission(item, "POST");
        } else if (item.id.length > 0) {
            await this.addOrUpdatePermission(item, "PATCH");
        }
    }

    onChangeProperty(e, item, property) {
        let items = this.state.items;
        let permissionItem = item.id === this.state.permissionItem.id ? this.state.permissionItem : item;
        let changeFlag = false;

        switch (property) {
            case "adGroupName":
                if (e.target.value) {
                    changeFlag = true;
                    this.setState({ isAdGroupNameError: false });
                    //Check AdGroupName already exist while add new
                    let isAdGroupExist = this.state.items.some(obj => obj.adGroupName.toLowerCase() === e.target.value.toLowerCase() && obj.id !== item.id);
                    if (isAdGroupExist) {
                        permissionItem.adGroupName = e.target.value;
                        this.setState({
                            isAdGroupNameError: true
                        });
                        this.setMessage(false, true, MessageBarType.error, <Trans>adGroupNameExist</Trans>);

                        break;
                    }
                    permissionItem.adGroupName = e.target.value;
                }
                break;
            case "displayName":
                if (e.target.value) {
                    permissionItem.displayName = e.target.value;
                    changeFlag = true;
                }
                break;
            case "permissions":
                if (e.selected) {
                    permissionItem.permissions.push({ "id": "", "name": e.text });
                } else {
                    let index = permissionItem.permissions.findIndex(obj => obj["name"] === e.text);
                    if (index !== -1) {
                        permissionItem.permissions.splice(index, 1);
                    }
                }
                changeFlag = true;
                break;
            case "type":
                permissionItem.teamsMembership.name = e.text;
                permissionItem.teamsMembership.value = e.key;
                changeFlag = true;
                break;
        }

        if (changeFlag) {
            if (item.id.length === 0) {
                items[items.length - 1] = permissionItem;
            } else {
                let index = items.findIndex(obj => obj.id === permissionItem.id);
                if (index !== -1) {
                    items[index] = permissionItem;
                }
            }

            this.setState({ permissionItem, items });
        }
    }

    createRowItem() {
        return this.schema;
    }

    onAddRow() {
        let newItems = [];
        newItems.push(this.createRowItem());

        let currentItems = this.state.items.concat(newItems);

        this.setState({
            items: currentItems,
            permissionItem: this.schema
        });
    }

    deleteRow(item) {
        let items = this.state.items;        

        // remove item from backend
        if (item.id && item.id.length > 0) {
            if (!window.confirm("Do you want to delete the item?")) {
                return;
            }

            items = items.filter(p => p.id !== item.id);
            this.deletePermission(item);
        }
        else {
            items = items.reduce((result, element) => {
                if (element.adGroupName !== item.adGroupName) {
                    result.push(element);
                }
                else if (element.id !== item.id) {
                    result.push(element);
                }

                return result;
            }, []);
        }

        this.setState({ items: items });
    }

    async addOrUpdatePermission(permissionItem, methodType) {
        this.setState({ isUpdate: true });

        this.apiService.callApi('Roles', methodType, { body: JSON.stringify(permissionItem) })
            .then(async (response) => {
                if (response.ok) {
                    let newId = response.headers.get("location");
                    permissionItem.id = newId;
                    this.setMessage(false, true, MessageBarType.success, methodType === "POST" ? <Trans>permissionAddSuccess</Trans> : <Trans>permissionUpdatedSuccess</Trans>);
                } else {
                    this.setMessage(false, true, MessageBarType.error, <Trans>errorOoccuredPleaseTryAgain</Trans>);
                }
            })
            .catch(error => {
                this.logService.log("addOrUpdatePermission: ", error);
                this.setMessage(false, true, MessageBarType.error, <Trans>errorOoccuredPleaseTryAgain</Trans>);
            });
    }

    async deletePermission(permissionItem) {
        this.setState({ isUpdate: true });

        this.apiService.callApi('Roles', "DELETE", { id: permissionItem.id })
            .then(async (response) => {
                if (response.ok) {
                     this.setMessage(false, true, MessageBarType.success, <Trans>permissionDeletedSuccess</Trans>);
                } else {
                    this.setMessage(false, true, MessageBarType.error, <Trans>errorOoccuredPleaseTryAgain</Trans>);
                }
            })
            .catch(error => {
                this.logService.log("deletePermission: ", error);
                this.setMessage(false, true, MessageBarType.error, <Trans>errorOoccuredPleaseTryAgain</Trans>);
            });
    }

    permissionsList(columns, isCompactMode, items) {
        return (
            <div className='ms-Grid-row LsitBoxAlign p20ALL '>
                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
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
            </div>
        );
    }

    render() {
        const { columns, isCompactMode, items } = this.state;
        if (this.state.loading) {
            return (
                <div className='ms-BasicSpinnersExample ibox-content pt15'>
                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                </div>
            );
        } else {
            const permissionsList = this.permissionsList(columns, isCompactMode, items);

            return (
                <div className='ms-Grid bg-white ibox-content'>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 p-10'>
                            <PrimaryButton iconProps={{ iconName: 'Add' }} className='pull-right mr20' onClick={() => this.onAddRow()} >&nbsp;<Trans>addNewRow</Trans></PrimaryButton>
                        </div>
                    </div>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                            {permissionsList}
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