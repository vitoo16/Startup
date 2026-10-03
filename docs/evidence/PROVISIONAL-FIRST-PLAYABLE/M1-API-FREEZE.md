# M1 public outcome/playback API freeze

Date: 2026-10-03  
Owner model: GPT-5.6 Sol  
Source closure: independent Astra re-verification after PR #13

## Freeze decision

The first-playable public outcome/playback API is frozen for downstream implementation.

Independent review concluded:

- F1 restored playback provenance: **CLOSED**
- F2 course Presentation read model: **CLOSED**
- M1 outcome/playback: **CLOSED**
- Presentation API sufficiency: **SUFFICIENT**
- historical retry equivalence: **CLEAN**
- playback reward independence: **CLEAN**
- H1/H2/R2/M2: **CLEAN**
- Save/schema: **version 1 remains valid; no migration required**
- remaining BLOCKER/HIGH/MEDIUM/LOW findings in scope: **0**

The independent review reported 24/24 course probes and 54/54 total focused probes. Repository suites remained 45/45 SimulationChecks, 19/19 H1/H2/R2, and 35/35 M2.

PR #13 was merged before the independent re-verification. The merge commit on main is
`6a4580b6509b7f84d20652260938295a55535ad5`, whose second parent is the reviewed final PR head
`fe171576d303edb6394fd739ed3e25ab6dcf99b5`.

## Frozen Presentation-facing surface

Downstream code may depend on these public semantics.

### Command result

`CommandResult.Outcome` is nullable and is authoritative for successful committed or already-committed commands.

`SimulationOutcome` publishes:

- operation and command identity;
- activity identity;
- prior/new revision;
- start/end simulation instants;
- minutes consumed;
- committed cue;
- playback cursor;
- cash and ledger changes;
- employment/career/rank changes;
- skill changes;
- nullable typed course change;
- grants and history entries.

Historical retry reconstructs the same semantic outcome from retained authoritative receipts through production simulation; it does not mutate live state or storage.

### Snapshot

`GameSnapshot` publishes:

- current simulation instant/revision;
- cash/arrears/career/rank;
- cue;
- `CurrentActivityId`;
- `PlaybackCursor`;
- nullable `ActiveCourseSnapshot`;
- skill levels and history.

`ActiveCourseSnapshot` publishes course instance, definition, target skill, progress units, target units, and target level.

### Course transition

`CourseChange` is nullable and appears only when a command changes course state.

Kinds:

- `Activated`
- `Progressed`
- `Completed`

Completion reasons:

- `None`
- `StudyTargetReached`
- `SkillTargetAlreadyMet`

The previous ambiguous public fields `GameSnapshot.StudyUnits`,
`SimulationOutcome.CourseInstanceId`, and `SimulationOutcome.StudyUnitsDelta` must not return.

### Playback provenance

Restore publication requires root `CurrentCue` and `PlaybackCursor` to match deterministic production replay when the required content is available.

Unavailable required content retains `UnsupportedContent` precedence.

`AcknowledgePlayback` remains reward-independent.

## Freeze guard

`scripts/Test-M1ApiContractStatic.ps1` is a dedicated static CI gate.

It protects:

- required public DTO/property names;
- course change enums and completion reasons;
- current activity/playback publication;
- removal of the three ambiguous legacy course fields;
- replay binding of root cue/cursor;
- typed course-change construction;
- containment of candidate serializer validation;
- absence of mutable state DTO exposure from the public outcome contract.

This guard is intentionally separate from the 31-check static Unity foundation suite so evidence counts remain semantically distinct.

## Change policy after freeze

A downstream implementation may consume the frozen API.

Changing the frozen names or semantics requires:

1. an explicit architecture reason;
2. updated deterministic/public-consumer regression coverage;
3. save-impact analysis;
4. Astra review before downstream consumers are updated.

Do not use Gemini or bulk content generation to modify this API.

Gemini may now be used for read-only/batch work such as test matrices, content inventories, localization inventories, and data drafting that conforms to the frozen contract.

## Runtime/platform boundary

This freeze is based on engine-free .NET/static and independent architecture evidence.

It does not close:

- Unity Editor import/compile;
- EditMode;
- PlayMode;
- IL2CPP/AOT;
- Android/iOS builds/signing;
- physical-device save/resume/filesystem behavior;
- on-device receipt-replay performance.

Those remain separate M0/M7 platform gates.

## Task ledger boundary

No milestone/task checkbox is changed by this freeze record.

The implementation plan still requires runtime/Unity evidence for milestone completion even where substantial engine-free implementation already exists.
