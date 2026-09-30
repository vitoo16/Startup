# ADR-006 — Separate career, transferable skills, and courses

Status: implementation contract proposed under M1-T01; dependency gate pending. Date: 2026-09-30.

Context: Players can earn the same transferable capability through work or paid study. Rejoining a career and restoring a background must not farm grants.

Decision: Career XP/rank/tenure and skill level are separate state. Skills have level 0–5, exposure progress, and a granted floor; effective level is their maximum. Milestone IDs are stable per character/career milestone, independent of employer instance. Backgrounds explicitly seed earned grants.

Allow one active course. Charge once at enrollment; study consumes eligible integer minutes and advances fixed-point work units by positive LearningSpeed. Consume only the minutes required to reach target. Completion goes through the idempotent grant path. Reaching the target elsewhere completes the course without a second grant or automatic refund.

Consequence: Existing skill never decreases; exposure is not erased by a floor grant. Study removes time from businesses. LearningSpeed changes affect subsequent minutes, not previously earned progress. The six approved skills are the complete MVP catalog.

Verification required: rejoin/restore grant replay, prior backgrounds, course purchase replay, speed/remainder limits, and career grant during active course. See [contracts](../architecture/FIRST_PLAYABLE_CONTRACTS.md). Tests pending implementation.
