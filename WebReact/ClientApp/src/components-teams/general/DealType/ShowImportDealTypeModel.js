/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React from 'react';
import { Modal } from 'office-ui-fabric-react/lib/Modal';
import { PrimaryButton, IconButton, ActionButton } from 'office-ui-fabric-react/lib/Button';
import { I18n, Trans } from "react-i18next";
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar } from 'office-ui-fabric-react/lib/MessageBar';
import { Checkbox } from 'office-ui-fabric-react/lib/Checkbox';

export default function ShowDealTypeModel(props) {

    return (
        <Modal isOpen={props.showModel} isBlocking='true' containerClassName="ms-modalExample-container" onDismiss={props.closeModel}>
            <div className="ms-modalExample-header">
                <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6'>
                    <ActionButton className="pull-right" onClick={props.closeModel}>
                        <i className="ms-Icon ms-Icon--StatusCircleErrorX pull-right f30" aria-hidden="true" />
                    </ActionButton>
                </div>
            </div>
            <div className="ms-modalExample-body">
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6'>
                        <h3>{props.modelMsgObject.modelHeader}</h3>
                    </div>
                </div>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg7'>
                        <div className="ms-Grid-row bg-white">
                            <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg12 p15">
                                {props.modelMsgObject.modelMsg}
                                {props.modelMsgObject.modelButtonFlag ? (<div>along with given below processes and roles</div>) : null}
                            </div>
                        </div>
                        {
                            props.modelMsgObject.modelButtonFlag ?
                                <div className="ms-Grid-row bg-white">
                                    <h5>Processes</h5>
                                    <div>
                                        {
                                            props.dealTypeJson.processes.length > 0 ?
                                                <div>
                                                    {
                                                        props.dealTypeJson.processes.map((process, idx) => {
                                                            return (
                                                                <div className="ms-Grid-col ms-sm10 ms-md6 ms-lg4 p15" key={idx}>
                                                                    <div className="ms-Grid-row bg-grey GrayBorder text-center">
                                                                        <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg12 purpleBG text-center">
                                                                            <h5>{process.processStep}</h5>
                                                                        </div>
                                                                    </div>
                                                                </div>
                                                            );
                                                        })
                                                    }

                                                </div> :
                                                <div>
                                                    No new Process will get added with the given Business Process.
                                                </div>
                                        }
                                    </div>
                                </div> : null
                        }
                        {
                            props.modelMsgObject.modelButtonFlag ?
                                <div className="ms-Grid-row bg-white">
                                    <h5>Roles</h5>
                                    <div>
                                        {
                                            props.dealTypeJson.rolemapping.length > 0 ?
                                                <div>
                                                    {
                                                        props.dealTypeJson.rolemapping.map((role, idx) => {
                                                            return (
                                                                <div className="ms-Grid-col ms-sm10 ms-md6 ms-lg4 p15" key={idx}>
                                                                    <div className="ms-Grid-row bg-grey GrayBorder text-center">
                                                                        <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg12 purpleBG text-center">
                                                                            <h5>{role.adGroupName}</h5>
                                                                        </div>
                                                                    </div>
                                                                </div>
                                                            );
                                                        })
                                                    }
                                                </div> :
                                                <div>
                                                    No new Roles will get added with the given Business Process.
                                                </div>
                                        }
                                    </div>
                                </div> : null
                        }
                        <div className="ms-Grid-row p-10 ">
                            {
                                props.modelMsgObject.modelButtonFlag ?
                                    <Checkbox
                                        label={<Trans>Please check if you want to create a Default Opportunity Team with the given Business Process.</Trans>}
                                        onChange={(e, item) => props.handleModelCheckbox(e, item)}
                                        checked={props.createOpportunityTeam}
                                    /> : null
                            }

                        </div>
                        <div className="ms-Grid-row p-10 ">
                            <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg9'>
                                <div className='ms-BasicSpinnersExample p-10 pull-right'>
                                    {
                                        props.messagebarObject.isUpdate ?
                                            <Spinner size={SpinnerSize.large} ariaLive='assertive' />
                                            : ""
                                    }
                                    {
                                        props.messagebarObject.isUpdateMsg ?
                                            <MessageBar
                                                messageBarType={props.messagebarObject.MessageBarType}
                                                isMultiline={false}
                                            >
                                                {props.messagebarObject.MessagebarText}
                                            </MessageBar>
                                            : ""
                                    }
                                </div>
                            </div>
                            <PrimaryButton text={<Trans>save</Trans>}
                                onClick={props.handleSaveButton}
                                disabled={!props.modelMsgObject.modelButtonFlag}
                            />
                        </div>
                    </div>
                </div>
            </div>
        </Modal>);
}