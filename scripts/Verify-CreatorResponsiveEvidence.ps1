[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Directory,
    [string]$ReportPath,
    [ValidateSet('Creator', 'MainMenu')]
    [string]$Mode = 'Creator'
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
$pages = if ($Mode -eq 'Creator') {
    @('CreatorIdentityPage', 'CreatorLookPage', 'CreatorWardrobePage', 'CreatorTraitsPage')
} else {
    @('StartupMainMenuHome', 'StartupMainMenuSettings', 'StartupMainMenuNewGameConfirmation')
}
$scales = @(100, 150, 200)
if ($Mode -eq 'MainMenu') { $scales = @(0) }
$prefix = if ($Mode -eq 'Creator') { 'creator-' } else { 'main-menu-' }
$expectedCount = $dimensions.Count * $scales.Count * $pages.Count
$expected = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$inspected = [System.Collections.Generic.List[object]]::new()

foreach ($d in $dimensions) {
    foreach ($scale in $scales) {
        foreach ($page in $pages) {
            $leaf = if ($Mode -eq 'Creator') {
                "creator-$($d.width)x$($d.height)-text$scale-$page.png"
            } else {
                "main-menu-$($d.width)x$($d.height)-$page.png"
            }
            [void]$expected.Add($leaf)
            $path = Join-Path $Directory $leaf
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
                throw "Missing real Unity $Mode evidence: $leaf"
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
$actual = @(Get-ChildItem -LiteralPath $Directory -Filter "${prefix}*.png" -File)
if ($actual.Count -ne $expected.Count) {
    throw "Unexpected count of responsive captures: expected $($expected.Count), found $($actual.Count)."
}
foreach ($file in $actual) {
    if (-not $expected.Contains($file.Name)) {
        throw "Unknown responsive screenshot: $($file.Name)"
    }
}
if ($Mode -eq 'Creator') {
    if ($inspected.Count -ne 36) { throw "Not all 36 configurations produced valid PNGs." }
} elseif ($inspected.Count -ne $expectedCount) {
    throw "Not all $expectedCount $Mode configurations produced valid PNGs."
}

$suiteName = if ($Mode -eq 'Creator') {
    'Startup Life Character Creator native screenshot provenance'
} else {
    'Startup Life Main Menu native screenshot provenance'
}
$report = [ordered]@{
    suite = $suiteName
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
Write-Output "PASS $expectedCount Unity native-size $Mode responsive PNG files; human/device QA still required."
