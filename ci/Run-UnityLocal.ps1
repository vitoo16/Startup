$ErrorActionPreference = 'Stop'
$reportDir = Join-Path $env:RUNNER_TEMP 'startup-life/unity-tests'
& ./scripts/Test-UnityHeadless.ps1 -Mode All -EnableGraphics -PrepareFirstPlayableScene -OutputDirectory $reportDir
if ($LASTEXITCODE -ne 0) { throw 'Unity native test failed' }
