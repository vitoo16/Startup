# Startup Life — Main Menu real Unity portrait evidence

**Purpose:** Verify the three scrollable Main Menu pages on real Unity-rendered portrait dimensions. **This is not automatic visual approval.** Static tests using synthetic PNGs only verify that the validator rejects missing/wrong captures.

## Prerequisites

- Pull the exact commit being reviewed only with a clean working tree.
- Unity 6000.3.25f1 + graphical Editor / renderer. Close any other Editor/player sessions sharing the same `persistentDataPath`.
- Do not confirm **Trò chơi mới** on a valuable save for this evidence. The responsive PlayMode matrix visits pages directly without changing game state; other PlayMode tests in the complete suite may manipulate checkpoints, so keep an independent backup of progress before running the entire suite.
- Use the existing `FirstPlayable.unity`; Unity may still need the project's scene repair/migration command if the local scene is stale.

## PowerShell 7 (Windows)

From repo root in a PowerShell session with a working `unity` CLI:

```powershell
cd C:\Users\viett\Startup
$env:STARTUP_LIFE_MAIN_MENU_RESPONSIVE_EVIDENCE = "$env:TEMP\StartupLife\main-menu-evidence"
# Use a fresh directory; stale files are not accepted as new QA evidence.
if (Test-Path $env:STARTUP_LIFE_MAIN_MENU_RESPONSIVE_EVIDENCE) {
    throw "Existing evidence directory found. Move or rename it before rerunning."
}
pwsh -NoProfile -File .\scripts\Test-UnityHeadless.ps1 `
    -Mode PlayMode -EnableGraphics `
    -OutputDirectory "$env:TEMP\StartupLife\unity-menu-tests"
pwsh -NoProfile -File .\scripts\Verify-MainMenuResponsiveEvidence.ps1 `
    -Directory $env:STARTUP_LIFE_MAIN_MENU_RESPONSIVE_EVIDENCE `
    -ReportPath "$env:TEMP\StartupLife\main-menu-evidence-report.json"
```

The suite uses `Screen.SetResolution` and refuses to certify unsupported resolutions. It can capture exactly **nine** PNGs: `main-menu-<width>x<height>-<page>.png`, with `width x height` in `360x640`, `390x844`, `430x932`; each of `StartupMainMenuHome`, `StartupMainMenuSettings`, `StartupMainMenuNewGameConfirmation` must exist for all sizes.

The verifier checks PNG signatures/IHDR, expected dimensions, filenames, count and SHA-256. It does **not** prove PNGs originated from Unity, actual font appearance, touch feel, accessibility scaling, smoothness or device compatibility. Review screenshots visually, then verify the same flow manually on at least one target Android device.

### Acceptance record

Review `main-menu-evidence-report.json`, Unity PlayMode NUnit XML, the Unity Editor console, the 9 PNGs and portrait touch interactions. Mark **FAIL** on cropped warning/confirmation actions, broken scroll, untranslated Vietnamese, text truncation or inaccessible buttons. Keep `VISUAL_PENDING` if any image/device evidence is missing.

**Scope:** menu UI/evidence tooling only. No change to save serialization, gameplay time, approved sprite registry or C03 Idle V3 owner-approved motion direction.

## 2026-10-21 Unity compiler recovery (before screenshot capture)

Actual Windows Unity 6000.3.25f1 log reported `CS0234` in `UiPanelReveal.cs` on `Application.isPlaying`. The game's `StartupLife.Application` namespace shadowed `UnityEngine.Application`; source now explicitly uses `UnityEngine.Application.isPlaying`. Editor diagnostics and mobile baseline helpers likewise use fully qualified Unity Application properties. This was **a source compile failure**, not a failed visual or gameplay assertion.

- **Skills:** startup-life-session-orchestrator, unity-cli, ui-ugui; active production roadmap and project workflow read.
- **Files:** `UiPanelReveal.cs`, `MobileBaselineBuilder.cs`, `StartupLifeLocalSaveDiagnostics.cs`, `tools/UiPanelRevealCompileChecks/*`, `tools/tests/test_presentation_motion_source.py`, `engine-free-ci.yml`, this document.
- **CI acceptance:** Roslyn must compile the real `UiPanelReveal.cs` against a fixture intentionally declaring `StartupLife.Application`; reduced-motion and outside-Play paths must pass. This is **not** full Unity compilation.
- **Unity acceptance:** after updating HEAD, first open Editor and wait for C# compilation; ensure Console has no errors. Only then rerun graphical PlayMode matrix. Exact-head CI and Unity results must be recorded separately.
- **Visual evidence:** none created in this fix. UI visual/device approval remains pending.
- **Save impact:** none. No save or state changes.
- **Known limits:** the compile check only covers the linked reveal source; Unity still compiles the full project and may expose independent errors on the next run.

