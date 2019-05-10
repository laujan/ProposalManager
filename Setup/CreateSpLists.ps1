$lists = @([ordered] @{
    Name = "Opportunities"
    Fields = @(
        @{ Name = "OpportunityId"; Indexed = $true; Type = "Text"; }
        @{ Name = "Name"; Indexed = $true; Type = "Text"; }
        @{ Name = "OpportunityState"; Indexed = $false; Type = "Text"; }
        @{ Name = "OpportunityObject"; Indexed = $false; Type = "Note"; }
        @{ Name = "TemplateLoaded"; Indexed = $false; Type = "Text"; }
        @{ Name = "Reference"; Indexed = $true; Type = "Text"; })},
    [ordered] @{
    Name = "Permissions"
    Fields = @(
        @{ Name = "Name"; Indexed = $false; Type = "Text"; })},
    [ordered] @{
    Name = "Workflow Items"
    Fields =  @(
        @{ Name = "ProcessStep"; Indexed = $false; Type = "Text"; }
        @{ Name = "Channel"; Indexed = $false; Type = "Text"; }
        @{ Name = "ProcessType"; Indexed = $false; Type = "Text"; }
        @{ Name = "RoleId"; Indexed = $false; Type = "Text"; }
        @{ Name = "RoleName"; Indexed = $false; Type = "Text"; })},
    [ordered] @{
    Name = "Dashboard"
    Fields = @(
        @{ Name = "CustomerName"; Indexed = $false; Type = "Text"; }
        @{ Name = "OpportunityID"; Indexed = $false; Type = "Text"; }
        @{ Name = "Status"; Indexed = $false; Type = "Text"; }
        @{ Name = "StartDate"; Indexed = $false; Type = "DateTime"; }
        @{ Name = "TargetCompletionDate"; Indexed = $false; Type = "DateTime"; }
        @{ Name = "OpportunityName"; Indexed = $true; Type = "Text"; }
        @{ Name = "TotalNoOfDays"; Indexed = $false; Type = "Number"; DefaultValue = "1" }
        @{ Name = "ProcessNoOfDays"; Indexed = $false; Type = "Note";  }
        @{ Name = "ProcessEndDates"; Indexed = $false; Type = "Note";  }
        @{ Name = "ProcessLoanOfficers"; Indexed = $false; Type = "Note";  }
    [ordered] @{
    Name = "Templates"
    Fields = @(
        @{ Name = "TemplateName"; Indexed = $false; Type = "Text"; }
        @{ Name = "Description"; Indexed = $false; Type = "Text"; }
        @{ Name = "LastUsed"; Indexed = $false; Type = "DateTime"; }
        @{ Name = "CreatedBy"; Indexed = $false; Type = "Note"; }
        @{ Name = "ProcessList"; Indexed = $false; Type = "Note"; }
        @{ Name = "DefaultTemplate"; Indexed = $false; Type = "Text"; })},
    [ordered] @{
    Name = "Group"
    Fields = @(
        @{ Name = "GroupName"; Indexed = $false; Type = "Text"; }
        @{ Name = "Process"; Indexed = $false; Type = "Note"; })},
    [ordered] @{
    Name = "Regions"
    Fields = @{ Name = "Name"; Indexed = $false; Type = "Text"; }},
    [ordered] @{
    Name = "Role"
    Fields = @(
        @{ Name = "AdGroupName"; Indexed = $false; Type = "Text"; }
        @{ Name = "Role"; Indexed = $false; Type = "Text"; }
        @{ Name = "TeamsMembership"; Indexed = $false; Type = "Text"; }
        @{ Name = "Permissions"; Indexed = $false; Type = "Note"; })},
    [ordered] @{
    Name = "Cateories"
    Fields = @{ Name = "Name"; Indexed = $false; Type = "Text"; }},
    [ordered] @{
    Name = "Industry"
    Fields = @{ Name = "Name"; Indexed = $false; Type = "Text"; }},
    [ordered] @{
    Name = "Tasks"
    Fields = @{ Name = "Name"; Indexed = $false; Type = "Text"; }},
    [ordered] @{
    Name = "OpportuniyMetaData"
    Fields = @(
        @{ Name = "FieldName"; Indexed = $false; Type = "Text"; }
        @{ Name = "FieldType"; Indexed = $false; Type = "Text"; }
        @{ Name = "FieldScreen"; Indexed = $false; Type = "Text"; }
        @{ Name = "FieldValue"; Indexed = $false; Type = "Note"; })}
)


#Connect-PnPOnline $pmpAdminSite.Url
foreach($list in $lists)
{
    Write-Host "Creating list $($list.Name)"
    #New-PnPList -Title $list.Name -Template GenericList
    foreach($field in $list.Fields){
        $xml = "<Field Type=`"$($field.Type)`" DisplayName=`"$($field.Name)`" Name=`"$($field.Name)`" Indexed=`"$($field.Indexed)`">"
        
        if($field.Type -eq "Number")
        {
            $xml += "<Default>$($field.DefaultValue)</Default>"
        }

        $xml += "</Field>"

        Write-Host $xml -ForegroundColor Yellow
        #Add-PnPFieldFromXml -List $list.Name -FieldXml '<Field Type="$list.Type" DisplayName="$list.Name" Name="$list.Name" Indexed="$list.Indexed" />'
    }
}
