[CmdletBinding()]
param([string]$ReportPath)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$checks = [System.Collections.Generic.List[object]]::new()
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Check {
    param([string]$Name, [bool]$Passed, [string]$Detail)
    $checks.Add([ordered]@{ name = $Name; passed = $Passed; detail = $Detail })
    if (-not $Passed) { $failures.Add("$Name - $Detail") }
}
function Read-Text([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required file missing: $RelativePath" }
    return [IO.File]::ReadAllText($path)
}

$presentationAsm = (Read-Text 'Assets/StartupLife/Scripts/Presentation/StartupLife.Presentation.asmdef') | ConvertFrom-Json
$editorAsm = (Read-Text 'Assets/StartupLife/Scripts/Editor/StartupLife.Editor.asmdef') | ConvertFrom-Json
$coordinator = Read-Text 'Assets/StartupLife/Scripts/Presentation/FirstPlayableFlow.cs'
$bootstrap = Read-Text 'Assets/StartupLife/Scripts/Presentation/StartupLifeBootstrapper.cs'
$businessView = Read-Text 'Assets/StartupLife/Scripts/Presentation/LifeScreenViewController.cs'
$businessOverlay = Read-Text 'Assets/StartupLife/Scripts/Presentation/BusinessManagementOverlay.cs'
$work = Read-Text 'Assets/StartupLife/Scripts/Presentation/WorkShiftPlaybackController.cs'
$lifecycle = Read-Text 'Assets/StartupLife/Scripts/Presentation/FirstPlayableLifecycle.cs'
$builder = Read-Text 'Assets/StartupLife/Scripts/Editor/FirstPlayableShellBuilder.cs'
$playMode = Read-Text 'Assets/StartupLife/Tests/PlayMode/FirstPlayableFlowTests.cs'
$lifecyclePlayMode = Read-Text 'Assets/StartupLife/Tests/PlayMode/FirstPlayableLifecycleTests.cs'

Add-Check 'Presentation references Content' ('StartupLife.Content' -in @($presentationAsm.references)) ($presentationAsm.references -join ', ')
Add-Check 'Presentation references uGUI' ('Unity.ugui' -in @($presentationAsm.references)) ($presentationAsm.references -join ', ')
Add-Check 'Presentation references Localization' ('Unity.Localization' -in @($presentationAsm.references)) ($presentationAsm.references -join ', ')
Add-Check 'Editor references Input System' ('Unity.InputSystem' -in @($editorAsm.references)) ($editorAsm.references -join ', ')

$forbiddenCoordinator = @('GameState', 'CommandReceipt', 'ExportCheckpoint', 'ISaveStore', 'JsonSaveSerializer', 'AtomicFileSaveStore')
$coordinatorLeaks = @($forbiddenCoordinator | Where-Object { $coordinator.Contains($_, [StringComparison]::Ordinal) })
Add-Check 'Canonical flow uses only public command/read contracts' ($coordinatorLeaks.Count -eq 0) ($(if ($coordinatorLeaks.Count -eq 0) { 'clean' } else { $coordinatorLeaks -join ', ' }))
Add-Check 'Bootstrap binds the canonical flow' ($bootstrap -match 'new\s+FirstPlayableFlow\(session,\s*new\s+GuidCommandIdSource\(\)\)') 'same seam as engine-free consumers'
Add-Check 'Canonical command ids fail closed' ($coordinator -match 'IsNullOrWhiteSpace\(commandId\)') 'invalid injected ids must not dispatch'
Add-Check 'Duplicate command seam is removed' (-not (Test-Path (Join-Path $repositoryRoot 'Assets/StartupLife/Scripts/Presentation/FirstPlayableFlowCoordinator.cs'))) 'one authoritative FirstPlayableFlow'
Add-Check 'Duplicate presentation harness is removed' (-not (Test-Path (Join-Path $repositoryRoot 'tools/PresentationFlowChecks/PresentationFlowChecks.csproj'))) 'unique assertions merged into PresentationChecks'

Add-Check 'Bootstrap requires authored Content asset' ($bootstrap -match 'StartupLifeContentCatalogAsset\s+contentAsset') 'content asset must be serialized into composition root'
Add-Check 'Bootstrap builds approved save pipeline' (
    ($bootstrap -match 'new\s+JsonSaveSerializer') -or
    (($bootstrap -match 'new\s+V3SaveCompatibilitySerializer') -and
     ($bootstrap -match 'new\s+V2ToV3Migration') -and
     ($bootstrap -match 'new\s+StagedV3SaveSerializer') -and
     ($bootstrap -match 'new\s+VersionedReceiptReplay'))
) 'expected legacy v2 or complete replay-checked versioned v3 serializer'
Add-Check 'Bootstrap uses atomic local store' ($bootstrap -match 'new\s+AtomicFileSaveStore') 'expected AtomicFileSaveStore'
Add-Check 'Bootstrap restores before creating a new run' ($bootstrap -match 'GameSession\.TryRestore') 'expected TryRestore'
Add-Check 'Bootstrap does not hardcode first playable catalog' (-not ($bootstrap -match 'FirstPlayableContentTemplate|new\s+ContentCatalog')) 'Presentation must consume Content asset'

$businessCommandNames = @('LaunchBusiness','PauseBusiness','ResumeBusiness',
    'SetBusinessPricing','ReinvestBusiness','CloseBusiness')
$missingBusinessUnwraps = @($businessCommandNames | Where-Object {
    -not ($businessView -match ('flow\.' + $_ + '\s*\([^;]*?\)\.Command'))
})
Add-Check 'M9 business Unity actions unwrap to CommandResult' ($missingBusinessUnwraps.Count -eq 0) ($missingBusinessUnwraps -join ', ')
Add-Check 'M9 Unity scene runtime business panel wiring' (
    ($bootstrap -match 'lifeScreen\.EnsureBusinessManagementUi\(\)') -and
    ($businessView -match 'BusinessManagementOverlay\.Install') -and
    ($businessOverlay -match 'BusinessManagementButton') -and
    ($businessOverlay -match 'BusinessManagementOverlay') -and
    ($businessOverlay -match 'BusinessManagementScroll')) 'Existing LifePanel uses dynamically wired uGUI'
Add-Check 'M9 business finance and controls are visible through overlay' (
    ($businessOverlay -match 'LastSettledFinanceValue') -and
    ($businessOverlay -match 'BusinessPortfolioValue') -and
    ($businessOverlay -match 'AddLaunch\(content\.transform') -and
    ($businessOverlay -match 'PauseBusinessButton') -and
    ($businessOverlay -match 'ResumeBusinessButton') -and
    ($businessOverlay -match 'CloseBusinessButton')) 'Runtime controls dispatch canonical player commands'
Add-Check 'Work playback acknowledges committed activity' ($work -match 'AcknowledgePlayback\(expectedCursor\s*\+\s*1\)') 'playback completion must use reward-independent acknowledgement'
Add-Check 'Work playback does not mutate domain state' (-not ($work -match 'GameState|CommandReceipt|\.Cash\s*=|\.Xp\s*=')) 'view must not mutate domain state'
Add-Check 'Bootstrap owns pause lifecycle callback' ($bootstrap -match 'OnApplicationPause\(bool\s+pauseStatus\)') 'mobile lifecycle must enter through the composition root'
Add-Check 'Bootstrap avoids focus duplicate checkpointing' (-not ($bootstrap -match 'OnApplicationFocus\s*\(')) 'focus churn must not duplicate lifecycle actions'
Add-Check 'Lifecycle reads only public presentation state' (($lifecycle -match 'flow\.Refresh\(\)') -and -not ($lifecycle -match 'GameState|CommandReceipt|ExportCheckpoint|ISaveStore|JsonSaveSerializer|AtomicFileSaveStore')) 'lifecycle must not bypass FirstPlayableFlow'
Add-Check 'Lifecycle pause stops active work coroutine' (($work -match 'SuspendForLifecycle\(\)') -and ($work -match 'StopAllCoroutines\(\)')) 'paused coroutine locals must not drive resumed state'
Add-Check 'Pending work replay requires unacknowledged cursor' ($work -match 'snapshot\.PlaybackCursor\s*!=\s*0') 'only cursor zero may replay a committed work cue'
Add-Check 'Restored work cue is acknowledgement-only' ((([regex]::Matches($work, 'AdvanceBoundary\(\)')).Count -eq 1) -and (([regex]::Matches($work, 'AcknowledgePlayback\(expectedCursor\s*\+\s*1\)')).Count -eq 2)) 'restore path must never recommit AdvanceBoundary'

Add-Check 'Editor builder creates scene through EditorSceneManager' ($builder -match 'EditorSceneManager\.NewScene') 'scene must be generated through Unity'
Add-Check 'Editor builder configures portrait reference canvas' ($builder -match 'referenceResolution\s*=\s*new\s+Vector2\(1080,\s*1920\)') 'expected 1080x1920'
Add-Check 'Editor builder uses MobileSafeArea' ($builder -match 'typeof\(MobileSafeArea\)') 'safe area required'
Add-Check 'Editor builder uses InputSystem UI module' ($builder -match 'InputSystemUIInputModule') 'Input System EventSystem required'
Add-Check 'Editor builder assigns default UI input actions' ($builder -match 'AssignDefaultActions\(\)') 'input module must have actions'
Add-Check 'Editor builder creates localization table' ($builder -match 'CreateStringTableCollection\(LocalizedKeyLabel\.Table') 'FirstPlayableUI localization table required'
Add-Check 'Editor builder seeds Content asset through content builder' ($builder -match 'FirstPlayableContentBuilder\.CreateOrUpdate\(\)') 'content asset must precede scene composition'

Add-Check 'PlayMode first playable journey source exists' ($playMode -match 'CreateWorkStudyAndAdvanceDayThroughViews') 'expected full first playable flow test'
Add-Check 'PlayMode test cleans persistent save' ($playMode -match 'DeleteSave\(\)') 'tests must isolate autosave state'
Add-Check 'PlayMode test drives UI buttons' ($playMode -match '\.onClick\.Invoke\(\)') 'test should exercise view dispatch'
Add-Check 'Lifecycle PlayMode covers fresh launch' ($lifecyclePlayMode -match 'FreshLaunchWithoutSaveShowsCharacterCreation') 'M7-T02 fresh launch test required'
Add-Check 'Lifecycle PlayMode covers pause before acknowledge' ($lifecyclePlayMode -match 'PauseDuringWorkRestoresPendingCueWithoutDuplicateRewards') 'W1/W3 restore test required'
Add-Check 'Lifecycle PlayMode covers pause after acknowledge' ($lifecyclePlayMode -match 'AcknowledgedWorkCueDoesNotReplayAfterRestart') 'W2 restore test required'
Add-Check 'Lifecycle PlayMode covers course restart' ($lifecyclePlayMode -match 'MidCourseRestartRestoresExactProgressAndCompletesOnce') 'mid-course restore test required'
Add-Check 'Lifecycle PlayMode covers next day restart' ($lifecyclePlayMode -match 'NextDayRestartDoesNotRecommitBoundary') 'next-day restore test required'
Add-Check 'Lifecycle PlayMode covers backup recovery' ($lifecyclePlayMode -match 'CorruptPrimaryRecoversBackupAndAllowsNextWrite') 'backup recovery test required'
Add-Check 'Lifecycle PlayMode covers unreadable fail closed' ($lifecyclePlayMode -match 'UnreadablePrimaryFailsClosedWithoutUsingBackup') 'unreadable primary contract required'
Add-Check 'Lifecycle PlayMode covers both invalid' ($lifecyclePlayMode -match 'BothInvalidSavesFailWithoutStartingNewRun') 'fatal save contract required'
Add-Check 'Lifecycle PlayMode covers wall clock rule' ($lifecyclePlayMode -match 'RealElapsedPauseDoesNotAdvanceSimulation') 'wall clock must not advance simulation'

$presentationFiles = Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'Assets/StartupLife/Scripts/Presentation') -Filter '*.cs' -File
$presentationText = ($presentationFiles | ForEach-Object { [IO.File]::ReadAllText($_.FullName) }) -join [Environment]::NewLine
Add-Check 'Presentation source does not construct ContentCatalog' (-not ($presentationText -match 'new\s+ContentCatalog\s*\(')) 'balance/content construction belongs to Content assembly'
Add-Check 'Presentation source does not inspect receipts' (-not ($presentationText -match '\bCommandReceipt\b')) 'views must not inspect receipts'

$report = [ordered]@{
    suite = 'Startup Life M7 first playable shell source contract'
    passed = @($checks | Where-Object passed).Count
    failed = $failures.Count
    checks = $checks
}

if ($ReportPath) {
    $resolved = [IO.Path]::GetFullPath($ReportPath)
    $directory = Split-Path -Parent $resolved
    if ($directory) { New-Item -ItemType Directory -Force -Path $directory | Out-Null }
    $report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $resolved -Encoding utf8
    Write-Output "Report: $resolved"
}
if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Error $failure }
    throw "M7 shell source gate failed: $($failures.Count) check(s)."
}
Write-Output "M7 first playable shell source: $($report.passed)/$($checks.Count) checks passed."
