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

    $checks.Add([ordered]@{
        name = $Name
        passed = $Passed
        detail = $Detail
    })

    if (-not $Passed) {
        $failures.Add("$Name`: $Detail")
    }
}

function Read-RepoText {
    param([Parameter(Mandatory)][string]$RelativePath)
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file is missing: $RelativePath"
    }
    return [IO.File]::ReadAllText($path)
}

function Has-Line {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][string]$Pattern
    )
    return [regex]::IsMatch($Text, $Pattern, [Text.RegularExpressions.RegexOptions]::Multiline)
}

$requiredFiles = @(
    'ProjectSettings/ProjectVersion.txt',
    'ProjectSettings/EditorSettings.asset',
    'ProjectSettings/VersionControlSettings.asset',
    'ProjectSettings/ProjectSettings.asset',
    'ProjectSettings/EditorBuildSettings.asset',
    'Packages/manifest.json',
    'Packages/packages-lock.json',
    'Assets/Settings/UniversalRP.asset',
    'Assets/Settings/Renderer2D.asset',
    'Assets/Settings/InputSystem_Actions.inputactions',
    'Assets/StartupLife/Scenes/MobileBaseline.unity',
    'Assets/StartupLife/Scripts/Editor/MobileBaselineBuilder.cs',
    'Assets/StartupLife/UI/Fonts/NotoSans-Regular.ttf',
    'Assets/StartupLife/UI/Fonts/OFL.txt',
    'docs/tooling/skills.lock.json'
)

$missingFiles = @($requiredFiles | Where-Object { -not (Test-Path -LiteralPath (Join-Path $repositoryRoot $_) -PathType Leaf) })
Add-Check 'foundation files exist' ($missingFiles.Count -eq 0) ($(if ($missingFiles.Count -eq 0) { "$($requiredFiles.Count) required files present" } else { "missing: $($missingFiles -join ', ')" }))

$projectVersion = Read-RepoText 'ProjectSettings/ProjectVersion.txt'
Add-Check 'Unity editor patch pinned' ($projectVersion -match '(?m)^m_EditorVersion: 6000\.3\.25f1\s*$') 'expected Unity 6000.3.25f1'
Add-Check 'Unity editor revision pinned' ($projectVersion -match '(?m)^m_EditorVersionWithRevision: 6000\.3\.25f1 \(e1dba0a9aba4\)\s*$') 'expected revision e1dba0a9aba4'

$editorSettings = Read-RepoText 'ProjectSettings/EditorSettings.asset'
Add-Check 'Force Text serialization' (Has-Line $editorSettings '^\s*m_SerializationMode:\s*2\s*$') 'EditorSettings.m_SerializationMode must be 2'

$versionControl = Read-RepoText 'ProjectSettings/VersionControlSettings.asset'
Add-Check 'Visible Meta Files enabled' (Has-Line $versionControl '^\s*m_Mode:\s*Visible Meta Files\s*$') 'VersionControlSettings must use Visible Meta Files'

$playerSettings = Read-RepoText 'ProjectSettings/ProjectSettings.asset'
$playerPatterns = [ordered]@{
    'product identity' = @('^\s*companyName:\s*Startup Life\s*$', '^\s*productName:\s*Startup Life\s*$')
    'portrait reference resolution' = @('^\s*defaultScreenWidth:\s*1080\s*$', '^\s*defaultScreenHeight:\s*1920\s*$')
    'landscape autorotation disabled' = @('^\s*allowedAutorotateToLandscapeRight:\s*0\s*$', '^\s*allowedAutorotateToLandscapeLeft:\s*0\s*$')
    'Input System active' = @('^\s*activeInputHandler:\s*1\s*$')
}
foreach ($entry in $playerPatterns.GetEnumerator()) {
    $ok = $true
    foreach ($pattern in $entry.Value) {
        if (-not (Has-Line $playerSettings $pattern)) { $ok = $false; break }
    }
    Add-Check $entry.Key $ok ($entry.Value -join '; ')
}

$manifest = (Read-RepoText 'Packages/manifest.json') | ConvertFrom-Json
$lock = (Read-RepoText 'Packages/packages-lock.json') | ConvertFrom-Json
$requiredPackages = [ordered]@{
    'com.unity.render-pipelines.universal' = '17.3.0'
    'com.unity.2d.animation' = '13.0.6'
    'com.unity.2d.psdimporter' = '12.0.2'
    'com.unity.inputsystem' = '1.20.0'
    'com.unity.localization' = '1.5.8'
    'com.unity.test-framework' = '1.6.0'
    'com.unity.ugui' = '2.0.0'
    'com.unity.pipeline' = '0.8.0-exp.1'
}
foreach ($entry in $requiredPackages.GetEnumerator()) {
    $actual = $manifest.dependencies.PSObject.Properties[$entry.Key].Value
    Add-Check "package pinned: $($entry.Key)" ($actual -eq $entry.Value) "expected $($entry.Value), got $actual"
}

$lockProblems = [System.Collections.Generic.List[string]]::new()
foreach ($property in $manifest.dependencies.PSObject.Properties) {
    $locked = $lock.dependencies.PSObject.Properties[$property.Name]
    if ($null -eq $locked) {
        $lockProblems.Add("$($property.Name): missing from lock")
        continue
    }
    if ($locked.Value.depth -ne 0) {
        $lockProblems.Add("$($property.Name): lock depth $($locked.Value.depth), expected 0")
    }
    if ($locked.Value.version -ne $property.Value) {
        $lockProblems.Add("$($property.Name): manifest $($property.Value), lock $($locked.Value.version)")
    }
}
Add-Check 'manifest and lock direct dependencies agree' ($lockProblems.Count -eq 0) ($(if ($lockProblems.Count -eq 0) { "$($manifest.dependencies.PSObject.Properties.Count) direct dependencies aligned" } else { $lockProblems -join '; ' }))

$buildSettings = Read-RepoText 'ProjectSettings/EditorBuildSettings.asset'
Add-Check 'mobile baseline is launch scene' ($buildSettings -match '(?ms)m_Scenes:\s*\n\s*- enabled:\s*1\s*\n\s*path:\s*Assets/StartupLife/Scenes/MobileBaseline\.unity') 'MobileBaseline.unity must be the first enabled build scene'
Add-Check 'Input System settings registered' ($buildSettings -match 'com\.unity\.input\.settings\.actions:') 'EditorBuildSettings must register Input System actions'
Add-Check 'Localization settings registered' ($buildSettings -match 'com\.unity\.localization\.settings:') 'EditorBuildSettings must register Localization settings'

$baselineBuilder = Read-RepoText 'Assets/StartupLife/Scripts/Editor/MobileBaselineBuilder.cs'
$baselineRequirements = [ordered]@{
    'baseline explicitly sets portrait' = 'PlayerSettings\.defaultInterfaceOrientation\s*=\s*UIOrientation\.Portrait'
    'baseline uses orthographic camera' = 'camera\.orthographic\s*=\s*true'
    'baseline uses 1080x1920 CanvasScaler' = 'scaler\.referenceResolution\s*=\s*new Vector2\(1080, 1920\)'
    'baseline installs safe area component' = 'typeof\(MobileSafeArea\)'
    'baseline validates Vietnamese glyphs' = 'font\.TryAddCharacters\(GlyphSample'
    'baseline writes localization keys' = 'CreateStringTableCollection\("FoundationUI"'
}
foreach ($entry in $baselineRequirements.GetEnumerator()) {
    Add-Check $entry.Key ([regex]::IsMatch($baselineBuilder, $entry.Value)) $entry.Value
}

$tracked = @(& git -C $repositoryRoot ls-files)
if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed' }
$generatedPrefixes = @('Library/', 'Temp/', 'Obj/', 'obj/', 'Build/', 'Builds/', 'Logs/', 'UserSettings/', 'TestResults/', 'Artifacts/')
$trackedGenerated = @($tracked | Where-Object {
    $path = $_
    $generatedPrefixes | Where-Object { $path.StartsWith($_, [StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
})
Add-Check 'generated Unity folders are not tracked' ($trackedGenerated.Count -eq 0) ($(if ($trackedGenerated.Count -eq 0) { 'clean' } else { $trackedGenerated -join ', ' }))

$assetFiles = @($tracked | Where-Object { $_ -like 'Assets/*' -and $_ -notlike '*.meta' -and $_ -notlike '*/.gitkeep' })
$missingMeta = @($assetFiles | Where-Object { "$_`n" -and ("$_.meta" -notin $tracked) })
Add-Check 'tracked Unity assets have .meta sidecars' ($missingMeta.Count -eq 0) ($(if ($missingMeta.Count -eq 0) { "$($assetFiles.Count) tracked assets paired" } else { "missing meta: $($missingMeta -join ', ')" }))

$skillsLock = (Read-RepoText 'docs/tooling/skills.lock.json') | ConvertFrom-Json
Add-Check 'community Unity skill revision pinned' ($skillsLock.upstreams.communityUnityGameSkills.revision -eq 'dafb97ef00f94e64e42e6260bc6b3af74cc83dad') 'expected reviewed community revision dafb97ef00f94e64e42e6260bc6b3af74cc83dad'
$requiredSkills = @($skillsLock.skills | Where-Object requiredByLedger)
$badCommunitySkills = @($requiredSkills | Where-Object { $_.provenance -eq 'community-pinned' -and $_.contentMatchesPinnedSource -ne $true })
Add-Check 'required community skill content matches pin' ($badCommunitySkills.Count -eq 0) ($(if ($badCommunitySkills.Count -eq 0) { "$($requiredSkills.Count) required skills inventoried" } else { ($badCommunitySkills.name -join ', ') }))

$report = [ordered]@{
    suite = 'Startup Life static Unity foundation'
    editor = '6000.3.25f1'
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
    throw "Static Unity foundation gate failed: $($failures.Count) check(s)."
}

Write-Output "Static Unity foundation: $($report.passed)/$($checks.Count) checks passed."
