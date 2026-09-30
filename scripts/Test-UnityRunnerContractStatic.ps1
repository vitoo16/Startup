[CmdletBinding()]
param(
    [string]$ReportPath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$checks = [System.Collections.Generic.List[object]]::new()
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Check {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][bool]$Passed,
        [Parameter(Mandatory)][string]$Detail
    )
    $checks.Add([ordered]@{ name = $Name; passed = $Passed; detail = $Detail })
    if (-not $Passed) { $failures.Add("$Name`: $Detail") }
}

function Read-RepoText {
    param([Parameter(Mandatory)][string]$RelativePath)
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required file is missing: $RelativePath" }
    [IO.File]::ReadAllText($path)
}

$required = @(
    'scripts/Test-UnityHeadless.ps1',
    'scripts/Invoke-UnityBuild.ps1',
    '.github/workflows/unity-runtime-smoke.yml',
    'docs/ci/UNITY_RUNNER_CONTRACT.md'
)
$missing = @($required | Where-Object { -not (Test-Path -LiteralPath (Join-Path $repositoryRoot $_) -PathType Leaf) })
Add-Check 'runner contract files exist' ($missing.Count -eq 0) ($(if ($missing.Count -eq 0) { "$($required.Count) files present" } else { "missing: $($missing -join ', ')" }))

foreach ($relativePath in @('scripts/Test-UnityHeadless.ps1', 'scripts/Invoke-UnityBuild.ps1')) {
    $tokens = $null
    $parseErrors = $null
    [Management.Automation.Language.Parser]::ParseFile((Join-Path $repositoryRoot $relativePath), [ref]$tokens, [ref]$parseErrors) | Out-Null
    $messages = @($parseErrors | ForEach-Object Message)
    Add-Check "PowerShell syntax: $relativePath" ($messages.Count -eq 0) ($(if ($messages.Count -eq 0) { 'clean' } else { $messages -join '; ' }))

    $text = Read-RepoText $relativePath
    Add-Check "no machine-specific Windows path: $relativePath" (-not ($text -match '(?i)[A-Z]:[/\\]Users[/\\]')) 'runner scripts must resolve tools through PATH/UNITY_CLI_PATH, not a developer machine path'
    Add-Check "pins Unity 6000.3.25f1: $relativePath" ($text -match "expectedEditorVersion\s*=\s*'6000\.3\.25f1'") 'expected explicit project Editor guard'
}

$testScript = Read-RepoText 'scripts/Test-UnityHeadless.ps1'
Add-Check 'headless tests use official unity test command' ($testScript -match "'test',\s*\$repositoryRoot") 'expected Unity CLI test command'
Add-Check 'headless tests require both EditMode and PlayMode in All mode' ($testScript -match "@\('EditMode', 'PlayMode'\)") 'All mode must cover EditMode and PlayMode'
Add-Check 'headless tests retain NUnit and JUnit' (($testScript -match "'nunit,junit'") -and ($testScript -match "--junit-output")) 'both report formats must be retained'
Add-Check 'headless tests distinguish test failure exit code' ($testScript -match "8 \{ 'Unity tests completed with one or more test failures\.' \}") 'exit 8 must remain an explicit test-failure result'
Add-Check 'headless tests distinguish infrastructure exit code' ($testScript -match "6 \{ 'Unity did not produce a valid test verdict") 'exit 6 must remain an explicit infrastructure/no-verdict result'

$buildScript = Read-RepoText 'scripts/Invoke-UnityBuild.ps1'
Add-Check 'build wrapper uses Unity 6 Build Profile' ($buildScript -match "'--profile',\s*\$Profile") 'mobile build wrapper must not guess a non-desktop target build path'
Add-Check 'build wrapper requires provenance' (($buildScript -match "'--provenance-path'") -and ($buildScript -match 'provenanceExists')) 'successful build must retain provenance evidence'
Add-Check 'build wrapper documents iOS Xcode export boundary' ($buildScript -match 'Xcode project folder') 'iOS Unity export must not be described as signed IPA/TestFlight evidence'

$workflow = Read-RepoText '.github/workflows/unity-runtime-smoke.yml'
Add-Check 'Unity runtime workflow is manual only' (($workflow -match '(?m)^\s*workflow_dispatch:\s*$') -and -not ($workflow -match '(?m)^\s*(push|pull_request|schedule):\s*$')) 'runtime workflow must not auto-queue unavailable self-hosted runners'
Add-Check 'runtime build defaults off' ($workflow -match '(?ms)run_build:.*?default:\s*false') 'run_build must default false until profiles/modules are provisioned'
foreach ($label in @('self-hosted', 'startup-life', 'windows', 'android', 'macos', 'ios', 'unity-6000-3')) {
    Add-Check "runner label present: $label" ($workflow -match [regex]::Escape($label)) "expected runner label $label"
}
Add-Check 'runtime workflow executes real Unity tests' ($workflow -match 'Test-UnityHeadless\.ps1 -Mode All') 'both platform routes must invoke the headless test gate'
Add-Check 'runtime workflow builds only through wrapper' ($workflow -match 'Invoke-UnityBuild\.ps1') 'profile build must use the reviewed wrapper'
Add-Check 'runtime workflow retains evidence on failure' (($workflow -match 'if:\s*always\(\)') -and ($workflow -match 'upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a')) 'artifact step must always run and remain action-SHA pinned'

$contract = Read-RepoText 'docs/ci/UNITY_RUNNER_CONTRACT.md'
Add-Check 'runner contract names iOS Mac blocker' ($contract -match 'user has no Mac host') 'current external iOS blocker must remain explicit'
Add-Check 'runner contract does not claim provisioned state' ($contract -match 'Status: \*\*prepared, not provisioned\*\*') 'contract must not imply runners exist'
Add-Check 'runner contract requires Build Profile before build' ($contract -match 'does not currently contain an accepted Android/iOS Build Profile') 'missing mobile profiles must remain an explicit gate'

$tracked = @(& git -C $repositoryRoot ls-files 'Assets/Settings/Build Profiles/**' 'Assets/**/*.buildprofile' 'Assets/**/*.BuildProfile')
if ($LASTEXITCODE -ne 0) { throw 'git ls-files build-profile probe failed' }
$report = [ordered]@{
    suite = 'Startup Life Unity runner contract static verification'
    passed = @($checks | Where-Object passed).Count
    failed = $failures.Count
    trackedBuildProfileCandidates = $tracked
    runtimeReady = $false
    note = 'Static PASS means the runner/test/build contract is internally consistent. It does not prove a licensed runner, Unity test result, mobile build, or device.'
    checks = $checks
}

if ($ReportPath) {
    $resolved = [IO.Path]::GetFullPath($ReportPath)
    $parent = Split-Path -Parent $resolved
    if ($parent) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
    $report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $resolved -Encoding utf8
    Write-Output "Report: $resolved"
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Error $failure }
    throw "Unity runner contract static gate failed: $($failures.Count) check(s)."
}

Write-Output "Unity runner contract static gate: $($report.passed)/$($checks.Count) checks passed. Runtime/platform evidence remains pending."
