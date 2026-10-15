[CmdletBinding()]
param(
    [ValidateSet('EditMode', 'PlayMode', 'All')]
    [string]$Mode = 'All',
    [string]$OutputDirectory,
    [string]$UnityCliPath,
    [int]$TimeoutSeconds = 900,
    [switch]$AllowEditorInstall,
    [switch]$EnableGraphics
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

    # Never count files from an earlier CI attempt as current Editor proof.
    foreach ($stalePath in @($nunitPath, $junitPath, $logPath)) {
        if (Test-Path -LiteralPath $stalePath -PathType Leaf) {
            Remove-Item -LiteralPath $stalePath -Force
        }
    }

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
    # Responsive PlayMode assertions use Screen.SetResolution + real screenshots.
    # Keep EditMode headless; remove -nographics ONLY on explicitly graphical
    # PlayMode runs. Missing GPU/display must fail the tests, never fake PASS.
    $graphicalPlayMode = ($testMode -eq 'PlayMode' -and $EnableGraphics.IsPresent)
    if (-not $graphicalPlayMode) {
        $arguments.Add('--')
        $arguments.Add('-nographics')
    }

    Write-Output "Running Unity $testMode tests with Editor $editorVersion..."
    & $cli @arguments 2>&1 | Tee-Object -FilePath $logPath
    $exitCode = $LASTEXITCODE

    $nunitExists = Test-Path -LiteralPath $nunitPath -PathType Leaf
    $junitExists = Test-Path -LiteralPath $junitPath -PathType Leaf
    $result = [ordered]@{
        mode = $testMode
        graphicalPlayModeRequested = $graphicalPlayMode
        exitCode = $exitCode
        nunitReport = $nunitPath
        nunitExists = $nunitExists
        junitReport = $junitPath
        junitExists = $junitExists
        log = $logPath
    }
    if ($exitCode -eq 0 -and $nunitExists -and $junitExists) {
        try {
            $verdict = & (Join-Path $PSScriptRoot 'Read-UnityTestVerdict.ps1') `
                -NUnitPath $nunitPath -JUnitPath $junitPath
            $result.verdict = $verdict
        }
        catch {
            $result.reportError = $_.Exception.Message
            $failure = "$testMode report validation failed: $($_.Exception.Message) See $logPath"
        }
    }
    $results.Add($result)

    if ($null -ne $failure) { break }
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
    suite = 'Startup Life Unity test gate'
    editorVersion = $editorVersion
    unityCli = Split-Path -Leaf $cli
    requestedMode = $Mode
    enableGraphics = $EnableGraphics.IsPresent
    success = ($null -eq $failure)
    results = $results
    note = 'Native screenshot evidence requires EnableGraphics and verification of captured PNG files. Unity tests do not prove physical-device behavior.'
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath -Encoding utf8

Write-Output "Summary: $summaryPath"
if ($failure) { throw $failure }
Write-Output "Unity headless gate passed for: $($modes -join ', ')."
