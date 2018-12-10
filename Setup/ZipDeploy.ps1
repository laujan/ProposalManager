param(
    [Parameter(Mandatory=$true)]
    [string]$sourcePath,
    [Parameter(Mandatory=$true)]
    [string]$username,
    [Parameter(Mandatory=$true)]
    [string]$password,
    [Parameter(Mandatory=$true)]
    [string]$appName
)

$zipFile = [System.IO.Path]::ChangeExtension([System.IO.Path]::GetTempFileName(), "zip")

Compress-Archive -Path $sourcePath -DestinationPath $zipFile -Force -CompressionLevel Fastest

#PowerShell
$filePath = $zipFile
$apiUrl = "https://$appName.scm.azurewebsites.net/api/zipdeploy"
$base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $username, $password)))
Write-Information "Deploying..."
$result = Invoke-RestMethod -Uri $apiUrl -Headers @{Authorization=("Basic $base64AuthInfo")} -Method POST -InFile $filePath -ContentType "multipart/form-data"