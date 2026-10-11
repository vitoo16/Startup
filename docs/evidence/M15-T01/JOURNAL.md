# M15-T01 — Event journal and staged V4 migration evidence

**Owner:** GPT-6 Technical Director. **Branch:** `feat/m15-event-selection-foundation`.
**Milestone:** **PARTIAL**. Do not merge as final acceptance, mark CLOSED, or claim Unity runtime proof.

## Verified source slices

1. Pure deterministic event selector with tags, calendar windows, weights, cooldowns, caps and its own `EventRng` stream, independent of career quota scheduler.
2. Prospective, immutable-candidate event journal with stable pending-ticket IDs, category rolling-window policies, optimistic revision checks, command-ID idempotency, conflict rejection, and detached state snapshots.
3. Each offered ticket stores its *offer context* (origin date, date, stage, tags). `EventJournalReplay.Validate` reselects all offers from the original RNG and stored offer context using the same production selector. The journal's structural/content validator verifies choice/outcome identities and pending states; final PRNG is compared against the trace. This detects journal/RNG tampering and content drift; it **does not prove that recorded stage/tags match the authoritative GameState** until receipt integration.
4. `StagedEventV4SaveSerializer` admits a prospective **wire schema 4** envelope with an embedded byte-identical V3 envelope and SHA-256, plus event journal. Both V3 and journal must pass independent cold replay; envelope generation is `core.Revision + journal.Revision`, strictly checked. `MigrateVerified` accepts validated legacy V2/V3 saves in memory; migration **does not write to disk**.
5. All version-4 orchestration lives in the dedicated leaf `StartupLife.Integration` assembly. Core, Application, Infrastructure and Simulation assembly references retain their original direction.

## Exact verification evidence (2026-10-11)

At implementation commit `f2d02f34270cb0dee32a394ed5f8bc06b37c18cf`:

- Engine-free CI **SUCCESS**: https://github.com/vitoo16/Startup/actions/runs/38109273106
- CodeQL C# and GitHub Actions **SUCCESS**: https://github.com/vitoo16/Startup/actions/runs/38109271689
- SimulationChecks **63/63** PASS (includes replay of journal RNG, pending state and corruption rejection).
- BusinessOwnershipChecks **90/90**, AstraFoundation **20/20**, RestoreInvariant **35/35**, Presentation **13/13**, ContentCatalog **13/13** PASS.
- EconomicChecks **65/65** PASS, including `M15 staged V4 migration retains verified V3/V2 bytes and restores pending choice`.
- .NET builds **0 errors / 0 warnings**. No frozen validation thresholds or historical fixtures weakened.

## Mandatory closeout

- **Owner model:** GPT-6 Technical Director.
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-mcp-bridge; unity-qa-release; unity-game-economy; unity-debug-profiler.
- **Acceptance criteria:** Pure deterministic event selection and event-journal cold replay PASS; source-only V2/V3 -> staged V4 admission PASS; authoritative GameSession event commands/outcome application **NOT YET IMPLEMENTED**.
- **Tests:** Engine-free suite and CodeQL SUCCESS at implementation SHA above.
- **Visual evidence:** NOT RUN (no UI changes; deferred Unity EditMode/PlayMode/IL2CPP/device gates remain NOT RUN).
- **Save impact:** No active gameplay save schema changed. Prospective staged V4 payload is intentionally **not wired into `ISaveSerializer`, `GameSession` or `AtomicFileSaveStore`**. Legacy V2/V3 checkpoint bytes and economic replay authority remain untouched.
- **Known limitations:** No combined GameSession receipt/CAS authority, event-outcome cash/XP/economy writes, committed next-day event triggers, external proof of offer tag/stage source, durable live event UI, or Vietnamese authored content. A standalone V4 journal roundtrip is not proof of real-game save/resume.
- **Files changed (this increment):** Core/EventJournalContracts.cs, Core/EventV4Payload.cs, Simulation/EventDecisionJournal.cs, Simulation/EventJournalReplay.cs, Integration/StagedEventV4SaveSerializer.cs, Integration/StartupLife.Integration.asmdef, Unity .meta files, tools/SimulationChecks/StartupLife.Integration.csproj, tools/SimulationChecks/StartupLife.Provisional.csproj, tools/SimulationChecks/Program.cs, tools/EconomicChecks/EconomicChecks.csproj, tools/EconomicChecks/Program.cs and this evidence.
- **Commit:** `f2d02f34270cb0dee32a394ed5f8bc06b37c18cf` (implementation; documentation commit separate).

## Next integration gate

Design and implement a **single authoritative GameSession/receipt/CAS unit of work** spanning V4 journal transitions and the existing V3 economic substate. Both offer and resolve must be normal idempotent commands with replayed receipts, caller revision checks, failed/ambiguous write recovery, event `EventRng` isolation, and deterministic, exactly-once *applied* rewards. Until then, keep the V4 serializer staged and reject claims that M15-T01 is closed.

## Increment 3 — shared indexed history, offer command idempotency and long-run verification

**Implementation SHA:** `b8e802468baf5af653d6e47c36f0adf42a992676`.

- `Simulation/EventSelectionHistory.cs` indexes committed occurrences and ordered per-category event dates **once per offer**, replacing the prior full-ticket scan performed for each eligible event/category. The same index is built **incrementally** during V4 cold replay.
- Cold replay now invokes the original `GameEventSelector.Select` (no duplicate RNG or eligibility engine). With N tickets and E authored event definitions, replay history management is O(N) and event selection O(N × E) rather than repeated O(N²) ticket rebuilding; history storage O(N). Category rolling-window lookup uses a binary lower bound, with representable-calendar clamping for very long windows.
- `GameEventSelector.Select` allocates the event-context tag lookup **once per selection**, not once for each candidate.
- Tickets persist `OfferCommandId`; an **explicit** matching ID with the same offer context returns `AlreadyCommitted` even after cold restore, without drawing the RNG again. Reusing a command ID for another offer or for resolution is rejected. Historical planner calls that omit an explicit ID remain non-idempotent; authoritative GameSession integration must always supply caller command identity.
- Structural journal validation rejects duplicate offer/resolve command identities and inconsistent run-origin dates. Rejected candidates do not mutate the committed source graph.
- New regression exercises **240 committed events**, 240 resolutions, RNG parity and corrupted historical contexts; tests also cover exact retry, conflict, restored duplicate command IDs and maximum-width category windows. No timed performance benchmark claimed: reduced complexity is established by code structure, not by device profiling.

**Exact-head evidence:**

- [Engine-free CI #38110083740](https://github.com/vitoo16/Startup/actions/runs/38110083740): **SUCCESS** — Simulation **66/66**, BusinessOwnership **90/90**, AstraFoundation **20/20**, RestoreInvariant **35/35**, Presentation **13/13**, ContentCatalog **13/13**, EconomicChecks **65/65**; builds **zero errors/warnings**.
- [C# and Actions CodeQL #38110082386](https://github.com/vitoo16/Startup/actions/runs/38110082386): **SUCCESS**.

**Owner model:** GPT-6. **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-mcp-bridge, unity-qa-release, unity-game-economy, unity-debug-profiler. **Visual evidence:** NOT RUN, source-only update. **Save impact:** prospective V4 ticket adds `OfferCommandId`; active V2/V3 saves, receipt contracts, migrations and evaluators unchanged. **Known limitations:** no real GameSession CAS/event-command integration, production rewards, Unity runtime or device performance proof. **Milestone:** PARTIAL, PR Draft, no merge.

**Files changed in increment 3:** `Core/EventJournalContracts.cs`, `Simulation/EventSelectionHistory.cs` and `.meta`, `Simulation/EventDecisionJournal.cs`, `Simulation/EventJournalReplay.cs`, `Simulation/GameEventSelector.cs`, `tools/SimulationChecks/Program.cs`, and this evidence file.
