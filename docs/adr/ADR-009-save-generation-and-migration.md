# ADR-009 — Versioned save generations and recovery

Status: implementation contract proposed under M1-T01; dependency gate pending. Date: 2026-09-30.

Context: Interrupted mobile writes, schema changes, and presentation replay must preserve a coherent simulation checkpoint.

Decision: Save all authoritative state and receipts as one versioned envelope with generation/checksum. Write/flush/validate temp, use tested platform replacement, retain the previous valid generation, and read back before publishing a committed outcome. Temp alone is never a commit. If replacement lacks required guarantees, implement a tested two-slot generation/commit-marker fallback. Protect valid backup from corrupt primary.

Restore validates primary then explicitly reports any rollback to backup. Future schemas or unsupported content are preserved and rejected, never silently downgraded. Sequential migrations increment one version, operate on copies, validate, and preserve originals until replacement. Use a clearly synthetic v0 fixture for the initial migration runner. Every later schema change requires version and fixture, including additions.

Consequence: Initial save contains empty reserved collections for deferred systems, current activity, pending choice, RNG/deck, command/operation receipts, and playback cursor. Timestamps are diagnostic only. Mobile filesystem durability, JSON serializer/AOT stripping, and low-storage behavior require device evidence; backup rollback cannot guarantee preservation of newer corrupt data.

Verification required: round-trip, synthetic older fixture, future-version preservation, corrupt primary/backup, writes interrupted at each stage, ambiguous acknowledgement, and deterministic continuation. See [contracts](../architecture/FIRST_PLAYABLE_CONTRACTS.md). No save implementation or Unity test result claimed.
