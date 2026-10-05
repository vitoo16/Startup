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
$contentReportPath = Join-Path $reportDirectory 'content-catalog-report.json'
$presentationReportPath = Join-Path $reportDirectory 'presentation-adapter-report.json'
$businessReportPath = Join-Path $reportDirectory 'm8-business-ownership-report.json'

# Temporary focused-only validation on this unmerged branch; restored before final full CI.
& $DotnetPath build "$startupRoot/tools/BusinessOwnershipChecks/BusinessOwnershipChecks.csproj" --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Focused M8 parser containment build failed.' }
& $DotnetPath run --project "$startupRoot/tools/BusinessOwnershipChecks/BusinessOwnershipChecks.csproj" --configuration Release --no-build -- $businessReportPath
if ($LASTEXITCODE -ne 0) { throw 'Focused M8 parser containment regression failed.' }
Write-Output 'FOCUSED VALIDATION ONLY: BusinessOwnershipChecks. Full engine-free runner will be restored after this passes.'
return

& $DotnetPath build "$startupRoot/tools/SimulationChecks/SimulationChecks.csproj" --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Provisional .NET build failed.' }
& $DotnetPath run --project "$startupRoot/tools/SimulationChecks/SimulationChecks.csproj" --configuration Release --no-build -- $ReportPath
if ($LASTEXITCODE -ne 0) { throw 'Provisional simulation checks failed.' }

& $DotnetPath build "$startupRoot/tools/BusinessOwnershipChecks/BusinessOwnershipChecks.csproj" --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'M8-T01 business ownership acceptance build failed.' }
& $DotnetPath run --project "$startupRoot/tools/BusinessOwnershipChecks/BusinessOwnershipChecks.csproj" --configuration Release --no-build -- $businessReportPath
if ($LASTEXITCODE -ne 0) { throw 'M8-T01 business ownership acceptance checks failed.' }

& $DotnetPath build "$startupRoot/tools/AstraFoundationChecks/AstraFoundationChecks.csproj" --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Astra foundation regression build failed.' }
& $DotnetPath run --project "$startupRoot/tools/AstraFoundationChecks/AstraFoundationChecks.csproj" --configuration Release --no-build -- $astraReportPath
if ($LASTEXITCODE -ne 0) { throw 'Astra foundation regression checks failed.' }

& $DotnetPath build "$startupRoot/tools/RestoreInvariantChecks/RestoreInvariantChecks.csproj" --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Astra M2 restore invariant build failed.' }
& $DotnetPath run --project "$startupRoot/tools/RestoreInvariantChecks/RestoreInvariantChecks.csproj" --configuration Release --no-build -- $restoreReportPath
if ($LASTEXITCODE -ne 0) { throw 'Astra M2 restore invariant checks failed.' }

& $DotnetPath build "$startupRoot/tools/PresentationChecks/PresentationChecks.csproj" --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Presentation adapter regression build failed.' }
& $DotnetPath run --project "$startupRoot/tools/PresentationChecks/PresentationChecks.csproj" --configuration Release --no-build -- $presentationReportPath
if ($LASTEXITCODE -ne 0) { throw 'Presentation adapter regression checks failed.' }
& $DotnetPath build "$startupRoot/tools/ContentCatalogChecks/ContentCatalogChecks.csproj" --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Content catalog bridge build failed.' }
& $DotnetPath run --project "$startupRoot/tools/ContentCatalogChecks/ContentCatalogChecks.csproj" --configuration Release --no-build -- $contentReportPath
if ($LASTEXITCODE -ne 0) { throw 'Content catalog bridge checks failed.' }

Write-Output 'These reports verify .NET behavior only. Unity EditMode, PlayMode, IL2CPP, and devices remain separate gates.'
