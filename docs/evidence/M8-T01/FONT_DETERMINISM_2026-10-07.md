# M8-T01 — Unity/TMP font determinism closeout

Date: 2026-10-07 (+07:00)
Owner model: GPT-5.6 Sol
Starting main: `ce7bb299f120f6fc9ebae78413d8764c2c8057ce`
Branch: `fix/m8-t01-font-determinism`
Normalized runtime-tested head: `1d9bd9c564dd3d714fae6aabbc1c9b4a49007d39`
Unity: `6000.3.25f1`, revision `e1dba0a9aba4`
Unity CLI: `1.0.0-beta.12`

## Starting state and preserved evidence

The primary checkout already contained unrelated tracked and untracked work, so this closeout used a separate clean worktree created from exact merged main. No existing checkout changes were reset, discarded, or deleted.

The first acceptance run's font diff and sanitized Unity logs/reports were preserved outside the repository before the experiment. The new Pass A, Pass B, failed formatting experiment, and final canonical run are also retained under the local evidence root:

`C:\Users\viett\.codex\visualizations\2026\10\07\01a11439-c4ae-7e03-9e6c-976d7db9991d\m8-t01-font-evidence`

All retained Unity logs have access-token values redacted. Visual evidence is not applicable because this task changes tracked TMP serialization only and does not change UI layout, art, scenes, or presentation.

## Original blocker

The prior M8-T01 runtime acceptance passed all tests but left two tracked TMP assets modified:

- `Assets/StartupLife/UI/Fonts/NotoSansVietnamese.asset`
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`

That dirty post-run tree prevented runtime closeout. The original font patch was retained externally rather than silently restored.

## Pass A — clean main reproduction

Pass A started with both fonts byte-equal to main and no `Library` cache. Unity performed a true fresh import and reproduced changes in exactly the two expected font assets.

`NotoSansVietnamese.asset` remained a Dynamic TMP font (`m_AtlasPopulationMode: 1`). Its committed runtime glyph and character tables remained `0 / 0`, its atlas reference count remained `1`, its 1×1 empty atlas texture and all asset GUIDs/subasset IDs remained stable. Unity rebuilt its OpenType feature data:

| Metric | Main | Pass A candidate |
|---|---:|---:|
| Ligature substitutions | 11 | 21 |
| Glyph-pair adjustments | 1,213 | 3,336 |
| Mark-to-base adjustments | 8,224 | 23,901 |
| Glyph table | 0 | 0 |
| Character table | 0 | 0 |
| Atlas references | 1 | 1 |

The feature-table population is substantive normalization, not a claim that the whole Noto diff is serialization-only. A previous run produced the same table kinds but different pair/mark totals (`3,278 / 23,387`), so the generated candidate required a second fresh-import proof before acceptance.

`LiberationSans SDF - Fallback.asset` remained Dynamic with glyph/character tables `0 / 0`, one 1×1 empty atlas texture, and unchanged GUIDs/subasset IDs. Unity upgraded the embedded Material serialization from version `6` to `8` and added the Unity 6.3 canonical serialized fields. Its generated output matched the prior run byte-for-byte.

Pass A runtime result:

- EditMode: **8/8 PASS**, duration `0.4215911 s`;
- PlayMode: **11/11 PASS**, duration `7.36877 s`;
- skipped / inconclusive / failed: `0 / 0 / 0` in both suites;
- runner exit: `0`;
- C# compiler diagnostics: `0 errors`, `0 warnings`.

## Pass B — normalized candidate proof

The exact Unity-generated candidate was committed, the isolated worktree's `Library` was moved to a verified sibling evidence path, and Unity rebuilt all generated cache state. No project-owned source or configuration was deleted.

Pass B result:

- fresh import: **PASS** (`4,532` assets imported, local/cache-server hits `0`);
- compile: **PASS**, `0` C# errors and `0` C# warnings;
- EditMode: **8/8 PASS**, duration `0.4231081 s`;
- PlayMode: **11/11 PASS**, duration `7.4565326 s`;
- skipped / inconclusive / failed: `0 / 0 / 0` in both suites;
- runner exit: `0`;
- `git status --short`: empty;
- ordinary post-run `git diff --check`: exit `0`;
- both normalized font blobs: byte-identical to the candidate commit.

Classification: **ONE-TIME DETERMINISTIC UNITY FONT NORMALIZATION**. Noto does not show recurring tracked dynamic population after normalization, so this task does not switch it to Static and does not regenerate its atlas.

## Canonical whitespace experiment

Unity's canonical fallback serialization contains six newly emitted empty fields whose lines end with one space. A formatting-only experiment removed those spaces and passed a branch-range whitespace check, but the next exact fresh import restored all six bytes and made the tree dirty. The experiment was therefore reverted. No `.gitattributes` exception or hidden Git configuration was added.

This is a known Unity canonical-serialization limitation for branch-range `git diff --check`. The required post-run check operates on the tracked worktree; after committing Unity's canonical bytes, it passes because Unity no longer mutates the assets. Do not trim or hand-resave those six fields.

## Final canonical acceptance

The final acceptance ran from normalized canonical head `1d9bd9c564dd3d714fae6aabbc1c9b4a49007d39` after moving away the isolated `Library` cache and forcing another true fresh import.

- exact Unity version/revision: **PASS**;
- fresh import: **PASS**;
- compile: **PASS**;
- C# errors / warnings: `0 / 0`;
- EditMode: **8/8 PASS**, duration `0.4537145 s`;
- PlayMode: **11/11 PASS**, duration `7.2859694 s`;
- skipped / inconclusive / failed: `0 / 0 / 0` in both suites;
- runner exit: `0`;
- post-run `git status --short`: empty;
- post-run ordinary `git diff --check`: exit `0`;
- tracked post-run font mutations: `0`.

## Scope and closeout

Gameplay, business, save, migration, M1 API, scenes, content definitions, packages, and `ProjectSettings` are unchanged. `SaveVersion` remains `2`; there is no save-schema or migration impact. M8-T02 is not implemented in this branch.

Skills used: `startup-life-session-orchestrator`; `startup-life-gameplay-guardian`; `unity-game-director`; `unity-gameplay-systems`; `unity-mcp-bridge`; `unity-qa-release`; `unity-game-economy`; `unity-cli`; `startup-life-asset-quality-gate`; `vietnam-art-direction`; `ui`; `ui-ugui`; `optimize-text-mesh-pro`. Community Unity skills came from pinned revision `tea-x-random/unity-game-skills@dafb97ef00f94e64e42e6260bc6b3af74cc83dad`.
Tests: canonical `scripts/Test-UnityHeadless.ps1 -Mode All` fresh-import runs above.
Visual evidence: N/A.
Save impact: none; SaveVersion 2 unchanged.
Known limitations: Unity canonical fallback YAML contains six trailing-space lines, so a branch-range whitespace scan reports those generated bytes; required clean-worktree and ordinary post-run diff checks pass. Physical-device behavior was outside this font-determinism closeout.
Files changed: the two normalized TMP font assets; this report; `HUONG_DAN_FONT_DETERMINISM.md`; `docs/IMPLEMENTATION_PLAN_MVP.md`; and the completed-task set in `scripts/validate-documentation.ps1`. The final commit after the runtime-tested head contains documentation/validation-only changes and does not require another Unity run.

`M8-T01 CLOSED — Unity runtime acceptance and tracked-asset determinism verified`

`M8-T02 READY AFTER FONT-NORMALIZATION CLOSEOUT MERGES TO MAIN`

Do not merge the closeout PR automatically. Do not begin M8-T02 before the closeout reaches main.
