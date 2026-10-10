# M9-T02 — Sol implementation evidence (DRAFT / NOT CLOSED)

**Updated:** 2026-10-10 (Asia/Ho_Chi_Minh). **Implementer:** Sol role. **Architecture:** `M9-T02-TA-1.0-FINAL` (RC5).
**Approved source baseline:** `051a931410f2dec0ec51906f7f06191a0deaf77c`.
**Feature branch:** `feat/m9-t02-segments-daily-finance`. **Draft PR:** https://github.com/vitoo16/Startup/pull/39 .
**Status:** SOURCE IMPLEMENTATION IN PROGRESS — **NO MERGE**, **NO UNITY RUNTIME PASS**, **M9-T02 OPEN**.

## Repo and session environment

- Original M9-T01 CLOSED. Issue #36 Android/URP separate and still not accepted by this implementation.
- Actual project Unity Editor is `6000.3.25f1 (e1dba0a9aba4)` per pinned ProjectVersion.
- Repository has 63 local `.agents/skills/*/SKILL.md` entries including project orchestrator, gameplay guardian and unity-cli; five required community Unity game skills read from `tea-x-random/unity-game-skills` pinned `dafb97ef00f94e64e42e6260bc6b3af74cc83dad`, not locally installed in this environment.
- Implementation uses the authorized GitHub connector to create immutable blobs/commits on the isolated feature branch. Local environment lacks runnable dotnet and Unity; **GitHub Actions .NET8** is the actual compiler and engine-free test runner. All UI, Editor, PlayMode and IL2CPP tests **NOT RUN**.
- Legacy public `OwnerDayAllocator.Plan(GameState,ContentCatalog)`, `CommandKind` 0..10, `BusinessType` 1..4, original `first-playable.v1` content asset and SaveVersion=2 remain unmodified. No economic-reward path is currently wired into the v2 GameSession.

## Historical source archive

- Byte-identical v2 source snapshots: `docs/evidence/M9-T02/frozen-v2-source/` (8 original Git blobs).
- `FROZEN_V2_SOURCE_MANIFEST.md` pins each original SHA-1 + SHA-256.
- `scripts/Test-M9T02FrozenV2.ps1` checks all 8 original copies in CI; all 8 verified in prior successful runs.
- `Simulation/ArchivedV2SimulationEngine.cs` contains the original v2 evaluator and `ArchivedV2RestoreValidator` with class-name-only changes, verified byte-semantically against archived `SimulationEngine.cs.txt` by CI. `Content/BundledHistoricalV2ContentResolver.cs` builds a frozen catalog from exact authored baseline values, independent of editable v3 content/templates; semantic parity is tested.
- `Simulation/VersionedReceiptReplay.cs` validates the complete v2 checkpoint/prefix with these frozen rules and rejects an unregistered v3 suffix. It is NOT yet connected to all real restore, retry and `GameSession.OutcomeForReceipt` consumers.

## Scoped source artifacts

| Slice | Scope committed | Actual status |
|---|---|---|
| S0 | SHA-locked original v2 source/8 hashes; frozen v2 evaluator with verified class-name-only copy; baseline-authored v2 content resolver | Archive/evaluator/checkpoint parity PASS; v3 runtime-registration and dependent historical helpers review pending |
| S1 | `Core/EconomicDefinitions.cs`, `Content/M9T02FunctionalEconomy.cs` — immutable 6 ID-keyed segments (4 active), 4 finite pools/profiles, per-posture price | Pure model PASS, production authored content activation pending |
| S2 | `Simulation/CustomerDemandCalculator.cs` — BigInteger demand, largest remainder, stable ties, finite original/day remaining, no double-sell | Pure calculator PASS, actual persisted fulfillment bridge pending |
| S3 | `Core/ObligationContracts.cs`, `Simulation/BusinessObligationScheduler.cs`, `ObligationIncomeDistributor.cs` — 8 cadence triggers, cumulative exact proration, due ordering, source-linked withheld income | Pure model PASS, linked v3 GameState/arrear ledger posting pending |
| S4 | `Simulation/BusinessAffordabilityPlanner.cs`, `BusinessEpochExecution.cs`, `BusinessMarketEpochPlanner.cs` — quota-normalized P-03 + global owner-slot matching + one-wallet reserved units + frozen epoch + market/price reservation mapping, Study exclusion | Pure model PASS; real committed GameSession/AdvanceBoundary integration pending |
| S5 | `Simulation/BusinessPolicyTransitions.cs`, `BusinessDaySettlement.cs` — P-04 pure transitions and old-day cash/profit/arrears conservation against attributed market+cost units | Pure model PASS; actual command IDs 11/12, v3 transaction, ledger and midnight source posting pending |
| S6 | `EconomicActivationAnchor`, `EconomicV3Payload`, `V3FinancialProvenance`, `V2ToV3Migration`, `StagedV3SaveSerializer`, `IVersionedReceiptReplay`/frozen-prefix implementation; stage-2 checkpoint plus original source v0/1/2 bytes, activation, schema3 checksum and generation | **Staged migration/wire verification PASS**; full v3 suffix validator, live `GameSession`/restore/archived dispatcher consumers, same-generation CAS and CurrentVersion=3 **NOT IMPLEMENTED** |
| S7 | `Application/BusinessFinanceProjection.cs` verified detached read model built only from conserved committed day settlement | Pure adapter tested (old head and later S6 builds); player-facing UI integration pending |
| S8 | Independent Astra source audit → canonical Unity runtime acceptance → independent final evidence audit | NOT STARTED |

The implementation intentionally does not credit money or units, change schema version or mutate the real Unity asset until all v3 replay/migration and wallet invariants can be wired atomically.

## Engine-free CI evidence (exact prior source heads)

- `6e5926c0729ae7d5374eb725d1b557e2e3e63e0f`: [CI #37856211821](https://github.com/vitoo16/Startup/actions/runs/37856211821) SUCCESS, 9/9 new economy checks.
- `637bfea32ac8f64d692d55d8f7ddf52da9e92bbe`: [CI #37856615307](https://github.com/vitoo16/Startup/actions/runs/37856615307) SUCCESS, 12/12 checks.
- `7fdc10d06c5f6ed4e1033151ba0f5a6e40053b6e`: [CI #37857241866](https://github.com/vitoo16/Startup/actions/runs/37857241866) SUCCESS, 13/13 checks and 8/8 frozen-source hashes.
- `d83351afe9585f9e7e85a25f1464763e8eb099a4`: [CI #38045657054](https://github.com/vitoo16/Startup/actions/runs/38045657054) SUCCESS, 28/28 checks.
- `0bee9d2e2d5d21c2a78cea6a50e44aa087c94224`: [CI #38045843534](https://github.com/vitoo16/Startup/actions/runs/38045843534) SUCCESS, 31/31 checks (gross380, fixed20, variable80, profit280, final cash380; linked obligation settlement).
- `b194fb0bc2566aacd70267f99965b989f8a69f5c`: [CI #38046159789](https://github.com/vitoo16/Startup/actions/runs/38046159789) SUCCESS, 33/33 checks (market/owner/wallet integration + replan ordinal nonreuse).
- `aa5f062fbf693acd7e20a7fe485cdf6dad2dc1d1`: [CI #38046296969](https://github.com/vitoo16/Startup/actions/runs/38046296969) SUCCESS, 35/35 checks (midnight/midday activation anchor).
- `131571359f5898f3efb64ff8c4d4123df8e484e7`: [CI #38046433155](https://github.com/vitoo16/Startup/actions/runs/38046433155) SUCCESS, 35/35 M9-T02 economic checks plus 35/35 restore invariant checks and 0 compile errors; historical V1→V2 stage remains fixed to 2.

- `adee67876eb4c70e4480715398861f28ae71c4a5`: [CI #38048284886](https://github.com/vitoo16/Startup/actions/runs/38048284886) **SUCCESS**, 44/44 M9-T02 checks, staged 0/1/2→3 migration, typed provenance, frozen v2 semantic evaluator, full baseline regression and 0 .NET errors.
- `a489fca03c991671c2b5943672f1f29e68e27b63`: [CI #38048504688](https://github.com/vitoo16/Startup/actions/runs/38048504688) **SUCCESS**, 45/45 M9-T02 checks, standalone schema3 checksum/generation/FutureVersion/UnsupportedContent admission, baseline regressions PASS and 0 .NET errors.
- These are tests of staged/isolated source. No live v3 save or financial gameplay is claimed.

## Remaining hard safety gates (DO NOT BYPASS)

1. Bundle registered frozen v2 resolver/evaluator in the active Unity bootstrap and verify dependent frozen helpers; preserve real authored v2 business definitions when enabling new v3 economic profiles.
2. Persist newly introduced typed v3 provenance inside the authoritative live `GameState` transaction/restore flow; enforce full obligation/arrears, calendar, operation, market and cash ledger invariants beyond the standalone graph tests.
3. Atomic GameSession source mutation: advance boundaries, Study replanning, salary/living ordering, fixed dues and income/arrear ledger without same-day revenue funding.
4. Move tested staged 0→1→2→3 source migration and schema3 admission into production `JsonSaveSerializer`, preserve original-source CAS/backup repair, implement v3 suffix replay (currently intentionally rejected), and route ALL restore/idempotency/receipt consumers through the same dispatcher. Standalone schema4 FutureVersion precedence already has CI proof.
5. Unity 6000.3.25f1 fresh import, EditMode/PlayMode, raw logs/XML/evidence ZIP, local acceptance.
6. Independent Astra source audit and final evidence audit on exact CI/Unity SHA; owner-authorized merge/closeout only after all gates.

**No merge, no milestone CLOSE, no invented Unity/pass/replay evidence.**
