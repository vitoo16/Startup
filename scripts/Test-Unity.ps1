param(
    [ValidateSet('editor','playmode')][string]$Mode = 'editor',
    [int]$TimeoutSeconds = 180
)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
$unityCliPath = 'C:/Users/viett/AppData/Local/Unity/bin/unity.exe'
$reportDirectory = Join-Path $projectPath 'docs/evidence/M0-T03'
if (!(Test-Path -LiteralPath $unityCliPath)) { throw 'Pinned local Unity CLI not found.' }
$status = (& $unityCliPath status --format json | ConvertFrom-Json)
$instance = $status.data.instances | Where-Object { $_.project.TrimEnd('\','/') -eq $projectPath.TrimEnd('\','/') }
if (!$instance -or $instance.version -ne '6000.3.25f1' -or $instance.state -ne 'ready') {
    throw 'Open the pinned project Editor and wait for compilation before testing.'
}
$startResult = (& $unityCliPath command run_tests --mode $Mode --filter 'StartupLife' --async_tests true --project-path $projectPath --format json | ConvertFrom-Json)
if (!$startResult.success) { throw ($startResult.errors | ConvertTo-Json -Compress) }
$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
do {
    Start-Sleep -Seconds 2
    $report = (& $unityCliPath command test_status --project-path $projectPath --format json | ConvertFrom-Json)
    if (!$report.success) { throw ($report.errors | ConvertTo-Json -Compress) }
    if ([DateTime]::UtcNow -gt $deadline) { throw 'Unity test timeout; inspect Editor and retain logs.' }
} while ($report.data.result.status -eq 'running')
New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null
$report | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $reportDirectory "$Mode-tests.json") -Encoding utf8
$summary = $report.data.result.summary
if ($report.data.result.status -ne 'completed' -or !$summary -or $summary.total -lt 1 -or $summary.failed -gt 0 -or $summary.inconclusive -gt 0 -or $summary.passed -ne $summary.total) {
    throw "Unity $Mode gate failed or did not execute all tests. See the retained report."
}
Write-Output "Unity $Mode tests: $($summary.passed)/$($summary.total) passed."
