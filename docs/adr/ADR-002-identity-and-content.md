# ADR-002 — Stable identity and immutable content

Status: implementation contract proposed under M1-T01; dependency gate pending. Date: 2026-09-30.

Context: Saves must outlive scene reloads, translated labels, salary changes, and business closure.

Decision: Use ordinal stable ContentIds and revisioned immutable definitions. Use run-scoped monotonic InstanceIds for owned entities and employers; never recycle IDs. Localized names, Unity IDs, array positions, and runtime string hashes cannot identify saved content. Persist definition revisions for planned activities and salary-rate segments; apply balance changes prospectively or through explicit compatibility mappings.

Consequence: Duplicate/missing IDs and invalid references fail catalog validation. Unknown required content on restore blocks load with a diagnostic; it is not silently replaced. Closed business instances and former employers stay in history. One active instance per business type and four total is an independent invariant.

Verification required: invalid/duplicate catalog fixtures, rename mapping, historical identity round-trip, and rejected unknown revision. Detailed fields: [first playable contracts](../architecture/FIRST_PLAYABLE_CONTRACTS.md). No runtime validation claimed.
