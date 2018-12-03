Function UpdateAppSettingsClient {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$pathToJson,
        [Parameter(Mandatory = $true)] $appId,
        [Parameter(Mandatory = $true)] $appUri,
        [Parameter(Mandatory = $true)] $tenantId
    )

    $appSettings = Get-Content -Path $pathToJson

    $index = 0;
    foreach($i in $appSettings.GetEnumerator())
    {

        if($i.StartsWith("export const clientId"))
        {
            $appSettings[$index] = "export const clientId = '$appId'; //Registered Application Id from apps.dev.microsoft.com."
            Set-Content -Path $pathToJson $appSettings
        }
        else
        { 
            if($i.StartsWith("export const webApiScopes"))
            {
               $appSettings[$index] = "export const webApiScopes = [`"api://$appId/access_as_user`"];// web Api scope generated at app registration from apps.dev.microsoft.com."
                Set-Content -Path $pathToJson $appSettings
            }
            else
            {
                if($i.StartsWith("export const appUri"))
                {
                   $appSettings[$index] = "export const appUri = '$appUri';"
                   Set-Content -Path $pathToJson $appSettings
                }
                else
                {
                    if($i.StartsWith("export const authority"))
                    {
                        $appSettings[$index] = "export const authority = 'https://login.microsoftonline.com/$tenantId';"
                        Set-Content -Path $pathToJson $appSettings
                    }
                }
            }
        }

        $index++
    }

    Write-Host "AppSettings.js has been updated"
}