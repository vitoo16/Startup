# Astra follow-up — legacy identity and content classification

Date: 2026-09-30
Owner: Sol
Source: Astra verification after PR #6

Scope is limited to closing H1/H2 follow-up findings before M2:
- make new batch-child identities disjoint from the entire pre-PR caller/receipt ID space while preserving legacy retries;
- remove ambiguous prefix ownership between old batch roots and new callers;
- classify structurally valid missing skills/scenes as unsupported content before catalog-dependent consistency checks;
- validate malformed persisted fields before reporting content incompatibility;
- extend engine-free regression coverage for these cases.

No M1/M2 implementation, task checkbox change, save-schema migration, Unity runtime claim, or device claim is included here.
