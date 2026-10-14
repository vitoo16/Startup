[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$NUnitPath,
    [Parameter(Mandatory)][string]$JUnitPath
)

$ErrorActionPreference = 'Stop'

function Read-TestXml {
    param([string]$Path, [string]$Label)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label report is missing: $Path"
    }
    $document = [System.Xml.XmlDocument]::new()
    $document.XmlResolver = $null
    try {
        $document.Load($Path)
    }
    catch {
        throw "$Label report is not valid XML: $($_.Exception.Message)"
    }
    if ($null -ne $document.DocumentType) {
        throw "$Label report may not contain a DOCTYPE."
    }
    return $document
}

function Read-Count {
    param(
        [System.Xml.XmlElement]$Element,
        [string]$Attribute,
        [bool]$Optional = $false
    )
    if (-not $Element.HasAttribute($Attribute)) {
        if ($Optional) { return 0 }
        throw "Test report missing count '$Attribute'."
    }
    [int]$count = 0
    $raw = $Element.GetAttribute($Attribute)
    if (-not [int]::TryParse($raw, [ref]$count) -or $count -lt 0) {
        throw "Test report has invalid count '$Attribute': $raw"
    }
    return $count
}

$nunit = Read-TestXml $NUnitPath 'NUnit'
$nunitRoot = $nunit.DocumentElement
if ($null -eq $nunitRoot -or $nunitRoot.LocalName -ne 'test-run') {
    throw 'NUnit report must have a test-run root (NUnit 3).'
}
$total = Read-Count $nunitRoot 'total'
$passed = Read-Count $nunitRoot 'passed'
$failed = Read-Count $nunitRoot 'failed'
$skipped = Read-Count $nunitRoot 'skipped'
$inconclusive = Read-Count $nunitRoot 'inconclusive'
if ($total -le 0 -or $passed -ne $total -or $failed -ne 0 -or
    $skipped -ne 0 -or $inconclusive -ne 0 -or
    $nunitRoot.GetAttribute('result') -ne 'Passed') {
    throw "NUnit has no complete passing test verdict: total=$total passed=$passed failed=$failed skipped=$skipped inconclusive=$inconclusive."
}

$junit = Read-TestXml $JUnitPath 'JUnit'
$junitRoot = $junit.DocumentElement
if ($null -eq $junitRoot) { throw 'JUnit report root is missing.' }
$suites = @()
if ($junitRoot.LocalName -eq 'testsuite') {
    $suites = @($junitRoot)
}
elseif ($junitRoot.LocalName -eq 'testsuites') {
    $suites = @($junitRoot.SelectNodes('./testsuite'))
}
else {
    throw 'JUnit report must have testsuite or testsuites root.'
}
if ($suites.Count -eq 0) { throw 'JUnit report contains no suites.' }

$junitTotal = 0
$junitFailed = 0
$junitErrors = 0
$junitSkipped = 0
foreach ($suite in $suites) {
    $junitTotal += Read-Count $suite 'tests'
    $junitFailed += Read-Count $suite 'failures'
    $junitErrors += Read-Count $suite 'errors' $true
    $junitSkipped += Read-Count $suite 'skipped' $true
}
$caseCount = $junit.SelectNodes('//testcase').Count
$errorCaseCount = $junit.SelectNodes('//testcase[failure or error or skipped]').Count
if ($junitTotal -ne $total -or $caseCount -ne $junitTotal -or
    $junitFailed -ne 0 -or $junitErrors -ne 0 -or
    $junitSkipped -ne 0 -or $errorCaseCount -ne 0) {
    throw "JUnit does not match the complete NUnit PASS: tests=$junitTotal cases=$caseCount failures=$junitFailed errors=$junitErrors skipped=$junitSkipped NUnit=$total."
}

[pscustomobject]@{
    total = $total
    passed = $passed
    failed = $failed
    skipped = $skipped
    inconclusive = $inconclusive
    junitCases = $caseCount
    reportValidated = $true
}
