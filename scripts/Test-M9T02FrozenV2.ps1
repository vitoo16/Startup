param(
    [string]$ReportPath = 'docs/evidence/M9-T02/frozen-v2-source/checksums-runtime.json'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not [IO.Path]::IsPathRooted($ReportPath)) { $ReportPath = Join-Path $root $ReportPath }
$archive = 'docs/evidence/M9-T02/frozen-v2-source/'
# Baseline Git blob SHA-1 identifiers are the immutable source-compatibility authority.
# Produce SHA-256 proofs from actual checked-in frozen bytes during CI.
$expected = [ordered]@{
    'Business.cs.txt' = '505e036003431fa31dab749311d4deffa098f667'
    'ContentCatalogSource.cs.txt' = '3cd16f2c1b5023d10e52f96f27bc68c6afc37bd4'
    'Definitions.cs.txt' = '55dead577b734e6d4497984160132dd01eb00242'
    'FirstPlayableContent.asset.yaml' = 'fe78dc29d590ecacca6056eba07a7b2cf4605557'
    'FirstPlayableContentTemplate.cs.txt' = '7bb5a829c5d1647b7df123249dd8a1233bda231a'
    'HistoricalV1Codec.cs.txt' = '3c407d78c3b344ee042408fc04fce4b2119dac15'
    'JsonSaveSerializer.cs.txt' = '1733cdf32e9779dd5c489fd4c6a612eec2e9de28'
    'SimulationEngine.cs.txt' = '4adbbce053db01ac5ee161e74838349740899f4b'
}
$expectedSha256 = [ordered]@{
    'Business.cs.txt' = 'c76306ddbc1a1c20418f0366a3323fb7cf84212e1ffaa12e0e69cd2944242a15'
    'ContentCatalogSource.cs.txt' = 'b776a8f8ed04fa49422cf378931b31aa74d0114bc99cc4a066a328e4a79bdd9a'
    'Definitions.cs.txt' = '35b70b7229029a144a5ec110a57bd1f985d6c274acfd49c66df843f2b341b8c0'
    'FirstPlayableContent.asset.yaml' = '1f637ade3b8cdb7334deab7cef75db5c25751f24d7654c6577d899cc8b999ae0'
    'FirstPlayableContentTemplate.cs.txt' = '927065c9559b6b223112a5ff39ccdde49794f160e59e2022300cc8bd9b207a3e'
    'HistoricalV1Codec.cs.txt' = '487507084edc90bfb17b7c2be23c154394be7d7d986beb64d009859715df7fc4'
    'JsonSaveSerializer.cs.txt' = 'ac4d8def1e01841ba8cd65a76f122962993f228cb483c72db7f390b8381450d4'
    'SimulationEngine.cs.txt' = '871d51d694e1ad5451a08d1eb120f3ccf9ddff7e42d0b4eb247250776ad7a0e0'
}
# Reconstructed archived evaluator may differ only by the two class identifiers.
# Any other content mutation is a hard error, even when gameplay CI still passes.
$frozenEvaluatorPath = Join-Path $root 'Assets/StartupLife/Scripts/Simulation/ArchivedV2SimulationEngine.cs'
$frozenSourcePath = Join-Path $root ($archive + 'SimulationEngine.cs.txt')
$frozenRuntime = [IO.File]::ReadAllText($frozenEvaluatorPath).Replace("`r`n", "`n")
$frozenBaseline = [IO.File]::ReadAllText($frozenSourcePath).Replace("`r`n", "`n")
$begin = $frozenRuntime.IndexOf('#nullable enable', [StringComparison]::Ordinal)
if ($begin -lt 0) { throw 'Frozen v2 evaluator missing original C# source marker' }
$renamed = [regex]::Replace($frozenBaseline, '\bSimulationRestoreValidator\b','ArchivedV2RestoreValidator')
$renamed = [regex]::Replace($renamed, '\bSimulationEngine\b','ArchivedV2SimulationEngine')
if (-not [string]::Equals($frozenRuntime.Substring($begin),$renamed,[StringComparison]::Ordinal)) {
    throw 'Frozen v2 evaluator changed relative to byte-archived baseline beyond identifier renames'
}
Write-Output 'PASS frozen v2 runtime evaluator exactly matches original semantics (identifier renames only)'

$results = [System.Collections.Generic.List[object]]::new()
foreach ($name in $expected.Keys) {
    $relative = $archive + $name
    $absolute = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) {
        throw "Missing historical frozen source $relative"
    }
    $actualBlob = (& git -C $root hash-object -- $relative).Trim()
    if ($LASTEXITCODE -ne 0 -or $actualBlob -cne $expected[$name]) {
        throw "Historical source mismatch: $relative expected $($expected[$name]) got $actualBlob"
    }
    $sha256 = (Get-FileHash -LiteralPath $absolute -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($sha256 -cne $expectedSha256[$name]) { throw "Historical SHA256 mismatch: $relative" }
    $results.Add([ordered]@{ path = $relative; gitBlobSha1 = $actualBlob; sha256 = $sha256 })
    Write-Output ("PASS frozen v2 source {0} gitSHA1={1} SHA256={2}" -f $name, $actualBlob, $sha256)
}
$directory = Split-Path -Parent $ReportPath
if (-not (Test-Path $directory)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
[ordered]@{
    contract = 'M9-T02-TA-1.0-FINAL'
    baseline = '051a931410f2dec0ec51906f7f06191a0deaf77c'
    files = $results
    passed = $results.Count
    failed = 0
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ReportPath -Encoding utf8
