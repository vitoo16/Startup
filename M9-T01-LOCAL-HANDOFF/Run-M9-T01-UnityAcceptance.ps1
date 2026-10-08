# Requires PowerShell 7 on Windows. Run as the same interactive user whose Unity license is active.
# External handoff script; NOT part of the approved Unity repository / SHA.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$RepositoryPath,
    [Parameter(Mandatory = $true)][string]$UnityCliPath,
    [Parameter(Mandatory = $true)][string]$UnityEditorPath,
    [Parameter(Mandatory = $true)][string]$EvidenceRoot,
    [ValidateRange(60, 7200)][int]$TimeoutSeconds = 900
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

$approvedSha = '68a5c30c267e2a79c4f6ed12b37f08a132ef90cd'
$approvedEditor = '6000.3.25f1'
$approvedRevision = 'e1dba0a9aba4'
$approvedRepo = 'vitoo16/Startup'
$failedPhase = 'preflight'
$evidence = $null
$staging = $null
$repo = $null
$archive = $null
$manifest = [ordered]@{
    schema = 'startup-life.m9-t01.unity-acceptance.v1'
    generatedUtc = [DateTime]::UtcNow.ToString('o')
    approvedSha = $approvedSha
    repository = $approvedRepo
    pr = 33
    approvedEngineFreeCi = [ordered]@{ workflow = 'Engine-free CI'; run = 143; runId = 37803360563; conclusion = 'success'; evidenceType = 'source-only' }
    sourceAudit = 'APPROVED (supplied handoff; not a Unity runtime claim)'
    environment = [ordered]@{}
    preflight = [ordered]@{}
    fonts = @()
    freshImport = [ordered]@{ executed = $false }
    runner = [ordered]@{ executed = $false }
    tests = @()
    sourceOnlyRegressions = @('Owner-time: 1-4 businesses, Study(180), 241-minute 81/80/80 remainder, 30/120 cap redistribution, career revision fail-closed, permutation and cold restore are covered by engine-free CI #143. Unity execution of these specific cases is NOT asserted without matching Unity test identities.')
    integrity = [ordered]@{}
    findings = @()
    evidenceFiles = @()
    verdict = 'UNITY ACCEPTANCE BLOCKED — LOCAL EXECUTION REQUIRED'
}

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
}
function Git-Lines([string]$workingPath, [string[]]$arguments) {
    $lines = @(& git -C $workingPath @arguments 2>&1)
    $exit = $LASTEXITCODE
    if ($exit -ne 0) { throw "git $($arguments -join ' ') failed ($exit): $($lines -join '; ')" }
    return @($lines | ForEach-Object { [string]$_ } | Where-Object { $_.Length -gt 0 })
}
function Assert-GitClean([string]$path, [string]$label) {
    $entries = @(Git-Lines $path @('status', '--porcelain=v1', '--untracked-files=all'))
    if ($entries.Count -gt 0) { throw "$label is dirty (tracked or untracked): $($entries -join '; ')" }
    return @()
}
function Get-Hash([string]$path) { return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Get-CriticalSourceHashes([string]$worktree) {
    $paths = @(
        'Assets/StartupLife/Scripts/Core/SaveSchema.cs',
        'Assets/StartupLife/Scripts/Core/GameState.cs',
        'Assets/StartupLife/Scripts/Core/OwnerTime.cs',
        'Assets/StartupLife/Scripts/Simulation/CommittedTimeTrace.cs',
        'Assets/StartupLife/Scripts/Simulation/OwnerDayAllocator.cs',
        'Assets/StartupLife/Scripts/Application/GameSession.cs',
        'Assets/StartupLife/UI/Fonts/NotoSansVietnamese.asset',
        'Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset',
        'ProjectSettings/ProjectVersion.txt',
        'Packages/manifest.json',
        'Packages/packages-lock.json'
    )
    $result = [ordered]@{}
    foreach ($relative in $paths) {
        $file = Join-Path $worktree $relative
        Assert-True (Test-Path -LiteralPath $file -PathType Leaf) "Critical tracked file missing: $relative"
        $result[$relative] = Get-Hash $file
    }
    return $result
}
function Save-Text([string]$name, [string]$content) {
    Set-Content -LiteralPath (Join-Path $evidence $name) -Value $content -Encoding utf8
}
function Invoke-CliJson([string]$exe, [string[]]$arguments) {
    $output = @(& $exe @arguments 2>&1)
    $exitCode = $LASTEXITCODE
    Assert-True ($exitCode -eq 0) "$exe $($arguments -join ' ') failed with exit code $exitCode"
    $json = ($output -join "`n") | ConvertFrom-Json
    Assert-True ([bool]$json.success) "CLI responded unsuccessfully: $($arguments -join ' ')"
    return $json
}
function Read-NUnit([string]$path, [string]$mode) {
    [xml]$doc = Get-Content -LiteralPath $path -Raw
    $root = $doc.SelectSingleNode('/test-run')
    Assert-True ($null -ne $root) "$mode NUnit file does not contain /test-run"
    $counts = [ordered]@{}
    foreach ($key in @('total','passed','failed','skipped','inconclusive')) {
        $raw = $root.GetAttribute($key)
        Assert-True ($raw -match '^\d+$') "$mode NUnit lacks integer attribute $key"
        $counts[$key] = [int]$raw
    }
    Assert-True ($counts.total -gt 0) "$mode executed no NUnit tests"
    Assert-True (($counts.passed + $counts.failed + $counts.skipped + $counts.inconclusive) -eq $counts.total) "$mode NUnit counters do not reconcile"
    $failedSuites = @($doc.SelectNodes('//test-suite[@result="Failed" or @result="Inconclusive"]'))
    $failedCases = @($doc.SelectNodes('//test-case[@result="Failed" or @result="Inconclusive"]'))
    $identities = @($doc.SelectNodes('//test-case') | ForEach-Object { $_.GetAttribute('fullname') })
    return [ordered]@{ mode=$mode; counts=$counts; failedSuiteCount=$failedSuites.Count; failedCaseCount=$failedCases.Count; identities=$identities }
}
function Read-JUnit([string]$path, [string]$mode) {
    [xml]$doc = Get-Content -LiteralPath $path -Raw
    $cases = @($doc.SelectNodes('//testcase'))
    $failed = @($doc.SelectNodes('//testcase[failure or error]'))
    $skipped = @($doc.SelectNodes('//testcase[skipped]'))
    Assert-True ($cases.Count -gt 0) "$mode JUnit executed no test cases"
    return [ordered]@{ total=$cases.Count; failed=$failed.Count; skipped=$skipped.Count }
}

try {
    $failedPhase = 'preflight'
    Assert-True ($PSVersionTable.PSVersion.Major -ge 7) 'PowerShell 7 or newer is required by the repository Unity runner contract.'
    Assert-True ($IsWindows) 'This local handoff must run on Windows with the pinned Editor.'
    $repo = (Resolve-Path -LiteralPath $RepositoryPath).Path.TrimEnd('\','/')
    Assert-True (Test-Path -LiteralPath (Join-Path $repo 'ProjectSettings/ProjectVersion.txt') -PathType Leaf) 'RepositoryPath is not a Unity project root.'
    $out = [IO.Path]::GetFullPath($EvidenceRoot).TrimEnd('\','/')
    Assert-True (-not ($out.Equals($repo,[StringComparison]::OrdinalIgnoreCase) -or $out.StartsWith(($repo + [IO.Path]::DirectorySeparatorChar),[StringComparison]::OrdinalIgnoreCase))) 'EvidenceRoot must be outside the source repository.'
    Assert-True (-not (Test-Path -LiteralPath $out)) 'EvidenceRoot already exists; choose a new unique path, never overwrite prior evidence.'
    $evidence = Join-Path $out 'evidence'
    $staging = Join-Path $out 'isolated-worktree'
    New-Item -ItemType Directory -Force -Path $evidence | Out-Null
    $manifest.environment.platform = [Environment]::OSVersion.VersionString
    $manifest.environment.powershell = $PSVersionTable.PSVersion.ToString()
    $manifest.environment.user = [Environment]::UserName
    $manifest.environment.host = [Environment]::MachineName
    $manifest.environment.originalRepository = $repo
    $manifest.environment.externalEvidenceRoot = $out
    $manifest.environment.isolatedWorktree = $staging

    $statusBefore = @(Assert-GitClean $repo 'Original repository before acceptance')
    $manifest.preflight.originalStatusBefore = $statusBefore
    $top = [IO.Path]::GetFullPath((@(Git-Lines $repo @('rev-parse','--show-toplevel')) -join '').TrimEnd('\','/'))
    $repoNorm = [IO.Path]::GetFullPath($repo)
    Assert-True ($top.Equals($repoNorm,[StringComparison]::OrdinalIgnoreCase)) 'RepositoryPath must be the actual git worktree root.'
    $origin = (@(Git-Lines $repo @('remote','get-url','origin')) -join '').Trim()
    Assert-True ($origin -match '(?i)(?:github\.com[:/])vitoo16/Startup(?:\.git)?$') "Unexpected origin: $origin"
    $head = (@(Git-Lines $repo @('rev-parse','HEAD')) -join '').Trim()
    Assert-True ($head -ceq $approvedSha) "Exact-head mismatch. Expected $approvedSha, current checkout is $head. NO checkout/reset attempted."
    $branch = (@(Git-Lines $repo @('branch','--show-current')) -join '').Trim()
    $manifest.preflight.origin = $origin
    $manifest.preflight.head = $head
    $manifest.preflight.branch = $(if ($branch) { $branch } else { '(detached HEAD)' })
    $pv = Get-Content -LiteralPath (Join-Path $repo 'ProjectSettings/ProjectVersion.txt') -Raw
    Assert-True ($pv -match '(?m)^m_EditorVersion:\s*6000\.3\.25f1\s*$') 'Pinned ProjectVersion.txt editor version mismatch.'
    Assert-True ($pv -match '(?m)^m_EditorVersionWithRevision:\s*6000\.3\.25f1 \(e1dba0a9aba4\)\s*$') 'Pinned ProjectVersion.txt editor revision mismatch.'
    $manifest.preflight.projectVersion = "$approvedEditor ($approvedRevision)"
    $manifest.preflight.worktreeClean = $true

    $failedPhase = 'isolated worktree'
    $worktreeOut = @(& git -C $repo worktree add --detach $staging $approvedSha 2>&1)
    $worktreeExit = $LASTEXITCODE
    Save-Text 'worktree-add.log' ($worktreeOut -join "`n")
    Assert-True ($worktreeExit -eq 0) "Failed to create a clean external worktree ($worktreeExit). Existing checkout preserved."
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $staging 'Library'))) 'Isolated worktree already has Library; cannot prove fresh import.'
    Assert-GitClean $staging 'Isolated worktree before hydration' | Out-Null
    $stageHead = (@(Git-Lines $staging @('rev-parse','HEAD')) -join '').Trim()
    Assert-True ($stageHead -ceq $approvedSha) 'Isolated worktree SHA mismatch.'
    $manifest.preflight.isolatedHead = $stageHead

    $failedPhase = 'Git LFS and fonts'
    $lfsVersion = @(Git-Lines $staging @('lfs','version')) -join ' '
    $manifest.environment.gitLfs = $lfsVersion
    $lfsLog = Join-Path $evidence 'git-lfs-pull.log'
    & git -C $staging lfs pull *> $lfsLog
    $lfsExit = $LASTEXITCODE
    $manifest.preflight.gitLfsPullExit = $lfsExit
    Assert-True ($lfsExit -eq 0) "git lfs pull failed ($lfsExit). See git-lfs-pull.log"
    $requiredFonts = @('Assets/StartupLife/UI/Fonts/NotoSans-Regular.ttf', 'Assets/TextMesh Pro/Fonts/LiberationSans.ttf')
    foreach ($relative in $requiredFonts) {
        $file = Join-Path $staging $relative
        Assert-True (Test-Path -LiteralPath $file -PathType Leaf) "Missing required font: $relative"
        $pointerLines = @(Git-Lines $staging @('show',("HEAD:" + $relative)))
        $pointer = $pointerLines -join "`n"
        $match = [regex]::Match($pointer,'(?m)^oid sha256:([a-f0-9]{64})$')
        Assert-True $match.Success "No committed Git LFS SHA-256 oid for font: $relative"
        $expectedHash = $match.Groups[1].Value
        $actualHash = Get-Hash $file
        $size = (Get-Item -LiteralPath $file).Length
        Assert-True ($size -gt 1024) "Required font looks like an LFS pointer, not a binary: $relative"
        Assert-True ($actualHash -eq $expectedHash) "LFS font SHA-256 mismatch: $relative"
        $manifest.fonts += [ordered]@{ path=$relative; size=$size; lfsOid=$expectedHash; actualSha256=$actualHash; verified=$true }
    }
    $manifest.preflight.stageStatusAfterHydration = @(Assert-GitClean $staging 'Isolated worktree after LFS hydration')
    $manifest.integrity.criticalHashesBefore = Get-CriticalSourceHashes $staging

    $failedPhase = 'Unity environment'
    $cli = (Resolve-Path -LiteralPath $UnityCliPath).Path
    $editor = (Resolve-Path -LiteralPath $UnityEditorPath).Path
    Assert-True (Test-Path -LiteralPath $cli -PathType Leaf) 'Unity CLI executable missing.'
    Assert-True (Test-Path -LiteralPath $editor -PathType Leaf) 'Unity Editor executable missing.'
    $cliVersion = @(& $cli --version 2>&1)
    Assert-True ($LASTEXITCODE -eq 0) 'Unity CLI --version failed.'
    $manifest.environment.unityCliVersion = ($cliVersion -join ' ').Trim()
    $manifest.environment.unityCliExecutable = $cli
    $manifest.environment.unityEditorExecutable = $editor
    $installed = Invoke-CliJson $cli @('editors','--installed','--format','json')
    $editorEntries = @($installed.data | Where-Object { $_.version -eq $approvedEditor })
    Assert-True ($editorEntries.Count -ge 1) "Unity CLI does not list $approvedEditor as installed."
    $editorMatch = @($editorEntries | Where-Object {
        $_.location -and ([IO.Path]::GetFullPath([string]$_.location)).Equals([IO.Path]::GetFullPath($editor),[StringComparison]::OrdinalIgnoreCase)
    })
    Assert-True ($editorMatch.Count -ge 1) 'UnityEditorPath is not the pinned installed Editor reported by unity editors --installed.'
    $license = Invoke-CliJson $cli @('license','status','--format','json')
    $licenseActive = [bool]$license.data.active
    $manifest.environment.licenseActive = $licenseActive
    Assert-True $licenseActive 'Unity license is not active for noninteractive execution.'
    $probeLog = Join-Path $evidence 'unity-editor-version.log'
    $probeProc = Start-Process -FilePath $editor -ArgumentList '-version' -Wait -PassThru -NoNewWindow -RedirectStandardOutput $probeLog
    $versionExit = $probeProc.ExitCode
    $manifest.environment.editorVersionProbeExit = $versionExit
    Assert-True ($versionExit -eq 0) 'Unity Editor -version failed.'
    $probeText = Get-Content -LiteralPath $probeLog -Raw
    Assert-True ($probeText.Contains($approvedEditor)) 'Unity Editor binary does not report the approved version.'
    # The editor's complete revision is also verified from the fresh import log below.
    $manifest.environment.reportedEditorVersion = $probeText.Trim()
    if (Test-Path Env:STARTUP_LIFE_M7_EVIDENCE) {
        $manifest.environment.m7EvidenceOriginallySet = $true
        Remove-Item Env:STARTUP_LIFE_M7_EVIDENCE -ErrorAction SilentlyContinue
    } else { $manifest.environment.m7EvidenceOriginallySet = $false }
    Assert-True (-not (Test-Path Env:STARTUP_LIFE_M7_EVIDENCE)) 'STARTUP_LIFE_M7_EVIDENCE must be unset for -nographics tests.'
    $manifest.environment.m7EvidenceDuringTests = 'UNSET'

    $failedPhase = 'fresh import'
    $manifest.freshImport.executed = $true
    $importLog = Join-Path $evidence 'unity-fresh-import-editor.log'
    $importStd = Join-Path $evidence 'unity-fresh-import-console.log'
    $importArgs = @('-batchmode','-nographics','-quit','-projectPath',$staging,'-logFile',$importLog)
    $manifest.freshImport.executable = $editor
    $manifest.freshImport.arguments = $importArgs
    $manifest.freshImport.libraryAbsentBefore = (-not (Test-Path (Join-Path $staging 'Library')))
    Assert-True $manifest.freshImport.libraryAbsentBefore 'Fresh-import gate found an existing Library cache.'
    $psiImp = [System.Diagnostics.ProcessStartInfo]::new($editor, ($importArgs -join ' '))
    $psiImp.UseShellExecute = $false
    $pImp = [System.Diagnostics.Process]::Start($psiImp)
    $pImp.WaitForExit()
    $importExit = $pImp.ExitCode
    $manifest.freshImport.exitCode = $importExit
    $manifest.freshImport.editorLog = 'unity-fresh-import-editor.log'
    $manifest.freshImport.consoleLog = 'unity-fresh-import-console.log'
    Assert-True ($importExit -eq 0) "Unity fresh import failed (exit $importExit)."
    Assert-True (Test-Path -LiteralPath $importLog -PathType Leaf) 'Unity import succeeded but raw Editor log was not retained.'
    $importText = Get-Content -LiteralPath $importLog -Raw
    Assert-True ($importText.Contains($approvedEditor) -and $importText.Contains($approvedRevision)) 'Editor log does not demonstrate the full pinned 6000.3.25f1 (e1dba0a9aba4).'
    $compileWarns = @([regex]::Matches($importText,'(?im)\bwarning CS\d{4}\b'))
    $compileErrors = @([regex]::Matches($importText,'(?im)\berror CS\d{4}\b|script compilation failed|compilation failed|scripts have compiler errors'))
    $importErrors = @([regex]::Matches($importText,'(?im)asset import failed|failed to import (?:asset|package)|assetdatabase refresh failed|failed to import project'))
    $warningLines = @($importText -split "`r?`n" | Where-Object { $_ -match '(?i)\bwarn(?:ing)?\b' } | Select-Object -Unique)
    $otherErrorLines = @($importText -split "`r?`n" | Where-Object { $_ -match '(?i)(?:error importing|import error|exception during import|asset import exception)' } | Select-Object -Unique)
    $manifest.freshImport.compileWarnings = $compileWarns.Count
    $manifest.freshImport.compileErrors = $compileErrors.Count
    $manifest.freshImport.importFailures = $importErrors.Count
    $manifest.freshImport.otherWarningLines = $warningLines
    $manifest.freshImport.otherImportErrorLines = $otherErrorLines
    Assert-True ($compileWarns.Count -eq 0 -and $compileErrors.Count -eq 0 -and $importErrors.Count -eq 0 -and $otherErrorLines.Count -eq 0) 'Unity import log reports compilation warnings/errors or import failures. Review raw Editor log.'
    $unreviewedWarnings = @($warningLines | Where-Object { $_ -notmatch '(?i)Start importing .*Warning.*\.png' -and $_ -notmatch 'Unity\.ILPP\.Runner\.PostProcessingAssemblyLoadContext' })
    Assert-True ($unreviewedWarnings.Count -eq 0) 'Unity import logged warnings whose environmental/compiler significance is unreviewed. Exact lines are retained in the manifest; do not label a clean PASS without review.'
    $manifest.freshImport.success = $true

    $failedPhase = 'canonical Unity tests'
    $testDir = Join-Path $evidence 'unity-tests'
    New-Item -ItemType Directory -Force -Path $testDir | Out-Null
    $runner = Join-Path $staging 'scripts/Test-UnityHeadless.ps1'
    Assert-True (Test-Path -LiteralPath $runner -PathType Leaf) 'Canonical Test-UnityHeadless.ps1 not found at approved SHA.'
    $psExe = (Get-Command pwsh).Source
    $runnerArgs = @('-NoProfile','-NonInteractive','-File',$runner,'-Mode','All','-OutputDirectory',$testDir,'-UnityCliPath',$cli,'-TimeoutSeconds',"$TimeoutSeconds")
    $manifest.runner.executed = $true
    $manifest.runner.executable = $psExe
    $manifest.runner.arguments = $runnerArgs
    $manifest.runner.canonicalFile = 'scripts/Test-UnityHeadless.ps1'
    $manifest.runner.outputDirectory = $testDir
    $runnerLog = Join-Path $evidence 'canonical-runner-console.log'
    & $psExe @runnerArgs 2>&1 | Tee-Object -FilePath $runnerLog
    $runnerExit = $LASTEXITCODE
    $manifest.runner.exitCode = $runnerExit
    Assert-True ($runnerExit -eq 0) "Canonical Unity test runner failed (exit $runnerExit)."
    $runnerSummaryPath = Join-Path $testDir 'unity-headless-summary.json'
    Assert-True (Test-Path -LiteralPath $runnerSummaryPath -PathType Leaf) 'Canonical summary JSON is missing.'
    $runnerSummary = Get-Content -LiteralPath $runnerSummaryPath -Raw | ConvertFrom-Json
    Assert-True ($runnerSummary.requestedMode -eq 'All' -and [bool]$runnerSummary.success) 'Canonical runner did not record a successful -Mode All invocation.'
    Assert-True (@($runnerSummary.results).Count -eq 2) 'Canonical runner must contain both EditMode and PlayMode results.'
    foreach ($mode in @('EditMode','PlayMode')) {
        $entry = @($runnerSummary.results | Where-Object { $_.mode -eq $mode })
        Assert-True ($entry.Count -eq 1 -and $entry[0].exitCode -eq 0) "$mode was missing or returned nonzero exit code."
        $slug = $mode.ToLowerInvariant()
        $nunitPath = Join-Path $testDir "$slug-results.xml"
        $junitPath = Join-Path $testDir "$slug-results.junit.xml"
        $unityLog = Join-Path $testDir "$slug-unity-cli.log"
        foreach ($p in @($nunitPath,$junitPath,$unityLog)) { Assert-True (Test-Path -LiteralPath $p -PathType Leaf) "$mode required runtime evidence is missing: $p" }
        $nunit = Read-NUnit $nunitPath $mode
        $junit = Read-JUnit $junitPath $mode
        Assert-True ($nunit.counts.total -eq $junit.total) "$mode NUnit/JUnit test counts differ."
        Assert-True ($nunit.counts.failed -eq 0 -and $nunit.counts.skipped -eq 0 -and $nunit.counts.inconclusive -eq 0 -and $nunit.counts.passed -eq $nunit.counts.total) "$mode NUnit has failures, skips, inconclusive or missing passes."
        Assert-True ($nunit.failedSuiteCount -eq 0 -and $nunit.failedCaseCount -eq 0 -and $junit.failed -eq 0 -and $junit.skipped -eq 0) "$mode has a failed or skipped test/suite hidden in XML."
        $manifest.tests += [ordered]@{ mode=$mode; exitCode=[int]$entry[0].exitCode; nunitCounts=$nunit.counts; junitCounts=$junit; failedSuites=$nunit.failedSuiteCount; testIdentities=$nunit.identities; nunit=("unity-tests/$slug-results.xml"); junit=("unity-tests/$slug-results.junit.xml"); unityLog=("unity-tests/$slug-unity-cli.log") }
    }

    $failedPhase = 'post-run integrity'
    $manifest.integrity.isolatedStatusAfter = @(Assert-GitClean $staging 'Isolated runtime-tested worktree')
    $manifest.integrity.criticalHashesAfter = Get-CriticalSourceHashes $staging
    foreach ($relative in @($manifest.integrity.criticalHashesBefore.Keys)) {
        Assert-True ($manifest.integrity.criticalHashesBefore[$relative] -eq $manifest.integrity.criticalHashesAfter[$relative]) "Critical tracked file hash changed: $relative"
    }
    $manifest.integrity.originalStatusAfter = @(Assert-GitClean $repo 'Original checkout after acceptance')
    $actualHead = (@(Git-Lines $staging @('rev-parse','HEAD')) -join '').Trim()
    Assert-True ($actualHead -ceq $approvedSha) 'Tested SHA changed during acceptance.'
    $diffOutput = @(& git -C $staging diff --check 2>&1)
    $diffExit = $LASTEXITCODE
    $manifest.integrity.diffCheckExit = $diffExit
    Assert-True ($diffExit -eq 0) "Post-run git diff --check failed ($diffExit): $($diffOutput -join ' ')"
    $manifest.integrity.testedHead = $actualHead
    $manifest.integrity.success = $true
    $manifest.verdict = 'UNITY ACCEPTANCE PASSED — READY FOR ASTRA FINAL AUDIT'
} catch {
    $reason = $_.Exception.Message
    $manifest.findings += [ordered]@{ phase=$failedPhase; severity='BLOCKER'; message=$reason }
    if ($failedPhase -in @('fresh import','canonical Unity tests','post-run integrity')) {
        $manifest.verdict = 'UNITY ACCEPTANCE FAILED — CORRECTION REQUIRED'
    } else {
        $manifest.verdict = 'UNITY ACCEPTANCE BLOCKED — LOCAL EXECUTION REQUIRED'
    }
    Write-Warning "M9-T01 $($manifest.verdict): $reason"
} finally {
    if ($evidence -and (Test-Path -LiteralPath $evidence)) {
        if ($repo -and (Test-Path -LiteralPath $repo)) {
            try { $manifest.integrity.originalStatusAtFinalize = @(Git-Lines $repo @('status','--porcelain=v1','--untracked-files=all')) }
            catch { $manifest.findings += [ordered]@{phase='finalize'; severity='LOW'; message="Could not capture original git status: $($_.Exception.Message)"} }
        }
        if ($staging -and (Test-Path -LiteralPath $staging)) {
            try { $manifest.integrity.isolatedStatusAtFinalize = @(Git-Lines $staging @('status','--porcelain=v1','--untracked-files=all')) }
            catch { $manifest.findings += [ordered]@{phase='finalize'; severity='LOW'; message="Could not capture isolated worktree status: $($_.Exception.Message)"} }
        }
        $manifest.generatedUtc = [DateTime]::UtcNow.ToString('o')
        $hashList = [System.Collections.Generic.List[object]]::new()
        foreach ($f in Get-ChildItem -LiteralPath $evidence -Recurse -File | Sort-Object FullName) {
            $relative = [IO.Path]::GetRelativePath($evidence,$f.FullName).Replace('\','/')
            if ($relative -in @('manifest.json','SHA256SUMS.txt')) { continue }
            $hashList.Add([ordered]@{ path=$relative; sha256=(Get-Hash $f.FullName); size=$f.Length })
        }
        $manifest.evidenceFiles = $hashList.ToArray()
        $manifestPath = Join-Path $evidence 'manifest.json'
        $manifest | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath $manifestPath -Encoding utf8
        $shaLines = @($hashList | ForEach-Object { "$($_.sha256)  $($_.path)" })
        $shaLines += "$(Get-Hash $manifestPath)  manifest.json"
        Set-Content -LiteralPath (Join-Path $evidence 'SHA256SUMS.txt') -Value $shaLines -Encoding utf8
        $archive = Join-Path (Split-Path -Parent $evidence) 'M9-T01-unity-evidence.zip'
        Compress-Archive -LiteralPath $evidence -DestinationPath $archive -CompressionLevel Optimal
        Set-Content -LiteralPath ($archive + '.sha256') -Value ("$(Get-Hash $archive)  $(Split-Path $archive -Leaf)") -Encoding utf8
        Write-Output "Evidence directory: $evidence"
        Write-Output "Evidence package: $archive"
        Write-Output "Manifest: $manifestPath"
        Write-Output "VERDICT: $($manifest.verdict)"
    }
}
if ($manifest.verdict -ne 'UNITY ACCEPTANCE PASSED — READY FOR ASTRA FINAL AUDIT') { exit 1 }
exit 0
