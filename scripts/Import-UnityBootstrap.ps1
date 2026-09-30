param(
    [string]$BootstrapPath = 'C:/Users/viett/.codex/tooling/startup-life/UnityBootstrap-6000.3.25f1'
)
$ErrorActionPreference = 'Stop'
$workspacePath = Split-Path $PSScriptRoot -Parent
$sourceVersion = Get-Content -LiteralPath (Join-Path $BootstrapPath 'ProjectSettings/ProjectVersion.txt') -Raw
if ($sourceVersion -notmatch 'm_EditorVersion: 6000\.3\.25f1\b') { throw 'Bootstrap Editor version differs from the project pin.' }
$copyPlan = @()
foreach ($folderName in @('Assets', 'Packages', 'ProjectSettings')) {
    $sourceRoot = Join-Path $BootstrapPath $folderName
    foreach ($sourceFile in Get-ChildItem -LiteralPath $sourceRoot -File -Recurse) {
        $relativePath = [IO.Path]::GetRelativePath($BootstrapPath, $sourceFile.FullName)
        $destinationPath = Join-Path $workspacePath $relativePath
        if (Test-Path -LiteralPath $destinationPath) {
            $sourceHash = (Get-FileHash -LiteralPath $sourceFile.FullName).Hash
            $destinationHash = (Get-FileHash -LiteralPath $destinationPath).Hash
            if ($sourceHash -ne $destinationHash) { throw "Refusing to overwrite existing workspace file: $relativePath" }
        } else {
            $copyPlan += [pscustomobject]@{Source=$sourceFile.FullName; Destination=$destinationPath}
        }
    }
}
# Validate every collision before copying anything. Documentation and authored C# stay in place.
foreach ($item in $copyPlan) {
    New-Item -ItemType Directory -Force -Path (Split-Path $item.Destination -Parent) | Out-Null
    Copy-Item -LiteralPath $item.Source -Destination $item.Destination
}
Write-Output "Imported $($copyPlan.Count) Editor-generated project files; no existing files overwritten."
