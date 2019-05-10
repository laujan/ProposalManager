/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { Component } from 'react';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { IconButton, PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react/lib/DetailsList';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { Trans } from "react-i18next";
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';

export class Permissions extends Component {
    displayName = Permissions.name

    constructor(props) {
        super(props);

        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;

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
                    //console.log("Permissions_Log onrender item : ", item);
                    return (
                        <TextField
                            id={'txtRole' + item.id}
                            value={item.displayName}
                            onBlur={(e) => this.onChangeProperty(e, item, "displayName")}
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

        let rowCounter = 0;

        this.schema = {
            adGroupName: "",
            displayName: "",
            id: "",
            permissions: [],
            teamsMembership: { name: "Member", value: 0 }
        };

        this.state = {
            items: [],
            rowItemCounter: rowCounter,
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
        try {
            if (this.state.loading) {
                let rolesList = await this.getAllRoles();
                let permissionsList = await this.getAllPemissionTypes();
                let data = await this.getAllPermissionsList();
                this.setState({ items: data.items, loading: data.loading, rowItemCounter: data.rowItemCounter, permissionTypes: permissionsList, roles: rolesList });
            }
        } catch (error) {
            this.setState({ loading: true });
        }
    }

    async getAllRoles() {
        this.setState({ loading: true });
        let requestUrl = 'api/Roles';
        const response = await fetch(requestUrl, { method: "GET", headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() } });
        const data = await response.json();

        try {
            let allRoles = data;
            let rolesList = allRoles.map(role => { return { "key": role.displayName, "text": role.displayName }; });
            return rolesList;
        }
        catch (err) {
            console.log("Permission.js getAllRoles :, ", err);
            return false;
        }
    }

    async getAllPemissionTypes() {
        this.setState({ loading: true });
        let requestUrl = 'api/Permissions';
        const response = await fetch(requestUrl, { method: "GET", headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() } });
        const data = await response.json();
        try {
            let allPermissions = data;
            let permissionsList = allPermissions.map(permission => { return { "key": permission.name, "text": permission.name }; });
            return permissionsList;
        }
        catch (err) {
            console.log("Permission.js getAllPemissionTypes :, ", err);
            return false;
        }
    }

    async getAllPermissionsList(shadowLoading = false) {
        if (!shadowLoading)
            this.setState({ loading: true });
        //WAVE-4 : Changing RoleMappong to Roles:
        let requestUrl = 'api/Roles';
        const response = await fetch(requestUrl, { method: "GET", headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() } });
        const data = await response.json();
        try {
            let allPermissions = data;

            for (let p = 0; p < allPermissions.length; p++) {
                let permissionsList = [];
                for (let i = 0; i < allPermissions[p].permissions.length; i++) {
                    permissionsList.push(allPermissions[p].permissions[i].name);
                }
                allPermissions[p].selPermissions = permissionsList;
            }

            return { items: allPermissions, loading: false, rowItemCounter: allPermissions.length };
        }
        catch (err) {
            console.log("Permission.js getAllPermissionsList :, ", err);
            return false;
        }
    }

    async addRow(e, item) {
        if (this.state.isAdGroupNameError) {
            this.setState({
                MessagebarText: <Trans>adGroupNameExist</Trans>,
                MessageBarType: MessageBarType.error,
                isUpdate: false,
                isUpdateMsg: true
            });
            setTimeout(function () { this.setState({ isUpdateMsg: false, isUpdate: false, MessageBarType: MessageBarType.error, MessagebarText: "" }); }.bind(this), 3000);
            return false;
        }
        if (item.id.length === 0) {
            await this.addOrUpdatePermission(item, "POST", <Trans>permissionAddSuccess</Trans>);
        } else if (item.id.length > 0) {
            await this.addOrUpdatePermission(item, "PATCH", <Trans>permissionUpdatedSuccess</Trans>);
        }
    }

    onChangeProperty(e, item, property) {

        let items = JSON.parse(JSON.stringify(this.state.items));
        let permissionItem = item.id === this.state.permissionItem.id ? JSON.parse(JSON.stringify(this.state.permissionItem)) : item;
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
                            MessagebarText: <Trans>adGroupNameExist</Trans>,
                            MessageBarType: MessageBarType.error,
                            isUpdate: false,
                            isUpdateMsg: true,
                            isAdGroupNameError: true
                        });
                        setTimeout(function () { this.setState({ isUpdateMsg: false, isUpdate: false, MessageBarType: MessageBarType.error, MessagebarText: "" }); }.bind(this), 3000);
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
            permissionItem : this.schema
        });
    }

    deleteRow(item) {
        this.setState({ isUpdate: true });
        let items = this.state.items;
        if (item.id === "" || item.id.length === 0) {
            items = items.filter(p => p.adGroupName !== item.adGroupName);
            this.setState({ items: items, isUpdate: false });
            return false;
        } else {
            this.deletePermission(item);
        }
                
    }


    permissionsList(columns, isCompactMode, items, selectionDetails) {
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

    async addOrUpdatePermission(permissionItem, methodType, dispSuccessMsg) {
        //WAVE-4 : Changing RoleMappong to Roles:
        this.setState({ isUpdate: true });
        this.requestUpdUrl = 'api/Roles';
        let options = {
            method: methodType,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'authorization': 'Bearer    ' + window.authHelper.getWebApiToken()
            },
            body: JSON.stringify(permissionItem)
        };
        try {
            let response = await fetch(this.requestUpdUrl, options);
            console.log(response);
            if (response.ok) {
                let updatedPermissions = await this.getAllPermissionsList(true);
                this.setState({
                    items: updatedPermissions.items,
                    isUpdate: false
                });
                this.setMessage(false, true, MessageBarType.success, dispSuccessMsg);
            } else {
                dispSuccessMsg = <Trans>errorOoccuredPleaseTryAgain</Trans>;
                this.setMessage(false, true, MessageBarType.error, dispSuccessMsg, permissionItem = this.schema);
            }
        } catch (error) {
            console.log("Permission.js getAllPermissionsList :, ", error);
            this.setState({ isUpdate: false });
            dispSuccessMsg = <Trans>errorOoccuredPleaseTryAgain</Trans>;
            this.setMessage(false, true, MessageBarType.error, dispSuccessMsg, permissionItem = this.schema);
            return false;
        } finally {
            await this.loadAllPermissionData();
            setTimeout(function () { this.setMessage(false, false, "", ""); }.bind(this), 2000);
            return "done";
        }
    }

    setMessage(isUpdate, isUpdateMsg, MessageBarType, MessagebarText) {
        this.setState({ isUpdate, isUpdateMsg, MessageBarType, MessagebarText });
    }

    async deletePermission(permissionItem) {
        //WAVE-4 : Changing RoleMappong to Roles:
        // API Update call        
        this.requestUpdUrl = 'api/Roles/' + permissionItem.id;
        let options = {
            method: "DELETE",
            headers: {
                'authorization': 'Bearer ' + window.authHelper.getWebApiToken()
            }
        };
        try {
            let response = await fetch(this.requestUpdUrl, options);
            if (response.ok) {
                let updatedPermissions = await this.getAllPermissionsList(true);
                this.setState({
                    items: updatedPermissions.items
                });
                this.setMessage(false, true, MessageBarType.success, <Trans>permissionDeletedSuccess</Trans>);
                return response.json;
            } else {
                this.setMessage(false, true, MessageBarType.error, <Trans>errorOoccuredPleaseTryAgain</Trans>);
            }
        } catch (error) {
            this.setState({ isUpdate: false });
            console.log("Permission getAllPermissionsList :, ", error);
            return false;
        } finally {
            await this.loadAllPermissionData();
            setTimeout(function () { this.setMessage(false, false, "", ""); }.bind(this), 2000);
            return "done";
        }
    }

    render() {
        const { columns, isCompactMode, items, selectionDetails } = this.state;
        const permissionsList = this.permissionsList(columns, isCompactMode, items, selectionDetails);
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