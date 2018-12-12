function CheckDevPack
{
    [CmdletBinding()]
    param(
        [string]$Version = "4.6.1"
    )
    
    try
    {
        $devPacks = Get-ChildItem -Path "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework" | ?{ $_.PSIsContainer } | Select-Object Name
    }
    catch
    {
        return $false
    }

    $v461 = new-object System.Version($Version)

     foreach($pack in $devPacks)
     {
        $packVersion = new-object System.Version($pack.Name.Replace("v",""))

        $result = $packVersion.CompareTo($v461);

        if($result -gt 0 -or $result -eq 0)
        {
            return $true;
        }
    }

    return $false
}