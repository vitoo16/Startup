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


## Increment 4 — physical V4 CAS reuse, fault injection, cold retries

**Implementation SHA:** `fe73d7cf1176ae1eee52a0d055ff77c65a6d0ad5`.

- Added `Integration/EventV4CompatibilitySerializer.cs`, which implements the existing `ISaveSerializer` and `IActiveSaveSchema` ports. It maps validated V4 wire into a **runtime-only** `GameState` projection with combined revision, pending choice, and separate event RNG. It delegates V2/V3 source admission to the pre-existing verified V3 migration and staged V4 serializer. `GameState.EventV4` is `[IgnoreDataMember]`: frozen GameState V2 and economic V3 wire bytes are not extended.
- This adapter strictly refuses any candidate changing a projected cash/scheduler/economic/receipt field outside the attached event journal. The embedded V3 checkpoint remains sealed and is revalidated on independent cold reads. One redundant full cold V3+event replay per candidate serialization was removed by returning the already validated V3 projection from the staged serializer.
- Added `Integration/EventV4CommitGateway.cs` as a **staged, stateless transaction port**, using the existing `AtomicFileSaveStore` through `ISaveStore` instead of duplicating cross-process locks, temp writes, CAS, backup rotation, or disk recovery. Offers and resolutions invoke the existing `EventDecisionJournal` planner. Rejected/no-eligible requests never call store.Commit. On committed writes it independently re-reads and compares the entire cold-admitted journal to the expected candidate. Ambiguous promotion returns RecoveryRequired, not a speculative second write.
- New **real file** integration tests in `tools/EconomicChecks/Program.cs` cover: V2 cold admission without altering source bytes, first schema4 commit, persisted pending choice, untouched career RNG, cold retry idempotence before and after resolve, invalid choice preserving byte-exact checkpoint, full journal/cash mutation rejection, checksum corruption recovering valid backup *without* rewriting the damaged primary, injected partial temp-write failure preserving original save, injected post-replace ambiguity, retry-after-ambiguity, and stale competing writer rejection.
- This is not a second gameplay simulation engine and does not clone the physical CAS implementation. The staged gateway still needs routing through the authoritative `GameSession` for ordinary economic commands, event-day generation, unified operation receipts, and **exactly-once applied rewards**. Do not present this as a complete M15-T01 production integration.

**Exact implementation-commit verification (2026-10-11):**

- Engine-free CI: https://github.com/vitoo16/Startup/actions/runs/38113274134 — **SUCCESS**; Simulation **66/66**, BusinessOwnership **90/90**, AstraFoundation **20/20**, RestoreInvariant **35/35**, Presentation **13/13**, ContentCatalog **13/13**, EconomicChecks **67/67**; all builds 0 errors / 0 warnings.
- C# and GitHub Actions CodeQL: https://github.com/vitoo16/Startup/actions/runs/38113272305 — **SUCCESS**.
- Owner model: GPT-6 Technical Director. Mandatory skills: startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-mcp-bridge, unity-qa-release, unity-game-economy, unity-debug-profiler.
- Visual evidence: NOT RUN; no UI, scene or asset modifications. Unity runtime, IL2CPP and device profiling deferred per owner.
- Save impact: new **opt-in V4** `ISaveSerializer` adapter and event-only staged gateway; active runtime registration unchanged. Legacy V2/V3 serializers and immutable checkpoints untouched. `GameState` gains only an ignored runtime association to prospective V4. Actual physical CAS tested using the preexisting atomic store.
- Files changed in increment 4: `Core/GameState.cs`, `Integration/StagedEventV4SaveSerializer.cs`, `Integration/EventV4CompatibilitySerializer.cs` and `.meta`, `Integration/EventV4CommitGateway.cs` and `.meta`, `tools/EconomicChecks/Program.cs`, this evidence.
- **Milestone: M15-T01 PARTIAL; PR #52 Draft. No merge.** Remaining work: command routing in GameSession, unified V4 economic+event replay/receipt chain, authoritative origin/stage/tags, applied reward projection, triggering on day boundaries, and required deferred Unity acceptance.


## Increment 5 — One V4 CAS for real V3 GameSession receipts and event choices

**Implementation SHA:** `87895467d692bca69279461712f2cfe48c043b24`.

- Introduced `Integration/EventV4CoreCommandRunner.cs` as a small **in-memory candidate executor** for the real existing `Application.GameSession` + `V3EconomicReceiptProcessor`. It does not reimplement careers, courses, time, business rules or receipt replay. An ephemeral V3 `ISaveStore` exposes an independently validated checkpoint to `GameSession`; only the V4 gateway can publish the resulting verified V3 receipt suffix to disk.
- Extended `EventV4CommitGateway.ExecuteCore` to dispatch ordinary `GameCommand` through the V3 GameSession runner, while using the *same physical* `AtomicFileSaveStore` CAS as event `Offer`/`Resolve`. The global V4 generation remains `validated V3 revision + event journal revision`; the inner V3 receipt revision stays separate and is validated by existing frozen-prefix + V3 suffix replay.
- The gateway shares its candidate serialization, physical write, ambiguous-result handling, cold reread and full V4 envelope comparison across event and core commands (`CommitCandidate`). It rejects stale global revisions, IDs already committed in the other command domain, and direct `AdvanceBoundary` while an event choice is pending. Core-command retries with identical command ID/payload remain `AlreadyCommitted`, even when callers supply the earlier revision.
- `StagedEventV4SaveSerializer.ReplaceCore` now returns the already verified V3 `GameState` with the rebased payload, rather than performing two identical full V3 replays in succession. Independent cold-read replay remains mandatory. No new filesystem lock, backup, append log, economic evaluator, or historical schema was invented.
- Engine-free `EconomicChecks` proves: V2 source in-memory migration, real V3 `CreateCharacter` receipt, persisted V4 event offer, blocked day-boundary during pending choice, resolved event, real V3 `PurchaseCourse` with authored price debit, simulated failure **after** atomic file promotion, cold retry without charging again, command-ID conflict across event/V3 domains, stale generation rejection, resumed time advance, and successful cold replay of the interleaved V3 receipts + V4 tickets. The expected wallet uses the authored first-day living cost and course price, never hard-coded cash balances.
- **Exact implementation CI:** [Engine-free #38114690694](https://github.com/vitoo16/Startup/actions/runs/38114690694) **SUCCESS**; Simulation **66/66**, BusinessOwnership **90/90**, AstraFoundation **20/20**, RestoreInvariant **35/35**, Presentation **13/13**, ContentCatalog **13/13**, EconomicChecks **68/68**, all builds **0 errors and 0 warnings**. [CodeQL #38114689326](https://github.com/vitoo16/Startup/actions/runs/38114689326) runs independently for C# and Actions and must be checked for exact-head success.
- **Owner:** GPT-6. **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-mcp-bridge; unity-qa-release; unity-game-economy; unity-debug-profiler.
- **Files changed:** `Integration/EventV4CoreCommandRunner.cs` and `.meta`, `Integration/EventV4CommitGateway.cs`, `Integration/EventV4CompatibilitySerializer.cs`, `Integration/StagedEventV4SaveSerializer.cs`, `tools/EconomicChecks/Program.cs`, and this journal.
- **Visual evidence:** NOT RUN (no view/art/scene changes; Unity runtime deferred under project directive). **Save impact:** staged V4 can now carry both verified progressing V3 economic receipt history and deterministic event journal in one atomic envelope; production V2/V3 reader/serializer, archived source fixtures and active save wiring remain unchanged.
- **Remaining gate — do not mark M15-T01 CLOSED:** Production bootstrap/command routing must register this gateway for player gameplay; event **origin/stage/tags must be derived from authoritative game state**, not supplied by the UI; event outcome **cash/XP effects and their exact-once receipt accounting** have not been implemented; player-facing pending-choice UI and consolidated Unity acceptance remain outstanding. The inner V3 receipts are real and replayed; event selections/choices are persisted, but the journal alone does not prove an applied economic reward.
- **Status:** **M15-T01 PARTIAL**, PR #52 **Draft**, **no merge**.


## Increment 6 — Authority-bound event eligibility context and V3 receipt-frontier anchoring

**Implementation SHA:** `5a2c42168fc6d4031f93828fe666fb35371ff427`. **Owner:** GPT-6 Technical Director.

- Added one pure `Simulation/AuthoritativeEventContext.cs` with `FromCore(GameState, EventJournalState)`. It derives the event eligibility stage and canonical tags from the **receipt-verified V3 game projection**, not UI claims. Current bounded MVP tiers are 0 (character not created), 1 (created, no active owned business), 2 (has active owned business). Tags include `character-created`, `employed`/`unemployed`, `career/<id>`, `course-active`, `course/<id>`, `business-active`, `business/<id>`. A closed business no longer qualifies as active; no false “stable/growing” tiers are invented without provenance for revenue/reputation/profitability. **This is not the final six-stage business progression described in GDD §17.**
- `EventV4CommitGateway.Offer(runId, commandId, expectedRevision)` is the preferred UI-safe API; its event context is rebuilt from the verified save and journal. An overload accepting an explicit `GameEventContext` remains for compatibility only and rejects any mismatch with canonical source-derived date/origin/stage/tags. Prior matching offer command IDs remain safely idempotent after the inner core has progressed; hostile changed legacy retry contexts are rejected.
- `EventJournalState.OriginDateIso` is frozen to the original V3 migration activation date on opt-in V2/V3→V4 migration, and tickets store `CoreRevisionAtOffer` as the V3 receipt frontier when offered. `EventDecisionJournal` preserves source-only pure planner compatibility (unset origin and -1 frontier) but the actual V4 serializer **requires authoritative migration origin and nonnegative V3 frontier**.
- The V4 cold validator checks origin against the independently verified V3 activation, checks every ticket's core revision is not in the future and, when a ticket's offer revision still equals the current inner V3 revision, recomputes stage/tags against the authoritative core. The original RNG replay and versioned economic replay remain mandatory; no validator or historical fixture was relaxed.
- New engine-free regressions exercise career employed/unemployed, active study, active/closed business stage tags; reject client-supplied fake `business-active`/`employed`; detect persisted forged stage, tags, origin and future V3 receipt frontier without altering file bytes; continue interleaved V3 receipt/event cold replay and actual atomic CAS.
- **Exact source verification:** [Engine-free CI #38115518884](https://github.com/vitoo16/Startup/actions/runs/38115518884) **SUCCESS**: Simulation **66/66**, BusinessOwnership **90/90**, AstraFoundation **20/20**, RestoreInvariant **35/35**, Presentation **13/13**, ContentCatalog **13/13**, Economy **69/69**. Builds **0 errors and 0 warnings**. [CodeQL #38115517060](https://github.com/vitoo16/Startup/actions/runs/38115517060) **SUCCESS**, C# and Actions.
- **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-mcp-bridge, unity-qa-release, unity-game-economy, unity-debug-profiler. **Visual evidence:** NOT RUN (source-only, no new Unity scene/art/UI). **Save impact:** prospective **V4-only** event journal fields `OriginDateIso` and ticket `CoreRevisionAtOffer`; frozen V2/V3 GameState wire, economic suffix serializer and historical fixtures are unchanged.
- **Known limitation — historical source proof:** An older ticket whose V3 offer frontier predates the *current* V3 core can still have its stored historical stage/tags replayed for deterministic selection without a fully reconstructed receipt-prefix context proof. Production V4 gateway prevents forged contexts at offer time, but cold read currently proves authority for matching/current receipt frontiers only. Completing **all historical** source-derived context validation requires a tested V3 replay-prefix snapshot/observer (rather than copying whole checkpoint per ticket). Do not call that gate PASS.
- **Remaining M15-T01:** apply event outcome effects through one unified and replayed economic receipt, register V4 command handler in production bootstrap, implement immutable receipt-prefix context evidence, and deferred Unity/device acceptance. **Milestone PARTIAL; PR #52 Draft; not merged or CLOSED.**
- **Files changed:** `Core/EventJournalContracts.cs`, `Simulation/EventDecisionJournal.cs`, `Simulation/AuthoritativeEventContext.cs` (+ `.meta`), `Integration/EventV4CommitGateway.cs`, `Integration/StagedEventV4SaveSerializer.cs`, `tools/EconomicChecks/Program.cs`, this journal.


## Increment 7 — Exact historical event-context proof during V3 receipt replay

**Implementation SHA:** `3de3b2e8cc75737f036c96b9254d03225ef2f35e`. **Owner:** GPT-6 Technical Director.

- Extended the existing `VersionedReceiptReplay.ValidateV3` dispatcher with an optional read-only **receipt-frontier observer**, available through a small `IVersionedV3FrontierReplay` opt-in interface. The underlying `V3EconomicReceiptProcessor` and replay step loop are unchanged. The observer sees the archived V2 cutover projection and the finalized state after each authenticated V3 receipt. Legacy production callers still use the previous `ValidateV3` path.
- Added `StagedV3SaveSerializer.DeserializeVerified(bytes, observer)` and `V3SaveCompatibilitySerializer.DeserializeAndValidateWithFrontiers` as V4-only extensions. The staged V3 read still performs checksum, activation, archived frozen V2 replay, versioned V3 receipt replay, economic invariants and final-state equality. A V3 checkpoint with no suffix observations uses the **frozen original V2 checkpoint** instead of the mutable V3 projection.
- The V4 cold validator now groups tickets by `CoreRevisionAtOffer` and matches each ticket's date/stage/canonical tags against `AuthoritativeEventContext.FromCore` at its **actual V3 replay frontier**, not just at the latest/current revision. Missing or out-of-range receipt frontiers fail closed. The already existing deterministic `EventJournalReplay` still validates event selection and RNG. Replayed V3 historical state is not copied to long-lived snapshots or stored in the save.
- Added regression adversarial assertions for **resolved older tickets** after later `PurchaseCourse` and `AdvanceBoundary`: forged previous employment tags, forged previous stage, shifted offer frontier and invalid negative frontier all fail save serialization, while an unchanged interleaved source independently cold-replays and leaves the checkpoint byte-for-byte unchanged.
- **Exact-source engine-free CI:** https://github.com/vitoo16/Startup/actions/runs/38116405444 **SUCCESS** — Simulation **66/66**, BusinessOwnership **90/90**, AstraFoundation **20/20**, RestoreInvariant **35/35**, Presentation **13/13**, ContentCatalog **13/13**, EconomicChecks **69/69**. All projects **0 errors / 0 warnings**.
- **Exact-source CodeQL:** https://github.com/vitoo16/Startup/actions/runs/38116403742 **SUCCESS** (C# and Actions).
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-mcp-bridge; unity-qa-release; unity-game-economy; unity-debug-profiler.
- **Files changed:** `Core/IVersionedV3FrontierReplay.cs` + `.meta`, `Simulation/VersionedReceiptReplay.cs`, `Infrastructure/StagedV3SaveSerializer.cs`, `Infrastructure/V3SaveCompatibilitySerializer.cs`, `Integration/StagedEventV4SaveSerializer.cs`, `tools/EconomicChecks/Program.cs`, and this journal.
- **Visual evidence:** N/A (no new UI, art or scenes); Unity runtime still deferred by owner until consolidated MVP acceptance. **Save impact:** None: no new persistent fields or schema; original archived v2/v3 contracts, fixtures, serializers and save checks remain intact. The V4-only historical verification reads existing receipt and journal fields.
- **Remaining M15-T01 gates:** event outcomes still do not mutate money/skill/career by a receipt-authorized deterministic effect; production bootstrap has not yet registered the V4 command routing; the Unity runtime gate is deferred. This increment resolves the previous historical event-context attestation gap but **does not authorize claiming event rewards have applied exactly once**. **M15-T01 PARTIAL, PR #52 DRAFT, NOT MERGED.**
