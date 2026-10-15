[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Directory,
    [string]$ReportPath
)

$ErrorActionPreference = 'Stop'
$Directory = [IO.Path]::GetFullPath($Directory)
if (-not (Test-Path -LiteralPath $Directory -PathType Container)) {
    throw "Unity native screenshot directory missing: $Directory"
}
$dimensions = @(
    @{ width = 360; height = 640 },
    @{ width = 390; height = 844 },
    @{ width = 430; height = 932 }
)
$pages = @('CreatorIdentityPage', 'CreatorLookPage', 'CreatorWardrobePage', 'CreatorTraitsPage')
$scales = @(100, 150, 200)
$expected = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$inspected = [System.Collections.Generic.List[object]]::new()

foreach ($d in $dimensions) {
    foreach ($scale in $scales) {
        foreach ($page in $pages) {
            $leaf = "creator-$($d.width)x$($d.height)-text$scale-$page.png"
            [void]$expected.Add($leaf)
            $path = Join-Path $Directory $leaf
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
                throw "Missing real Unity creator evidence: $leaf"
            }
            $data = [IO.File]::ReadAllBytes($path)
            if ($data.Length -lt 128) { throw "Screenshot is too small: $leaf" }
            $signature = [byte[]]@(137,80,78,71,13,10,26,10)
            for ($index=0; $index -lt $signature.Length; $index++) {
                if ($data[$index] -ne $signature[$index]) {
                    throw "Invalid PNG signature: $leaf"
                }
            }
            # PNG IHDR dimensions are big endian, bytes 16..23.
            $actualWidth = ([uint32]$data[16] -shl 24) -bor
                ([uint32]$data[17] -shl 16) -bor
                ([uint32]$data[18] -shl 8) -bor [uint32]$data[19]
            $actualHeight = ([uint32]$data[20] -shl 24) -bor
                ([uint32]$data[21] -shl 16) -bor
                ([uint32]$data[22] -shl 8) -bor [uint32]$data[23]
            if ($actualWidth -ne $d.width -or $actualHeight -ne $d.height) {
                throw "Wrong screenshot dimensions: $leaf is $actualWidth x $actualHeight"
            }
            $inspected.Add([ordered]@{
                file = $leaf
                width = $actualWidth
                height = $actualHeight
                sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
            })
        }
    }
}
$actual = @(Get-ChildItem -LiteralPath $Directory -Filter 'creator-*.png' -File)
if ($actual.Count -ne $expected.Count) {
    throw "Unexpected count of responsive captures: expected $($expected.Count), found $($actual.Count)."
}
foreach ($file in $actual) {
    if (-not $expected.Contains($file.Name)) {
        throw "Unknown responsive screenshot: $($file.Name)"
    }
}
if ($inspected.Count -ne 36) { throw "Not all 36 configurations produced valid PNGs." }

$report = [ordered]@{
    suite = 'Startup Life Character Creator native screenshot provenance'
    success = $true
    count = $inspected.Count
    note = 'PNG native-size/byte integrity only. Human layout and physical device review remain required.'
    screenshots = $inspected
}
if ($ReportPath) {
    $directory = Split-Path -Parent ([IO.Path]::GetFullPath($ReportPath))
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ReportPath -Encoding utf8
}
Write-Output "PASS 36 Unity native-size responsive PNG files; human/device QA still required."
