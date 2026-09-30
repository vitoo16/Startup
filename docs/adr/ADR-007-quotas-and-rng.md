# ADR-007 — Deterministic quotas and independent RNG streams

Status: implementation contract proposed under M1-T01; dependency gate pending. Date: 2026-09-30.

Context: Career scenes need controlled percentages and resumable variety; unrelated events must not perturb them.

Decision: Apportion scene weights into a cycle with largest remainders and ordinal-ID ties. Developer's 20-slot cycle is 8/4/3/2/2/1. Order by greatest remaining count while excluding the previous type whenever alternatives exist; resolve equal counts by unbiased draw over sorted IDs. Persist actual deck/cursor/cycle/generation/signature and the previous scene. Rank changes rebuild only unconsumed slots under new weights and preserve committed outcomes.

Use explicitly versioned xorshift32-v1 with independent nonzero scheduler/event states, unbiased bounded draws, and fixed reference vectors. Never use engine/global RNG or runtime hashes. Restore the stored deck rather than rebuilding it. A transition cycle need not equal the new full-cycle quotas; the next full cycle must.

Consequence: Arithmetic and ordering are portable, with unavoidable repeats only when one remaining type exists. Scene progression does not depend on rendered duration. This generator is for reproducible game variety, not security.

Verification required: quota counts, invalid weights, tied apportionment, stream isolation, save continuation, cycle seam, and rank-change prefix preservation. Algorithm detail: [contracts](../architecture/FIRST_PLAYABLE_CONTRACTS.md). Runtime reference-vector tests remain pending.
