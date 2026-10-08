[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Failure {
    param([Parameter(Mandatory)][string]$Message)
    $failures.Add($Message)
}

$requiredFiles = @(
    'AGENTS.md',
    'README.md',
    'docs/GDD.md',
    'docs/MVP_PLAN.md',
    'docs/AI_PRODUCTION_WORKFLOW.md',
    'docs/SKILLS_MANIFEST.md',
    'docs/IMPLEMENTATION_PLAN_MVP.md',
    'docs/evidence/M0-T01/SESSION.md',
    'docs/adr/ADR-010-windows-android-first-ios-acceptance-deferral.md',
    'docs/evidence/M7-T02/RECONCILIATION.md'
)

foreach ($relativePath in $requiredFiles) {
    $absolutePath = Join-Path $repositoryRoot $relativePath
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        Add-Failure "Missing required file: $relativePath"
    }
}

$legacyRootFiles = @(
    'startup_life_FULL_GDD_v1.md',
    'startup_life_MVP_PLAN_v1.md',
    'AI_PRODUCTION_WORKFLOW.md',
    'SKILLS_MANIFEST.md'
)

foreach ($relativePath in $legacyRootFiles) {
    if (Test-Path -LiteralPath (Join-Path $repositoryRoot $relativePath)) {
        Add-Failure "Duplicate authoritative root document remains: $relativePath"
    }
}

$sourceHashes = [ordered]@{
    'docs/GDD.md' = 'aa2635429901b64b7f9fb5226814415072e8b28fb7b17294dcc2095367fa60ca'
    'docs/MVP_PLAN.md' = '33973cc9dda02ecc902b3644064b65e1f27b79df89f0a0ea8448943bc2bc8e5a'
    'docs/AI_PRODUCTION_WORKFLOW.md' = '6a5918f0725febf030ea1cf82650551b8bce27e4077e7ecbb6305e23ca2f3fb5'
    'docs/SKILLS_MANIFEST.md' = '8b6d59a7484757b679425a8078ea67ca83aac4044d17379d524f27ee2290e0b8'
}

foreach ($entry in $sourceHashes.GetEnumerator()) {
    $absolutePath = Join-Path $repositoryRoot $entry.Key
    if (Test-Path -LiteralPath $absolutePath -PathType Leaf) {
        $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $absolutePath).Hash.ToLowerInvariant()
        if ($actualHash -ne $entry.Value) {
            Add-Failure "Source parity failed for $($entry.Key): expected $($entry.Value), got $actualHash"
        }
    }
}

$ledgerPath = Join-Path $repositoryRoot 'docs/IMPLEMENTATION_PLAN_MVP.md'
if (Test-Path -LiteralPath $ledgerPath -PathType Leaf) {
    $ledger = [IO.File]::ReadAllText($ledgerPath)
    $taskPattern = '(?m)^- \[(?<status>[ x])\] \*\*(?<id>M\d+-T\d{2})\s+—'
    $taskMatches = [regex]::Matches($ledger, $taskPattern)
    $taskIds = @($taskMatches | ForEach-Object { $_.Groups['id'].Value })

    $milestoneCounts = [ordered]@{
        0 = 4; 1 = 3; 2 = 2; 3 = 2; 4 = 2; 5 = 2; 6 = 2; 7 = 2; 8 = 2; 9 = 2
        10 = 2; 11 = 2; 12 = 3; 13 = 4; 14 = 2; 15 = 2; 16 = 4; 17 = 2; 18 = 3; 19 = 2
    }
    $expectedTaskIds = @(
        foreach ($milestone in $milestoneCounts.Keys) {
            foreach ($taskNumber in 1..$milestoneCounts[$milestone]) {
                'M{0}-T{1:D2}' -f $milestone, $taskNumber
            }
        }
    )

    $duplicateIds = @($taskIds | Group-Object | Where-Object Count -gt 1 | ForEach-Object Name)
    foreach ($duplicateId in $duplicateIds) {
        Add-Failure "Duplicate task ID: $duplicateId"
    }

    foreach ($expectedTaskId in $expectedTaskIds) {
        if ($expectedTaskId -notin $taskIds) {
            Add-Failure "Missing expected task ID: $expectedTaskId"
        }
    }
    foreach ($taskId in $taskIds) {
        if ($taskId -notin $expectedTaskIds) {
            Add-Failure "Unexpected task ID: $taskId"
        }
    }

    $checkedTaskIds = @($taskMatches | Where-Object { $_.Groups['status'].Value -eq 'x' } | ForEach-Object { $_.Groups['id'].Value })
    $expectedCompletedTaskIds = @('M0-T01', 'M2-T02', 'M3-T02', 'M6-T01', 'M6-T02', 'M7-T01', 'M7-T02', 'M8-T01', 'M8-T02', 'M9-T01')
    $completedTaskDiff = @(Compare-Object -ReferenceObject $expectedCompletedTaskIds -DifferenceObject $checkedTaskIds)
    if ($completedTaskDiff.Count -ne 0) {
        Add-Failure "Completed task set differs from reconciled ledger. Expected: $($expectedCompletedTaskIds -join ', '); actual: $($checkedTaskIds -join ', ')"
    }

    $skillLinePattern = '(?m)^  \*\*Owner:\*\* .+? \*\*Skills:\*\* (?<skills>.+?)\. \*\*Dependencies:\*\* (?<dependencies>.+?)\.\s*$'
    $skillLines = [regex]::Matches($ledger, $skillLinePattern)
    if ($skillLines.Count -ne $taskMatches.Count) {
        Add-Failure "Expected one owner/skills/dependencies line per task: found $($skillLines.Count) for $($taskMatches.Count) tasks"
    }

    $bundleDefinitions = @{
        DOC = @('startup-life-session-orchestrator')
        BOOT = @('startup-life-session-orchestrator', 'skill-installer')
        SETUP = @('startup-life-session-orchestrator', 'new-unity-project', 'unity-cli', 'unity-package-management', 'unity-project-setup', 'unity-mcp-bridge')
        GAME = @('startup-life-session-orchestrator', 'startup-life-gameplay-guardian', 'unity-game-director', 'unity-gameplay-systems', 'unity-mcp-bridge', 'unity-qa-release')
        ECON = @('startup-life-session-orchestrator', 'startup-life-gameplay-guardian', 'unity-game-director', 'unity-gameplay-systems', 'unity-mcp-bridge', 'unity-qa-release', 'unity-game-economy')
        SAVE = @('startup-life-session-orchestrator', 'startup-life-gameplay-guardian', 'unity-game-director', 'unity-gameplay-systems', 'unity-mcp-bridge', 'unity-qa-release', 'unity-debug-profiler')
        UI = @('startup-life-session-orchestrator', 'ui', 'ui-ugui', 'unity-ui-designer', 'unity-mcp-bridge', 'vietnam-art-direction', 'unity-qa-release')
        ART = @('startup-life-session-orchestrator', 'vietnam-art-direction', 'startup-life-asset-quality-gate', 'unity-art-direction', 'unity-asset-designer', 'unity-asset-pipeline', 'unity-scene-composition')
        IMAGE = @('startup-life-session-orchestrator', 'vietnam-art-direction', 'startup-life-asset-quality-gate', 'unity-art-direction', 'unity-asset-designer', 'unity-asset-pipeline', 'unity-scene-composition', 'unity-image-generator')
        RIG = @('startup-life-session-orchestrator', 'vietnam-art-direction', 'startup-life-asset-quality-gate', 'unity-art-direction', 'unity-asset-designer', 'unity-asset-pipeline', 'unity-scene-composition', 'sprite-editor', 'unity-animation', 'unity-mcp-bridge', 'unity-qa-release')
        LOC = @('startup-life-session-orchestrator', 'vietnam-art-direction', 'localization', 'unity-localization', 'unity-qa-release')
        QA = @('startup-life-session-orchestrator', 'unity-cli', 'unity-debug-profiler', 'unity-mcp-bridge', 'unity-qa-release', 'unity-localization')
    }
    $bundleAliases = @($bundleDefinitions.Keys)
    $taskRequirements = [ordered]@{
        'M0-T01' = @{ Skills = @('DOC'); Dependencies = @() }
        'M0-T02' = @{ Skills = @('BOOT'); Dependencies = @('M0-T01') }
        'M0-T03' = @{ Skills = @('SETUP', 'UI', 'LOC'); Dependencies = @('M0-T02') }
        'M0-T04' = @{ Skills = @('SETUP', 'QA'); Dependencies = @('M0-T03') }
        'M1-T01' = @{ Skills = @('GAME', 'SAVE'); Dependencies = @('M0-T01', 'M0-T02') }
        'M1-T02' = @{ Skills = @('GAME'); Dependencies = @('M0-T04', 'M1-T01') }
        'M1-T03' = @{ Skills = @('SAVE'); Dependencies = @('M1-T02') }
        'M2-T01' = @{ Skills = @('GAME', 'ECON'); Dependencies = @('M1-T02', 'M1-T03') }
        'M2-T02' = @{ Skills = @('GAME'); Dependencies = @('M2-T01') }
        'M3-T01' = @{ Skills = @('ECON'); Dependencies = @('M2-T02') }
        'M3-T02' = @{ Skills = @('ECON'); Dependencies = @('M3-T01') }
        'M4-T01' = @{ Skills = @('GAME', 'ECON'); Dependencies = @('M3-T02') }
        'M4-T02' = @{ Skills = @('GAME', 'ECON'); Dependencies = @('M4-T01') }
        'M5-T01' = @{ Skills = @('GAME'); Dependencies = @('M4-T02') }
        'M5-T02' = @{ Skills = @('GAME', 'SAVE'); Dependencies = @('M5-T01') }
        'M6-T01' = @{ Skills = @('GAME'); Dependencies = @('M4-T02', 'M5-T02') }
        'M6-T02' = @{ Skills = @('GAME', 'ECON'); Dependencies = @('M6-T01', 'M3-T02', 'M2-T02') }
        'M7-T01' = @{ Skills = @('UI', 'LOC', 'GAME'); Dependencies = @('M6-T02') }
        'M7-T02' = @{ Skills = @('SAVE', 'UI', 'QA'); Dependencies = @('M7-T01') }
        'M8-T01' = @{ Skills = @('ECON'); Dependencies = @('M7-T02') }
        'M8-T02' = @{ Skills = @('GAME', 'ECON'); Dependencies = @('M8-T01') }
        'M9-T01' = @{ Skills = @('GAME', 'ECON'); Dependencies = @('M8-T02') }
        'M9-T02' = @{ Skills = @('ECON'); Dependencies = @('M9-T01') }
        'M10-T01' = @{ Skills = @('GAME', 'ECON'); Dependencies = @('M9-T02') }
        'M10-T02' = @{ Skills = @('ECON', 'UI'); Dependencies = @('M10-T01') }
        'M11-T01' = @{ Skills = @('SAVE', 'QA'); Dependencies = @('M10-T02') }
        'M11-T02' = @{ Skills = @('SAVE'); Dependencies = @('M11-T01') }
        'M12-T01' = @{ Skills = @('UI', 'LOC', 'GAME'); Dependencies = @('M11-T02') }
        'M12-T02' = @{ Skills = @('UI', 'LOC', 'ECON'); Dependencies = @('M12-T01') }
        'M12-T03' = @{ Skills = @('UI', 'LOC'); Dependencies = @('M12-T02') }
        'M13-T01' = @{ Skills = @('ART'); Dependencies = @('M7-T01') }
        'M13-T02' = @{ Skills = @('IMAGE'); Dependencies = @('M13-T01') }
        'M13-T03' = @{ Skills = @('RIG'); Dependencies = @('M13-T02', 'M0-T03') }
        'M13-T04' = @{ Skills = @('IMAGE', 'RIG', 'UI'); Dependencies = @('M13-T03', 'M7-T01', 'M10-T02') }
        'M14-T01' = @{ Skills = @('RIG', 'GAME'); Dependencies = @('M13-T04', 'M5-T02') }
        'M14-T02' = @{ Skills = @('RIG', 'QA', 'manage-sprite-atlas', 'audio-setup-mixers', 'optimize-audio'); Dependencies = @('M14-T01') }
        'M15-T01' = @{ Skills = @('GAME', 'ECON', 'SAVE'); Dependencies = @('M10-T02', 'M11-T02') }
        'M15-T02' = @{ Skills = @('GAME', 'ECON', 'UI', 'LOC'); Dependencies = @('M15-T01', 'M12-T03') }
        'M16-T01' = @{ Skills = @('GAME', 'ECON', 'RIG', 'LOC'); Dependencies = @('M12-T03', 'M14-T02', 'M15-T02') }
        'M16-T02' = @{ Skills = @('GAME', 'ECON', 'IMAGE', 'RIG', 'LOC'); Dependencies = @('M16-T01') }
        'M16-T03' = @{ Skills = @('ECON', 'IMAGE', 'RIG', 'UI', 'LOC'); Dependencies = @('M16-T02', 'M9-T02') }
        'M16-T04' = @{ Skills = @('IMAGE', 'RIG', 'UI', 'LOC'); Dependencies = @('M16-T03') }
        'M17-T01' = @{ Skills = @('ECON', 'QA'); Dependencies = @('M16-T04') }
        'M17-T02' = @{ Skills = @('GAME', 'ECON', 'UI', 'QA'); Dependencies = @('M17-T01') }
        'M18-T01' = @{ Skills = @('QA', 'UI', 'LOC'); Dependencies = @('M17-T02') }
        'M18-T02' = @{ Skills = @('QA', 'UI', 'LOC'); Dependencies = @('M18-T01', 'M0-T04') }
        'M18-T03' = @{ Skills = @('QA', 'manage-sprite-atlas', 'optimize-audio'); Dependencies = @('M18-T02') }
        'M19-T01' = @{ Skills = @('GAME', 'ECON', 'SAVE', 'ART', 'UI', 'LOC', 'QA'); Dependencies = @('M18-T03') }
        'M19-T02' = @{ Skills = @('QA', 'DOC'); Dependencies = @('M19-T01') }
    }
    for ($index = 0; $index -lt [Math]::Min($taskMatches.Count, $skillLines.Count); $index++) {
        $taskId = $taskMatches[$index].Groups['id'].Value
        $skillsText = $skillLines[$index].Groups['skills'].Value
        $skills = @([regex]::Matches($skillsText, '`(?<name>[^`]+)`') | ForEach-Object { $_.Groups['name'].Value })
        $residue = [regex]::Replace($skillsText, '`[^`]+`', '').Replace(',', '').Trim()

        if ($skills.Count -eq 0 -or $residue.Length -gt 0) {
            Add-Failure "$taskId must list only explicit backticked skill names: $skillsText"
        }
        if ('startup-life-session-orchestrator' -notin $skills) {
            Add-Failure "$taskId does not include startup-life-session-orchestrator"
        }
        foreach ($alias in $bundleAliases) {
            if ($skills -ccontains $alias) {
                Add-Failure "$taskId still uses bundle alias $alias"
            }
        }
        $duplicateSkills = @($skills | Group-Object | Where-Object Count -gt 1 | ForEach-Object Name)
        if ($duplicateSkills.Count -gt 0) {
            Add-Failure "$taskId repeats skills: $($duplicateSkills -join ', ')"
        }

        $expectedSkills = [System.Collections.Generic.List[string]]::new()
        foreach ($skillRequirement in $taskRequirements[$taskId].Skills) {
            $requiredSkills = if ($bundleDefinitions.ContainsKey($skillRequirement)) {
                $bundleDefinitions[$skillRequirement]
            }
            else {
                @($skillRequirement)
            }
            foreach ($requiredSkill in $requiredSkills) {
                if (-not $expectedSkills.Contains($requiredSkill)) {
                    $expectedSkills.Add($requiredSkill)
                }
            }
        }
        if (($skills -join '|') -cne ($expectedSkills -join '|')) {
            Add-Failure "$taskId skill expansion differs from the approved requirement. Expected: $($expectedSkills -join ', ')"
        }

        $dependenciesText = $skillLines[$index].Groups['dependencies'].Value.Trim()
        $dependencyIds = @()
        if ($dependenciesText -ne 'none') {
            $dependencyIds = @([regex]::Matches($dependenciesText, 'M\d+-T\d{2}') | ForEach-Object Value)
            if ($dependencyIds.Count -eq 0) {
                Add-Failure "$taskId has an unparseable dependency list: $dependenciesText"
            }
            foreach ($dependencyId in $dependencyIds) {
                if ($dependencyId -notin $expectedTaskIds) {
                    Add-Failure "$taskId references unknown dependency $dependencyId"
                }
                if ($dependencyId -eq $taskId) {
                    Add-Failure "$taskId depends on itself"
                }
            }
        }
        $expectedDependencies = @($taskRequirements[$taskId].Dependencies)
        if (($dependencyIds -join '|') -cne ($expectedDependencies -join '|')) {
            Add-Failure "$taskId dependencies differ from the approved requirement. Expected: $($expectedDependencies -join ', ')"
        }
    }

    foreach ($requiredHeading in @('### ACTIVE_STATUS', '### CURRENT_STATE', '### SPEC_CONFLICTS', '### GAP_ANALYSIS', '### Evidence and status conventions', '### Legacy M0–M8 traceability')) {
        if (-not $ledger.Contains($requiredHeading)) {
            Add-Failure "Ledger is missing required heading: $requiredHeading"
        }
    }

    $legacyRows = [regex]::Matches($ledger, '(?m)^\| M[0-8] — .+ \| .+ \|$')
    if ($legacyRows.Count -ne 9) {
        Add-Failure "Expected 9 legacy M0-M8 mapping rows, found $($legacyRows.Count)"
    }

    if (-not $ledger.Contains('M7-T02 closes under ADR-010')) {
        Add-Failure 'Ledger does not record M7-T02 closure under ADR-010'
    }
    if (-not $ledger.Contains('M18-T02 — iOS lifecycle, signing, and IL2CPP QA')) {
        Add-Failure 'Ledger does not retain M18-T02 as the later iOS owner'
    }
}

$adr010Path = Join-Path $repositoryRoot 'docs/adr/ADR-010-windows-android-first-ios-acceptance-deferral.md'
$reconciliationPath = Join-Path $repositoryRoot 'docs/evidence/M7-T02/RECONCILIATION.md'
if ((Test-Path -LiteralPath $adr010Path -PathType Leaf) -and (Test-Path -LiteralPath $reconciliationPath -PathType Leaf)) {
    $adr010 = [IO.File]::ReadAllText($adr010Path)
    $reconciliation = [IO.File]::ReadAllText($reconciliationPath)

    if (-not $adr010.Contains('M18-T02')) {
        Add-Failure 'ADR-010 does not assign later iOS acceptance to M18-T02'
    }
    if (-not $reconciliation.Contains('iOS: DEFERRED — NOT RUN')) {
        Add-Failure 'M7-T02 reconciliation does not preserve deferred iOS as NOT RUN'
    }
    if (-not $reconciliation.Contains('Physical Android: NOT RUN — owned by M18-T01')) {
        Add-Failure 'M7-T02 reconciliation does not preserve physical Android ownership under M18-T01'
    }
    if (($adr010 -match '(?im)^.*\biOS\b.*\bPASS\b') -or
        ($reconciliation -match '(?im)^.*\biOS\b.*\bPASS\b')) {
        Add-Failure 'Deferred iOS must not be represented as PASS'
    }
}

$markdownFiles = @(Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter '*.md' | Where-Object {
    $_.FullName -notmatch '[\\/](Library|Temp|Logs|obj|Packages)[\\/]'
})

foreach ($markdownFile in $markdownFiles) {
    $content = [IO.File]::ReadAllText($markdownFile.FullName)
    foreach ($link in [regex]::Matches($content, '\[[^\]]+\]\((?<target>[^)]+)\)')) {
        $target = $link.Groups['target'].Value.Trim()
        if ($target.StartsWith('<') -and $target.EndsWith('>')) {
            $target = $target.Substring(1, $target.Length - 2)
        }
        if ($target -match '^(https?://|mailto:|#|codex:)') {
            continue
        }
        $targetPath = $target.Split('#')[0]
        if ([string]::IsNullOrWhiteSpace($targetPath)) {
            continue
        }
        $decodedTarget = [Uri]::UnescapeDataString($targetPath)
        $resolvedTarget = Join-Path $markdownFile.DirectoryName $decodedTarget
        if (-not (Test-Path -LiteralPath $resolvedTarget)) {
            $relativeSource = [IO.Path]::GetRelativePath($repositoryRoot, $markdownFile.FullName)
            Add-Failure "Broken local link in ${relativeSource}: $target"
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Documentation validation failed with $($failures.Count) issue(s):" -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host "- $failure" -ForegroundColor Red
    }
    exit 1
}

Write-Host 'Documentation validation passed.' -ForegroundColor Green
Write-Host "Canonical source parity: $($sourceHashes.Count)/$($sourceHashes.Count)"
Write-Host "Task ledger: 49 unique expected IDs; reconciled complete set: $($expectedCompletedTaskIds -join ', ')"
Write-Host 'Task requirements: exact approved dependencies and expanded skills'
Write-Host "Markdown links checked: $($markdownFiles.Count) files"

