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
$businessReportPath = Join-Path $reportDirectory 'm8-business-ownership-report.json'

& $DotnetPath build "$startupRoot/tools/BusinessOwnershipChecks/BusinessOwnershipChecks.csproj" --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'M8-T01 business ownership acceptance build failed.' }
& $DotnetPath run --project "$startupRoot/tools/BusinessOwnershipChecks/BusinessOwnershipChecks.csproj" --configuration Release --no-build -- $businessReportPath
if ($LASTEXITCODE -ne 0) { throw 'M8-T01 business ownership acceptance checks failed.' }

Write-Output 'Focused M8-T01 business restore-provenance proof only. Full engine-free regression follows after this gate passes.'
