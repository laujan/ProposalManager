/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React from 'react';
import { PrimaryButton, ActionButton } from 'office-ui-fabric-react/lib/Button';
import { Trans } from "react-i18next";
import { Modal } from 'office-ui-fabric-react/lib/Modal';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar } from 'office-ui-fabric-react/lib/MessageBar';
import { PreviewDealTypeR } from '../DealType/PreviewDealTypeR';

export default function ShowPreviewModel() {
    console.log("ShowPreviewModel==>,", this.state);
    return (
        <Modal
            isOpen={this.state.showPreviewModel}
            onDismiss={this.closePreviewModal.bind(this)}
            isBlocking='true'
            containerClassName="ms-modalExample-container"
        >
            <div className="ms-modalExample-header">
                <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6'>
                    <div className="ms-Grid-row bg-white">
                        <h4>Display Preview</h4>
                    </div>
                </div>
                <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6'>
                    <ActionButton
                        className="pull-right"
                        onClick={this.closePreviewModal.bind(this)}
                    >
                        <i className="ms-Icon ms-Icon--StatusCircleErrorX pull-right f30" aria-hidden="true" />
                    </ActionButton>
                </div>
            </div>
            <div className="ms-modalExample-body">

                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg12 ibox-content'>
                        <div className="ms-Grid-row bg-white">
                            <PreviewDealTypeR dealTypeObject={this.state.dealTypeObj} />

                        </div>
                    </div>

                </div>
                <div className="ms-Grid-row">
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg9'>
                        <div className='ms-BasicSpinnersExample p-10 pull-right'>
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
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg3'><br />
                        <PrimaryButton text={<Trans>save</Trans>} className="pull-right p-10" onClick={this.saveDealType.bind(this)} />
                    </div>
                </div>
            </div>

        </Modal>);
}