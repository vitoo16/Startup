# M0-T02 — Skill and tooling preparation

Status: **Partial; task remains unchecked**. Date: 2026-09-30.

Owner model: GPT-5.6 Sol began the inventory/Git setup; primary Codex took over when that worker and the implementation worker hit account usage limits. This record does not attribute primary-agent work to Sol.

## Skills used

startup-life-session-orchestrator, skill-installer, unity-cli, new-unity-project, unity-package-management, unity-project-setup, unity-mcp-bridge. Project invariants override newer-release, monetization, and cloud defaults.

## Acceptance and tests

- Installed 16 community skills from reviewed revision `dafb97ef00f94e64e42e6260bc6b3af74cc83dad`; upstream validator reported 27 skills and zero errors. The [skill lock](../../tooling/skills.lock.json) records 31 installed skill hashes, including 30 required by the expanded ledger and an additional MCP skill.
- `scripts/Test-Tooling.ps1`: **37/37 checks passed**. [Report](tooling-report.json) includes versions and hash checks without account/token/machine details.
- `scripts/Test-Tooling.ps1 -RequireEditor`: **38/39 passed, exit 1**. [Editor gate report](editor-gate-report.json) correctly fails for missing 6000.3.25f1; license status is now active.
- Working versions: official Unity CLI 1.0.0-beta.11, bundled Python 3.12.14, uv 0.12.21, isolated .NET SDK 8.0.425; Git and Git LFS diagnostics passed.
- Official CLI downloaded and validated the 6000.3.25f1 Editor installer. Both automatic install attempts failed with `ELEVATION_FAILED`; a manual administrator installation is being verified. 6000.6.3f1 exists but is not the selected project version.
- Unity CLI template discovery did not list Universal 2D for 6000.3.25f1. The Editor's actual bundled template/package catalog must be checked before setup; no package versions or URP assets were fabricated.
- Local Git `main`, Git LFS configuration, `.gitignore`, and `.gitattributes` exist. No Unity asset metadata was hand-authored.

## Visual evidence and save impact

None for tooling. No scene, art, or released schema changed in this task.

## Known limitations

Unity 6.3 import/compile, required-package resolution, connected Editor automation/reconnection, platform modules, and community-skill compatibility are unverified. The user has no Mac; iOS/Xcode/signing/iPhone QA cannot pass. These are external/verification gates, not passing checks. Official skill installation has installer/content hash pins; no official upstream commit was available in the existing installer lock.

## Files changed

`docs/tooling/skills.lock.json`, `scripts/Test-Tooling.ps1`, `scripts/Install-UnityEditor.ps1`, `.gitignore`, `.gitattributes`, this record and its reports. README and ledger link this evidence. Tool installations live outside the repository.

Commit: none yet; reviewed documentation may be committed separately. Provisional Unity source awaits Editor metadata/import and is not a verified Unity commit.

## Continued provisioning evidence

The user completed administrator installation of Unity 6000.3.25f1. The pinned-Editor tooling gate now passes **39/39** and reports an active license. Earlier failed-install and 38/39 results above are historical. The actual bundled Universal 2D template was verified and initialized successfully; [M0-T03 evidence](../M0-T03/SESSION.md) tracks project integration. Skill/automation compatibility and both mobile routes remain acceptance gates.
