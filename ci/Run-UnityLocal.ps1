$ErrorActionPreference = 'Stop'
$root = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [IO.Path]::GetTempPath() }
$reportDir = Join-Path $root 'startup-life/unity-tests'
$creatorDir = Join-Path $reportDir 'creator-responsive'
$mainMenuDir = Join-Path $reportDir 'main-menu-responsive'

# Never let screenshots from an earlier local run certify this checkout.
foreach ($dir in @($creatorDir, $mainMenuDir)) {
    if (Test-Path -LiteralPath $dir) { Remove-Item -LiteralPath $dir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}

$previousCreator = $env:STARTUP_LIFE_CREATOR_RESPONSIVE_EVIDENCE
$previousMenu = $env:STARTUP_LIFE_MAIN_MENU_RESPONSIVE_EVIDENCE
try {
    $env:STARTUP_LIFE_CREATOR_RESPONSIVE_EVIDENCE = $creatorDir
    $env:STARTUP_LIFE_MAIN_MENU_RESPONSIVE_EVIDENCE = $mainMenuDir

    & ./scripts/Test-UnityHeadless.ps1 -Mode All -EnableGraphics -PrepareFirstPlayableScene -OutputDirectory $reportDir
    if ($LASTEXITCODE -ne 0) { throw 'Unity native test failed' }

    # Both matrices must run in a graphics-capable Editor/Player, not a fake
    # headless resolution. Check all current-run PNG dimensions and hashes.
    & ./scripts/Verify-CreatorResponsiveEvidence.ps1 -Directory $creatorDir -ReportPath (Join-Path $reportDir 'creator-responsive-report.json')
    & ./scripts/Verify-MainMenuResponsiveEvidence.ps1 -Directory $mainMenuDir -ReportPath (Join-Path $reportDir 'main-menu-responsive-report.json')
}
finally {
    if ($null -eq $previousCreator) {
        Remove-Item Env:STARTUP_LIFE_CREATOR_RESPONSIVE_EVIDENCE -ErrorAction SilentlyContinue
    } else {
        $env:STARTUP_LIFE_CREATOR_RESPONSIVE_EVIDENCE = $previousCreator
    }
    if ($null -eq $previousMenu) {
        Remove-Item Env:STARTUP_LIFE_MAIN_MENU_RESPONSIVE_EVIDENCE -ErrorAction SilentlyContinue
    } else {
        $env:STARTUP_LIFE_MAIN_MENU_RESPONSIVE_EVIDENCE = $previousMenu
    }
}
