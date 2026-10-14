[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$validator = Join-Path $repositoryRoot 'scripts/Read-UnityTestVerdict.ps1'
$folder = Join-Path ([IO.Path]::GetTempPath()) ('startup-unity-report-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$nunitFile = Join-Path $folder 'nunit.xml'
$junitFile = Join-Path $folder 'junit.xml'
$verified = 0

function Write-Reports {
    param(
        [int]$Total = 2,
        [int]$Passed = 2,
        [int]$Failed = 0,
        [int]$Skipped = 0,
        [int]$Inconclusive = 0,
        [string]$Result = 'Passed',
        [int]$JUnitTotal = 2,
        [int]$JUnitFailed = 0,
        [int]$JUnitSkipped = 0,
        [int]$Cases = 2
    )
    [IO.File]::WriteAllText($nunitFile,
        "<test-run total='$Total' passed='$Passed' failed='$Failed' skipped='$Skipped' inconclusive='$Inconclusive' result='$Result'><test-suite /></test-run>")
    $testcases = (1..$Cases | ForEach-Object { "<testcase name='case-$_' />" }) -join ''
    [IO.File]::WriteAllText($junitFile,
        "<testsuites><testsuite tests='$JUnitTotal' failures='$JUnitFailed' errors='0' skipped='$JUnitSkipped'>$testcases</testsuite></testsuites>")
}

function Assert-Rejected {
    param([string]$Description)
    $rejected = $false
    try { $null = & $validator -NUnitPath $nunitFile -JUnitPath $junitFile }
    catch { $rejected = $true }
    if (-not $rejected) { throw "Expected report rejection: $Description" }
    $script:verified++
}

try {
    Write-Reports
    $passing = & $validator -NUnitPath $nunitFile -JUnitPath $junitFile
    if (-not $passing.reportValidated -or $passing.total -ne 2 -or $passing.passed -ne 2) {
        throw 'Matching non-empty Unity reports must pass.'
    }
    $verified++

    Write-Reports -Total 0 -Passed 0 -JUnitTotal 0 -Cases 0
    Assert-Rejected 'zero discovered tests'

    Write-Reports -Total 2 -Passed 1 -Failed 1 -Result 'Failed'
    Assert-Rejected 'NUnit test failure'

    Write-Reports -Total 2 -Passed 1 -Skipped 1 -Result 'Passed'
    Assert-Rejected 'skipped NUnit tests'

    Write-Reports -JUnitTotal 1
    Assert-Rejected 'JUnit/NUnit count disagreement'

    Write-Reports -Cases 1
    Assert-Rejected 'truncated JUnit testcase list'

    Write-Reports -JUnitSkipped 1
    Assert-Rejected 'skipped JUnit cases'

    Write-Reports
    [IO.File]::WriteAllText($nunitFile, '<broken')
    Assert-Rejected 'malformed NUnit XML'

    Write-Reports
    Remove-Item -LiteralPath $junitFile -Force
    Assert-Rejected 'missing JUnit report'
}
finally {
    Remove-Item -LiteralPath $folder -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Output "Unity test report parser: $verified/9 regression cases passed (engine-free; no Unity execution)."
