param(
	[ValidateSet("prepare", "prd", "dev")]
    [Parameter(Mandatory)][string] $Mode,
    [Parameter(Mandatory)][string] $ElinPath
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest


$elinRootDirPath = $ElinPath
$engineDirPath = "MonoBleedingEdge\EmbedRuntime"
$engineDllName = "mono-2.0-bdwgc.dll"

$debugEngineDllUrl = "https://elin-modding-resources.github.io/Elin.Docs/mono-2.0-bdwgc.dll"

$engineDllDirPath = Join-Path -Path $elinRootDirPath -ChildPath $engineDirPath
$engineDllPath = Join-Path -Path $engineDllDirPath -ChildPath $engineDllName

$dllDirPath = Join-Path -Path $PSScriptRoot -ChildPath "debug-dll"
$prdDllDirPath = Join-Path -Path $dllDirPath -ChildPath "prd"
$devDllDirPath = Join-Path -Path $dllDirPath -ChildPath "dev"
$dllLastActionPath = Join-Path -Path $dllDirPath -ChildPath "last-action.txt"

$prdDllPath = Join-Path -Path $prdDllDirPath -ChildPath $engineDllName
$devDllPath = Join-Path -Path $devDllDirPath -ChildPath $engineDllName

function Invoke-Prepare {
	New-Item -Path $dllDirPath -ItemType Directory -Force
	New-Item -Path $prdDllDirPath -ItemType Directory -Force
	New-Item -Path $devDllDirPath -ItemType Directory -Force

	Copy-Item -Path $engineDllPath -Destination "${engineDllPath}.prepare-$((Get-Date).ToString('yyyyMMddHHmmss')).bak" -Force
	Copy-Item -Path $engineDllPath -Destination $prdDllPath -Force
	Invoke-WebRequest -Uri $debugEngineDllUrl -OutFile $devDllPath
	Set-Content -Path $dllLastActionPath -Value "prepare"
}

function Invoke-Prd {
	Copy-Item -Path $prdDllPath -Destination $engineDllPath -Force
	Set-Content -Path $dllLastActionPath -Value "prd"
}

function Invoke-Dev {
	Copy-Item -Path $devDllPath -Destination $engineDllPath -Force
	Set-Content -Path $dllLastActionPath -Value "dev"
}


switch ($Mode) {
	"prepare" {
		Invoke-Prepare
	}
	"prd" {
		Invoke-Prd
	}
	"dev" {
		Invoke-Dev
	}
}