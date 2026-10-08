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
