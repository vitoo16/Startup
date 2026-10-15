#!/usr/bin/env python3
"""Graphical Unity runner + screenshot validation contract (not Unity proof)."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
CLI=Path("scripts/Test-UnityHeadless.ps1")
WORKFLOW=Path(".github/workflows/unity-runtime-smoke.yml")
VERIFY=Path("scripts/Verify-CreatorResponsiveEvidence.ps1")

def check(root=ROOT):
    root=Path(root)
    cli=(root/CLI).read_text(encoding="utf-8")
    workflow=(root/WORKFLOW).read_text(encoding="utf-8")
    validator=(root/VERIFY).read_text(encoding="utf-8")
    required_cli=[
        "[switch]$EnableGraphics",
        "$testMode -eq 'PlayMode' -and $EnableGraphics.IsPresent",
        "if (-not $graphicalPlayMode)",
        "$arguments.Add('-nographics')",
        "graphicalPlayModeRequested = $graphicalPlayMode",
        "enableGraphics = $EnableGraphics.IsPresent",
        "[switch]$PrepareFirstPlayableScene",
        "StartupLife.Editor.FirstPlayableRuntimeGate.BuildAndVerify",
        "Repaired AND reopened/verified first playable Character Creator."
    ]
    for item in required_cli:
        if item not in cli: raise AssertionError("Unity CLI graphical contract: "+item)
    required_workflow=[
        "STARTUP_LIFE_CREATOR_RESPONSIVE_EVIDENCE:",
        "Remove-Item -LiteralPath $dir -Recurse -Force",
        "-EnableGraphics",
        "-PrepareFirstPlayableScene",
        "Verify-CreatorResponsiveEvidence.ps1",
        "Upload Android Unity evidence",
        "Upload iOS Unity evidence"
    ]
    for item in required_workflow:
        if item not in workflow: raise AssertionError("Missing workflow gating: "+item)
    if workflow.count("Verify-CreatorResponsiveEvidence.ps1")!=2:
        raise AssertionError("Both graphical runner routes must validate captures")
    for s in ["@('CreatorIdentityPage', 'CreatorLookPage', 'CreatorWardrobePage', 'CreatorTraitsPage')",
              "$scales = @(100, 150, 200)",
              "width = 360; height = 640", "width = 390; height = 844",
              "width = 430; height = 932", "Invalid PNG signature",
              "Wrong screenshot dimensions", "Get-FileHash",
              "if ($inspected.Count -ne 36)"]:
        if s not in validator: raise AssertionError("Missing strict PNG evidence gate: "+s)
    return 36

if __name__ == '__main__':
    print("PASS graphical Unity CLI and 36-screenshot contract:",check(),
          "(Unity graphics/device not executed)")
