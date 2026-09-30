# ADR-008 — Automatic business time sharing and shared-wallet reservation

Status: implementation contract proposed under M1-T01; dependency gate pending. Date: 2026-09-30.

Context: Four businesses must not multiply available owner time or spend the same shared cash independently.

Decision: A single day planner handles all active businesses. Reserve employment, sleep/unavailable intervals and study, then kiosk's required daytime block, then capped equal shares of remaining eligible time for side businesses. Redistribute unused minutes with stable definition/instance-ID ties. Respect actual windows, not only daily totals. Maximum four active instances, one per type; ManagerOperable rejects in MVP. FullTimeRequired cannot operate while employed.

Reserve due fixed obligations first; allocate affordable variable-cost capacity from the opening wallet. Reallocate released time deterministically after affordability caps. Same-day projected revenue cannot fund upfront costs. SimulateBusinessDay consumes one plan tied to the input revision and settles all results atomically. Customer NPCs are cosmetic. Closure preserves obligations and history.

Consequence: Eligibility remains data-driven and player interaction remains light. Shared allocation is a Simulation concern, not four independent MonoBehaviours. These seams do not require implementing businesses before the first playable gate.

Verification required: four-business permutation tests, kiosk/study conflicts, differing windows, integer remainder allocation, insolvency/recovery, and conservation of cash/time. See [contracts](../architecture/FIRST_PLAYABLE_CONTRACTS.md). Runtime implementation is deferred to planned milestones.
