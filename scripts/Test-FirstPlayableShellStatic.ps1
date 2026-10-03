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
$coordinator = Read-Text 'Assets/StartupLife/Scripts/Presentation/FirstPlayableFlowCoordinator.cs'
$bootstrap = Read-Text 'Assets/StartupLife/Scripts/Presentation/StartupLifeBootstrapper.cs'
$work = Read-Text 'Assets/StartupLife/Scripts/Presentation/WorkShiftPlaybackController.cs'
$builder = Read-Text 'Assets/StartupLife/Scripts/Editor/FirstPlayableShellBuilder.cs'
$playMode = Read-Text 'Assets/StartupLife/Tests/PlayMode/FirstPlayableFlowTests.cs'

Add-Check 'Presentation references Content' ('StartupLife.Content' -in @($presentationAsm.references)) ($presentationAsm.references -join ', ')
Add-Check 'Presentation references uGUI' ('Unity.ugui' -in @($presentationAsm.references)) ($presentationAsm.references -join ', ')
Add-Check 'Presentation references Localization' ('Unity.Localization' -in @($presentationAsm.references)) ($presentationAsm.references -join ', ')
Add-Check 'Editor references Input System' ('Unity.InputSystem' -in @($editorAsm.references)) ($editorAsm.references -join ', ')

$forbiddenCoordinator = @('GameState', 'CommandReceipt', 'ExportCheckpoint', 'ISaveStore', 'JsonSaveSerializer', 'AtomicFileSaveStore')
$coordinatorLeaks = @($forbiddenCoordinator | Where-Object { $coordinator.Contains($_, [StringComparison]::Ordinal) })
Add-Check 'Coordinator uses only public command/read contracts' ($coordinatorLeaks.Count -eq 0) ($(if ($coordinatorLeaks.Count -eq 0) { 'clean' } else { $coordinatorLeaks -join ', ' }))

Add-Check 'Bootstrap requires authored Content asset' ($bootstrap -match 'StartupLifeContentCatalogAsset\s+contentAsset') 'content asset must be serialized into composition root'
Add-Check 'Bootstrap builds Json serializer' ($bootstrap -match 'new\s+JsonSaveSerializer') 'expected JsonSaveSerializer'
Add-Check 'Bootstrap uses atomic local store' ($bootstrap -match 'new\s+AtomicFileSaveStore') 'expected AtomicFileSaveStore'
Add-Check 'Bootstrap restores before creating a new run' ($bootstrap -match 'GameSession\.TryRestore') 'expected TryRestore'
Add-Check 'Bootstrap does not hardcode first playable catalog' (-not ($bootstrap -match 'FirstPlayableContentTemplate|new\s+ContentCatalog')) 'Presentation must consume Content asset'

Add-Check 'Work playback acknowledges committed activity' ($work -match 'AcknowledgePlayback\(snapshot\.PlaybackCursor\s*\+\s*1\)') 'playback completion must use reward-independent acknowledgement'
Add-Check 'Work playback does not mutate domain state' (-not ($work -match 'GameState|CommandReceipt|\.Cash\s*=|\.Xp\s*=')) 'view must not mutate domain state'

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
