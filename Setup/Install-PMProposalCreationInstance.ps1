[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [pscredential]$Credential
)

. .\RegisterAppV2.ps1

$replyUrls = @([string]::Empty, 'auth', 'auth/end')

$applicationPermissions = @()

# Register Azure AD application (Endpoint v2)
$proposalCreationRegistration = RegisterApp -ApplicationName "ProposalCreation" -RelativeReplyUrls $replyUrls -Credential $credential