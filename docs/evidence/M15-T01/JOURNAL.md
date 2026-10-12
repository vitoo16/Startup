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


## Increment 8 — Exactly-once event-scoped gameplay outcome receipts

**Implementation SHA:** `4e9ba5d89a7e143dcee04307b2eda66959c6f7f4`. **Owner:** GPT-6 Technical Director.

- `EventChoiceDefinition` now optionally authors immutable **`GrantedTags`** for event-specific gameplay effects. Allowed tags must be unique and carry the `event/` namespace; attempts to grant trusted V3-derived conditions such as `employed`, `business-active` or skill/career identity directly are rejected.
- The prospective V4-only `EventJournalState` includes `OutcomeReceipts` (one `EventOutcomeReceipt` per resolved ticket: immutable canonical receipt ID = `ticket.Id + "/effect"`, `TicketId`, `ResolutionCommandId`, `OutcomeId` and granted tags). `EventDecisionJournal.Resolve` appends this receipt **within the same journal revision and physical CAS transaction** as the choice. Neither a second CAS nor a separate ledger engine is introduced.
- `EventDecisionJournal.Validate` rejects missing, reordered, duplicated, forged or unauthored receipts and checks that the persisted effect tags match the **catalog's chosen outcome** exactly. The journal remains deterministic on cold replay, and retries with identical command IDs return `AlreadyCommitted` without adding a receipt or granting the tag again.
- `AuthoritativeEventContext.FromCore` applies only prior resolved `event/` outcome tags to future event eligibility. When checking a saved historical offer, the V3 receipt-prefix observer passes the ticket ID so a ticket cannot use its own later reward (nor a later ticket's reward) to qualify for itself. The authoritatively derived V3 employment, business, study and calendar tags remain independent.
- Regression `M15 outcome effect receipts unlock later events exactly once and reject forged replay` demonstrates first-day networking choice grants `event/contact`, next-day `contact-followup` is eligible only afterward, retry is idempotent, and removed/modified/duplicated receipts or altered event context fail validation.
- The real-file V4/V3 integration test now additionally simulates **ambiguous after-replace atomic event resolution**, verifies cold retry `AlreadyCommitted` and exactly one event receipt, then executes real receipt-backed `GameSession.AdvanceBoundary` commands into the next simulation day; the second event is offered **solely because** the previous choice granted `event/networked`. A fresh `AtomicFileSaveStore` cold read reproduces both tickets and the single applied receipt. Forging the earlier applied receipt fails strict V4 serialization and leaves the file byte-identical.
- **Exact implementation verification:** [Engine-free CI #38117911959](https://github.com/vitoo16/Startup/actions/runs/38117911959) **SUCCESS**: Simulation **66/66**, BusinessOwnership **90/90**, AstraFoundation **20/20**, RestoreInvariant **35/35**, Presentation **13/13**, ContentCatalog **13/13**, EconomicChecks **70/70**; all builds **0 warnings / 0 errors**. [CodeQL #38117910130](https://github.com/vitoo16/Startup/actions/runs/38117910130) runs for Actions and C# independently; final status must be checked on exact HEAD.
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-mcp-bridge; unity-qa-release; unity-game-economy; unity-debug-profiler.
- **Files changed:** `Core/EventDefinitions.cs`, `Core/EventJournalContracts.cs`, `Simulation/EventDecisionJournal.cs`, `Simulation/AuthoritativeEventContext.cs`, `Integration/StagedEventV4SaveSerializer.cs`, `tools/EconomicChecks/Program.cs`, and this journal.
- **Save impact:** staged **V4-only** additional `OutcomeReceipts` member and optional data-driven outcome tags; V2/V3 serialized schemas, original archives, and frozen fixtures are unchanged. The prospective V4 format has not been activated for production users; a deliberate V4 format/migration policy is needed before shipping support for any pre-receipt experimental schema4 checkpoint. No existing production V4 save migration is claimed.
- **Known limitations:** This is a genuine exactly-once **event-system eligibility effect**, NOT a wallet/skill/career reward. There is **no event cash, XP or salary debit/credit** in V3 economic history yet; do not invent a cash overlay that the V3 gameplay engine cannot spend or cold-replay. Next work must integrate full economic event outcome transactions into one authenticated V4 gameplay replay frontier and register V4 routing in production bootstrap. Visual/UI/Unity runtime acceptance remains deferred.
- **Milestone:** **M15-T01 PARTIAL**, PR #52 **Draft**, **NOT MERGED / NOT CLOSED**.


## Increment 9 — UI-facing V4 single-command result and pending-choice read model

**Implementation SHA:** `4c9f9b3f673e8b4f63222c3bab303538636b79bc`. **Owner model:** GPT-6 Technical Director.

- Inspected actual live Unity `Presentation/StartupLifeBootstrapper.cs`, `FirstPlayableFlow.cs`, `WorkShiftPlaybackController.cs` and `LifeScreenViewController.cs`. Production currently instantiates a schema-3 `GameSession` as its `IGameCommands` authority; WorkShift playback expects genuine `SimulationOutcome` for cues; Advance Day/Month expects replayable batched receipt identities and targets. **Replacing the bootstrap with an unqualified V4 gateway now would regress playback and/or batch retry semantics.** Therefore this increment does **not** silently turn on a second session/store or claim live bootstrap registration.
- `EventV4CommitGateway.ExecuteCore` now carries the real inner `GameSession` `CommandResult` only after successful full V4 CAS and cold verification; write failures do not claim an applied outcome. Exact V3 command operation/cue/time/economic delta remain attached for UI consumers and no new gameplay engine is added.
- Added `EventV4CommitGateway.ExecuteGameplay`: a presentation-compatible single-command `CommandResult` that exposes the **global V4 revision** (`v3Core.Revision + eventJournal.Revision`) and forwards independently verified inner V3 `SimulationOutcome` (including cues, operation identity, duration, cash delta). A repeated committed command returns global `AlreadyCommitted` without synthesizing a second outcome. `SimulationOutcome.Revision` retains its precise **inner V3 receipt revision**, not the outer V4 sequence.
- Added `EventV4CommitGateway.ReadPendingChoice()` and immutable `EventPendingChoiceSnapshot` with run identity, global revision, event/ticket IDs and the authored immutable choice roster. The UI can use the returned revision for `Resolve` and may not supply claimed stage/tags/eligible choices. No pending journal choice returns null; invalid/unverified V4 read rejects.
- Engine-free real-file regression now checks an actual character-creation `SimulationOutcome.CashDelta`, pending event's authored `choice.LabelKey`, successful resolution clearing the read model after an ambiguous write retry, and a later `AdvanceBoundary` returning global V4 revision **5** while preserving the genuine inner V3 outcome revision **3** and playback cue. A stale retry returns `AlreadyCommitted` at V4 revision 5 without fabricating a new outcome.
- **Exact-source CI:** [Engine-free #38119453337](https://github.com/vitoo16/Startup/actions/runs/38119453337) **SUCCESS**, Simulation **66/66**, BusinessOwnership **90/90**, AstraFoundation **20/20**, RestoreInvariant **35/35**, Presentation **13/13**, ContentCatalog **13/13**, EconomicChecks **70/70**; 0 errors and 0 warnings. [CodeQL #38119450193](https://github.com/vitoo16/Startup/actions/runs/38119450193) **SUCCESS** for C# and Actions.
- **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-mcp-bridge, unity-qa-release, unity-game-economy, unity-debug-profiler.
- **Files changed:** `Integration/EventV4CommitGateway.cs`, `tools/EconomicChecks/Program.cs`, this journal.
- **Visual evidence:** Not applicable to source-only API; no Unity scene/view/asset changed. **Save impact:** None; no new wire fields, snapshots or schema migration. All original historical V2/V3 and speculative V4 save assumptions remain unchanged.
- **Remaining gates — do not close M15-T01:** Real Unity bootstrap and `IGameCommands` routing must preserve `AdvanceDay/AdvanceMonth` **batch-root identity, durable target and correct stop/resume**, including pending choice interruption; no silent iterative shortcut that could double-advance after retry. UI pending-choice presentation/interaction still needs integration with actual view. Event cash/XP effects must become spendable, jointly replayed V3 receipts before shipping; event tag effects only are currently proven. Unity runtime acceptance deferred by owner. **M15-T01 PARTIAL; PR #52 Draft, not merged.**


## Increment 10 — Resumable Day/Month V4 batches with single V3 receipt authority

**Verified code SHA:** `baf7015ad6a0c3d2f74de7b28f2efd4d865d639b`. **Owner:** GPT-6 Technical Director.

- `EventJournalState.AdvanceBatches` stores one durable intent per batch root: root command identity, mode `day/month`, original V3 receipt frontier/date, and the **original computed target date**. Each batch registration increments the V4 journal revision once; individual time steps increment the **existing inner V3 receipt revision**, preserving one physical outer generation and one `AtomicFileSaveStore` CAS.
- `EventAdvanceBatchHistory` deterministically derives fixed-length internal child IDs from the run/root identity, scans verified V3 receipts for step count, and rejects missing/duplicated/out-of-order/orphaned child identities, non-`AdvanceBoundary` payload, forged targets, wrong start frontiers, and child dates exceeding the registered target. Internal namespace `v4batch/` is rejected for public gameplay commands. The child receipt uses **the real existing `GameSession` engine**, no duplicate scheduler/economy or parallel disk writer.
- `EventV4CommitGateway.AdvanceDay/AdvanceMonth` creates the V4 durable intent once, then emits one V3 core receipt per step through the same `ExecuteCoreInternal` path and verified CAS as ordinary commands. Retry with **the same root command** detects the stored original mode/target and resumes from the last verified child. Wrong-mode retries and new conflicting batch roots are rejected; direct outside `AdvanceBoundary` cannot bypass an active batch. Pending choice blocks a batch without losing its target; after choice resolution the same original root resumes. A retry of a completed batch performs no write.
- One correctness bug identified by CI: `EventDecisionJournal.Validate` originally imposed an event-ticket-only upper revision bound; now the exact upper bound includes **one registration revision per batch intent** and final revision still must equal `ticketCount + resolvedCount + batchCount`. No historic V2/V3 validators, content economic thresholds or tests were weakened.
- Added fail-closed pre-character start guard so a batch request before `CreateCharacter` cannot install an impossible-to-progress active intent. Recovery-without-state reports an unavailable/default instant rather than a made-up date.
- **Actual file-backed tests** simulate ambiguous `after-replace` during the first child after registering an intent, validate that exactly one child persisted, inject a pending event and prove `PlayerChoice` stop on cold retry, resolve the choice and resume to original next-day target with the original stale root. A second month-root moves from Oct 31 to Nov 1 through actual V3 receipt time. Adversarial serializations of changed target, shifted start frontier, missing/duplicate batches all fail and leave bytes unchanged. Pre-character attempt is also tested read-only.
- **Exact code CI:** [Engine-free #38124818316](https://github.com/vitoo16/Startup/actions/runs/38124818316) **SUCCESS** — Simulation **66/66**, BusinessOwnership **90/90**, AstraFoundation **20/20**, RestoreInvariant **35/35**, Presentation **13/13**, ContentCatalog **13/13**, EconomicChecks **71/71**; **0 errors/warnings**. [CodeQL #38124816473](https://github.com/vitoo16/Startup/actions/runs/38124816473) **SUCCESS** for C# and Actions. Final evidence-only/test HEAD gets a separate verification run.
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-mcp-bridge; unity-qa-release; unity-game-economy; unity-debug-profiler.
- **Files changed:** `Core/EventJournalContracts.cs`, `Simulation/EventAdvanceBatchHistory.cs` + `.meta`, `Simulation/EventDecisionJournal.cs`, `Integration/StagedEventV4SaveSerializer.cs`, `Integration/EventV4CommitGateway.cs`, `tools/EconomicChecks/Program.cs`, this journal.
- **Save impact:** New **prospective schema-4-only** `AdvanceBatches` journal records, with all inner V2/V3 frozen bytes, original economic receipt evaluator and external V4 envelope schema number unchanged. Existing *experimental* schema-4 checkpoints without new members are **not** claimed compatible; formal migration/format-version policy required before general production save activation.
- **Visual/runtime evidence:** None; this is a source-only infrastructure and deterministic gameplay regression increment. Deferred Unity runtime/device QA still NOT PASS.
- **Remaining M15-T01:** V4 has not been installed into `StartupLifeBootstrapper`/unified `IGameCommands` presentation flow; pending choice view and touch interaction still require runtime plumbing; V3 financial/career/skill event effects need spendable single-authority replay (tag effects only have exactly-once proof). **PARTIAL; PR #52 remains Draft; not merged or CLOSED.**


## Increment 11 — Presentation command-port bridge (source-only)

**Implementation SHA:** `7b98ef873e7e142ec9cb04c0d40758654761eb59`. **Owner:** GPT-6 Technical Director.

- Added `Integration/EventV4GameCommands.cs` plus Unity `.meta`. It implements the existing `IGameCommands` contract by delegating `Execute`, `AdvanceDay`, `AdvanceMonth` to `EventV4CommitGateway`, and projecting only a validated V4 read into the existing `GameSnapshot` and `FirstPlayableFlow`. No forked flow, economy runner, CAS writer or separate session.
- Added a real-file EconomicChecks regression in the existing M15 batch fixture: migrated V4 snapshot starts with the expected run/revision and the same adapter sees the committed character name and global revision after V3-driven `CreateCharacter`.
- **Exact code CI:** [Engine-free #38148437187](https://github.com/vitoo16/Startup/actions/runs/38148437187) SUCCESS. CodeQL on documentation HEAD remains to be checked separately. No Unity runtime or device run claimed.
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; repo workflow and skills manifest. **Tests:** engine-free CI above. **Visual evidence:** none (no Unity UI/scene changes). **Save impact:** none (no new wire field/schema version). **Files changed:** `Integration/EventV4GameCommands.cs`, `.meta`, `tools/EconomicChecks/Program.cs`, this journal.
- **Known limitations:** The adapter is intentionally NOT registered in `StartupLifeBootstrapper`: currently there is no event pending-choice screen or approved automatic V3→V4 production migration. The V3 bootstrap remains the only live production writer to prevent an invisible blocking choice. Event money/XP still lacks spendable joint economic replay. M15-T01 PARTIAL; PR #52 Draft, not merged.


## Increment 12 — Opt-in uGUI pending-event choice surface

**Code SHA:** `8933e77dcc5c8be904507c2c180334573ba18963`. **Owner model:** GPT-6 Technical Director.

- Added `Presentation/EventChoiceOverlay.cs` and Unity `.meta`, installed only via `LifeScreenViewController.BindEventChoices` when the authorized V4 gateway is supplied. The current V3 `StartupLifeBootstrapper` remains untouched; **the V4 production cutover is NOT enabled**, since no approved authored M15-T02 runtime catalog or full event effect integration is present.
- The overlay derives every ticket, option and localization key from verified `EventV4CommitGateway.ReadPendingChoice()`; displays a blocking 96+ reference-unit uGUI/TMP choice surface with a real ScrollRect/Viewport/Mask/VerticalLayoutGroup and reusable choice buttons. Refresh does not invoke gameplay commands and does not duplicate UI children.
- Buttons route through `EventV4CommitGateway.Resolve` with authoritative run/ticket/global revision. An ambiguous write retains the **same** command ID and limits retry to the **same option**; no independent UI eligibility engine or save writer exists. Invalid V4 checkpoint leaves input blocked instead of making a choice disappear.
- Added `EventLocalizationCatalog` to keep `event.choice.*` keys separate from existing `dashboard.*` invariants; the existing `DashboardLocalizationEditorSync` now synchronizes and checks both manifests. No hand-edit of Unity StringTable YAML or additional localization pipeline.
- Added `StartupLife.Integration` to the presentation assembly's dependencies (one-way) and added one headless PresentationCheck for event localization integrity. Previous dashboard-only tests are unchanged.
- **Exact code CI:** [Engine-free #38150197883](https://github.com/vitoo16/Startup/actions/runs/38150197883) SUCCESS: Simulation **66/66**, Business **90/90**, Astra **20/20**, Restore **35/35**, Presentation **14/14**, Content **13/13**, Economy **71/71**; compiler **0 warnings/errors**. Exact final documentation SHA and CodeQL must be verified separately.
- **Skills used:** `startup-life-session-orchestrator`, `startup-life-gameplay-guardian`, `ui-ugui` plus `scrollview-setup`, `localization`; project workflow/manifest and Unity source contracts inspected. **Visual evidence:** no Unity Editor/runtime/device screenshot; on-screen layout, safe area and localization table updates await deferred Unity QA. **Save impact:** none; no schema changes, no V2/V3 fixtures or rules changed.
- **Files changed:** `Presentation/EventChoiceOverlay.cs` + `.meta`, `Presentation/EventLocalizationCatalog.cs` + `.meta`, `Presentation/LifeScreenViewController.cs`, `Presentation/StartupLife.Presentation.asmdef`, `Editor/DashboardLocalizationEditorSync.cs`, `tools/PresentationChecks/StartupLife.PresentationModel.csproj`, `tools/PresentationChecks/Program.cs`, this journal. Dashboard manifest was preserved without introducing event-prefixed keys.
- **Known limitations:** opt-in UI is source-created but not connected to live production bootstrap; no authored event title/body/choice text assets registered for a production event catalog yet, so no runtime claim. Wallet/XP effects are not jointly replayed and spendable; they remain blocked. **M15-T01 PARTIAL, PR #52 Draft and unmerged.**


## Increment 13 — Authored Vietnamese event story catalog, deterministic eligibility and cold-replay proof

**Implementation SHA:** `25f5ef1e798f32d6066d0b57e14a67d59f6594f1`. **Owner model:** GPT-6 Technical Director.

- Added `Content/FirstPlayableEventContent.cs` (+ Unity meta): **12** Vietnamese first-playable stories (life, employed career, courses, business, networking/founder chains), each with a title/body, at least two choices and authored IDs. A single immutable source generates both `GameEventDefinition` objects and localized keys/translations; no duplicated UI copy or independent eligibility logic. Four category policies control event cadence. Consequences grant `event/`-namespace narrative unlocks **only**, never pretend to award spendable cash, Career XP or skills.
- Extended immutable `GameEventDefinition` with **optional paired title/body keys**; previously authored fixtures and test catalogs remain valid with no new required constructor parameters. V4 read model now returns these keys; optional uGUI overlay renders the title and story inside the same scroll layout, with fallback to existing localized UI keys for legacy test definitions.
- `EventLocalizationCatalog` concatenates the authored narrative translations through the **existing Unity StringTable editor sync**. No manual asset YAML or extra localization engine.
- New headless content tests check unique immutable IDs, policies and controlled event effects, plus auth-tag/stage gated chains; presentation tests check every title/body/choice has a Vietnamese translation. New real-file economic test migrates a V2 checkpoint, creates a character using V3, offers a real authored event, verifies the title/body after V4 cold load, resolves one option, and retries the same command with an `AlreadyCommitted` result after another cold restore.
- **Exact code SHA CI:** [Engine-free #38162072893](https://github.com/vitoo16/Startup/actions/runs/38162072893) **SUCCESS**, Simulation **66/66**, Business **90/90**, Astra **20/20**, Restore **35/35**, Presentation **14/14**, Content **15/15**, Economic **72/72**, **0 compilation warnings/errors**. Exact-head documentation and CodeQL checks to be verified separately.
- **Skills used:** `startup-life-session-orchestrator`, `startup-life-gameplay-guardian`, `localization`, previously loaded `ui-ugui` for overlay continuation; repo `AGENTS.md`, workflow, milestone evidence and MVP/GDD reviewed.
- **Visual evidence:** None; localized Unity StringTable authoring sync/runtime layout/device QA remains deferred (not PASS). **Save impact:** no wire format or schema-version change; V2/V3 validators and frozen fixtures untouched. New V4 authored content has new IDs and only event-scoped tag receipts.
- **Files changed:** Core `EventDefinitions.cs`, Content `FirstPlayableEventContent.cs` + meta, Integration `EventV4CommitGateway.cs`, Presentation `EventChoiceOverlay.cs`, `EventLocalizationCatalog.cs`, headless .csprojs and ContentCatalog/Presentation/Economic tests, this evidence journal.
- **Known limitations:** The production Unity bootstrap is still intentionally V3; no auto-migration/cutover and no live event-offer scheduler yet. This 12-story increment is **not** the complete approximately-20-event M15 content target. Gameplay spendable money/XP effects must still be implemented as replayable authoritative economic receipts. Unity visual/device/test play remains NOT RUN. PR #52 remains Draft, M15-T01 PARTIAL, do not merge/close.


## Increment 14 — Twenty authored Vietnamese events; receipt-gated chain integrity

**Owner model:** GPT-6 Technical Director. **Implementation SHA:** `eb152d42f2d3f7f8346162bc91384a6c3c2b6a7c`.

- Completed the initial **20/20** immutable authored Vietnamese narratives within the existing single `FirstPlayableEventContent.Stories` source: 8 new follow-ups on mentoring, team learning, savings priorities, study-circle presentations, business customer feedback, supplier relationships, pitch feedback and community collaboration. The same source generates definitions and localization entries, no duplication in the UI or alternate event authoring runtime.
- Each follow-up requires existing authenticated `event/` receipt grants (and authoritative employment/course/business tags when appropriate). None directly changes wallet, Career XP or Skill; event choices remain **tag-only** until the authoritative V3 economic receipt integration is complete.
- Extended `ContentCatalogChecks` to assert exactly 20 unique events and defend against forward or missing unlock dependencies, tag grant collisions, invalid authoring and mutable catalog entries. A second chain regression exercises **Offer → Resolve(join) → Offer(follow-up) → cold journal replay**, rejects tampered event tags, and verifies that `Resolve(decline)` grants no tag and consumes no extra selection RNG when the follow-up is ineligible.
- The previously green real-file V4 authored-offer/resolve/cold-replay test was preserved, and all previous V2/V3 migration tests and frozen fixtures remain unchanged.
- **Verification on implementation SHA:** [Engine-free CI #38163758166](https://github.com/vitoo16/Startup/actions/runs/38163758166) **SUCCESS**: Simulation 66/66, Business 90/90, Astra 20/20, Restore 35/35, Presentation 14/14, Content **16/16**, Economic 72/72, **0 errors / 0 warnings**. [CodeQL #38163756317](https://github.com/vitoo16/Startup/actions/runs/38163756317) **SUCCESS** for C# + GitHub Actions.
- **Skills used:** `startup-life-session-orchestrator`, `startup-life-gameplay-guardian`, `vietnam-art-direction`, `localization`; `AGENTS.md`, workflow, GDD, MVP plan and M15 evidence were read. **Acceptance:** complete 20 story content and prove non-forgeable authored unlock chains at engine-free source level. **Visual evidence:** NOT RUN (text authoring only; Unity Editor/device localization/layout checks deferred). **Save impact:** no save schema or receipt format changes; V2/V3 bytes and validators untouched.
- **Files changed:** `Content/FirstPlayableEventContent.cs`, `tools/ContentCatalogChecks/Program.cs`, this journal.
- **Known limitations:** M15-T01 **PARTIAL**. V4 production bootstrap, runtime offer trigger/pending choice UI, and unified spendable event cash/XP effects still need implementation and runtime acceptance. Before shipping dynamic catalog updates, a versioned V4 event catalog policy must pin historical replay definitions across content revisions; source CI does not prove such upgrades. PR #52 remains Draft, unmerged.


## Increment 15 — Reject V4 batch-root command-ID collisions at event admission

**Owner model:** GPT-6 Technical Director. **Branch:** `feat/m15-event-selection-foundation`. **Status:** M15-T01 PARTIAL, PR #52 remains Draft and unmerged.

- The existing V4 cold validator already prohibited a day/month batch root from sharing an ID with an event offer or resolution, but `EventV4CommitGateway.Offer` and `Resolve` only checked V3 receipts. This allowed an invalid attempt to enter the planner before failing later at candidate validation.
- Both event entrypoints now use one shared `ConflictsWithCoreOrBatch` predicate. It rejects collisions with authenticated V3 receipt identities and all durable batch roots, and disallows the reserved `v4batch/` child namespace for any external event command. Retry identities belonging to previously committed event offers/resolutions still flow to the existing event journal idempotency logic.
- Extended the existing real-file V4 day/month batch test to reject root and child-namespace reuse while an interrupted batch is active, while a choice is pending, and after the batch reaches its target. Each rejected attempt must leave saved bytes unchanged.
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler; unity-cli (project and published community skills read before editing).
- **Tests:** Engine-free EconomicChecks CI requested for the new exact commit; no local .NET or Unity runner available. Result must be recorded from the exact-head CI, not inferred from static inspection. Legacy suites and frozen thresholds unchanged.
- **Visual evidence:** NOT RUN, no visible UI or art changes in this increment. Unity runtime/device acceptance still deferred.
- **Save impact:** None. V2/V3 frozen serializers, archived fixtures, V4 wire model, reward model and receipts are unchanged. Only command admission and regression coverage changed.
- **Known limitations:** Event cash/XP effects remain tag-only, production V4 bootstrap and automatic event offer flow not wired, catalog-version replay policy unresolved. This increment alone does not close M15-T01.
- **Files changed:** `Assets/StartupLife/Scripts/Integration/EventV4CommitGateway.cs`, `tools/EconomicChecks/Program.cs`, this journal.


## Increment 16 — Reserved batch namespace verified by planner and V4 cold journal replay

**Owner:** GPT-6 Technical Director. **Status:** M15-T01 PARTIAL, PR #52 Draft, NOT MERGED.

- `EventDecisionJournal` now applies one shared `ConflictsWithBatchIdentity` check during prospective offers, resolutions, and structural journal validation. Neither a batch root nor the reserved `v4batch/` internal child prefix can be admitted as an event command identity. Null batch records remain a closed-validation error rather than a null-reference bypass.
- EconomicChecks extends the existing real-file V4 resumed batch fixture: direct planners reject reserved command IDs; forged pending-ticket offer IDs and forged resolved-ticket IDs (including a matching forged outcome receipt) fail verified V4 serialization without mutating persisted bytes.
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler; unity-cli.
- **Tests:** Engine-free CI for this source commit pending exact-head verification. No local Unity Editor, .NET runtime or on-device QA in this session. Do not claim engine-free SUCCESS until the workflow reports it.
- **Visual evidence:** NOT RUN; nonvisual journal admission change.
- **Save impact:** None. No schema changes, no historical fixture mutations and no new reward ledger. Previously unapproved/invalid internal-namespace journal identities now fail closed.
- **Known limitations:** Production bootstrap remains V3; events still grant authenticated `event/` tags only, not spendable cash/XP. Runtime and catalog-version acceptance remain deferred.
- **Files changed:** Simulation/EventDecisionJournal.cs; tools/EconomicChecks/Program.cs; this journal.


## Increment 17 — Pending event freezes core gameplay until authoritative resolve

**Owner:** GPT-6 Technical Director. **Branch:** `feat/m15-event-selection-foundation`; **M15-T01 remains PARTIAL**, PR #52 Draft/unmerged.

- Changed the existing `EventV4CommitGateway.ExecuteCoreInternal` pending choice gate to reject **all gameplay commands** except `AcknowledgePlayback` (which is a presentation-only acknowledgement). Previously only `AdvanceBoundary` was blocked, allowing purchases, resignation or business actions while a modal event decision remained unresolved. Existing V3 gameplay engine, V4 CAS and journal are reused; no second command authority.
- Retains retry precedence: a committed command ID is still recognized as `AlreadyCommitted` before pending-state gating; stale IDs and conflicting IDs still reject before evaluation.
- Added actual-file EconomicChecks assertions using the existing V2→V3→V4 integrated scenario: both `PurchaseCourse` and `Resign` must return `event.choice_required`, leave cash and V3 receipt count stable and preserve the entire save file byte-for-byte. Existing post-resolution course charge and cold retry tests remain unchanged.
- **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-game-economy, unity-mcp-bridge, unity-qa-release, unity-debug-profiler; project workflow/manifest reviewed.
- **Tests:** Engine-free CI must be checked for the exact implementation SHA; Unity Editor/PlayMode/device tests NOT RUN.
- **Visual evidence:** Not applicable to this source-only admission rule; no view or art altered.
- **Save impact:** None; no wire/schema change or frozen historical fixture edit.
- **Known limitations:** Staged V4 only, no production bootstrap cutover, automatic event-day trigger, spendable event cash/XP or production Unity proof.
- **Files changed:** Integration/EventV4CommitGateway.cs, tools/EconomicChecks/Program.cs, docs/evidence/M15-T01/JOURNAL.md.


## Increment 18 — Remove duplicate V4 event replay during validation

**Owner:** GPT-6 Technical Director. **Milestone:** M15-T01 PARTIAL, PR #52 Draft/unmerged.

- `StagedEventV4SaveSerializer.ValidatePayload` previously called the full deterministic `EventJournalReplay.Validate` twice per V4 save validation: once before V3 cold receipt/frontier admission and again after it. Removed only the second, provably redundant invocation. The intervening frontier observer, `EventAdvanceBatchHistory.Validate`, and the sealed source validation are read-only; the journal is not modified between the checks. This avoids recomputing weighted event selection and occurrence/category indexes twice on every read/write.
- Added an EconomicChecks adversarial fixture mutation changing a persisted offered `EventId` to a different authored event that was not selected; the single surviving event replay must still reject the corrupted V4 payload. Earlier context/tag/receipt/RNG corruption and real-file save tests remain intact, with **no lowered validator thresholds**.
- **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-game-economy, unity-mcp-bridge, unity-qa-release, unity-debug-profiler; repository workflow reviewed.
- **Tests:** Exact-head engine-free CI pending. No timed device performance claim; removed duplicate full replay by inspection and regression only.
- **Visual evidence:** NOT RUN, no visible UI or asset changes. **Save impact:** None: V2/V3 frozen fixtures, V4 wire model and serializer output bytes remain unchanged.
- **Known limitations:** No automatic event scheduling from day boundary, spendable financial XP event rewards, live V4 bootstrap cutover, or Unity runtime/device QA; remain explicitly NOT IMPLEMENTED/NOT RUN.
- **Files changed:** Integration/StagedEventV4SaveSerializer.cs, tools/EconomicChecks/Program.cs, docs/evidence/M15-T01/JOURNAL.md.


## Increment 19 — Replay-safe negative event-day checkpoints (journal foundation)

**Owner:** GPT-6 Technical Director. **M15-T01 PARTIAL**, PR #52 Draft. Code-only source slice.
- A new **prospective V4-only** `EventNoEligibleDayRecord` captures a day, exact offer context, independent V3 receipt frontier, and command ID when the production selector finds no eligible event. Every recorded empty day consumes exactly one V4 journal revision but no event RNG.
- `EventDecisionJournal.CheckDay` reuses the existing offer implementation with a persistence flag; ordinary `Offer` retains read-only no-eligible behavior for legacy callers. Both share cold-idempotent command identities and same-day/chronology admission checks.
- `EventJournalReplay` traverses ticket offers and empty days chronologically with the existing `EventSelectionHistory.Select`, so a forged empty checkpoint cannot cover an eligible event or silently change RNG. No duplicated selector/algorithm.
- **Skills:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-game-economy, unity-mcp-bridge, unity-qa-release, unity-debug-profiler.
- **Verification:** engine-free CI pending exact SHA. **Visual:** NOT RUN (source only). **Save:** prospective V4 journal adds NoEligibleDays, with mandatory full cold replay; V2/V3 bytes and immutable fixtures untouched. Historical experimental V4 checkpoints without this field need a future explicit format policy, not silent activation.
- **Remaining:** V3 historical frontier binding and opt-in gateway/day-batch scheduling to be completed before auto-event claims; production bootstrap unchanged. **Files:** Core/EventJournalContracts.cs, Simulation/EventDecisionJournal.cs, Simulation/EventJournalReplay.cs, this journal.


## Increment 21 — Opt-in automatic event windows within durable Advance Day/Month

**Owner:** GPT-6 Technical Director. **M15-T01 remains PARTIAL**; PR #52 Draft/unmerged.
- `EventV4CommitGateway` supports explicit `automaticDailyChecks: true` (default **false**, preserving every existing V4 batch fixture and production V3 bootstrap). Before each verified V3 `AdvanceBoundary` child in `AdvanceDay/Month`, it checks whether the **current** simulation day already has a ticket or a persisted empty-day check. Otherwise it invokes the existing `CheckCurrentDay` transaction and performs a fresh cold read before moving time.
- A newly offered pending choice immediately returns `PlayerChoice` without advancing simulation time; the already durable batch root can resume after resolution. A no-eligible day is committed and cannot be redrawn after a crash. Direct public boundary commands are rejected in this opt-in mode so callers cannot bypass automatic windows; only registered batch children can advance.
- File-backed regression tests automatic offer/stop, byte-identical pending retry, V3 time and event resolution, original batch-root resume, next-day no-eligible check, independent RNG and cold idempotent completion.
- **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-game-economy, unity-mcp-bridge, unity-qa-release, unity-debug-profiler.
- **Tests:** exact-head CI pending. **Visual:** NOT RUN. **Save impact:** only staged V4 journal NoEligibleDays, no schema3 or immutable fixture changes. **Known limitations:** live production V4 bootstrap cutover, event cash/XP and Unity runtime proof remain pending.
- **Files changed:** Integration/EventV4CommitGateway.cs, tools/EconomicChecks/Program.cs, this journal.


## Increment 22 — Backward-compatible V4 daily-check field admission

**Owner:** GPT-6 Technical Director. **Task:** M15-T01 staged save compatibility. **Status:** PARTIAL, PR #52 remains Draft; no merge.

- Prior experimental V4 checkpoints were serialized before `EventJournalState.NoEligibleDays` was introduced. `DataContractJsonSerializer` does not execute ordinary property/field initializers on deserialization. Without an explicit migration default, such authentic older checkpoints deserialize `NoEligibleDays == null` and fail strict replay despite otherwise valid state. Added a single `[OnDeserializing]` initializer for this **additive field only**. An explicitly serialized `null` still fails closed through the existing journal validator.
- Added an engine-free regression that constructs a real V4 pending-choice envelope, removes only the new optional JSON member, **recomputes** its checksum, restores it from the actual atomic file store without rewriting bytes on read, resolves the persisted choice using existing V4 CAS, reopens the upgraded checkpoint, verifies the member is now serialized, and cold retries the outcome byte-identically. An explicitly null member remains rejected.
- This does not add or duplicate V3 evaluation logic, loosen event replay, change balance data or mutate historical V2/V3 fixtures. Existing V4 negative-day rows must still pass all authoritative V3 frontier checks.
- **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-game-economy, unity-mcp-bridge, unity-qa-release, unity-debug-profiler.
- **Tests:** exact-commit engine-free CI to be recorded; local Unity runtime and device QA NOT RUN. **Visual evidence:** NOT RUN (save-only).
- **Save impact:** older staged V4 wire shape admitted in memory; write-after-commit upgrades optional field, no bulk migration of bytes. **Known limitations:** spendable cash/XP event effects require a V3-native receipt kind with V4 event proof; no new reward API added in this increment. V4 production bootstrap stays inactive.
- **Files changed:** `Assets/StartupLife/Scripts/Core/EventJournalContracts.cs`, `tools/EconomicChecks/Program.cs`, this journal.


## Increment 23 — Expose existing V3 financial read projections through validated V4 state

**Owner:** GPT-6 Technical Director; **M15-T01 PARTIAL**, PR #52 Draft/unmerged.

- `EventV4CompatibilitySerializer.Project` serialized the V3 core into a V4 `GameState` clone; its `EconomicV3` member is intentionally ignored by wire serialization, so V4 snapshots lost the authenticated financial-policy/settlement read context. The projection now attaches a new in-memory `EconomicV3Payload` wrapper with `Current` bound to the projected object and **already validated** V3 records/activation. The physical V4/V3 wire bytes, economic receipts and settlement evaluation remain exactly as before.
- The shared `BusinessFinanceProjection` accepts a V4 state *only* with an attached verified event journal, V3 economic records and matching current-state identity. `LifeDashboardProjection` includes those committed settlements; no new calculations.
- `EventV4GameCommands` exposes committed finance, business portfolio/operations and life dashboard via the **same existing** projections used by `GameSession`; optional economic catalog parameter is backward-compatible for existing V4 caller construction. The adapter reads verified V4 state fresh; never copies wallet/provenance into an alternate ledger.
- Extended existing real-file V2→V3→V4 GameSession test to check the attached V3 economic record graph and presentation readouts at the exact global V4 CAS revision.
- **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-game-economy, unity-mcp-bridge, unity-qa-release, unity-debug-profiler.
- **Tests:** exact-head engine-free CI pending. **Visual evidence:** Unity runtime/device NOT RUN; no view asset changes. **Save impact:** none (nonserialized read-model attachment only), V2/V3 frozen fixtures unchanged.
- **Known limitations:** V4 is still a staged source path, not production bootstrap; the new shared finance read models do not create spendable event money or XP. Event rewards require V3 receipt + V4 cause coupling.
- **Files changed:** Integration/EventV4CompatibilitySerializer.cs; Application/BusinessFinanceProjection.cs; Application/LifeDashboardProjection.cs; Integration/EventV4GameCommands.cs; tools/EconomicChecks/Program.cs; this journal.


## Increment 24 — Fix V4 event-only commit economic attachment after CI regression

**Owner:** GPT-6 Technical Director. **M15-T01 PARTIAL**, PR #52 Draft.

- **Detected by exact-head CI:** commit `4aad58b` compiled and CodeQL passed but M15 event-only CAS tests failed. Cause: `EventV4CompatibilitySerializer.RestoreCoreProjection` clones the V4 GameState through a serializer which ignores `EconomicV3`. The new economic read-attachment invariant therefore rejected the resulting unbound core when `Offer/Resolve` tried to commit. This was not an event-selector error.
- Extracted one shared `AttachEconomic(target, source)` helper and used it in both `Project` and `RestoreCoreProjection`. It creates a new wrapper and preserves the same already-verified economic records without changing the source `Current` reference, avoiding duplicated V3 provenance code and transaction side effects.
- The existing real-file M15 migration, Offer/Resolve, CAS failed write/retry, Day/Month batch, and opt-in daily event tests exercise this repaired path; the new V4 presentation readout assertions remain in place.
- **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-game-economy, unity-mcp-bridge, unity-qa-release, unity-debug-profiler.
- **Tests:** new exact-head CI required. **Visual:** NOT RUN. **Save:** no wire or schema changes, frozen historical sources untouched.
- **Known limitations:** production bootstrap and monetary/XP event payout still pending.
- **Files changed:** Integration/EventV4CompatibilitySerializer.cs, this journal.


## Increment 25 — Financial event reward authoring and fail-closed admission

**Owner:** GPT-6 Technical Director. **Milestone:** M15-T01 remains PARTIAL, PR #52 Draft.
- Introduced immutable `EventChoiceReward` content with signed VND cash delta and nonnegative Career XP (invalid zero-effect or nonrepresentable minimum-long amounts reject). `EventChoiceDefinition.Reward` is optional; existing twenty Vietnamese event choices remain tag-only and unchanged. These numbers are **authored intents, not executed grants**.
- `EventV4CommitGateway.Resolve` refuses a choice containing a financial reward with `event.reward_processor_unavailable` until a V3-native authenticated reward receipt evaluator exists. No V4-only wallet edits or shadow ledger. Pending choice and save bytes are preserved. Tag-only choices continue through the existing CAS.
- The V4 journal validator also rejects a previously resolved tag-only choice if catalog content is edited to introduce a payout. This prevents silently adopting reward effects for old committed saves; an explicit catalog/migration policy will be necessary before enabling them.
- **Tests:** engine-free SimulationChecks validates authoring and immutability; real-file EconomicChecks offers a rewarded event, proves rejected payout leaves cash/pending/revision/bytes unchanged, verifies an alternate tag-only resolution is cold-idempotent, and rejects a rewritten catalog's payout on old resolved bytes. Await exact-head CI.
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler.
- **Visual evidence:** NOT RUN (no production UI/art changes).
- **Save impact:** None to frozen V2/V3 and current V4 wire; authored reward data is not persisted/applied. Historical paid-event catalog migration and V3-native reward command still NOT IMPLEMENTED.
- **Known limitations:** Real spendable cash/XP outcomes remain blocked until both GameSession receipt replay and V4 cause binding verify exactly once. Production V4 bootstrap/Unity runtime QA remain deferred.
- **Files changed:** EventDefinitions.cs, EventDecisionJournal.cs, EventV4CommitGateway.cs, tools/SimulationChecks/Program.cs, tools/EconomicChecks/Program.cs, this journal.


## Increment 26 — One reward admission policy in the event planner

**Owner:** GPT-6 Technical Director. **Status:** M15-T01 PARTIAL; PR #52 Draft/unmerged.

- Moved unauthenticated monetary/XP choice rejection from the V4 gateway down to `EventDecisionJournal.Resolve`, the single planner already responsible for every event outcome transition. The adapter now only maps planner statuses and commits valid transitions; no duplicate reward gates. The unchanged journal validator still fails closed if a previously tag-only committed choice acquires a reward through a catalog edit.
- Expanded engine-free SimulationChecks to prove direct planner rejection keeps revision and pending identity stable while a tag-only fallback continues to resolve.
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler.
- **Tests:** exact-head CI pending. **Visual:** NOT RUN. **Save impact:** none, V2/V3 and V4 wire untouched. **Known limitations:** economic reward receipt and gameplay bootstrap remain future increments.
- **Files changed:** Simulation/EventDecisionJournal.cs; Integration/EventV4CommitGateway.cs; tools/SimulationChecks/Program.cs; JOURNAL.md.


## Increment 27 — Prevent reward-only event softlocks

**Owner:** GPT-6 Technical Director. **Status:** M15-T01 PARTIAL; PR #52 Draft/unmerged.

- During the temporary fail-closed phase before V3 financial reward receipts, journal/catalog validation now refuses an event in which **every** choice has an authored financial reward. Such a catalog would create a pending choice with no resolvable path. Tag-only fallback choices keep gameplay recoverable, and the ordinary planner still rejects rewarded outcomes without persistence.
- SimulationChecks verifies that reward-only catalogs reject at admission, not only at resolve time.
- **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-game-economy, unity-mcp-bridge, unity-qa-release, unity-debug-profiler. **Tests:** exact-head engine-free CI pending. **Visual:** NOT RUN. **Save:** no wire/schema change. **Limitations:** no spendable event cash/XP yet; bootstrap & Unity QA deferred.
- **Files changed:** EventDecisionJournal.cs, SimulationChecks/Program.cs, JOURNAL.md.


## Increment 28 — Resume durable V4 time batches from the existing presentation port

**Owner model:** GPT-6 Technical Director. **Milestone:** M15-T01 PARTIAL; PR #52 Draft/unmerged.
- `EventV4GameCommands` now reattaches a newly generated presentation `AdvanceDay/AdvanceMonth` request to the one incomplete matching batch **using only a freshly verified V4 journal**. The underlying gateway retains strict original-root, child identity and CAS validation; no additional scheduler/save source or runtime-only batch cursor.
- The real-file opt-in daily event regression uses a **different UI command ID after cold pending-choice resolution** and asserts only one durable batch record exists, original root still replays idempotently, and the target date is reached once.
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler.
- **Tests:** exact-head engine-free CI pending; **Visual:** NOT RUN; **Save impact:** none; **Known limitations:** no V3 monetary/XP event payout, production opt-in bootstrap/runtime smoke still pending. **Files changed:** Integration/EventV4GameCommands.cs; tools/EconomicChecks/Program.cs; this journal.


## Increment 29 — Staged Unity bootstrap V4 cutover without a second game flow

**Owner model:** GPT-6 Technical Director. **Status:** M15-T01 PARTIAL, PR #52 Draft/unmerged.
- Added default-**off** `enableVerifiedEventV4` to `StartupLifeBootstrapper`. When enabled for an existing validated V2/V3/V4 save, one `EventV4CommitGateway` owns the same existing physical `AtomicFileSaveStore` CAS path, `EventV4GameCommands` powers the original `FirstPlayableFlow`, and the existing life screen binds the *same* verified V3 financial/business dashboard projections and localized V4 pending-choice overlay.
- The V3 branch and fresh V3 character-creation behavior are untouched. V4 opt-in refuses a missing/unreadable/unsupported checkpoint with an explicit fatal/log reason; it never creates an unauthenticated initial V2 file via File.WriteAllBytes. Fresh V4 genesis is a separate future atomic migration gate.
- The presentation bridge resumes a durable unfinished batch after restart with the authenticated original root. No second clock engine, receipt book, wallet, or separate save file.
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler.
- **Tests:** exact-head engine-free CI/static Unity checks pending; no Unity Editor/PlayMode device pass claimed. **Visual evidence:** NOT RUN, bootstrap feature flag defaults OFF. **Save impact:** none for existing V2/V3 fixtures; V4 opt-in commits schema4 through verified gateway only. **Known limitations:** no authenticated financial event payouts, fresh V4 genesis, or on-device integration test. **Files changed:** Presentation/StartupLifeBootstrapper.cs; JOURNAL.md.


## Increment 30 — Recovery-safe presentation batch reconciliation

**Owner model:** GPT-6 Technical Director. **Status:** M15-T01 PARTIAL, PR #52 Draft/unmerged.

- `EventV4GameCommands.ResumeInterruptedBatch` now checks the verified-store load status before consulting batch history. Missing, unreadable or untrusted projections are delegated to the unchanged authoritative gateway, which returns its own `RecoveryRequired` stop without a UI adapter exception or synthetic state.
- The file-backed M15 automatic-day regression explicitly invokes a presentation adapter against a missing save and verifies `RecoveryRequired`; existing durable batch-root resume and exactly-once date tests remain unchanged.
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler.
- **Tests:** exact-head CI pending; **Visual evidence:** NOT RUN (Unity deferred); **Save impact:** none; **Known limitations:** opt-in V4 bootstrap requires existing save; V3-native event payout not implemented. **Files changed:** EventV4GameCommands.cs, tools/EconomicChecks/Program.cs, JOURNAL.md.


## Increment 31 — Atomic fresh-run V4 genesis (no separate seed writer)

**Owner model:** GPT-6 Technical Director. **Milestone:** M15-T01 PARTIAL; PR #52 Draft/unmerged.

- Opt-in Unity V4 now accepts **missing** save paths: create one random V2 initial state, migrate it in memory with frozen V2→V3→V4 serializers, and inject a strictly validated **zero-revision, uncreated** genesis checkpoint into the existing gateway. This is shared with V3 via one `CreateInitialState` helper. **No seed file** is written on bootstrap, refresh, pause, or before creating the character.
- The gateway's `Read` uses the verified in-memory genesis **only** when the atomic store reports truly `Missing`; corrupt, unsupported, unreadable and backup states remain authoritative store results. The first real V3 `CreateCharacter` command produces a canonical V3 receipt + revision one, wrapped as the first V4 envelope and published through the existing `AtomicFileSaveStore.Commit(..., expectedRevision: 0)`. Subsequent reads never return genesis once the file is present.
- File-backed regression covers no-file UI snapshot, failed first temp write, ambiguous after-promote recovery, cold reopen without genesis, byte-identical retry, and refusal to overwrite corrupt save bytes. Same file store, no new ledger, no duplicate time engine.
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler.
- **Tests:** exact-head CI required. **Visual evidence:** Unity runtime/device NOT RUN. **Save impact:** no schema changes and frozen V2/V3 fixtures untouched; first V4 on-disk artifact is already a verified command receipt, not an untracked seed. **Known limitations:** spendable monetary/XP event payouts still blocked pending V3/V4 receipt coupling.
- **Files changed:** EventV4CommitGateway.cs, StartupLifeBootstrapper.cs, tools/EconomicChecks/Program.cs, JOURNAL.md.


## Increment 32 — Fresh V4 genesis admission guard

**Owner model:** GPT-6 Technical Director. **Status:** M15-T01 PARTIAL; PR #52 Draft/unmerged.

- Added a guard specific to transient new-run V4 revision zero: manual event `Offer` and any core command other than `CreateCharacter` return `event.character_required` with no disk write. Prevents an uncreated character's seed from being persisted by an event-only transaction or playback acknowledgement.
- Extended the same file-backed genesis test with negative admission assertions. Historical V2→V4 tests that intentionally offer during replay experiments remain unchanged because they do not use transient genesis.
- **Skills:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-game-economy, unity-mcp-bridge, unity-qa-release, unity-debug-profiler. **Tests:** exact-head CI pending. **Visual:** NOT RUN. **Save:** no schema changes. **Remaining:** spendable V3-native cash/XP event payouts, Unity runtime/device QA. **Files:** EventV4CommitGateway.cs, EconomicChecks/Program.cs, JOURNAL.md.


## Increment 33 — Close V3/V4 command-identity collision during cold replay

**Owner:** GPT-6 Technical Director. **Milestone:** M15-T01 PARTIAL; PR #52 Draft/unmerged.

- The V4 command gateway already refused new event command IDs taken by a committed V3 receipt, but cold validation only rejected V3 receipt collisions for no-eligible-day records. A forged historical offer or resolution ID could reuse a real V3 receipt identity after recomputing the outer JSON envelope checksum. This violates the shared exactly-once command namespace.
- Added one linear-time set of **independently replay-verified** V3 receipt IDs to `StagedEventV4SaveSerializer.ValidatePayload`. It rejects collisions from any V4 offer ID, resolution ID, no-eligible-day ID, or durable Day/Month batch root. Removed the older quadratic special case for empty days; the check remains at the same or stronger gate.
- Expanded the existing file-backed V3→V4 event/finance integration test to forge a pending offer ID and later a resolved ticket ID *plus matching effect receipt*, with recomputed valid SHA envelope checksums. Both fail full cold deserialize as Corrupt, and no mutated save is committed.
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler.
- **Acceptance:** cold V4 cannot duplicate any V3 core receipt identity; real event/gameplay CAS flow and migrated saves still pass. **Tests:** exact-head engine-free CI pending. **Visual:** NOT RUN, no view changes. **Save impact:** no wire/schema or frozen fixture changes. **Limitations:** spendable V3-native event cash/XP requires single-revision V3/V4 composite receipt coupling; not enabled here.
- **Files:** Integration/StagedEventV4SaveSerializer.cs; tools/EconomicChecks/Program.cs; this journal.


## Increment 34 — Restore WorkShift one-boundary command under V4 auto-event policy

**Owner:** GPT-6 Technical Director. **Status:** M15-T01 PARTIAL, PR #52 Draft.
- Found a production integration bug: `WorkShiftPlaybackController` uses the single `FirstPlayableFlow.AdvanceBoundary` command to animate scheduled career scenes, while the opt-in V4 gateway previously rejected all direct boundaries (only Day/Month batches worked). This made the work action nonfunctional in V4 mode.
- Changed only `EventV4CommitGateway.ExecuteCoreInternal`: after normal command/expected-revision/ID/pending validation, a single direct boundary on an unprocessed day invokes the existing deterministic `CheckCurrentDay` CAS. An offered event stops the work step and persists pending choice without V3 clock receipt. An ineligible day persists its negative checkpoint, cold-reads verified state, then executes the SAME original boundary ID against the new global revision. Previously evaluated dates continue directly; pending decisions always block time.
- Added real-file EconomicChecks with V3 character, work boundary triggers event, event blocks work with byte-exact retries, cold resolution allows one V3 boundary, no second event draw, and duplicate work receipt retry is byte-identical.
- **Skills:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler.
- **Tests:** exact-head CI pending. **Visual evidence:** Unity runtime/PlayMode not run. **Save impact:** no schema or historical fixture changes. **Known limitations:** spendable financial event rewards still fail closed pending composite V3/V4 receipt proof; no Unity runtime acceptance yet.
- **Files changed:** EventV4CommitGateway.cs, tools/EconomicChecks/Program.cs, JOURNAL.md.


## Increment 35 — No-event workday and missing core runner admission

**Owner:** GPT-6 Technical Director. **Status:** M15-T01 PARTIAL; PR #52 Draft/unmerged.

- Consolidated the existing V3 core-runner admission to run **before** any opt-in workday scan. An unconfigured evaluator must not commit a daily journal revision without being able to advance the V3 clock.
- Added independent file-backed workday fixture where an authored event is ineligible (requires employment): checks first daily temp write fails without byte mutation, next retry commits exactly one negative-day V4 checkpoint plus one actual V3 boundary, cold duplicate ID reads without advancing time, and second same-day work step cannot draw/check events again. Also tests missing evaluator rejects before any scan.
- **Skills:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler.
- **Tests:** exact-head CI pending. **Visual evidence:** NOT RUN (source/backend only). **Save:** no wire changes and frozen fixtures untouched. **Outstanding:** spendable V3-native event payout and Unity runtime QA.
- **Files changed:** EventV4CommitGateway.cs; tools/EconomicChecks/Program.cs; JOURNAL.md.


## Increment 36 — Single daily-window eligibility helper across work and batches

**Owner:** GPT-6 Technical Director. **Status:** M15-T01 PARTIAL, PR #52 Draft/unmerged.
- Consolidated duplicate "has day already been checked?" predicates from the new single-work-boundary path and the preexisting durable Day/Month batch path into one `HasCheckedDay(EventJournalState, dateIso)`. The two command types now share identical semantics for event-ticket and negative-day checkpoints. No new scheduler or duplicate predicate is retained.
- The opt-in direct-work eligible/ineligible regression suites and preexisting batch tests cover both callers; source-only refactor with no save or art change.
- **Skills:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-game-economy, unity-mcp-bridge, unity-qa-release, unity-debug-profiler. **Tests:** CI exact SHA pending. **Visual:** NOT RUN. **Save:** none. **Known limits:** event money/XP composite receipt and Unity PlayMode remain incomplete.
- **Files:** EventV4CommitGateway.cs, JOURNAL.md.


## Increment 37 — Proof-bound event reward settlement planning (no wallet mutation)

**Owner:** GPT-6 Technical Director. **M15-T01 PARTIAL; PR #52 Draft/unmerged.**

- Added immutable `EventRewardSettlementPlan` derived through the *existing* `EventDecisionJournal.PlanReward` validator. It binds one exact pending V4 ticket, authored choice/outcome, external resolution command ID, next V3 core receipt frontier and the **single** next global V4 generation. No arithmetic values originate from UI or caller-owned commands.
- The planner rejects forged/unowned/reserved/duplicate command identities, mismatched pending ticket/date, stale combined revision, unaffordable negative cash, arithmetic overflow, and Career XP when there is no active employment. Cash/XP amounts are read from the catalog; the candidate GameState is never mutated.
- Added engine-free SimulationChecks for valid cross-domain binding and all the rejection conditions. `Resolve` intentionally still fails closed with `event.reward_processor_unavailable`; this increment is the trusted pure settlement covenant for later composite receipt implementation, NOT a claim that money/XP is now spendable.
- **Skills:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler.
- **Tests:** exact-HEAD engine-free CI pending. **Visual:** NOT RUN. **Save impact:** none (no persisted field, no V3 wire change). **Known limits:** V3 payout receipt and V4 cross-replay proof, Unity runtime/device QA are unfinished.
- **Files:** Core/EventDefinitions.cs; Simulation/EventDecisionJournal.cs; tools/SimulationChecks/Program.cs; this journal.


## Increment 38 — Preserve nonforgeable settlement-plan constructor across assembly boundaries

**Owner:** GPT-6 Technical Director. **Status:** M15-T01 PARTIAL, PR #52 Draft/unmerged.
- Exact-head CI found `CS1729`: Core's `internal EventRewardSettlementPlan` constructor was inaccessible from the Simulation assembly. Moved the immutable value type next to its sole constructing authority `EventDecisionJournal` in Simulation, retaining an **internal constructor** and public read-only properties. No public factory that could bypass validation.
- Strengthened plan admission to require projected event RNG and pending choice identity to equal the checked journal. Added adversarial tests for diverging RNG, pending choice and a reused V3 receipt ID in addition to the existing overflow/affordability tests.
- **Skills:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler. **Tests:** exact-head CI pending. **Visual:** NOT RUN. **Save:** none; schema/fixtures unchanged. **Limitations:** no spendable reward receipt yet.
- **Files:** Core/EventDefinitions.cs, Simulation/EventDecisionJournal.cs, tools/SimulationChecks/Program.cs, JOURNAL.md.


## Increment 39 — Cold-verified settlement plan bridge regression

**Owner:** GPT-6 Technical Director. **Status:** M15-T01 PARTIAL, PR #52 Draft/unmerged.

- Added a real-file V2→V3→V4 CAS test exercising the existing `EventDecisionJournal.PlanReward` against a **cold-verified pending event** with a genuine V3 character receipt, not a hand-constructed state. It proves the plan binds the next V3 receipt frontier, one next global V4 revision, canonical authored amount, pending ticket and external resolution command ID.
- The same test verifies planning is read-only, the current gateway still rejects premature cash payouts without spending the pending choice or modifying disk bytes, and a forged state revision cannot obtain a plan.
- **Skills:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler. **Tests:** exact-head CI required. **Save impact:** none. **Visual QA:** NOT RUN. **Remaining:** composite V3-native reward receipt and atomic V4 outcome coupling not yet implemented.
- **Files changed:** tools/EconomicChecks/Program.cs, this journal.


## Increment 40 — Canonical V3 reward receipt intent, public execution forbidden

**Owner model:** GPT-6 Technical Director. **M15-T01 PARTIAL**, PR #52 remains Draft/unmerged.

- Reserved append-only `CommandKind.SettleEventReward = 13` (existing V2/V3 command ordinals unchanged) for the future V4-authorized composite event settlement. The V3 `SimulationEngine` has no evaluator branch for this kind and therefore fails closed on direct V3 replay and execution.
- Added `EventRewardSettlementPlan.ToCoreCommand()` and exact `MatchesCoreCommand()`. The canonical command has **zero user-owned amounts**: it encodes ticket, choice and event IDs; authenticated cash and XP values remain only in the authored-choice settlement plan. A different event, option, kind or nonzero amount never matches.
- `EventV4CommitGateway.ExecuteCoreInternal` explicitly rejects public attempts to execute this command before calling the V3 evaluator or reaching any CAS writer. A future composite settlement must use a separate proof-bound internal path; never open this public command case.
- Extended SimulationChecks with canonical receipt round-trip and field-falsification tests, plus real-file EconomicChecks proving even a correctly encoded plan command submitted through public gameplay is rejected without modifying the save.
- **Skills:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler.
- **Tests:** exact-head engine-free CI pending. **Save impact:** existing receipt ordinal bytes and historical fixtures unchanged; new command type never admitted by V3 without V4 proof. **Visual evidence:** NOT RUN. **Remaining:** trusted V3 evaluator and V4 composite CAS+cross-replay binding for spendable cash/XP; Unity runtime QA pending.
- **Files:** Core/Contracts.cs; Simulation/EventDecisionJournal.cs; Integration/EventV4CommitGateway.cs; SimulationChecks; EconomicChecks; JOURNAL.md.


## Increment 41 — Reuse native V3 wallet and career rules for isolated reward candidates

**Owner:** GPT-6 Technical Director. **Status:** M15-T01 PARTIAL, PR #52 Draft/unmerged.

- The existing `SimulationEngine` now has `ApplyPlannedEventReward`: a **candidate-only** evaluator that consumes the immutable authored `EventRewardSettlementPlan`. It verifies V3 run/date/core revision, expected cash and career XP frontiers, preflights checked arithmetic and affordability, then uses the **same native ledger Entry(...) and career Promote(...)** paths as normal gameplay. It changes neither receipt history nor global revision and has no store writer.
- The plan now binds RunId, CheckedDateIso, ExpectedCashVnd and ExpectedCareerXp, derived only from the V4 planner input; the new evaluator rejects a V3 candidate from a mismatched source before posting anything.
- SimulationChecks exercises a cash+XP candidate ledger entry, rank/XP path, identity preservation and standalone V3 `SimulationEngine.Evaluate(SettleEventReward)` rejection with zero grant. This is **not spendable money yet**; it is an isolated trusted-candidate effect awaiting one proof-bound V4/V3 composite save-and-replay path.
- **Skills:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-debug-profiler. **Tests:** exact-head CI pending. **Visual:** NOT RUN. **Save:** no wire/schema/frozen fixture changes; new command still fail-closed in ordinary V3.
- **Files:** Simulation/EventDecisionJournal.cs, Simulation/SimulationEngine.cs, tools/SimulationChecks/Program.cs, JOURNAL.md.
