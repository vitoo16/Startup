param([string]$DotnetPath = '', [string]$ReportPath = 'docs/evidence/PROVISIONAL-FIRST-PLAYABLE/test-report.json')
$ErrorActionPreference = 'Stop'
$startupRoot = Split-Path -Parent $PSScriptRoot
if (-not $DotnetPath) {
    $startupBundledSdk = Join-Path ([Environment]::GetFolderPath('UserProfile')) '.codex/tooling/startup-life/dotnet/dotnet.exe'
    if (Test-Path -LiteralPath $startupBundledSdk) { $DotnetPath = $startupBundledSdk }
    else { $DotnetPath = (Get-Command dotnet -ErrorAction Stop).Source }
}
if (-not [IO.Path]::IsPathRooted($ReportPath)) { $ReportPath = Join-Path $startupRoot $ReportPath }
$reportDirectory = Split-Path -Parent $ReportPath
$astraReportPath = Join-Path $reportDirectory 'astra-foundation-highs-report.json'
$restoreReportPath = Join-Path $reportDirectory 'astra-m2-restore-invariants-report.json'

& $DotnetPath build "$startupRoot/tools/SimulationChecks/SimulationChecks.csproj" --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Provisional .NET build failed.' }
& $DotnetPath run --project "$startupRoot/tools/SimulationChecks/SimulationChecks.csproj" --configuration Release --no-build -- $ReportPath
if ($LASTEXITCODE -ne 0) { throw 'Provisional simulation checks failed.' }

& $DotnetPath build "$startupRoot/tools/AstraFoundationChecks/AstraFoundationChecks.csproj" --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Astra foundation regression build failed.' }
& $DotnetPath run --project "$startupRoot/tools/AstraFoundationChecks/AstraFoundationChecks.csproj" --configuration Release --no-build -- $astraReportPath
if ($LASTEXITCODE -ne 0) { throw 'Astra foundation regression checks failed.' }

& $DotnetPath build "$startupRoot/tools/RestoreInvariantChecks/RestoreInvariantChecks.csproj" --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Astra M2 restore invariant build failed.' }
& $DotnetPath run --project "$startupRoot/tools/RestoreInvariantChecks/RestoreInvariantChecks.csproj" --configuration Release --no-build -- $restoreReportPath
if ($LASTEXITCODE -ne 0) { throw 'Astra M2 restore invariant checks failed.' }

$presentationReportPath = Join-Path $reportDirectory 'presentation-adapter-report.json'

& $DotnetPath build "$startupRoot/tools/PresentationChecks/PresentationChecks.csproj" --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Presentation adapter regression build failed.' }
& $DotnetPath run --project "$startupRoot/tools/PresentationChecks/PresentationChecks.csproj" --configuration Release --no-build -- $presentationReportPath
if ($LASTEXITCODE -ne 0) { throw 'Presentation adapter regression checks failed.' }

Write-Output 'These reports verify .NET behavior only. Unity EditMode, PlayMode, IL2CPP, and devices remain separate gates.'
