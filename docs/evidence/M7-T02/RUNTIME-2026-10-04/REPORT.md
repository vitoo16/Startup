# M7-T02 runtime acceptance — 2026-10-04

**M7-T02 NOT CLOSED — physical Android acceptance and current iOS smoke unavailable.**
Windows, Unity Editor, the dedicated Android emulator, and required portrait/video evidence
were executed. The ledger checkbox remains open. **M8 NOT READY. NOT SAFE FOR GATE B.**

## Repository state and provenance

- Started in the local repository at the exact reviewed head `79b3f0bc35ff9c32e45b51fe04715d253bd3abd5`, on `feat/m7-t02-lifecycle-device-gate`. This pass did not start from main.
- Commit `17e685e8baf1ee8b74b2242d085330efbe20c13d`: `test: refresh next-day lifecycle evidence before capture`.
- Final tested runtime/source commit `57418f618cb2122ba8f71b6d82975184a14f7870`: `fix: localize and size fatal save status`.
- Final Unity tests executed those source changes before their commit; the final APK and emulator run identify commit `57418f6` explicitly. The final evidence/docs commit adds no runtime behavior; resolve it with `git rev-parse HEAD`.
- [PR #21](https://github.com/vitoo16/Startup/pull/21) was already merged externally at `71cfa55e8fd4ebd9d7cac03a31da8f93c15ae62f`. A final GitHub read reconfirmed its remote head remains `79b3f0bc35ff9c32e45b51fe04715d253bd3abd5`. This pass neither merged it nor created another PR. No push is claimed.
- Editor-only build utility was temporarily present during APK compilation and is retained under `android-final/`; build provenance accurately reports a dirty checkout. Generated Android settings/cache artifacts were archived outside the repo, removed/restored, and excluded from the commits. No signing credentials were committed.

## Actual minimal fixes

1. The next-day test called Flow directly and photographed an unrefreshed view: save Sep 2 00:00, visible Sep 1 17:00. A visible-date assertion reproduced the defect ([failing XML](nextday-visual-failing.xml), [rejected screenshot](rejected/05-next-day-stale-view.png)). Refreshing the existing view before capture fixed the fixture. The production button already performs this refresh.
2. Actual Android unreadable/both-invalid saves displayed missing-translation errors in a narrow text box ([old unreadable screen](android/E-unreadable-fatal.png), [old both-invalid screen](android/E-both-invalid-fatal.png)). Two new fatal text/layout assertions failed on the old assets: [9 executed, 7 passed, 2 failed](fatal-failing.xml). Added only `reason.save.read_failed` and `reason.save.unrecoverable`, plus centered safe-area text bounds. The existing scene and localization tables were saved through Unity Editor APIs, not raw YAML editing. These assertions now pass; both [fixed unreadable](android-final/E-unreadable-fatal.png) and [fixed both-invalid](android-final/E-both-invalid-fatal.png) Android screens were inspected.

No lifecycle architecture, gameplay balance, command authority, persisted field or save schema changed.

## Windows validation

Actual `./scripts/validate-documentation.ps1`: **PASS**, Windows PowerShell 7.6.5, exit 0.
Initial scan covered 322 Markdown files: [receipt](windows-documentation-receipt.json),
[log](logs/windows-documentation.log). Closeout execution is retained separately in
[final receipt](windows-documentation-final-receipt.json) and [final log](logs/windows-documentation-final.log).
Validator scope was not weakened.
The first closeout scan ran before its two newly linked output files had been copied into the
bundle; that missing-artifact diagnostic is retained in `logs/windows-documentation-closeout-missing-artifacts.log`.
After copying the receipt/log, the unchanged validator passed, checking 323 Markdown files.

## Unity Editor and regression

- Exact licensed installed Editor: **6000.3.25f1**. Unity CLI **1.0.0-beta.12** batch fallback; graphics enabled for PlayMode. No live UnityMCP action is claimed.
- Package resolution/import and Startup Life Core, Simulation, Application, Infrastructure, Presentation, Editor and tests compiled: **PASS**. Final Startup Life C# warnings/errors **0/0**.
- EditMode: **8/8 PASS, 0 failed, 0 skipped** — [XML](editmode-final-results.xml), [log](logs/editmode-final.log).
- PlayMode: **11/11 PASS, 0 failed, 0 skipped**, including all 9 lifecycle UnityTests — [XML](playmode-final-results.xml), [log](logs/playmode-final.log). Two existing fatal cases now also assert localized text, no overflow, adequate width and capture their screens.
- Fresh engine-free after the fatal UI repair: Simulation **49/49**; H1/H2/R2 **19/19**; M2 restore/provenance **35/35**; Presentation **11/11**; ContentCatalog **8/8**; .NET warnings/errors **0/0**. [Reports](regression/simulation-report.json), [log](logs/regression.log).
- Fresh static: foundation **31/31**; M1 API **31/31**; content bridge **8/8**; M7 shell **43/43**; runner **30/30**; security/release **28/28**. Individual JSON reports are under `regression/`.
- Starting-head remote [CI #72](https://github.com/vitoo16/Startup/actions/runs/37156141134) was green; it is a baseline and does not substitute for these local executions.

An initial external save observer lacked Windows delete sharing and caused two artificial persistence failures. An unobserved rerun passed 11/11; a corrected Win32 reader using share mask 7 also passed 11/11. [Failing XML](playmode-results.xml), [unobserved XML](playmode-unobserved-results.xml), [shared-reader XML](playmode-shared-results.xml), [reader](tooling/observe_shared.py). No production persistence change was justified.
The shared-reader run and [full raw checkpoints](unity-observed-states.ndjson) are starting-head gameplay evidence. The final XML/captures are from the repaired assets; those raw observer values are not mislabeled as a new run.

## Work pause — actual Android OS lifecycle on final APK

Run: `run-5ee7513f1d5d49ff83fa10556f833b18`. [Full observed payloads](android-final/observed-states.ndjson) and individual checksummed `.save.json` files retain all compared fields.

| State | Revision | Date/minute | Activity/cursor | XP/exposure/claims/cash |
|---|---|---|---|---|
| Before background | 5 | 2026-09-01 / 660 | `run-5ee7513f1d5d49ff83fa10556f833b18/activity/2026-09-01/540` / 0 | XP 10; communication exposure 1; claims 0; 2,000,000 |
| After actual background wait | 5 | 2026-09-01 / 660 | same / 0 | Entire payload identical |
| Relaunch and acknowledge | 6 | 2026-09-01 / 660 | same / 1 | XP 10; exposure 1; claims 0; 2,000,000 |
| Complete shift then restart after ack | 12 → 12 | 2026-09-01 / 1020 | final work activity / 1 | XP 40; exposure 4; one salary claim `5000000/11`; 2,000,000 |

**PASS**. Before-ack comparison differs only in `NextOperation`, `PlaybackCursor`, `Receipts`, `Revision`; all rewards, history, grants, consumed activities, economy, RNG, scheduler and course fields remain equal. After-ack restart compares the entire payload equal. Actual HOME/background and force-stop/relaunch used the private emulator; this is not a manually invoked Unity pause callback.
The matching Unity runtime before/after-ack and active coroutine interruption tests pass.

## Course restart

**PASS**. Final Android instance `run-5ee7513f1d5d49ff83fa10556f833b18/course/7`, definition `communication-basics`.
Revision **14 → 14**, Sep 1 minute **1080 → 1080**, progress **600000 → 600000**,
cash **1,800,000 → 1,800,000**. Entire payload/checksummed save is equal on relaunch.
Target **3600000** comes from loaded content BaseMinutes 360, rather than a new saved field.

The final Unity mid-course test also continues through completion and verifies exactly one
completion/history/grant and no second purchase charge. The shared-reader starting-head reference
reached revision 30, Sep 2 minute 1080, communication GrantedLevel 1, no active course, XP 80,
exposure 8, cash 1,800,000, one 200,000 charge. Android here verifies partial restore; Android
course completion is not separately claimed.

## Next-day continuation

**PASS**. Final Android revision **16 → 16**, date **2026-09-02 → 2026-09-02**,
minute **0 → 0**, XP 40, one salary claim, cash 1,800,000, same course/progress 600000.
Activity `run-5ee7513f1d5d49ff83fa10556f833b18/activity/2026-09-01/1320`, cursor 0. Entire payload is equal after restart;
no recommitted day boundary, duplicated summary/reward, or elapsed-time advance.
The corresponding final Unity test and two portrait captures also pass.

## Recovery and fatal boot

| Case | Unity | Final Android emulator |
|---|---|---|
| Corrupt primary + valid backup | PASS, same revision-2 run | PASS, same revision-16 run and Life state; no new run |
| Next normal write | PASS, Resign commits revision 3 | PASS, Resign commits revision 17 |
| Second reload | PASS, entire public checkpoint equal | PASS, entire serialized payload equal at revision 17 |
| Unreadable primary with valid backup | PASS, fails closed | PASS, readable fatal Vietnamese message; Life/Creation do not appear |
| Both invalid | PASS, fatal bootstrap | PASS, readable fatal Vietnamese message; Life/Creation do not appear |

Faults were injected only into the newly created emulator's synthetic test-app files while stopped.
Original checksummed saves were retained outside the device. The development fixture deliberately
copied revision 16 to backup, corrupted primary, then exercised the normal Resign button and reload.
Normal Resign changes activity/cue/history and employment; unchanged fields were compared exactly.
No temp file remained ([file listing](android-final/files-after-recovery.log)); the deliberate corrupt
quarantine file and ordinary lock file are expected. Finally the saved valid revision-17 fixture was
restored and relaunched ([usable final state](android-final/Z-restored-valid.png)).

The external harness initially omitted Resign's expected `CurrentActivity`/`CurrentCue` changes and
stopped after the successful write. [Original journey log](android-final/journey.log) retains that
over-strict assertion. [Resume log](android-final/resume.log) records the remaining E checks after
correcting only the external comparison. No production save fix or waived reward comparison.
Earlier too-early splash/launcher samples are retained under `android-final/attempt-*`; they are not passing screenshots.

## Wall-clock pause

**PASS**. Final Android actual background wait preserved the complete revision-5 payload,
Sep 1 minute 660, cash 2,000,000. Final Unity test separately executes a real 0.4-second
pause/wait/resume with no gameplay command and asserts unchanged date/minute/revision/cash.
The observed starting-head Unity reference was revision 1, Sep 1 minute 0, cash 2,000,000
before/after. No offline income/time progression is inferred or implemented.

## Portrait and recording evidence

All six required final Unity captures are **1080×1920**:

Every artifact's byte length and SHA-256 is recorded in [manifest](manifest.json).

1. [01 work before pause](visuals/01-work-before-pause.png)
2. [02 restored work before acknowledgement](visuals/02-work-restored-before-ack.png)
3. [03 course before restart](visuals/03-course-before-restart.png)
4. [04 course restored](visuals/04-course-after-restore.png)
5. [05 next day before restart](visuals/05-next-day-before-restart.png)
6. [06 next day restored](visuals/06-next-day-after-restore.png)

Capture assertions verify glyph availability, no missing translation, no overflow, touch targets
at least 95.9 reference pixels, safe-area button containment, and non-black UI-inclusive rendering.
Extra fatal captures 07/08 and actual Android course/day/fatal captures were inspected. Emulator
screens show the same persisted values, proper Vietnamese labels and no visible clipping.
This Pixel 2 emulator has no physical display cutout; no physical notch acceptance is claimed.

[Actual final lifecycle recording](android-final/lifecycle-emulator.mp4): **64.7481 seconds,
1080×1920, 10730894 bytes**. SHA-256 `c425ceec7ba4265cb9e93c68edb4647d5881dc9cf798813981b78028cc9b265f`.
It shows character creation/work, OS background, relaunch, partial course, restart/restored course,
then next-day restore. The MP4 fully decodes with exit 0, and decoded frames including the final
fully localized next-day Life screen were inspected. [Metadata](android-final/lifecycle-emulator.metadata.json)
describes discarded-output timestamp normalization during decoder verification; the original recording
was not edited. Splash and temporary localization loading frames during relaunch remain in the real clip.
The earlier starting-head 124.8568-second recording is retained in `android/` as baseline only.

## Android build and device boundary

- Unity **6000.3.25f1**; **IL2CPP ARM64 Development APK**; intended project configuration.
- Source `57418f618cb2122ba8f71b6d82975184a14f7870`; package **com.DefaultCompany.urp_2d**;
  version **1.0**, versionCode **1**, minSdk **25**, targetSdk **36**.
- Build **PASS**, **2 warnings / 0 errors**: PipelineRuntimeConfig absent and Android localization app-info metadata absent. Startup Life C# warnings/errors **0/0**. [Build log](logs/android-final-build.log), [provenance](android-final/build-provenance.json).
- Local APK: task scratch `m7-t02-runtime/android-final/StartupLife-M7T02.apk`, excluded from Git.
  SHA-256 `2f44d1afa0888df89cacb8ca67bbd1442f2f0129897f96ba9788be7bf5d7aa5c`. APK path is retained in provenance with the user directory redacted.
- Created **StartupLife_M7T02_API35**, Pixel 2 profile, Android 15/API 35, x86_64 with ARM64 translation;
  emulator 37.2.12/WHPX, SwiftShader GLES3. Serial **emulator-5560**, isolated ADB **port 5038**.
- Final A fresh launch, B work pause, C course restart, D next day, E backup recovery: **PASS on emulator**.
  [Acceptance receipt](android-final/acceptance.json), [command/timestamp audit](android-final/commands.ndjson).
- `bd10square` was excluded at the user's request; no app/save operation targeted it. Initial read-only
  inventory preceded that request. The ordinary ADB server/devices were not reset or stopped.
- **Physical Android A–E: NOT RUN.** Emulator evidence is labeled explicitly and does not close that gate.
- Private API 30 emulator initially crashed in native ARM translation; API 35 executed the intended
  ARM64 build. A native x86_64 build attempt failed under this Unity configuration; its log/provenance
  remain in the bundle. Neither failure was presented as passing platform acceptance.
- SwiftShader logs unsupported CoreCopy/HDRDebugView shaders; actual first-playable screenshots render
  correctly. Graphics/performance and physical-driver behavior require real device acceptance.

## iOS and save/schema decision

**iOS smoke: NOT RUN — external Mac/iOS route unavailable.** Windows is the available host;
no accessible provisioned Mac/iOS execution result was obtained. A workflow definition alone is not
current smoke evidence. iOS build/launch/save/pause/restart results are all NOT RUN.

SaveVersion remains **1**. **Persisted fields: no change. Migration: none.** No missing field was
found; no architecture escalation was needed. Authority remains
`FirstPlayableFlow → IGameCommands/GameSession → JsonSaveSerializer → AtomicFileSaveStore`.
No additional save manager, wall-clock progression or M8 implementation was introduced.

## Session closeout

Owner model: original implementation owner GPT-5.6 Sol; this local verification/fix pass executed
by the current Codex session. No runtime model switch is claimed.

Skills used: startup-life-session-orchestrator; startup-life-gameplay-guardian; pinned
unity-game-director, unity-gameplay-systems, unity-mcp-bridge, unity-qa-release,
unity-debug-profiler, unity-ui-designer, unity-localization; official unity-cli, ui, ui-ugui;
vietnam-art-direction. Community pin `dafb97ef00f94e64e42e6260bc6b3af74cc83dad`.

Acceptance criteria: actual Windows/Unity execution, work/course/day restore and recovery,
real-time pause invariance, phone portrait/video evidence, intended Android build and honest
platform classification. Physical Android and current iOS criteria remain outstanding.

Tests: final EditMode 8/8; PlayMode 11/11; engine-free/static counts above; A–E emulator state
comparisons and fatal screenshot review. Visual evidence: six required captures, two extra fatal
captures, emulator checkpoints and actual lifecycle MP4. Save impact: none, version 1.

Known limitations: physical Android unavailable by user constraint; Mac/iOS unavailable;
ARM translation/software renderer does not validate native physical-device performance/drivers.

Files changed: lifecycle test evidence assertions; FirstPlayableShellBuilder fatal status repair;
FirstPlayable scene fatal text bounds/alignment; shared/Vietnamese string tables; runtime evidence
bundle/manifest; SESSION execution addendum and M7-T02 ledger evidence link. M7 remains unchecked.
Commits: `17e685e`, `57418f6`, then the evidence/docs-only closeout commit.

Final decision: **M7-T02 NOT CLOSED. M8 NOT READY. NOT SAFE FOR GATE B.**
