/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { DetailsList, DetailsListLayoutMode, Selection, SelectionMode } from 'office-ui-fabric-react/lib/DetailsList';
import { MarqueeSelection } from 'office-ui-fabric-react/lib/MarqueeSelection';
import { DefaultButton, PrimaryButton, IconButton } from 'office-ui-fabric-react/lib/Button';
import { SearchBox } from 'office-ui-fabric-react/lib/SearchBox';
import { TooltipHost } from 'office-ui-fabric-react/lib/Tooltip';
import { I18n, Trans } from "react-i18next";
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { LinkContainer } from 'react-router-bootstrap';
import i18n from '../../../i18n';
import Accessdenied from '../../../helpers/AccessDenied';
import TemplatesCommon from './TemplatesCommon';

export class TemplateList extends Component {
    displayName = TemplateList.name

    constructor(props) {
        super(props);

        this.authHelper = window.authHelper;
        this.apiService = this.props.apiService;
        this.logService = this.props.logService;
        this.templatesCommon = new TemplatesCommon(this.apiService, this.logService);

        this.accessGranted = false;
        const columns = [
            {
                key: 'column1',
                name: <Trans>templateName</Trans>,
                fieldName: 'templateName',
                minWidth: 100,
                maxWidth: 200,
                isResizable: true
            },
            {
                key: 'column2',
                name: <Trans>lastUsed</Trans>,
                fieldName: 'lastUsed',
                minWidth: 100,
                maxWidth: 200,
                isResizable: true,
                ariaLabel: 'Last Used',
                onRender: (item) => {
                    return (
                        <div>
                            {new Date(item.lastUsed).toLocaleDateString(i18n.language)}
                        </div>
                    );
                }
            },
            {
                key: 'column3',
                name: <Trans>createdBy</Trans>,
                fieldName: 'createdDisplayName',
                minWidth: 100,
                maxWidth: 200,
                isResizable: true,
                ariaLabel: 'Created By'
            },
            {
                key: 'column4',
                name: <Trans>action</Trans>,
                headerClassName: 'ms-List-th dealTypeAction ',
                className: 'DetailsListExample-cell--FileIcon actioniconAlign  ',
                minWidth: 100,
                maxWidth: 100,
                onColumnClick: this.onColumnClick,
                onRender: (item) => {
                    return (
                        <div className=''>
                            <TooltipHost content={<Trans>edit</Trans>} calloutProps={{ gapSpace: 0 }} closeDelay={200}>
                                <IconButton iconProps={{ iconName: 'Edit' }} onClick={e => this.editTemplate(item)} />
                            </TooltipHost>
                        </div>
                    );
                }
            }
        ];

        this.state = {
            loading: true,
            channelName: "",
            columns: columns,
            selectedTemplateCount: 0,
            filterTemplateName: '',
            items: [],
            itemsOriginal: [],
            isUpdateMsg: false,
            MessageBarType: MessageBarType.success,
            haveGranularAccess: false,
            processItems: [],
            processItemsOriginal : []
        };

        this._onFilterByTemplateNameChanged = this._onFilterByTemplateNameChanged.bind(this);
        this.deleteTemplate = this.deleteTemplate.bind(this);
    }

    async componentDidMount() {
        this.logService.log("Templatelist_componentDidMount");
        if (this.authHelper.isAuthenticated() && !this.accessGranted) {
            try {
                await this.authHelper.callCheckAccess(["Administrator", "Opportunity_ReadWrite_Template", "Opportunities_ReadWrite_All"]);
                this.logService.log("Templatelist_componentDidUpdate callCheckAccess success");
                this.accessGranted = true;
                await this.fnGetTemplates();
                await this.fnGetProcess();

            } catch (error) {
                this.accessGranted = false;
                this.logService.log("Templatelist_componentDidUpdate error_callCheckAccess:", error);
            }
        }
    }

    async fnGetTemplates() {
        let templateObj = await this.templatesCommon.getAllTemplatesList();
        
        this.setState({
            loading: false,
            items: templateObj,
            itemsOriginal: templateObj
        });
    }

    async fnGetProcess() {
        let processItems, processItemsOriginal = [];
        processItems = await this.templatesCommon.getAllProcess();
        this.logService.log(processItems);
        if (processItems.length > 0) {
            this.setState({
                loading: false,
                processItems,
                processItemsOriginal: processItems
            });
        } else {
            this.setState({
                loading: false,
                processItems,
                processItemsOriginal
            });
        }
    }

    errorHandler(err, referenceCall) {
        this.logService.log("Get TemplatesList Ref: " + referenceCall + " error: " + JSON.stringify(err));
    }

    _selection = new Selection({
        onSelectionChanged: () => this.setState({ selectionDetails: this._getSelectionDetails() })
    });

    _getSelectionDetails() {
        return this._selection.getSelectedCount();
    }

    // Filter by Templatename
    _onFilterByTemplateNameChanged(text) {
        const items = this.state.itemsOriginal;

        this.setState({
            filterTemplateName: text,
            items: text ?
                items.filter(item => item.templateName.toString().toLowerCase().indexOf(text.toString().toLowerCase()) > -1) :
                items
        });
    }

    deleteTemplate(items) {
        this.setState({ isUpdate: true });

        this.apiService.callApi('Template', 'DELETE', { id: items[0].id })
            .then(response => {
                if (response.ok) {
                    let currentItems = this.state.items.filter(x => x.id !== items[0].id);

                    this.setState({
                        items: currentItems,
                        itemsOriginal: currentItems
                    });

                    this.setMessage(false, true, MessageBarType.success, <Trans>dealTypeDeletedSuccess</Trans>);
                } else {
                    this.setMessage(false, true, MessageBarType.error, <Trans>errorOoccuredPleaseTryAgain</Trans>);
                }
            })
            .catch(error => {
                this.setMessage(false, true, MessageBarType.error, `${<Trans>errorOoccuredPleaseTryAgain</Trans>} : ${error.message}`);
            });
    }

    setMessage(isUpdate, isUpdateMsg, MessageBarType, MessagebarText) {
        //Show message
        this.setState({ isUpdate, isUpdateMsg, MessageBarType, MessagebarText });

        //Schedule message hide
        setTimeout(function () {
            this.setState({ isUpdate: false, isUpdateMsg: false, MessageBarType: "", MessagebarText: "" });
        }.bind(this), 3000);
    }

    editTemplate(templateItem) {
        window.location = "/tab/generalAddTemplate?dealTypeId=" + templateItem.id;
    }

    render() {
        const { columns } = this.state;
        let showDeleteButton = this._selection.getSelection().length > 0 ? true : false;
        if (this.state.loading) {
            return (
                <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                </div>
            );
        } else {
            return (
                this.accessGranted
                    ?
                    <div className='ms-Grid bg-white  p-10 ibox-content'>
                        <div className='ms-Grid-row'>
                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 hide'>
                                <h2><Trans>dealTypeList</Trans></h2>
                            </div>
                        </div>
                        <div className='ms-Grid-row'>
                            <div className='ms-Grid-col ms-sm4 ms-md4 ms-lg2'>
                                <DefaultButton iconProps={{ iconName: 'Delete' }} className={showDeleteButton ? "" : "hide"} onClick={e => this.deleteTemplate(this._selection.getSelection())}>Delete</DefaultButton>
                            </div>
                            <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg5'>
                                <div className='ms-BasicSpinnersExample'>
                                    {
                                        this.state.isUpdate ?
                                            <Spinner size={SpinnerSize.large} ariaLive='assertive' className='pull-left' />
                                            : ""
                                    }
                                    {
                                        this.state.isUpdateMsg ?
                                            <MessageBar
                                                messageBarType={this.state.MessageBarType}
                                                isMultiline={false}
                                                className='pull-left'
                                            >
                                                {this.state.MessagebarText}
                                            </MessageBar>
                                            : ""
                                    }
                                </div>
                            </div>
                            <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg3 ml4percentage'>
                                <I18n>
                                    {
                                        t => {
                                            return (
                                                <SearchBox
                                                    placeholder={t('search')}
                                                    onChange={this._onFilterByTemplateNameChanged}
                                                />
                                            );
                                        }
                                    }
                                </I18n>
                            </div>
                            <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg1'>
                                <LinkContainer to={'generalAddTemplate'} >
                                    <PrimaryButton iconProps={{ iconName: 'Add' }} >&nbsp;<Trans>add</Trans></PrimaryButton>
                                </LinkContainer>
                            </div>
                        </div>
                        <div className='ms-Grid-row LsitBoxAlign width102 '>
                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                                {
                                    this.state.items.length > 0
                                        ?
                                        <MarqueeSelection selection={this._selection}>
                                            <DetailsList
                                                componentRef={this._detailsList}
                                                items={this.state.items}
                                                columns={columns}
                                                setKey="set"
                                                layoutMode={DetailsListLayoutMode.fixedColumns}
                                                selection={this._selection}
                                                selectionPreservedOnEmptyClick
                                                ariaLabelForSelectionColumn="Toggle selection"
                                                ariaLabelForSelectAllCheckbox="Toggle selection for all items"
                                                onItemInvoked={this._onItemInvoked}
                                                selectionMode={SelectionMode.single}
                                            />
                                        </MarqueeSelection>
                                        :
                                        <div><Trans>thereAreNoDealType</Trans></div>
                                }
                            </div>
                        </div>


                    </div>
                    :
                    <Accessdenied />
            );
        }
    }
}