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
    if (-not $Passed) { $failures.Add("$Name - $Detail") }
}

function Read-RepoText {
    param([Parameter(Mandatory)][string]$RelativePath)
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required file is missing: $RelativePath" }
    return [IO.File]::ReadAllText($path)
}

function Has-Pattern {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][string]$Pattern
    )
    return [regex]::IsMatch($Text, $Pattern, [Text.RegularExpressions.RegexOptions]::Multiline)
}

$contracts = Read-RepoText 'Assets/StartupLife/Scripts/Core/Contracts.cs'
$state = Read-RepoText 'Assets/StartupLife/Scripts/Core/GameState.cs'
$session = Read-RepoText 'Assets/StartupLife/Scripts/Application/GameSession.cs'
$simulation = Read-RepoText 'Assets/StartupLife/Scripts/Simulation/SimulationEngine.cs'

$requiredContracts = [ordered]@{
    'CommandResult publishes SimulationOutcome' = 'public\s+SimulationOutcome\?\s+Outcome\s*\{\s*get;\s*\}'
    'AdvanceResult publishes reached instant' = 'public\s+SimInstant\s+ReachedInstant\s*\{\s*get;\s*\}'
    'SimulationOutcome publishes activity identity' = 'public\s+string\s+ActivityId\s*\{\s*get;\s*\}'
    'SimulationOutcome publishes playback cursor' = 'public\s+int\s+PlaybackCursor\s*\{\s*get;\s*\}'
    'SimulationOutcome publishes typed course change' = 'public\s+CourseChange\?\s+CourseChange\s*\{\s*get;\s*\}'
    'CourseChange kind is frozen' = 'enum\s+CourseChangeKind\s*\{\s*Activated\s*,\s*Progressed\s*,\s*Completed\s*\}'
    'Course completion reasons are frozen' = 'enum\s+CourseCompletionReason\s*\{\s*None\s*,\s*StudyTargetReached\s*,\s*SkillTargetAlreadyMet\s*\}'
    'CourseChange publishes instance id' = 'public\s+string\s+InstanceId\s*\{\s*get;\s*\}'
    'CourseChange publishes definition id' = 'public\s+string\s+DefinitionId\s*\{\s*get;\s*\}'
    'CourseChange publishes skill id' = 'public\s+string\s+SkillId\s*\{\s*get;\s*\}'
    'CourseChange publishes prior progress' = 'public\s+long\s+PriorProgressUnits\s*\{\s*get;\s*\}'
    'CourseChange publishes new progress' = 'public\s+long\s+NewProgressUnits\s*\{\s*get;\s*\}'
    'CourseChange publishes target units' = 'public\s+long\s+TargetUnits\s*\{\s*get;\s*\}'
    'CourseChange publishes target level' = 'public\s+int\s+TargetLevel\s*\{\s*get;\s*\}'
}
foreach ($entry in $requiredContracts.GetEnumerator()) {
    Add-Check $entry.Key (Has-Pattern $contracts $entry.Value) $entry.Value
}

$requiredSnapshot = [ordered]@{
    'ActiveCourseSnapshot remains sealed' = 'public\s+sealed\s+class\s+ActiveCourseSnapshot'
    'Snapshot publishes current activity id' = 'public\s+string\s+CurrentActivityId\s*\{\s*get;\s*\}'
    'Snapshot publishes playback cursor' = 'public\s+int\s+PlaybackCursor\s*\{\s*get;\s*\}'
    'Snapshot publishes nullable active course' = 'public\s+ActiveCourseSnapshot\?\s+ActiveCourse\s*\{\s*get;\s*\}'
    'Active course publishes instance id' = 'public\s+string\s+InstanceId\s*\{\s*get;\s*\}'
    'Active course publishes definition id' = 'public\s+string\s+DefinitionId\s*\{\s*get;\s*\}'
    'Active course publishes skill id' = 'public\s+string\s+SkillId\s*\{\s*get;\s*\}'
    'Active course publishes progress units' = 'public\s+long\s+ProgressUnits\s*\{\s*get;\s*\}'
    'Active course publishes target units' = 'public\s+long\s+TargetUnits\s*\{\s*get;\s*\}'
    'Active course publishes target level' = 'public\s+int\s+TargetLevel\s*\{\s*get;\s*\}'
}
foreach ($entry in $requiredSnapshot.GetEnumerator()) {
    Add-Check $entry.Key (Has-Pattern $state $entry.Value) $entry.Value
}

Add-Check 'legacy snapshot StudyUnits stays removed' (-not (Has-Pattern $state 'public\s+long\s+StudyUnits\s*\{')) 'GameSnapshot.StudyUnits must not return'
Add-Check 'legacy outcome CourseInstanceId stays removed' (-not (Has-Pattern $contracts 'public\s+string\s+CourseInstanceId\s*\{')) 'SimulationOutcome.CourseInstanceId must not return'
Add-Check 'legacy outcome StudyUnitsDelta stays removed' (-not (Has-Pattern $contracts 'public\s+long\s+StudyUnitsDelta\s*\{')) 'SimulationOutcome.StudyUnitsDelta must not return'

Add-Check 'course outcome is built explicitly' (Has-Pattern $session 'BuildCourseChange\(before,\s*after,\s*history\)') 'GameSession must build typed CourseChange from authoritative before/after state'
Add-Check 'root cue and cursor remain replay-bound' (Has-Pattern $simulation 'state\.CurrentCue\s*==\s*replay\.CurrentCue\s*&&\s*state\.PlaybackCursor\s*==\s*replay\.PlaybackCursor') 'restore validation must bind root playback provenance'
Add-Check 'candidate serialization remains inside validation path' (Has-Pattern $session 'candidateBytes\s*=\s*serializer\.Serialize\(candidate\);') 'candidate serialization must remain in the controlled validation block'

$publicMutableLeaks = @(
    [regex]::Matches($contracts, 'public\s+(?:GameState|CourseState|EmploymentState|SkillState|LedgerEntry|CommandReceipt)\??\s+\w+\s*\{\s*get;') |
        ForEach-Object { $_.Value }
)
Add-Check 'outcome contract exposes no mutable state DTOs' ($publicMutableLeaks.Count -eq 0) ($(if ($publicMutableLeaks.Count -eq 0) { 'no mutable Core state DTOs exposed' } else { $publicMutableLeaks -join '; ' }))

$report = [ordered]@{
    suite = 'Startup Life M1 public API freeze'
    frozenFrom = 'Astra closure after PR #13'
    passed = @($checks | Where-Object passed).Count
    failed = $failures.Count
    checks = $checks
}

if ($ReportPath) {
    $resolvedReport = [IO.Path]::GetFullPath($ReportPath)
    $directory = Split-Path -Parent $resolvedReport
    if ($directory) { New-Item -ItemType Directory -Force -Path $directory | Out-Null }
    $report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $resolvedReport -Encoding utf8
    Write-Output "Report: $resolvedReport"
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Error $failure }
    throw "M1 public API freeze gate failed: $($failures.Count) check(s)."
}

Write-Output "M1 public API freeze: $($report.passed)/$($checks.Count) checks passed."
