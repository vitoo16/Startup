# M7-T02 follow-up publication

Current classification: **M7-T02 IMPLEMENTED — EXTERNAL ACCEPTANCE BLOCKERS: physical Android + iOS**.
The ledger checkbox remains unchecked. **M8 NOT READY — blocked by M7-T02**.
This follow-up publishes verified post-merge fixes/evidence; it does not close M7 or start M8.

## Remote baseline and local preservation

- Original `origin/main`: `71cfa55e8fd4ebd9d7cac03a31da8f93c15ae62f`.
- Freshly fetched `origin/main`: `71cfa55e8fd4ebd9d7cac03a31da8f93c15ae62f`.
- PR #21 is already merged, merge SHA `71cfa55e8fd4ebd9d7cac03a31da8f93c15ae62f`, implementation head `79b3f0bc35ff9c32e45b51fe04715d253bd3abd5`. No later main commit was present at publication preflight.
- Original local branch: `feat/m7-t02-lifecycle-device-gate`; original HEAD `11106836fa70bab9c70b0520c6e27c0f84804d84`. That branch and its five commits remain intact.
- Working tree was clean before transplantation. The previously unrelated `index.html` was absent at this task's preflight and was neither recreated, staged nor committed. No stash was needed or created.
- New branch: `fix/m7-t02-runtime-evidence`, created from fetched `origin/main`.

## Traceable transplantation

| Preserved original | Follow-up cherry-pick | Change |
|---|---|---|
| `17e685e8baf1ee8b74b2242d085330efbe20c13d` | `4f84eb4330576a850b78df0af32c29014d646385` | Refresh/assert visible next-day capture |
| `57418f618cb2122ba8f71b6d82975184a14f7870` | `477dc5ef1d45a927a2d8cd0d28d9551063dc0ac4` | Fatal-save Vietnamese messages and text bounds |
| `6bb6db443d34b1403940b620d3aeab7d66d4d4e2` | `909a6dd2e347085924422606235fbd13358ffa4b` | Runtime/emulator evidence |
| `ab5e93ab4520fb5eba894e02a74a3f6f9788b0d8` | `32ddcda58d4fb305a2a7c5604273c516b203852e` | Preserve exact artifact bytes |
| `11106836fa70bab9c70b0520c6e27c0f84804d84` | `1069171504c6c42ba0c8651e13c9c477ff906e33` | Include sanitized execution logs |

All five were cherry-picked in order. **Conflicts: none**. Immediately afterward,
the entire Git tree matched original local HEAD, including every evidence file.
Only this publication's evidence metadata/docs updates follow those five commits.

## Runtime evidence applicability

The executed APK identifies source `57418f618cb2122ba8f71b6d82975184a14f7870`.
Original final local HEAD `11106836fa70bab9c70b0520c6e27c0f84804d84` adds only evidence/docs.
The follow-up tree has **identical Assets, Packages and ProjectSettings** to both executed
runtime source and original final local HEAD; lifecycle tests and save/runtime source are included
in that comparison. Only parent/history changed during cherry-picking.

| Git subtree | Original verified local tree | Follow-up tree |
|---|---|---|
| Assets | `d842a944d1d49faf800fe9a4c7fdfae290f6e088` | same |
| Packages | `693d64c7000ebd74105ea7ca5aff040e268c4ee9` | same |
| ProjectSettings | `96655653f3cb972c63c66a44c6462da28c47bb5d` | same |

Existing Unity/Android PASS evidence is therefore applicable. No Unity rerun or new APK is
claimed for this history-only transplant. Full engine-free/static checks were freshly executed.
Any later production-tree change invalidates this equivalence claim until affected checks run.

## Evidence audit and metadata repair

The pre-update **243 artifacts** were verified directly against committed Git blobs/LFS SHA-256
and byte lengths. Git LFS fsck passed, including 57 media objects. The unchanged historical
manifest remains visible in preserved/cherry-picked history; the current manifest adds this
publication's files and refreshes hashes for repaired XML/report metadata.

The previous path redactor inserted `<PROJECT>`/`<USER>` into XML attribute values, making eight
otherwise genuine NUnit reports unparsable. Publication replaces only those placeholders with
`[PROJECT]`/`[USER]`. Test results, names, durations, execution timestamps and failures were not
changed. All eight repaired XML files were parsed and compared with the original unredacted
execution files: testcase fullname/result/duration tuples are identical.
This is an evidence-format repair, not a new test run or an altered PASS result.

[Audit receipt](evidence-audit.json) and [reproduction tool](tooling/audit_evidence.py) independently verify:

- EditMode **8/8 Startup Life tests PASS**; final runner total is 9 because it also includes `AddressableAssets.DocExampleCode.TestStub.RequiredTest`. This Addressables documentation stub is not a Startup Life test.
- PlayMode **11/11 PASS**, including all nine lifecycle tests; no project failures/skips.
- The fatal-regression failing report still contains **2 failures**; next-day failing report still contains **1 failure**. Fixes and passing evidence remain traceable.
- **104** checksummed save envelopes parse, preserve SaveVersion 1 and match their payload checksum.
- Android before/after-ack, background wait, course/day restart and recovery reload payload comparisons pass. Reward fields are not excluded from the work comparison.
- All **eight 1080×1920** Unity captures exist; the lifecycle MP4 retains its hash, **64.7481 seconds** and successful full-decode receipt.
- Android receipts explicitly retain physical Android/iOS **NOT RUN**.

Import/compile and warnings claims were checked against retained Editor/player logs:
Unity **6000.3.25f1**, Startup Life C# warnings/errors **0/0**, import/compile PASS.
The retained actual Android emulator run is Pixel 2/API35, Development IL2CPP ARM64;
fresh launch, work pause, course restart, next-day and recovery/filesystem continuation PASS.
Real elapsed time advances simulation: **NO**. These are historical executions on the identical
runtime tree, not new physical-device evidence.

## Fresh local follow-up regression

Reports are in [regression](regression/simulation-report.json); logs are in `logs/`.
Commands mirror `.github/workflows/engine-free-ci.yml`, with output paths outside the checkout
during execution, followed by inclusion of sanitized artifacts.

- Simulation **49/49**; H1/H2/R2 **19/19**; M2 restore/provenance **35/35**.
- Presentation **11/11**; Content **8/8**; every .NET harness build **0 warnings / 0 errors**.
- Static foundation **31/31**; M1 API **31/31**; content bridge **8/8**;
  M7 shell **43/43**; runner contract **30/30**; security/release **28/28**.
- Windows documentation validation **PASS**, unchanged scope. [Log](logs/windows-documentation.log), [receipt](windows-documentation-receipt.json).
- Clean-worktree check after final evidence commit: required before push. These tests do not change runtime files.
- GitHub CI is triggered by the new PR. Its final head/status must be read from GitHub; local PASS is not substituted for remote CI.

## Reproduction

To audit existing captures/checkpoints without running Unity or modifying app saves:

```powershell
python docs/evidence/M7-T02/RUNTIME-2026-10-04/publication/tooling/audit_evidence.py
```

For fresh engine-free regression, run the commands in `engine-free-ci.yml`, choosing a report
directory outside this checkout. For actual Unity/emulator reruns, use the original runtime
report/tooling and keep the tested source tree unchanged. Do not use `bd10square` or call emulator
PASS physical-device acceptance.

## External blockers and save decision

- **Physical Android: NOT RUN**.
- **iOS: NOT RUN — external Mac/iOS route unavailable**.
- SaveVersion **1**; persisted-field changes **none**; migration **none**.
- M7-T02 remains unchecked. **M8 NOT READY — blocked by M7-T02**.
- **NOT SAFE FOR GATE B — physical Android and current iOS acceptance remain unexecuted**.

## Publication and session closeout

The requested follow-up PR publishes post-merge runtime fixes/evidence only. It must remain
open/unmerged; no automatic merge is enabled. The final PR URL/head/CI status is returned after
push, with commits retained individually rather than squashed.

Owner model: current Codex publication/QA session; original implementation/runtime owner recorded
in the runtime report. No model switch is claimed.

Skills used: startup-life-session-orchestrator; startup-life-gameplay-guardian; pinned unity-qa-release
(`dafb97ef00f94e64e42e6260bc6b3af74cc83dad`); official unity-cli. Previously loaded runtime/UI/localization
skills remain the provenance for the unchanged verified fixes.

Acceptance criteria: preserve five local commits; transplant from latest main; prove runtime-tree
identity; audit committed evidence; run fresh engine-free checks; publish one follow-up PR without
merging; retain external blockers and unchecked task status.

Tests: XML/original-source equivalence; manifest/LFS and checksum audit; full engine-free/static
regression; Windows validator; clean-worktree gate; GitHub CI after publication.

Visual evidence: unchanged six lifecycle/two fatal captures and actual emulator video; no new
visual implementation or screenshot generation in this publication pass.

Save impact: none, SaveVersion 1. Known limitations: physical Android and Mac/iOS unavailable.
Files changed: publication notes/audit/reports; XML redaction placeholders; REPORT/SESSION status
clarification; refreshed manifest. Runtime/Assets/Packages/ProjectSettings remain identical.
Commits: five traceable cherry-picks above, followed by the evidence-only publication commit.
