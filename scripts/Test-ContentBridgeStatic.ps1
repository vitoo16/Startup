[CmdletBinding()]
param([string]$ReportPath)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$checks = [System.Collections.Generic.List[object]]::new()
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Check {
    param([string]$Name, [bool]$Passed, [string]$Detail)
    $checks.Add([ordered]@{ name = $Name; passed = $Passed; detail = $Detail })
    if (-not $Passed) { $failures.Add("$Name - $Detail") }
}

function Read-Text([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required file missing: $RelativePath" }
    return [IO.File]::ReadAllText($path)
}

$contentAsm = (Read-Text 'Assets/StartupLife/Scripts/Content/StartupLife.Content.asmdef') | ConvertFrom-Json
$presentationAsm = (Read-Text 'Assets/StartupLife/Scripts/Presentation/StartupLife.Presentation.asmdef') | ConvertFrom-Json
$editorAsm = (Read-Text 'Assets/StartupLife/Scripts/Editor/StartupLife.Editor.asmdef') | ConvertFrom-Json
$testsAsm = (Read-Text 'Assets/StartupLife/Tests/EditMode/StartupLife.Tests.EditMode.asmdef') | ConvertFrom-Json
$assetSource = Read-Text 'Assets/StartupLife/Scripts/Content/StartupLifeContentCatalogAsset.cs'
$builder = Read-Text 'Assets/StartupLife/Scripts/Editor/FirstPlayableContentBuilder.cs'

Add-Check 'Content assembly exists with expected name' ($contentAsm.name -eq 'StartupLife.Content') "got $($contentAsm.name)"
Add-Check 'Content assembly depends only on Core' (
    @($contentAsm.references).Count -eq 1 -and $contentAsm.references[0] -eq 'StartupLife.Core'
) ($contentAsm.references -join ', ')
Add-Check 'Presentation references Content' ('StartupLife.Content' -in @($presentationAsm.references)) ($presentationAsm.references -join ', ')
Add-Check 'Editor references Content' ('StartupLife.Content' -in @($editorAsm.references)) ($editorAsm.references -join ', ')
Add-Check 'EditMode tests reference Content' ('StartupLife.Content' -in @($testsAsm.references)) ($testsAsm.references -join ', ')
Add-Check 'ScriptableObject converts through immutable source' (
    $assetSource -match 'BuildCatalog\(\)\s*=>\s*source\.Build\(\)'
) 'StartupLifeContentCatalogAsset.BuildCatalog must delegate to ContentCatalogSource.Build'
Add-Check 'First playable content builder validates before save' (
    $builder -match '_\s*=\s*asset\.BuildCatalog\(\)'
) 'Editor builder must build/validate the catalog before SaveAssets'
Add-Check 'First playable content asset path is stable' (
    $builder -match 'Assets/StartupLife/Data/FirstPlayableContent\.asset'
) 'expected stable content asset path'

$report = [ordered]@{
    suite = 'Startup Life content bridge static contract'
    passed = @($checks | Where-Object passed).Count
    failed = $failures.Count
    checks = $checks
}

if ($ReportPath) {
    $resolved = [IO.Path]::GetFullPath($ReportPath)
    $directory = Split-Path -Parent $resolved
    if ($directory) { New-Item -ItemType Directory -Force -Path $directory | Out-Null }
    $report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $resolved -Encoding utf8
    Write-Output "Report: $resolved"
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Error $failure }
    throw "Content bridge static gate failed: $($failures.Count) check(s)."
}

Write-Output "Content bridge static contract: $($report.passed)/$($checks.Count) checks passed."
