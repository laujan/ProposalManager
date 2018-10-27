The following permission is required to call this API.

- User should have the Administrator Permission to call this API; in RoleMapping list in Sharepoint.

| **Key** | **Value** |
| --- | --- |
| Authorization | Bearer {token}. Required. |
| Content-Type | application/json |

### Request body

| **Option** | **Value** |
| --- | --- |
| raw | JSON(application/json) |

### Important Points:

 Postman is installed on the machine

- Download link: [https://www.getpostman.com/apps](https://www.getpostman.com/apps)

- Sending API requests from postman: [https://www.getpostman.com/docs/v6/postman/sending\_api\_requests/requests](https://www.getpostman.com/docs/v6/postman/sending_api_requests/requests)



You have to login in to the proposal manager application with the valid user who is authorized to carry out the required transaction. This is required for grabbing the &quot;webApi token&quot; to be used while invoking the API. Token validity may be for a short duration. If you get a 401 unauthorized response, please try to sign-in to the application again and fetch a fresh token.  Below screenshots are for fetching the token from different browsers:



