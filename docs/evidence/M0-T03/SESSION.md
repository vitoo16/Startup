# M0-T03 — Unity project and mobile baseline

Date: September 30, 2026. Status: in progress, acceptance incomplete.

Owner model: primary Codex; explicit fallback after GPT-5.6 Sol account usage limits. Astra retains architecture/audit ownership.

Skills used: startup-life-session-orchestrator, new-unity-project, unity-cli, unity-package-management, unity-project-setup, unity-mcp-bridge, ui, ui-ugui, unity-ui-designer, vietnam-art-direction, localization, unity-localization, unity-qa-release.

Acceptance criteria: real Unity 6000.3 patch; resolved compatible packages; clean compilation; Universal 2D, portrait, uGUI/TMP Vietnamese glyphs and safe area; Editor-generated metadata and retained package lock. No task checkbox closes before visual/compile evidence.

## Verified foundation

- Installed Editor: `6000.3.25f1` at `C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe`. Active license; tooling gate 39/39 passes.
- Created an external temporary project with the Editor's documented `-createProject` and `-cloneFromTemplate` options. The bundled archive identifies `com.unity.template.universal-2d@6.1.6`; initial creation completed successfully, exit 0.
- Resolved packages through `UnityEditor.PackageManager.Client.AddAndRemove`: URP 17.3.0, 2D Animation 13.0.6, PSD Importer 12.0.2, Input System 1.20.0, Test Framework 1.6.0, uGUI 2.0.0, Localization 1.5.8. Removed Multiplayer Center, collaboration proxy and Visual Scripting from the template.
- Imported 49 Editor-generated files into the workspace. The import script checks every collision before copying and refuses mismatched existing files. Existing documents and gameplay C# were retained. Library/cache files were not copied.
- Official CLI installed `com.unity.pipeline@0.8.0-exp.1`. Connection and compilation-reconnection checks remain pending.
- Noto Sans Regular is sourced from the Noto repository at commit `ffebf8c1ee449e544955a7e813c54f9b73848eac`, with its SIL Open Font License retained beside the font. No operating-system font was redistributed.

## Locality contract

Purpose: Vietnamese portrait layout and glyph test, development baseline only.
Context: fictional contemporary urban district, An Bình.
Anchors: Vietnamese interface, fictional district name, everyday work/study wording.
Prohibited stereotypes: iconic costume, lantern/old-town styling, generic Asian signage.
Text strategy: authored Localization keys and TMP; no text baked into imagery.
Palette: exploratory matte cream and green, pending Golden art approval.

Tests: external template import and package resolution succeeded; .NET backend rerun 35/35, zero build errors/warnings. Workspace Unity compilation/visual tests are being run. An initial workspace compile found the separate Universal 2D runtime assembly reference missing from the baseline builder; corrected using the installed package's actual asmdef.

Visual evidence: pending. No device screenshot or completed playable loop is claimed.

Save impact: none. Foundation UI does not read/write simulation saves; existing v1 remains provisional.

Known limitations: platform modules failed with `INSTALL_LOCK_CONFLICT`; no Mac/iPhone route available. Physical Android QA unverified. Full nine-assembly architecture and playable simulation integration are not yet accepted.

Files changed: generated `Assets/Scenes`, `Assets/Settings`, `Packages`, `ProjectSettings`; `Assets/Editor/ProjectBootstrap`; baseline Presentation/Editor scripts and EditMode tests under `Assets/StartupLife`; licensed font files; `scripts/Import-UnityBootstrap.ps1`; this record and ledger/index updates.

Commit: none; acceptance verification and metadata review pending.
