# M15-T01 — Transaction-planning event journal (source increment 2)

**Owner:** GPT-6 Technical Director. Branch: `feat/m15-event-selection-foundation`.
**Milestone:** M15-T01 remains PARTIAL; not CLOSED.

## Source behavior
- Pure C# prospective event journal: detached candidate transitions for event offer and choice resolution.
- Window/category policy: cooldown and rolling category occurrence caps; one offered event per simulation calendar day.
- Durable **candidate** ticket identity, stable choice/outcome IDs, checked revision, rejection without mutating source.
- Exactly-once outcome *selection* on command ID retry; incompatible retries are rejected. No actual cash, XP, course, or business mutation is performed.
- Journal never trusts client-supplied occurrence counts; reconstructs from its committed ticket history. Selectors draw only the independent EventRng and leave the career scheduler untouched.
- Structural/content validation catches missing/changed categories, mismatched IDs, duplicate commands, bad chronology, false resolution, and invalid revisions.
- Pure prospective event journal can be JSON serialized and restored in isolation.

## Gates
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-mcp-bridge; unity-qa-release; unity-game-economy; unity-debug-profiler.
- **Tests:** engine-free .NET regression/CI and CodeQL evidence required on final SHA; Unity tests NOT RUN.
- **Visual evidence:** NOT RUN; no scene, asset or UI edits.
- **Save impact:** **NONE in active GameSession save.** The new DTO is currently a prospective journal, deliberately not attached to schema 2 or V3. Integration must add a versioned migration and enforce receipt/CAS + cold-replay validation. An isolated journal JSON roundtrip must not be represented as real-game save/resume.
- **Known limitations:** not yet invoked by authoritative GameSession, no event reward mutation, no production event content, no pending-choice UI, no Unity runtime/device verification.
- **Files changed:** Assets/StartupLife/Scripts/Core/EventJournalContracts.cs(.meta), Simulation/EventDecisionJournal.cs(.meta), tools/SimulationChecks/Program.cs, this evidence file.

## Follow-on integration contract
Attach event journal to an explicitly versioned save successor instead of silently extending frozen v2/v3 bytes. Open offers and resolution must both be receipt-backed CAS transactions under GameSession, with verified replay of journal history and isolated PRNG state. Only after that integrate M15-T02 events/visual dialogs.
