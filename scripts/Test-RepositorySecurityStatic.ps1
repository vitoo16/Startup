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

$gitignore = Read-RepoText '.gitignore'
$requiredIgnoreFragments = @(
    'secrets.json',
    '*.secrets.json',
    '*.private.*',
    '.credentials/',
    '.env',
    '*.p8',
    '*.p12',
    '*.mobileprovision',
    'GoogleService-Info.plist'
)
foreach ($fragment in $requiredIgnoreFragments) {
    Add-Check "gitignore protects $fragment" ($gitignore.Contains($fragment)) "missing ignore fragment: $fragment"
}

$attributes = Read-RepoText '.gitattributes'
foreach ($pattern in @('*.png', '*.psd', '*.psb', '*.fbx', '*.wav', '*.ttf', '*.otf', '*.unitypackage')) {
    $escaped = [regex]::Escape($pattern)
    Add-Check "LFS rule exists: $pattern" ($attributes -match "(?m)^$escaped\s+filter=lfs\b") "expected Git LFS rule for $pattern"
}

$tracked = @(& git -C $repositoryRoot ls-files)
if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed' }
$sensitiveTracked = [System.Collections.Generic.List[string]]::new()
foreach ($path in $tracked) {
    $normalized = $path.Replace('\\', '/').ToLowerInvariant()
    $leaf = [IO.Path]::GetFileName($normalized)
    $isExample = $normalized.Contains('.example') -or $normalized.Contains('/examples/')
    $sensitive =
        $leaf -eq '.env' -or
        ($leaf.StartsWith('.env.') -and $leaf -ne '.env.example') -or
        $leaf -eq 'secrets.json' -or
        $leaf.EndsWith('.secrets.json') -or
        $leaf.EndsWith('.private.json') -or
        $leaf.EndsWith('.p8') -or
        $leaf.EndsWith('.p12') -or
        $leaf.EndsWith('.mobileprovision') -or
        $leaf -eq 'googleservice-info.plist'
    if ($sensitive -and -not $isExample) { $sensitiveTracked.Add($path) }
}
Add-Check 'no sensitive credential filenames are tracked' ($sensitiveTracked.Count -eq 0) ($(if ($sensitiveTracked.Count -eq 0) { 'clean' } else { $sensitiveTracked -join ', ' }))

$privateKeyPattern = '-----BEGIN ' + '(RSA |EC |OPENSSH )?PRIVATE KEY-----'
$keyHits = @(& git -C $repositoryRoot grep -I -n -E -- $privateKeyPattern)
$grepExit = $LASTEXITCODE
if ($grepExit -gt 1) { throw "git grep private-key scan failed with exit code $grepExit" }
Add-Check 'no private-key PEM blocks in tracked text' ($keyHits.Count -eq 0) ($(if ($keyHits.Count -eq 0) { 'clean' } else { $keyHits -join '; ' }))

$workflowDirectory = Join-Path $repositoryRoot '.github/workflows'
$workflowFiles = @(Get-ChildItem -LiteralPath $workflowDirectory -File | Where-Object { $_.Extension -in @('.yml', '.yaml') })
Add-Check 'GitHub workflows discovered for security scan' ($workflowFiles.Count -ge 2) "discovered $($workflowFiles.Count) workflow file(s)"

$unpinnedActions = [System.Collections.Generic.List[string]]::new()
$privilegedTriggers = [System.Collections.Generic.List[string]]::new()
$writePermissions = [System.Collections.Generic.List[string]]::new()
foreach ($workflowFile in $workflowFiles) {
    $text = [IO.File]::ReadAllText($workflowFile.FullName)
    if ($text -match '(?m)^\s*pull_request_target:\s*$') { $privilegedTriggers.Add($workflowFile.Name) }
    foreach ($line in ($text -split "`r?`n")) {
        $useMatch = [regex]::Match($line, '^\s*uses:\s*(?<action>actions/[^@\s]+)@(?<ref>[^\s#]+)')
        if ($useMatch.Success -and $useMatch.Groups['ref'].Value -notmatch '^[0-9a-fA-F]{40}$') {
            $unpinnedActions.Add("$($workflowFile.Name): $($useMatch.Groups['action'].Value)@$($useMatch.Groups['ref'].Value)")
        }
        if ($line -match '^\s*[A-Za-z0-9_-]+:\s*write\s*(#.*)?$') {
            $writePermissions.Add("$($workflowFile.Name): $($line.Trim())")
        }
    }
    if ($text -notmatch '(?ms)^permissions:\s*\n\s*contents:\s*read\s*$') {
        $writePermissions.Add("$($workflowFile.Name): missing top-level contents: read baseline")
    }
}
Add-Check 'first-party GitHub actions are commit-SHA pinned' ($unpinnedActions.Count -eq 0) ($(if ($unpinnedActions.Count -eq 0) { "$($workflowFiles.Count) workflow files checked" } else { $unpinnedActions -join '; ' }))
Add-Check 'no pull_request_target workflow' ($privilegedTriggers.Count -eq 0) ($(if ($privilegedTriggers.Count -eq 0) { 'clean' } else { $privilegedTriggers -join ', ' }))
Add-Check 'workflow permissions remain read-only' ($writePermissions.Count -eq 0) ($(if ($writePermissions.Count -eq 0) { 'contents: read only' } else { $writePermissions -join '; ' }))

$releaseContract = Read-RepoText 'docs/ci/RELEASE_SECURITY_VERSIONING.md'
Add-Check 'release contract keeps signing secrets out of Git' ($releaseContract.Contains('Secrets never enter Git')) 'missing secret-storage policy'
Add-Check 'release contract separates build from device QA' ($releaseContract.Contains('Unity mobile build/export') -and $releaseContract.Contains('physical-device')) 'artifact/device evidence boundary must remain explicit'
Add-Check 'release contract marks build-number injection pending' ($releaseContract.Contains('generic Build Profile wrapper intentionally does not mutate Android/iOS build numbers yet')) 'current release-number limitation must remain explicit'

$projectSettings = Read-RepoText 'ProjectSettings/ProjectSettings.asset'
$bundleVersionMatch = [regex]::Match($projectSettings, '(?m)^\s*bundleVersion:\s*(?<value>\S+)\s*$')
Add-Check 'Unity marketing version is present' ($bundleVersionMatch.Success) ($(if ($bundleVersionMatch.Success) { "current: $($bundleVersionMatch.Groups['value'].Value)" } else { 'bundleVersion missing' }))

$buildWrapper = Read-RepoText 'scripts/Invoke-UnityBuild.ps1'
Add-Check 'build wrapper does not bypass dirty-tree guard' (-not $buildWrapper.Contains('--allow-dirty-build')) 'release/build provenance must reject accidental dirty builds by default'

$report = [ordered]@{
    suite = 'Startup Life repository security and release hygiene'
    passed = @($checks | Where-Object passed).Count
    failed = $failures.Count
    workflowCount = $workflowFiles.Count
    trackedFileCount = $tracked.Count
    checks = $checks
    note = 'This is a high-signal repository policy scan, not a substitute for dedicated secret scanning, signing review, or store release QA.'
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
    throw "Repository security/release hygiene gate failed: $($failures.Count) check(s)."
}

Write-Output "Repository security/release hygiene: $($report.passed)/$($checks.Count) checks passed."
