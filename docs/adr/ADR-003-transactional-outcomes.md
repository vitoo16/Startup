# ADR-003 — Transactional commands and exactly-once outcomes

Status: implementation contract proposed under M1-T01; dependency gate pending. Date: 2026-09-30.

Context: Repeated input, animation callbacks, and reload can accidentally repeat a reward or charge.

Decision: One Application command gate evaluates an isolated candidate, validates it, durably commits state plus receipt/consumed activity/cues, then publishes. Command IDs bind canonical payloads. Simulation operation IDs and planned activity IDs prevent repeating automatic work under another command ID. Retry returns the original receipt; mismatched payload rejects. Failures before commit preserve all state, including RNG and revision.

Consequence: Presentation renders outcomes and cannot grant rewards. Persist every authoritative boundary initially. Ambiguous post-replace I/O failure enters RecoveryRequired, blocks fresh commands, and reconciles the stored generation before answering. Multi-boundary advance returns explicit committed progress and stop reason. Retain receipts for MVP; pruning needs a later audited compaction contract.

Verification required: duplicate request, fresh-ID replay of consumed activity, animation replay, save/reload retry, and fault injection before/after replacement. See [contracts](../architecture/FIRST_PLAYABLE_CONTRACTS.md). No durability or exactly-once runtime result is claimed yet.
