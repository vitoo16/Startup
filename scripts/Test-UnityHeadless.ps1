[CmdletBinding()]
param(
    [ValidateSet('EditMode', 'PlayMode', 'All')]
    [string]$Mode = 'All',
    [string]$OutputDirectory,
    [string]$UnityCliPath,
    [int]$TimeoutSeconds = 900,
    [switch]$AllowEditorInstall
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$expectedEditorVersion = '6000.3.25f1'

function Resolve-UnityCli {
    param([string]$ExplicitPath)

    if ($ExplicitPath) {
        if (-not (Test-Path -LiteralPath $ExplicitPath -PathType Leaf)) {
            throw "Unity CLI not found at explicit path: $ExplicitPath"
        }
        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    if ($env:UNITY_CLI_PATH) {
        if (-not (Test-Path -LiteralPath $env:UNITY_CLI_PATH -PathType Leaf)) {
            throw "UNITY_CLI_PATH does not point to a file: $($env:UNITY_CLI_PATH)"
        }
        return (Resolve-Path -LiteralPath $env:UNITY_CLI_PATH).Path
    }

    $command = Get-Command unity -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $command) {
        throw 'Unity CLI is unavailable. Install the reviewed Unity CLI or set UNITY_CLI_PATH.'
    }
    return $command.Source
}

function Read-PinnedEditorVersion {
    $versionFile = Join-Path $repositoryRoot 'ProjectSettings/ProjectVersion.txt'
    if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) {
        throw 'ProjectSettings/ProjectVersion.txt is missing.'
    }
    $text = [IO.File]::ReadAllText($versionFile)
    $match = [regex]::Match($text, '(?m)^m_EditorVersion:\s*(?<version>\S+)\s*$')
    if (-not $match.Success) { throw 'Could not parse m_EditorVersion.' }
    return $match.Groups['version'].Value
}

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path ([IO.Path]::GetTempPath()) 'StartupLife/unity-headless'
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$editorVersion = Read-PinnedEditorVersion
if ($editorVersion -ne $expectedEditorVersion) {
    throw "Pinned Editor changed unexpectedly. Expected $expectedEditorVersion; repository declares $editorVersion."
}

$cli = Resolve-UnityCli $UnityCliPath
$modes = if ($Mode -eq 'All') { @('EditMode', 'PlayMode') } else { @($Mode) }
$results = [System.Collections.Generic.List[object]]::new()
$failure = $null

foreach ($testMode in $modes) {
    $slug = $testMode.ToLowerInvariant()
    $nunitPath = Join-Path $OutputDirectory "$slug-results.xml"
    $junitPath = Join-Path $OutputDirectory "$slug-results.junit.xml"
    $logPath = Join-Path $OutputDirectory "$slug-unity-cli.log"

    $arguments = [System.Collections.Generic.List[string]]::new()
    foreach ($value in @(
        'test', $repositoryRoot,
        '--mode', $testMode,
        '--filter', 'StartupLife',
        '--output', $nunitPath,
        '--report-format', 'nunit,junit',
        '--junit-output', $junitPath,
        '--editor-version', $editorVersion,
        '--timeout', $TimeoutSeconds.ToString([Globalization.CultureInfo]::InvariantCulture),
        '--format', 'github'
    )) { $arguments.Add([string]$value) }
    if ($AllowEditorInstall) { $arguments.Add('--allow-install') }
    $arguments.Add('--')
    $arguments.Add('-nographics')

    Write-Output "Running Unity $testMode tests with Editor $editorVersion..."
    & $cli @arguments 2>&1 | Tee-Object -FilePath $logPath
    $exitCode = $LASTEXITCODE

    $nunitExists = Test-Path -LiteralPath $nunitPath -PathType Leaf
    $junitExists = Test-Path -LiteralPath $junitPath -PathType Leaf
    $result = [ordered]@{
        mode = $testMode
        exitCode = $exitCode
        nunitReport = $nunitPath
        nunitExists = $nunitExists
        junitReport = $junitPath
        junitExists = $junitExists
        log = $logPath
    }
    $results.Add($result)

    if ($exitCode -ne 0 -or -not $nunitExists -or -not $junitExists) {
        $meaning = switch ($exitCode) {
            8 { 'Unity tests completed with one or more test failures.' }
            6 { 'Unity did not produce a valid test verdict (compile/license/editor/infrastructure failure or timeout).' }
            2 { 'Unity CLI invocation was invalid.' }
            default { "Unity CLI exited $exitCode." }
        }
        if (-not $nunitExists -or -not $junitExists) {
            $meaning += ' Required NUnit/JUnit report file is missing.'
        }
        $failure = "$testMode gate failed. $meaning See $logPath"
        break
    }
}

$summaryPath = Join-Path $OutputDirectory 'unity-headless-summary.json'
[ordered]@{
    suite = 'Startup Life Unity headless tests'
    editorVersion = $editorVersion
    unityCli = Split-Path -Leaf $cli
    requestedMode = $Mode
    success = ($null -eq $failure)
    results = $results
    note = 'A successful result is real Unity test evidence. This script does not build mobile players or prove physical-device behavior.'
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath -Encoding utf8

Write-Output "Summary: $summaryPath"
if ($failure) { throw $failure }
Write-Output "Unity headless gate passed for: $($modes -join ', ')."
