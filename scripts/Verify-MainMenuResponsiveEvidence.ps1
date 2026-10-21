[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Directory,
    [string]$ReportPath
)

$ErrorActionPreference = 'Stop'
# Share the same PNG signature/IHDR/SHA-256 validation as Creator evidence;
# only the capture manifest (3 screens x 3 menu pages) differs.
& (Join-Path $PSScriptRoot 'Verify-CreatorResponsiveEvidence.ps1') `
    -Directory $Directory -ReportPath $ReportPath -Mode MainMenu
if ($LASTEXITCODE) { exit $LASTEXITCODE }
