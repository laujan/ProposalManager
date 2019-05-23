# Create Connection

This request will imitate the webhook request sent from Dynamics every time a Connection is created within an Opportunity in Dynamics 365.

## Permissions

The following permission is required to call this API.

- User should have the role of &#39;Relationship Manager&#39; in UserRoles list in Sharepoint and hence member of the AD group associated with this role.

## HTTP request

> POST \{applicationUrl}/api/webhooks/incoming/dynamicscrm/connection

### Request headers

| **Key** | **Value** |
| --- | --- |
| Content-Type | application/json |

### Request query params

| **Key** | **Value** |
| --- | --- |
| code | The secret webhook key configured in the Connection webhook. This is avaliable in the appsettings.json file once the Integration has been configured, in the WebHooks:DynamicsCrm:SecretKey section. In this case, you need the value from the "connection" property.|

### Request body

| **Option** | **Value** |
| --- | --- |
| raw | JSON(application/json) |

A sample object can be found [here](SampleConnectionJson.txt). Please note that you would need to change all occurences of the user id "cc03d281-c95c-e911-a979-000d3a1cc8fe" to a valid CRM User id from your organization in order for the payload to work. You would also need to provide a valid Opportunity Id.

The most safe and straightforward approach to get a working payload is to capture the original Dynamics request using a Webhook tester, such as https://webhook.site. Using the Plugin Registration tool as documented in the Setup Guide, duplicate the Connection creation webhook and point it to the URL provided by the Tester site. You can leave the key blank. After this, create a connection from Dynamics, and you will be able to see the request, with its payload. Note that the listener will display two requests: this is by design. The API will only process the first request, where the record1id.LogicalName = "opportunity" and the record2id.LogaicalName = "systemuser".

### Response

If successful, this method returns 201 Created response code, with no content.

### Example

##### Request

Here is an example of the request.

> POST \{applicationUrl}/webhooks/incoming/dynamicscrm/connection?code=thUlpI7mFY72k8nCFVr8Ny7INJk9jBfgGjy4x

##### Response

Here is an example of the response.

> 201 Created

Response body

> Empty



