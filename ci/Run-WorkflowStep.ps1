param([Parameter(Mandatory)][string]$Step)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$items = Get-Content (Join-Path $PSScriptRoot 'workflow-steps.json') -Raw | ConvertFrom-Json
$item = @($items | Where-Object { $_.id -eq $Step })
if ($item.Count -ne 1) { throw "Unknown workflow step: $Step" }
$item = $item[0]
Write-Host "Executing $($item.workflow) :: $($item.title)"
if (-not $env:RUNNER_TEMP) { throw 'RUNNER_TEMP is required.' }
New-Item -ItemType Directory -Force -Path $env:RUNNER_TEMP | Out-Null
if ($item.shell -eq 'pwsh') {
  $temp = Join-Path $env:RUNNER_TEMP "step-$Step.ps1"
  Set-Content -LiteralPath $temp -Encoding utf8 -Value @('$ErrorActionPreference = "Stop"', '$PSNativeCommandUseErrorActionPreference = $true', $item.run)
  & pwsh -NoProfile -NonInteractive -File $temp
  if ($LASTEXITCODE -ne 0) { throw "PowerShell step failed: $Step" }
}
elseif ($item.shell -eq 'bash') {
  $lines = @($item.run -split "\r?\n")
  for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i].Trim()
    if (-not $line -or $line.StartsWith('#')) { continue }
    if ($line -eq "python3 - <<'PY'") {
      $source = [System.Collections.Generic.List[string]]::new()
      $foundEnd = $false
      while (++$i -lt $lines.Count) {
        if ($lines[$i].Trim() -eq 'PY') { $foundEnd = $true; break }
        $source.Add($lines[$i])
      }
      if (-not $foundEnd) { throw "Unterminated heredoc: $Step" }
      $pyFile = Join-Path $env:RUNNER_TEMP "assert-$Step.py"
      [IO.File]::WriteAllText($pyFile, ($source -join [Environment]::NewLine), [Text.Encoding]::UTF8)
      & python $pyFile
    } else {
      $line = $line -replace '\bpython3\b', 'python'
      $line = $line.Replace('$' + '{RUNNER_TEMP}', '$env:RUNNER_TEMP').Replace('$RUNNER_TEMP', '$env:RUNNER_TEMP')
      & ([scriptblock]::Create($line))
    }
    if ($LASTEXITCODE -ne 0) { throw "Workflow step failed with exit $LASTEXITCODE : $Step" }
  }
} else { throw "Unsupported shell: $($item.shell)" }
