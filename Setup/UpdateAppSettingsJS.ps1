Function UpdateAppSettingsClient {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)][string]$Path,
        [Parameter(Mandatory = $true)] $AppId,
        [Parameter(Mandatory = $true)] $AppUri,
        [Parameter(Mandatory = $true)] $TenantId
    )

    (Get-Content $Path).
    Replace('<CLIENT_ID>',$AppId).
    Replace('<APP_URI>', $AppUri).
    Replace('<TENANT_ID>', $TenantId) |
    Set-Content $Path

}