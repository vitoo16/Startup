[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Profile,
    [Parameter(Mandatory)][string]$OutputPath,
    [string]$UnityCliPath,
    [int]$TimeoutSeconds = 1800,
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

$versionText = [IO.File]::ReadAllText((Join-Path $repositoryRoot 'ProjectSettings/ProjectVersion.txt'))
$versionMatch = [regex]::Match($versionText, '(?m)^m_EditorVersion:\s*(?<version>\S+)\s*$')
if (-not $versionMatch.Success) { throw 'Could not parse m_EditorVersion.' }
$editorVersion = $versionMatch.Groups['version'].Value
if ($editorVersion -ne $expectedEditorVersion) {
    throw "Pinned Editor changed unexpectedly. Expected $expectedEditorVersion; repository declares $editorVersion."
}

$cli = Resolve-UnityCli $UnityCliPath
$resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
$outputParent = Split-Path -Parent $resolvedOutput
if ($outputParent) { New-Item -ItemType Directory -Force -Path $outputParent | Out-Null }
$evidenceDirectory = Join-Path ([IO.Path]::GetTempPath()) 'StartupLife/unity-build'
New-Item -ItemType Directory -Force -Path $evidenceDirectory | Out-Null
$provenancePath = Join-Path $evidenceDirectory 'build-provenance.json'
$logPath = Join-Path $evidenceDirectory 'unity-build.log'

$arguments = [System.Collections.Generic.List[string]]::new()
foreach ($value in @(
    'build', $repositoryRoot,
    '--profile', $Profile,
    '--output-path', $resolvedOutput,
    '--editor-version', $editorVersion,
    '--timeout', $TimeoutSeconds.ToString([Globalization.CultureInfo]::InvariantCulture),
    '--provenance-path', $provenancePath,
    '--format', 'github'
)) { $arguments.Add([string]$value) }
if ($AllowEditorInstall) { $arguments.Add('--allow-install') }

Write-Output "Building Unity profile '$Profile' with Editor $editorVersion..."
& $cli @arguments 2>&1 | Tee-Object -FilePath $logPath
$exitCode = $LASTEXITCODE
$outputExists = Test-Path -LiteralPath $resolvedOutput
$provenanceExists = Test-Path -LiteralPath $provenancePath -PathType Leaf

$summaryPath = Join-Path $evidenceDirectory 'unity-build-summary.json'
[ordered]@{
    suite = 'Startup Life Unity build profile'
    editorVersion = $editorVersion
    profile = $Profile
    outputPath = $resolvedOutput
    exitCode = $exitCode
    outputExists = $outputExists
    provenance = $provenancePath
    provenanceExists = $provenanceExists
    log = $logPath
    success = ($exitCode -eq 0 -and $outputExists -and $provenanceExists)
    note = 'For iOS, Unity output is an Xcode project folder. Signing, archive, IPA, TestFlight and physical-device QA remain separate macOS/device gates.'
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $summaryPath -Encoding utf8

Write-Output "Summary: $summaryPath"
if ($exitCode -ne 0) { throw "Unity build failed with exit code $exitCode. See $logPath" }
if (-not $outputExists) { throw "Unity build exited successfully but output is missing: $resolvedOutput" }
if (-not $provenanceExists) { throw "Unity build output exists but provenance was not retained: $provenancePath" }
Write-Output "Unity build profile completed: $resolvedOutput"
