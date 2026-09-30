param([switch]$Install)
$ErrorActionPreference = 'Stop'
$startupProfile = [Environment]::GetFolderPath('UserProfile')
$startupCli = Join-Path $startupProfile 'AppData/Local/Unity/bin/unity.exe'
if (-not (Test-Path -LiteralPath $startupCli)) { throw 'Unity CLI missing. Use the reviewed official CLI installer first.' }
if (Get-Process 'UnitySetup64*' -ErrorAction SilentlyContinue) { throw 'An Editor installer is running. Finish it before another attempt.' }
$startupArguments = @('install', '6000.3.25f1', '--module', 'android', '--module', 'ios', '--yes', '--accept-eula', '--non-interactive', '--format', 'ndjson')
if (-not $Install) { $startupArguments += '--dry-run' }
& $startupCli @startupArguments
if ($LASTEXITCODE -ne 0) { throw 'Unity install failed. Inspect its error code; an elevation failure requires the Windows administrator installation flow.' }
Write-Output 'Installation output is not compile/build proof. Run Test-Tooling.ps1 -RequireEditor, then import, test, and verify platform modules.'
