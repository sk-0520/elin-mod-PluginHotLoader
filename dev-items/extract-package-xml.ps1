param(
    [Parameter(Mandatory)][string] $Assembly,
    [Parameter(Mandatory)][string] $Out
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$assemblyPath = $Assembly

if (! (Test-Path $assemblyPath)) {
    throw "Assembly not found: $assemblyPath"
}

$assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
$packageInstance = New-Object Elin.Plugin.Generated.MsBuildOnlyPackageXml

$utf8n = New-Object "System.Text.UTF8Encoding" -ArgumentList @($false)
[System.IO.File]::WriteAllText($Out, ($packageInstance.ToString()), $utf8n)
