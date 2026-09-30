# First playable contracts

Task: M1-T01. Owner: architecture agent (Astra role assigned by orchestrator). Date: 2026-09-30.

Status: architecture draft ready for implementation review; **M1-T01 is not complete**. The [implementation plan](../IMPLEMENTATION_PLAN_MVP.md) exists. M0-T02 tooling verification remains incomplete; this document supplies preparatory contracts without claiming dependency acceptance, compilation, or a playable build.

Scope: Developer employment, deterministic work scenes, evening study, monthly salary, and save/resume. Business contracts establish compatible boundaries for M8–M10; they do not expand the first playable. The approved plan overrides broader GDD examples. Vietnamese-first content, fictional contemporary HCMC setting, soft colored outlines, no offline advancement, nonnegative cash with arrears, and at most four businesses with one active instance per type remain locked.

## 1. Dependencies and ownership

Each arrow means “references.” This table is also the allowed dependency graph; transitive references are not a reason to add direct references.

| Assembly | Allowed project references |
|---|---|
| StartupLife.Core | None |
| StartupLife.Simulation | Core |
| StartupLife.Application | Core, Simulation |
| StartupLife.Content | Core |
| StartupLife.Infrastructure | Core |
| StartupLife.Presentation | Core, Application, Content, Infrastructure |
| StartupLife.Editor | Core, Simulation, Application, Content, Infrastructure, Presentation |
| StartupLife.Tests.EditMode | Core, Simulation, Application, Content, Infrastructure |
| StartupLife.Tests.PlayMode | Core, Simulation, Application, Content, Infrastructure, Presentation |

Core, Simulation, and Application have no Unity references. Core owns immutable definitions, IDs, state/value types, results, and save ports. Infrastructure implements those ports; the Presentation composition root supplies it to Application. Content converts validated ScriptableObjects into Core definitions once. Editor and tests are excluded from players. No runtime assembly references Editor or tests. See [ADR-001](../adr/ADR-001-assembly-boundaries.md).

State has one authoritative owner: the Application session. Domain services evaluate candidate state; they never mutate the live session or persist independently. Views receive deep immutable snapshots, not mutable state collections. Returned arrays must be copied or wrapped without exposing their writable backing store.

## 2. Values, IDs, and immutable definitions

- `ContentId`: nonempty lowercase ASCII dotted/slashed identifier; ordinal equality and sorting. Never use translated names, Unity instance IDs, or object hashes as identity.
- `RunId`: generated once when creating a save; supplied as explicit input to deterministic tests.
- `InstanceId`: run-scoped monotonic entity sequence; never reused after closure/resignation. Employers, employment periods, business instances, and course enrollments are distinct entities.
- `OperationId`: run ID plus monotonic operation sequence allocated only in a committing candidate. Automatic activities additionally bind their planned activity ID, preventing the same work slot being rewarded under a fresh request ID.
- `CommandId`: caller retry token, scoped to a run. Save canonical command kind/payload alongside each committed receipt. Same ID and same payload returns the original receipt; same ID with another payload rejects `command.id_conflict`.
- `Money`: checked signed `long`, whole VND. Cash, claims, fees, and obligations validate nonnegative magnitudes; ledger deltas may be negative. Overflow rejects the entire candidate.
- `SimDate`: Gregorian year/month/day, independent of timezone and local wall clock. `SimInstant`: date plus integer minute in `[0,1440)`. Intervals are half-open `[start,end)` and may be split at midnight.
- `FixedRate`: integer numerator at scale 10,000; positive where required. Money/progress rounding is explicit per operation. Multiplication uses exact intermediates followed by checked conversion, never floating point.
- `LocalizationKey`: stable content key. Simulation returns reason keys and typed arguments; it does not format Vietnamese strings.

Definition minimums (all include ID, revision, and display-name key):

| Definition | Required gameplay fields |
|---|---|
| CharacterStart | age bounds, cash, LearningSpeed, appearance choices, prior employment intervals, skill grants already received |
| DaySchedule | sleep/unavailable intervals, eligible owner windows, work days and shift intervals |
| Career | schedule ID, rank thresholds, salary by rank, quota configuration, career milestones, affinity tags |
| CareerScene | recipe ID, simulated duration/slot rule, Career XP, skill-exposure grants; visual seconds separate |
| Skill | five cumulative exposure thresholds defining levels 1–5 |
| Course | prerequisite skill floors, target skill/level, price, base study minutes |
| EconomyBalance | payday and living-expense due-day rules, living cost, permitted numeric bounds |
| Business | operation mode, capital, owner windows/minutes, fixed and variable cost policy, demand/capacity parameters |
| CustomerSegment | price/quality/channel/repeat coefficients |
| Event | eligibility, window, cooldown, weight, required choices, outcome definitions |

Content validation rejects duplicate IDs, missing references/localization keys, overlapping work/sleep intervals, nonpositive LearningSpeed, invalid targets/thresholds, weights not totaling 100%, and unsupported `ManagerOperable` content. No silent fallback to different content on load. Definitions referenced by an in-progress activity are resolved by pinned revision or explicit compatibility mapping.

## 3. Public C# shape

These are signatures to implement, not executable source or a claim of compilation. DTOs named below contain only Core values and collections. Use ordinary sealed classes/readonly structs supported by the pinned compiler; no engine or serializer attributes in Core.

```csharp
public interface IGameCommands
{
    CommandResult Execute(CommandEnvelope command);
    AdvanceResult AdvanceDay(CommandEnvelope request);
    AdvanceResult AdvanceMonth(CommandEnvelope request);
    GameSnapshot Snapshot();
}

public interface ISimulation
{
    Evaluation Evaluate(GameState before, GameCommand command, ContentCatalog content);
    Evaluation AdvanceBoundary(GameState before, AdvanceIntent intent, ContentCatalog content);
    Evaluation ApplyCareerScene(GameState before, PlannedActivity activity, ContentCatalog content);
    Evaluation SimulateBusinessDay(GameState before, BusinessDayPlan plan, ContentCatalog content);
}

public interface ISaveSerializer
{
    byte[] Serialize(SaveRoot save);
    LoadResult DeserializeAndValidate(byte[] bytes);
}

public interface ISaveStore
{
    SaveCandidates ReadCandidates();
    WriteResult Commit(byte[] validatedEnvelope, long expectedGeneration);
}

public interface ISaveMigration
{
    int FromVersion { get; }
    int ToVersion { get; } // exactly FromVersion + 1
    MigrationResult Migrate(SaveDocument oldDocument);
}
```

`GameCommand` variants: CreateCharacter, AcceptJob, Resign, PurchaseCourse, Study, LaunchBusiness, CloseBusiness, ChangePricing, SetReinvestment, ResolveChoice, AdvanceToBoundary. First playable supports only its relevant variants; unsupported variants reject explicitly. No command accepts caller-provided rewards, salary, eligibility, or scene ID to farm.

`CommandEnvelope`: RunId, CommandId, ExpectedRevision, command and canonical payload. New-game creation supplies its new RunId explicitly. Revision mismatch rejects `command.stale_state`; retry lookup happens first so a successful old command can return its receipt.

`CommandResult`: status (`Committed`, `AlreadyCommitted`, `Rejected`, `PersistenceFailed`, `RecoveryRequired`), reason key/arguments, authoritative revision, optional committed receipt. `SimulationOutcome`: OperationId, command/activity IDs, prior/new revision, simulated interval, ledger entries, progression deltas, history entries, and presentation cues. Deltas describe the commit; consuming them cannot apply it again.

`Evaluation`: either rejection or an isolated candidate plus outcome; evaluation has no file, clock, engine, global RNG, or live-state side effects. Application validates all candidate invariants before persistence. `AdvanceResult`: committed boundary receipts, reached instant, and stop reason (`TargetReached`, `PlayerChoice`, `PersistenceFailure`, `InvalidState`). Partial multi-boundary progress is explicit; a rejected individual command has no state changes.

## 4. Commit and playback protocol

1. Serialize commands through one gate. Resolve committed CommandId receipts, then validate revision and preconditions.
2. Copy/evaluate the current state; allocate IDs, ledger entries, skill grants, scheduler/RNG changes, current activity, and outcome together in the candidate.
3. Validate candidate. Persist one complete SaveRoot containing candidate state **and** its receipt, consumed activity marker, cues, and playback cursor.
4. Confirm the committed generation by reading/validating storage. Only then swap live state and publish the immutable snapshot and outcome.
5. On definitive write failure, discard candidate and retain the old state. If replacement may have succeeded but acknowledgement/readback fails, stop commands in `RecoveryRequired`, reread storage, and reconcile by generation and CommandId; never retry under a fresh ID or report an unambiguous rejection.

For MVP, persist every authoritative activity boundary and transaction. Day completion/pause/quit remain checkpoints; quit is never the sole durability path. This is intentionally stricter than the minimum autosave list and can be optimized only with equivalent crash tests.

Scenes render already committed cues. Skip, replay, animation events, changing render speed, or acknowledging a cue can only change playback state. A pending required choice pauses at a boundary before its effects. Choice resolution validates its saved choice instance ID and commits once. No simulation callback is driven by an animation event. Save the last completed boundary and presentation cursor on suspension; discard real elapsed time on resume.

Keep committed receipts and operation/activity IDs for the MVP. Do not prune until a separate compaction design proves old retries cannot recommit. This may grow saves; measure it in M11. A previous-generation backup restore explicitly rolls back to that generation and reports recovery; do not claim acknowledged commits survive catastrophic loss of both current generation and its journal-free storage.

## 5. Time, work, and transition boundaries

Calendar service advances only through application commands. Frame delta is a presentation pacing signal, never authoritative income or elapsed game time. `AdvanceDay` targets next midnight from the current instant; `AdvanceMonth` targets first midnight of the next calendar month, including from mid-month. Both repeatedly commit boundaries and stop at unresolved choices. Player decisions never silently default.

The day planner creates nonoverlapping sleep/unavailable, work, study, business, and free intervals. An employed weekday reserves its defined shift; office weekends and hospitality weekend shifts derive from career data. Unemployed/founder days have no employment reservation. Work is divided into configured simulated scene intervals; consuming a scene slot once grants its configured XP/exposure. Salary accrues only when the complete scheduled shift is completed; no payday cash is granted by scenes. Work completion, salary accrual, promotions, milestone grants, and new quota suffix are one boundary commit.

Accepting employment takes effect at the current free boundary; its first mandatory shift is the next full shift whose start is at or after that boundary. Resignation requested during visual playback applies at the already committed activity end; subsequent work segments stop. No reward or work is undone. An incomplete shift earns no completed-workday salary; completed scene XP is retained. UI should expose that consequence. Tenure is calendar duration of recorded employment intervals; a completed calendar month uses anniversary with end-of-month clamping. Re-evaluate tenure milestones at day boundaries even on non-work days. Salary and work XP never accrue merely from tenure.

At a date transition, finish activities ending at midnight, settle that day's business result, close the date, advance calendar, then process new-date payday, living-cost obligation, tenure milestones, and pending-choice eligibility in that order. A payday is the configured day clamped to the month's last date. Payroll is for earned claims completed strictly before that payday instant, so payday work is paid on the next payday. This cutoff applies uniformly at month end and after resignation.

## 6. Salary, money, and recoverable arrears

For each salary-rate/schedule segment in a calendar month, count scheduled workdays across the **whole month** using that segment's schedule revision (`N > 0`). Joining mid-month does not shorten the denominator. Each completed eligible shift earns `monthlySalary / N` exactly. Promotions/schedule changes create a new rate segment prospectively; prior claims are not recalculated. An unchanged rate worked for all N shifts earns exactly its monthly salary, including February and leap years.

Persist whole-VND earned claims plus exact reduced fractional-VND remainder, keyed by employment/rate/month. Fractions are accounting precision, never spendable currency. Aggregate due claims at payday, pay floor(total), and retain the fractional difference for a later payday even after resignation. Attribute consumed claims oldest first, then stable claim ID. Use exact rational intermediate arithmetic (`BigInteger` may be used internally and encoded as validated decimal strings for the fractional numerator/denominator); whole-VND amounts remain checked Int64. Serialization/AOT proof is required before this choice ships. Do not round each workday independently or discard month-end fractions.

Example: 10,000,000 VND / 22 scheduled workdays earns 5,000,000/11 VND per day. One completed day pays 454,545 VND with 5/11 VND retained. After a second day the cumulative earned amount is 909,090 + 10/11 VND; a previous 454,545 payment leaves a new payment of 454,545, not 909,090. All 22 days total exactly 10,000,000. A later month with 21 shifts uses 21, never the old denominator.

Every economic entry has stable transaction ID, operation ID, attribution (personal/employment/business), category, cash delta, and obligation/claim references. Ledger reconciliation is `cashAfter = cashBefore + sum(cashDeltas)`; accrued salary is a claim and has zero cash delta until paid.

An obligation spends min(cash, due) and records the remainder as nonnegative arrears. Income pays oldest arrears first (due instant, creation sequence, stable ID), then increases cash. No loans or interest. Course purchases, new investments, and reinvestment reject while arrears remain. Closure never deletes history or obligations. Any overflow invalidates the whole boundary; no clamping, wraparound, or partially paid ledger.

## 7. Skill grants and study

Career XP/rank live separately from transferable skills. Skill level is the maximum of earned exposure level and highest granted level, clamped to 0–5. Persist exposure progress and highest granted level separately; a grant never reduces progress or creates duplicate exposure. Career milestone grant ID is stable per character + career milestone (not employment instance), preventing resignation/rejoin farming. Starting backgrounds explicitly seed prior histories and consumed grants.

Only one active course. Purchase validates prerequisites, unmet target, available cash, and no arrears, then atomically charges and creates enrollment. Study requires positive integer eligible minutes and has no additional fee. Store work units at scale 10,000: `progress += eligibleMinutes * LearningSpeed`, target `baseStudyMinutes * 10,000`. Consume only `min(requested, available, ceil(remainingUnits/LearningSpeed))` minutes. Clamp course progress at target; excess fraction of the final integer minute gives no extra reward. Unused requested minutes remain available.

Completion calls the same idempotent grant path with enrollment completion ID. If another channel already reaches the target, complete the enrollment immediately without a second grant, refund, or further study consumption. Scene exposure remains modest data. LearningSpeed changes apply prospectively to future study minutes.

## 8. Quota and independent randomness

Developer starts with 20 slots: 8 Coding, 4 Meeting, 3 Bug Fixing, 2 Client Discussion, 2 Demo, 1 Documentation. For general decks, floor each `cycleLength * weight / 100`, distribute remaining slots by descending fractional remainder, tie by ordinal scene ID. Validate weights before allocation.

Ordering v1: among remaining types exclude the previous scene if any alternative exists; choose the type with largest remaining count, using one unbiased random selection among equal-count candidates sorted by ordinal ID. Decrement and repeat. This preserves exact counts and prevents avoidable adjacent repeats without claiming a globally optimal sequence. Carry the last consumed scene across cycle boundaries.

Use `xorshift32-v1` with nonzero saved unsigned 32-bit state: `x ^= x << 13; x ^= x >> 17; x ^= x << 5`, all modulo 2^32. DrawBelow(n) uses rejection sampling with threshold `2^32 mod n` to avoid modulo bias. Never use `System.Random`, Unity RNG, or string.GetHashCode. Scheduler and event streams have distinct nonzero initial states supplied by seed derivation v1; initialization and algorithm versions are saved and have reference-vector tests. Stream identity belongs to the save, not render order.

Seed derivation v1: SHA-256 of UTF-8 `startup-life/rng/v1/{seed}/{stream}`, where seed is invariant-culture unsigned-decimal UInt64 and stream is exactly `scheduler` or `events`. Read the first four digest bytes as little-endian UInt32. Replace zero with one; if the resulting event state equals scheduler state, increment event state modulo 2^32 and skip zero. Save both actual states. Algorithm reference from initial state 1: `270369, 67634689, 2647435461, 307599695, 2398689233`.

Persist actual deck, cursor, cycle, generation, last scene, career/rank/distribution signature, algorithm version, and RNG state. Restore uses this deck without regeneration. On a distribution change, preserve consumed prefix and its outcomes, apportion only remaining slot count under the new weights, and rebuild that suffix; increment generation and update signature in the same commit. This transition cycle is not claimed to match the new full-cycle quotas. Never mutate a committed activity's selected scene. Next complete cycle uses the new distribution normally.

## 9. Business extension boundary

Enforce at most four active business instances and at most one of each MVP type. An employed player cannot launch or personally operate FullTimeRequired; ManagerOperable rejects as unsupported. Closed businesses retain instance/history/obligations and do not occupy an active slot. Reopening creates a new instance.

Plan all businesses together, not in independent sequential calls that each see the full free day. Remove employment, sleep/unavailable, and player-selected study minutes; reserve the kiosk's required contiguous daytime block if available. Allocate remaining eligible minute intervals among compatible side businesses by equal shares, capped by requirement, redistributing unused shares until no eligible unmet demand remains. Allocate integer leftovers by stable BusinessDefinition ID, then InstanceId; resolve eligibility per interval, not just a scalar daily total. Fixed costs due are obligations regardless of output; insufficient variable-cost funds pause affected operation.

Reserve affordable operating costs using the opening shared wallet after due obligations. Allocate variable capacity deterministically across the businesses, cap by affordable costs, and release any resulting unused owner minutes back through the same deterministic allocator. No projected same-day revenue funds upfront costs. A `BusinessDayPlan` contains nonoverlapping owner allocations, cost reservations, input state revision, and content revisions. `SimulateBusinessDay` consumes that plan once and settles all affected businesses atomically. Cosmetic customers never determine orders.

## 10. Save and restore contract

SaveRoot v1 specification includes metadata (schema/content/algorithm versions, run/seed, generation, timestamps), character, calendar, employment/history/employers, skills/courses/grants, businesses/helper, economy/claims/fractions/arrears/ledger, deck and RNG states, events, history, settings, pending choice, planned/current activity, command/operation receipts, and presentation playback cursor. Fields for deferred systems can begin as empty collections. Any later shape change increments SaveVersion even if “additive.”

The file envelope stores payload bytes, checksum, generation, and schema version. Temporary write is flushed/closed, deserialized, checksum-checked, and semantically validated before promotion. Use platform-supported replacement; retain the previous validated primary as backup. Never overwrite a valid backup with corrupt primary bytes. A tested fallback may use two generation slots and a commit marker if replacement is unavailable; raw temp files are not committed saves.

Restore validates primary; falls back to validated backup only on corruption/incomplete write and reports rollback. Future schema or unsupported content rejects explicitly and preserves all files; do not quietly load an older backup and overwrite newer user progress. Sequential migrations work on a copy, validate each version, and preserve original bytes until final commit. The initial migration runner uses an explicitly synthetic v0 fixture; do not imply shipped v0 saves exist. Metadata timestamps never drive simulation time. Restore must not reapply rewards, reset RNG, recompute already selected scenes, or rerun starting grants.

## 11. Required behavior evidence

These are acceptance cases to implement, **not test results**:

| Case | Expected evidence |
|---|---|
| Rejected purchase / arithmetic overflow | byte-identical state, revision, RNG, ledger and receipts |
| Same command retry before/after restore | original receipt; no repeated charge, salary, XP, grant or deck consumption |
| Write fails before/after replace | old state or reconciled new generation; never a second commit on ambiguous failure |
| Workday → evening study → restore | identical scene deck/cursor, claims, study units, next operation and outcome |
| Render speeds, skip, replay, duplicate animation event | same authoritative state hash |
| Feb 28/29, month end, payday clamp, join/resign/promotion | exact completed-work claims, preserved remainder and no double payment |
| Career grant + course completion race | one resulting floor level; no refund or repeated grant |
| Quota cycles / rank change / reload | exact ordinary cycle counts, stable consumed prefix, deterministic suffix |
| Event draws between career draws | unchanged career sequence |
| Four businesses + study + kiosk | no minute or cash spent twice; iteration-order-independent result |
| Future/corrupt/migrated saves | preservation, explicit diagnostic, deterministic supported continuation |

## 12. Provisional work during external provisioning

**Architecture decision, amended 2026-09-30:** permit staged Windows Unity and Android work while the Mac/iOS portion of M0-T04 remains externally blocked. M0-T04 is retained as a full acceptance dependency; its external portion need not prevent reversible implementation and platform-specific verification of the first playable. This allowance changes work order only. It does not exclude iOS, reduce MVP scope, remove a task dependency from the ledger, or lower any acceptance criterion.

The reported installation/licensing progress is not baseline evidence. Proceed in these stages under one declared implementation owner (currently primary Codex following the explicit takeover; do not claim the unavailable Sol model performed the work):

1. **Engine-free preparation:** while the Editor is unavailable, author isolated Core/Simulation/Application/Infrastructure source and a .NET scenario harness against these contracts. Keep scene/UI integration outside this stage. Its reports prove only the exercised .NET behavior.
2. **Windows baseline:** execute M0-T03 after its available prerequisites are verified. Confirm the actual installed `6000.3.25f1` Editor, resolved/pinned packages, clean project import/compile, portrait Universal 2D/URP, uGUI/TMP, Vietnamese glyphs, safe area, metadata, and baseline screenshot. Installation completion or an active license alone does not satisfy this stage. Establish the local M0-T04 test/build entrypoints with honest failure exits and retained smoke-test reports; use the documented Editor/CLI fallback if MCP is unavailable and record its status separately.
3. **Provisional first playable integration:** after the Windows baseline is evidenced, integrate the reviewed simulation through the Application command/snapshot boundary into a minimal Unity shell. Work through M1–M7 in their functional order; prove the relevant pure behavior in Unity EditMode before wiring dependent gameplay, then run PlayMode input/playback/save-resume cases and collect portrait visual evidence. Android build/test work proceeds once matching Android modules/toolchain are verified; physical-device checks require an actual available device. An Editor screenshot or APK build is not a physical-device pass.

This permits the first playable's Developer workday, evening study, salary/payday, and save/resume integration using the approved small development placeholders. It does not authorize bypassing missing core behavior, migration coverage, content validation, Vietnamese art direction, or asset gates to make a convincing shell. Load each task's required skills before its implementation. Keep scene/prefab/asset edits in the Editor or generated Editor scripts; maintain one owner per affected system. The pure simulation remains independent of scenes and balance data remains outside presentation code.

Record the downstream work as **provisional implementation — external dependency verification pending** with separate .NET, Unity EditMode, Unity PlayMode, Android build, Android device, and iOS evidence. Retain original test fixtures and replay the same cases on each applicable platform. M0-T04 and the full M1–M7 milestone gates remain unchecked pending their complete dependency/evidence review; the overall first playable must not be called accepted. M0-T03 may be assessed only against its own complete stated acceptance evidence. The implementation-ledger owner records partial results and the Mac/iOS external blocker without treating it as a failed game behavior or a passed platform test.

When a Mac/iOS route becomes available, pin the matching Editor/packages, establish the missing M0-T04 baseline/CI/build route, and rerun the complete relevant first-playable and save/serializer/IL2CPP/lifecycle matrix on the required iPhone before closing the blocked acceptance dependency. Any platform-specific fixes require affected cross-platform regressions. No dual-platform, release-candidate, or milestone-completion claim is allowed on Android-only evidence.

Sequencing amendment closeout: skills used are the previously loaded session orchestrator, gameplay guardian, gameplay/director, MCP bridge, QA release, debug profiler, and economy skills. Validation: compared this allowance against M0-T03/M0-T04 and M1–M7 dependencies and evidence requirements; no acceptance text or ledger checkbox changed. Visual evidence: none. Save impact: none. Files changed by this amendment: this contract only. Known limitations: actual Editor/import/test/build/device results still require evidence; Mac/iOS remains external. Commit: none by this reviewer.

## Architecture review evidence — 2026-09-30

- A PowerShell local-link check inspected this contract and all nine ADRs: 10 documents, 9 ADRs, 0 broken relative links.
- An independent in-memory traversal of the nine-node allowed dependency table found no cycle. This checks the specified graph only; asmdefs do not yet have verified compiler evidence.
- JavaScript UInt32 bitwise reference calculation from state 1 produced the five xorshift32 values printed above. Exact BigInt division reproduced 454,545 + 5/11 after one 22-denominator workday, 909,090 + 10/11 after two, and exactly 10,000,000 after 22. These are specification arithmetic checks, not C# or Unity test results.
- Contract review covers employment, study, command rejection/retry, transactional storage ambiguity, business time/cash conservation, and restore. Implementation/AOT/device tests in section 11 remain pending. M1-T01 is not marked complete.

## Session closeout

- Owner model: architecture agent assigned Astra role; runtime implementation belongs to Sol.
- Skills used: startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-mcp-bridge, unity-qa-release, unity-debug-profiler; unity-game-economy added for salary/arrears decisions. Generic monetization/cloud defaults are excluded by the approved offline MVP scope.
- Acceptance criteria: preparatory contract coverage and acyclic dependency review only; M1-T01 gate awaits prerequisite verification and review.
- Tests: documentation reference/dependency and worked arithmetic checks recorded in the architecture review; no Unity/EditMode/PlayMode/IL2CPP tests performed here.
- Visual evidence: None; documentation only, no UI/assets/scenes changed.
- Save impact: initial specification only; no runtime save or shipped schema modified.
- Known limitations: M0-T02 not verified complete; Editor licensing/connection and device build evidence pending. Serializer selection, rational arithmetic AOT, mobile replace durability, and runtime performance require implementation evidence.
- Files changed: this document and ADR-001 through ADR-009 under docs/adr.
- Commit: Not created by this agent.
