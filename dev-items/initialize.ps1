param(
    [Parameter(Mandatory)][string] $ModName
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$rootDirectoryPath = Join-Path -Path $PSScriptRoot -ChildPath ".."

# 不要ファイル破棄
$removeItems = @(
	(Join-Path -Path ".github" -ChildPath "copilot-instructions.md"),
	(Join-Path -Path ".github" -ChildPath "workflows" | Join-Path -ChildPath "mod-template.yaml"),
	"docs",
	"Elin.Plugin.Generator.Test",
	"initialize.bat"
)
foreach ($item in $removeItems) {
	$itemPath = Join-Path -Path $rootDirectoryPath -ChildPath $item
	Remove-Item -Path $itemPath -Recurse -Force
}

# Mod 名をプロジェクトファイルに反映
$projectAsmPath = Join-Path -Path $rootDirectoryPath -ChildPath "Elin.Plugin.Main.Assembly.xml"
[xml]$asmXml = Get-Content -LiteralPath $projectAsmPath -Raw -Encoding UTF8
$asmXml.Project.PropertyGroup.AssemblyName = $ModName
$asmXml.Save($projectAsmPath)

# ソリューションから削除プロジェクトの破棄
$solutionPath = Join-Path -Path $rootDirectoryPath -ChildPath "Elin.Plugin.slnx"
[xml]$solutionXml = Get-Content -LiteralPath $solutionPath -Raw -Encoding UTF8
$solutionXml.SelectNodes("//*/Project") | ForEach-Object {
	if ($_.Path -eq "Elin.Plugin.Generator.Test/Elin.Plugin.Generator.Test.csproj") {
		$_.ParentNode.RemoveChild($_) | Out-Null
	}
}
$solutionXml.Save($solutionPath)
# ソリューションファイル名の変更
Rename-Item -Path $solutionPath -NewName "$ModName.slnx"
