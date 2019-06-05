/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/
import React, { Component } from 'react';
import { Image } from 'office-ui-fabric-react/lib/Image';
import { Label } from 'office-ui-fabric-react/lib/Label';

export class Help extends Component {
    displayName = Help.name
    constructor(props) {
        super(props);

        this.authHelper = window.authHelper;
    }
    render() {
        const bold = { 'fontWeight': 'bold' };
        return (
            <div className='ms-Grid bg-white p-10'>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12' >
                        <h3 style={bold} className="helpPageheading">Help</h3>
                    </div>
                </div>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12' >
                        <h5 className="helpHeading5">Overview</h5>
                        <Label>The purpose of this document is to provide an overview of some issues encountered with the solution and share guidance on how to address them. This also covers a list of known issues.</Label>
                    </div>
                </div>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12' >
                        <h4 style={bold} className="helpPageheading">Admin Consent permission</h4>
                        <Label>This section covers known issues and troubleshooting steps after for the deployment admin consent/permission issues while accessing Proposal Manager application.</Label>
                        <h5 className="helpHeading5">Known Issues</h5>
                        <div>
                            This section lists some of the key known issues.
                        </div>
                        <h5 className="helpHeading5">Troubleshooting</h5>
                        <span>Issue: Admin consent or permission issue after successful deployment of Proposal Manager application</span>

                        <Label className="ms-Label">
                            a) Login Azure portal -> click on Azure Active Directory -> Select your application-> Click on API permission -> Click on “Grant admin consent“ button in Grant consent section.
                        </Label>
                        <Image
                            src={require('../components-teams/Images/consent1.png')}
                            alt="grant admin consent"
                            width={600}
                        />
                        <Label className="ms-Label">
                            b) Access application url (https://&lt;SiteName&gt;.azurewebsites.net/) and sign-in with admin user credentials. Popup will prompt to accept the admin consent
                        </Label>
                        <Image
                            src={require('../components-teams/Images/consent2.png')}
                            alt="grant admin consent"
                            width={600}
                        />
                        <Label className="ms-Label">
                            c) Access application url with setup (https://&lt;SiteName&gt;.azurewebsites.net/Setup). Sign-in with Proposal manager admin user. Popup will prompt to accept to accept the admin consent
                        </Label>
                        <Image
                            src={require('../components-teams/Images/consent3.png')}
                            alt="grant admin consent"
                            width={600}
                        />
                        <Label className="ms-Label">
                            d) Azure portal go to app services -> select your app service and click on reset the app service once
                        </Label>
                        <Image
                            src={require('../components-teams/Images/consent4.png')}
                            alt="grant admin consent"
                            width={600}
                        />
                        <Label className="ms-Label">
                            e)Clear/reset the browser cache
                        </Label>

                    </div>
                </div>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12' >
                        <h4 style={bold} className="helpPageheading">Proposal Manager Teams add-in</h4>
                        <Label className="ms-Label">This section covers known issues and troubleshooting steps for the deployment and configuration of the Proposal Manager Teams add-in.</Label>

                        <h5 className="helpHeading5">Known Issues</h5>
                        <div>
                            This section lists some of the key known issues:
                            <ul>
                                <li>File selection and upload does not work on mobile devices
                                </li>
                                <li>Proposal Manager has been validated only on Microsoft Edge, Google Chrome and Mozilla on Desktop and on the default browser on iOS and Android phones
                                </li>
                                <li>Opportunities created from Dynamics 365 using the Web Hook will not show up in the dashboard
                                </li>
                                <li>In the mobile, after the authentication, proposal manager auth pop-up won’t close by itself, from a dev standpoint this mobile auth issue can be addressed by adding 7 URLs  explicitly to app settings.
                                </li>
                            </ul>
                        </div>
                        <h5 className="helpHeading5">Troubleshooting</h5>
                        <div>
                            <table className="ms-Table helpTable">
                                <thead>
                                    <tr>
                                        <th>Error</th>
                                        <th>Recommended Solution</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr className="ms-Table-row">
                                        <td>“Reply-to address does not match one defined for the application”</td>
                                        <td>Confirm that the reply-to URLs specified for the application in the Application Registration Portal exactly matches the reply-to address specified in the error message</td>
                                    </tr>
                                    <tr>
                                        <td>Setup page shows 500 error after first time successful load on Azure</td>
                                        <td>Scale up the web app to add more memory to see if it resolves the issue</td>
                                    </tr>
                                    <tr>
                                        <td>Landing page gets stuck at “Loading your experience”</td>
                                        <td>Make sure that pop-ups are enabled on the site to facilitate opening the sign-in window</td>
                                    </tr>
                                    <tr>
                                        <td>Clicking on the Proposal Manager add-in link from a channel in Teams Mobile app opens a page stuck in “Loading”</td>
                                        <td>Logon to the account in another window on the default browser in the phone on some other site as http://portal.microsoftonline.com before clicking on the add-in link in Teams</td>
                                    </tr>
                                    <tr>
                                        <td>Assigning Loan Officer from Dynamics page is not working</td>
                                        <td>Go to sharepoint page of opportunity list, click on settings, click on indexed columns, check whether Reference is available as indexed column if not create new index with reference column.</td>
                                    </tr>
                                    <tr>
                                        <td>After deployment, we need to edit power bi template data source (share point list url) and publish that into power bi service. </td>
                                        <td>As of now, we need to this manually after every deployment.<br />
                                            1)	download the powerbi template from github repo.<br />
                                            2)	Open that in the powerbi desktop<br />
                                            3)	Go to options and settings<br />
                                            4)	Change the data source (with new sharepoint url)<br />
                                            5)	Add the measures and columns as per getting started guide<br />
                                            6)	Deploy the template into powerbi service<br />
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>

                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12' >
                        <h4 style={bold} className="helpPageheading">Proposal Creation Office add-in</h4>
                        <Label>This section covers known issues and troubleshooting steps for the deployment and configuration of the Proposal Creation Office add-in.</Label>

                        <h5 className="helpHeading5">Known Issues</h5>
                        <div>
                            This section lists some of the key known issues:
                        </div>
                        <h5>Troubleshooting</h5>
                        <div>
                            This section details how to address some issues that could be encountered
                        </div>
                    </div>
                </div>

                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12' >
                        <h4 style={bold} className="helpPageheading">Project Smart Link Office add-in</h4>
                        <Label>This section covers known issues and troubleshooting steps for the deployment and configuration of the Project Smart Link Office add-in</Label>

                        <h5 className="helpHeading5">Known Issues</h5>
                        <div>
                            This section lists some of the key known issues
                        </div>
                        <h5 className="helpHeading5">Troubleshooting</h5>
                        <div>
                            This section details how to address some issues that could be encountered.
                            <Label>Issue: Getting error message “Load Document ID failed” when using Project Smart Link</Label>
                            <span>
                                This section details how to fix the solution, if you see the following message when logging in to Project Smart Link:
                            </span>
                            <Image
                                src={require('../components-teams/Images/smartlink1.png')}
                                alt="grant admin consent"
                                width={300}
                            />
                            <p>
                                This means the Document ID Service is not enabled in the opportunity’s site collection. To enable it, go to the section “Configure SharePoint Document ID Service” of the Project Smart Link Deployment guide, and follow the steps indicated.
                            </p>
                            <p>
                                Once activated, the service will not automatically set a document id to your existing documents, so this process needs to be done manually; the steps to do so are the following:
                            </p>
                            <ol>
                                <li>
                                    Open your browser and go to the SharePoint site collection in which you are working; the url must follow this pattern: https://&lt;YOUR_TENANT&gt;.sharepoint.com/sites/&lt;NAME OF THE OPPORTUNITY WITHOUT SPACES&gt;; for example, if your tenant is “MyTenant” and the opportunity is called “Lending Opportunity” then the url will be https://MyTenant.sharepoint.com/sites/LendingOpportunity
                                </li>
                                <li>
                                    In the “Documents” section of the site, locate the document (or documents) you want to use with Project Smart Link.
                                </li>
                                <li>
                                    For each of those documents, do the following:
                                </li>
                                <span>a. Open the Details pane of the document</span>
                                <Image
                                    src={require('../components-teams/Images/smartlink2.png')}
                                    alt="grant admin consent"
                                    width={300}
                                />
                                <p>b. Check whether the document already has a Document ID or not:</p>
                                <Image
                                    src={require('../components-teams/Images/smartlink3.png')}
                                    alt="grant admin consent"
                                    width={300}
                                />
                                <span>If you see the caption “Enter value here” then the document does not have a Document ID assigned yet. If this is the case, go to item c of this list. Otherwise, you can already use Project Smart Link.</span>
                                <p>c. To assign the Document ID to the document, rename it:</p>
                                <Image
                                    src={require('../components-teams/Images/smartlink4.png')}
                                    alt="grant admin consent"
                                    width={300}
                                />
                                <span>The name you choose is irrelevant, what matters is that you rename the file with a different name.</span>
                                <p>d. Once the Document ID is assigned, you can rename the document back to its original name</p>
                            </ol>

                        </div>
                    </div>
                </div>

                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12' >
                        <h4 style={bold} className="helpPageheading">Dynamics 365 Web Hook</h4>
                        <Label>This section covers known issues and troubleshooting steps for the deployment and configuration of the Dynamics 365 web hook.</Label>

                        <h5 className="helpHeading5">Known Issues</h5>
                        <div>
                            This section lists some of the key known issues:
                        </div>
                        <h5 className="helpHeading5">Troubleshooting</h5>
                        <div>
                            This section details how to address some issues that could be encountered
                        </div>
                    </div>
                </div>

            </div>
        );
    }
}