param(
    [switch]$RequireEditor,
    [string]$ReportPath = 'docs/evidence/M0-T02/tooling-report.json'
)
$ErrorActionPreference = 'Stop'
$startupRoot = Split-Path -Parent $PSScriptRoot
$startupChecks = [System.Collections.Generic.List[object]]::new()
function Add-Check([string]$Name, [bool]$Passed, [string]$Detail) {
    $startupChecks.Add([pscustomobject]@{ name = $Name; passed = $Passed; detail = $Detail })
}
function Find-Tool([string]$Name, [string[]]$Candidates) {
    foreach ($candidate in $Candidates) { if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate } }
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    return $null
}
function Read-JsonCommand([string]$Executable, [string[]]$Arguments) {
    $output = & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Tool command failed: $($Arguments -join ' ')" }
    $parsed = ($output -join "`n") | ConvertFrom-Json
    if (-not $parsed.success) { throw 'Tool returned an error envelope.' }
    return $parsed
}
$startupProfile = [Environment]::GetFolderPath('UserProfile')
$startupCli = Find-Tool 'unity' @("$startupProfile/AppData/Local/Unity/bin/unity.exe")
$startupPython = Find-Tool 'python' @("$startupProfile/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe")
$startupUv = Find-Tool 'uv' @("$startupProfile/.local/bin/uv.exe")
$startupDotnet = Find-Tool 'dotnet' @("$startupProfile/.codex/tooling/startup-life/dotnet/dotnet.exe")
$startupTools = @(
    @{ name = 'Unity CLI'; path = $startupCli; args = @('--version') },
    @{ name = 'Python'; path = $startupPython; args = @('--version') },
    @{ name = 'uv'; path = $startupUv; args = @('--version') },
    @{ name = '.NET SDK'; path = $startupDotnet; args = @('--version') },
    @{ name = 'Git'; path = (Find-Tool 'git' @()); args = @('--version') },
    @{ name = 'Git LFS'; path = (Find-Tool 'git' @()); args = @('lfs', 'version') }
)
foreach ($tool in $startupTools) {
    if (-not $tool.path) { Add-Check $tool.name $false 'Missing'; continue }
    $output = & $tool.path @($tool.args) 2>&1
    Add-Check $tool.name ($LASTEXITCODE -eq 0) (($output -join ' ').Trim())
}
$startupLock = Get-Content -LiteralPath "$startupRoot/docs/tooling/skills.lock.json" -Raw | ConvertFrom-Json
foreach ($skill in $startupLock.skills) {
    $exists = Test-Path -LiteralPath $skill.installedPath -PathType Leaf
    $matches = $exists -and ((Get-FileHash -LiteralPath $skill.installedPath -Algorithm SHA256).Hash.ToLowerInvariant() -eq $skill.sha256)
    Add-Check "Skill $($skill.name)" $matches $(if ($matches) { 'Installed bytes match lock' } else { 'Missing or changed; review required' })
}
$startupEditors = @()
$startupLicenseActive = $false
if ($startupCli) {
    $startupEditors = @((Read-JsonCommand $startupCli @('editors', '--installed', '--format', 'json')).data)
    $startupLicenseActive = [bool](Read-JsonCommand $startupCli @('license', 'status', '--format', 'json')).data.active
}
$startupPinnedEditor = @($startupEditors | Where-Object version -eq '6000.3.25f1')
$startupEditorReady = $startupPinnedEditor.Count -eq 1 -and (Test-Path -LiteralPath $startupPinnedEditor[0].location -PathType Leaf)
if ($RequireEditor) {
    Add-Check 'Pinned Unity 6.3 Editor' $startupEditorReady 'Requires installed 6000.3.25f1; other releases do not satisfy this gate'
    Add-Check 'Unity license' $startupLicenseActive 'Only active status retained; account and machine details excluded'
}
$startupFailed = @($startupChecks | Where-Object { -not $_.passed })
$startupReport = [pscustomobject]@{
    reportVersion = 1
    date = '2026-09-30'
    basicToolingPassed = ($startupFailed.Count -eq 0)
    requireEditor = [bool]$RequireEditor
    selectedEditor = '6000.3.25f1'
    pinnedEditorInstalled = $startupEditorReady
    licenseActive = $startupLicenseActive
    installedEditorVersions = @($startupEditors | ForEach-Object version)
    unity63CompatibilityVerified = $false
    unityCompileVerified = $false
    macAvailable = $false
    milestonePassed = $false
    checks = $startupChecks.ToArray()
}
if (-not [IO.Path]::IsPathRooted($ReportPath)) { $ReportPath = Join-Path $startupRoot $ReportPath }
New-Item -ItemType Directory -Path (Split-Path -Parent $ReportPath) -Force | Out-Null
$startupReport | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ReportPath -Encoding utf8
Write-Output "Tool checks: $($startupChecks.Count - $startupFailed.Count)/$($startupChecks.Count). Pinned Editor installed: $startupEditorReady. License active: $startupLicenseActive."
Write-Output 'Unity 6.3 compatibility, project compilation, and mobile gates remain unverified.'
if ($startupFailed.Count -gt 0) { $startupFailed | Format-Table -AutoSize; exit 1 }
