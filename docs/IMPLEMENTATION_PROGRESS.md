# Implementation progress

The [implementation ledger](IMPLEMENTATION_PLAN_MVP.md#active_status) is the authoritative task-status source. This page is an evidence index, not a second checklist.

- [M0-T01 canonical documentation](evidence/M0-T01/SESSION.md): source parity, stable task IDs, skill expansion, and links verified.
- [M0-T02 tooling preparation](evidence/M0-T02/SESSION.md): skill lock and working local tools; Unity compatibility gate pending.
- [M0-T03 Unity foundation](evidence/M0-T03/SESSION.md): pinned Editor, generated Universal 2D project, resolved packages; mobile baseline verification in progress.
- [M0-T04 engine-free CI foundation](evidence/M0-T04/SESSION.md): repository-hosted documentation + scene-independent simulation verification added; fresh PR workflow evidence and licensed Unity/mobile runners remain pending.
- [First playable contracts](architecture/FIRST_PLAYABLE_CONTRACTS.md) and [ADR index](adr/ADR-001-assembly-boundaries.md): preparatory architecture, no milestone completion.
- [Provisional Developer/work/study/save backend](evidence/PROVISIONAL-FIRST-PLAYABLE/SESSION.md): .NET build/test evidence only.
- [Architecture review](evidence/PROVISIONAL-FIRST-PLAYABLE/ARCHITECTURE_REVIEW.md): three reproduced/reviewed findings and fix reconciliation.
- [PR7 receipt compatibility and truncated-skill fix](evidence/M0-T04/RECEIPT-COMPATIBILITY-FIX.md): provenance-bound historical retries and corruption classification; engine-free evidence only.
- [H1/R2 closure](evidence/M0-T04/H1-R2-CLOSURE.md): durable historical restore/import and cross-field corruption precedence, with fresh-session/backup/fault regression evidence.
- [M2 restore provenance follow-up](evidence/M0-T04/M2-RESTORE-PROVENANCE.md): receipt-bound timeline, deterministic scheduler reconstruction and global entity references; 31 restore regressions with engine-free evidence only.

No playable Unity scene, mobile build, or released save schema has been verified. Existing baseline screenshots are development evidence only, not device acceptance. iOS requires external Mac/iPhone provisioning, which the user confirmed is unavailable on September 30, 2026.
