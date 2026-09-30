# Provisional first playable backend

Status: **prepared ahead of dependency verification**, not M1–M7 completion or Gate A. Date: 2026-09-30.

Owner model: primary Codex (current session model). GPT-5.6 Sol began primitives/asmdefs/harness scaffolding, then both Sol workers hit account usage limits. Primary explicitly took over and completed the backend/test slice. Astra authored contracts and reviewed this implementation. Gemini was unavailable and not used. See the [sequencing allowance](../../architecture/FIRST_PLAYABLE_CONTRACTS.md#12-provisional-groundwork-during-external-provisioning).

## Skills used

startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems (C# references), unity-game-economy, unity-mcp-bridge, unity-debug-profiler, unity-qa-release, unity-cli. No Editor/MCP action is claimed. Source was edited as C#; no serialized Unity scene, prefab, asset, or metadata was edited.

## Implemented behavior

- Immutable catalog definitions; detached immutable snapshots; candidate-state commands and durable commit-before-publication.
- Valid fresh character age/name/appearance, Developer employment, independent career XP/skill exposure, data-defined rank grants, and history.
- Gregorian date boundaries, configured sleep/work schedules, evening study, positive LearningSpeed, one course, one purchase charge, completion/obsolete-target handling.
- Versioned independent RNG streams; exact Developer quota deck; largest-remainder conversion; anti-repeat ordering; persisted deck/cursor and rank-change suffix preparation.
- Exact salary fractions per month's scheduled workday denominator; monthly payment cutoff, nonnegative shared personal cash, living costs, recoverable arrears, oldest-due settlement, resignation preserving claims/history.
- Retry receipts with payload conflict rejection; pending-choice advancement stop; playback acknowledgements do not grant rewards; no wall-clock/offline advancement.
- Checksummed JSON envelope, sequential synthetic-v0 migration, validated temporary-file/primary replacement/previous backup, cross-process writer lock, explicit ambiguous-commit recovery, future/content-version preservation, and no backup rollback on transient read failure.

Content values remain **test fixtures**, not production balancing or a ScriptableObject catalog.

## Tests and review

`scripts/Test-Simulation.ps1` built separate Core, Simulation, Application, and Infrastructure libraries under **.NET Standard 2.1 / C# 9** with the prescribed project references; **0 warnings, 0 errors**. The scenario harness runs on .NET 8. [Build/test log](build-and-tests.log).

**35/35 behavior checks passed**. [Machine-readable report](test-report.json). Cases include quota/RNG vectors, calendar/weekends/payday, exact salary and resignation remainder, command rejection/replay, course price/time/speed/completion, deterministic restore, overflow, arrears recovery, storage faults/lock contention, corrupt backup recovery, static synthetic-v0/current-v1 fixtures, and snapshot isolation. Fixtures are tracked under `tools/SimulationChecks/Fixtures`; v0 is explicitly synthetic, never a shipped save.

[Astra review](ARCHITECTURE_REVIEW.md) found three defects in the initial implementation. All demonstrated defects were fixed and received additional regression evidence. Original findings and follow-up hashes remain recorded.

## Visual evidence

None. This is engine-free logic only; no game scene or UI exists. No screenshot, PlayMode, device, or production-art claim.

## Save impact

Initial **provisional** schema version 1, not released. The current models cover only this backend subset and are not the complete approved SaveRoot. Before a published schema changes, increment SaveVersion and retain independent historical fixtures. Serializer/BigInteger reflection preservation and atomic replacement still require Unity/IL2CPP/mobile tests.

## Known limitations

M0 dependencies remain incomplete, and no Unity task checkbox is satisfied by this harness. Missing work includes the complete nine-assembly integration, public ISimulation/SimulationOutcome shape, prior-history backgrounds and full employer model, metadata timestamps/settings, businesses/helper/events, lifecycle callbacks/autosaves, Vietnamese UI/localization/font glyphs, content assets, art/rigging, build entrypoints/CI, performance, and physical-device evidence. Full save validation needs broader cross-career/signature/cursor fixtures. Receipts/history grow without compaction; performance and storage are unmeasured on mobile. Unsupported content is preserved, not silently remapped.

## Files changed

Core/Simulation/Application/Infrastructure C# and four provisional asmdefs under `Assets/StartupLife/Scripts`; separate .NET library/harness projects, fixtures, and `Program.cs` under `tools/SimulationChecks`; `scripts/Test-Simulation.ps1`; this evidence/report/log. README/implementation ledger link the work. Editor metadata is deferred until real import.

Commit: no Unity source commit yet, because the source has not been imported and has no Editor-generated metadata. This prevents a provisional backend from being represented as verified Unity work.
