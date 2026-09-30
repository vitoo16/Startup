param([string]$DotnetPath = '', [string]$ReportPath = 'docs/evidence/PROVISIONAL-FIRST-PLAYABLE/test-report.json')
$ErrorActionPreference = 'Stop'
$startupRoot = Split-Path -Parent $PSScriptRoot
if (-not $DotnetPath) {
    $startupBundledSdk = Join-Path ([Environment]::GetFolderPath('UserProfile')) '.codex/tooling/startup-life/dotnet/dotnet.exe'
    if (Test-Path -LiteralPath $startupBundledSdk) { $DotnetPath = $startupBundledSdk }
    else { $DotnetPath = (Get-Command dotnet -ErrorAction Stop).Source }
}
if (-not [IO.Path]::IsPathRooted($ReportPath)) { $ReportPath = Join-Path $startupRoot $ReportPath }
& $DotnetPath build "$startupRoot/tools/SimulationChecks/SimulationChecks.csproj" --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Provisional .NET build failed.' }
& $DotnetPath run --project "$startupRoot/tools/SimulationChecks/SimulationChecks.csproj" --configuration Release --no-build -- $ReportPath
if ($LASTEXITCODE -ne 0) { throw 'Provisional simulation checks failed.' }
Write-Output 'This report verifies .NET behavior only. Unity EditMode, PlayMode, IL2CPP, and devices remain separate gates.'
