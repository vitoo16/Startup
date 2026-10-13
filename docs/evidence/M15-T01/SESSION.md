# M15-T01 — Deterministic event selection foundation (source slice)

**Owner:** GPT-6 Technical Director. **Base:** `1a05decf3a6a365b7eca36383263270557b6281c`.
**Milestone status:** PARTIAL. This is a source-only increment, not M15-T01 closure.

## Implemented

- Validated, immutable event definitions with stable event/category/choice/outcome IDs, required and forbidden tags, minimum calendar year/stage, weights, repetition windows, per-event cooldown and lifetime occurrence caps.
- Detached event selection context containing run-relative dates and occurrence history. No mutable collection aliases escape constructors.
- Pure weighted selector that uses the **existing independent event PRNG stream**, orders candidates by stable event ID and does not draw when the event window contains no eligible options.
- Selection is read-only. **No event can grant cash/XP, create a pending choice or mutate save state via this module.** Future authoritative command must atomically commit selected event, occurrence history and RNG state, with replay-safe outcome IDs.

## Verification expectations

Engine-free SimulationChecks cover invalid content, ineligible contexts/no PRNG consumption, window and stage gates, tag filters, cooldown boundary, occurrence cap, catalog permutation invariance, separate career quota PRNG and invalid duplicate event catalog.

## Session closeout

- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-mcp-bridge; unity-qa-release; unity-game-economy; unity-debug-profiler (domain skills read from published upstream because the repo does not vendor these community skills).
- **Tests:** Engine-free CI pending PR run. Unity EditMode/PlayMode and runtime **NOT RUN** (owner-approved deferral).
- **Visual evidence:** NOT RUN; source-only selection logic does not change a screen.
- **Save impact:** None. Historical V2/V3 serializers, receipts and replay unchanged. M15 persistence + migration remains explicitly **NOT IMPLEMENTED**.
- **Known limitations:** No runtime integration; no durable pending choice, command outcomes, cooldown persistency, event catalog UI, or twenty Vietnamese authored events yet. This slice cannot be treated as complete event functionality.
- **Files changed:** Core/EventDefinitions.cs, Simulation/GameEventSelector.cs and their .meta files; tools/SimulationChecks/Program.cs; this evidence.
