# P04-BIZ-02 — Version-isolated Business Batch A

**Date (VN):** 2026-11-04
**Owner model:** GPT-6 Technical Director
**Baseline main:** `67ca150d0c83b3a6125e443a0c3321d539c48e80`
**Lane:** owner-authorized `main`
**Status:** IMPLEMENTED_PREVIEW; ACTIVE_RUNTIME_NOT_SWITCHED; Unity/device QA NOT RUN

## Objective

Provide differentiated Agency, Small Café and Fashion Store business definitions
and real economic inputs using the **existing** business operation,
owner-time and demand/settlement machinery. Do not change save schema,
retained V2 archive, original four ordinal values or currently active
first-playable / M9 functional economic catalog identity.

## Authored variants (provisional tuning, not approved release balance)

| Business ID | Type | Owner mode | Open windows | Owner min/day | Start capital | Standard price | Base owner capacity | Fixed daily | Variable/unit | Pool units/day |
| --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `marketing-agency` | 5 | Side hustle | Mon–Sat 18:00–22:00 | 180 | 1,200,000 | 550,000 | 4 | 60,000 | 115,000 | 9 |
| `small-cafe` | 6 | Full-time | Tue–Sun 09:00–18:00 | 480 | 6,000,000 | 42,000 | 36 + 20 helper | 250,000 | 18,000 | 95 |
| `fashion-store` | 7 | Full-time | Mon–Sat 10:00–19:00 | 420 | 4,000,000 | 170,000 | 24 | 125,000 | 80,000 | 35 |

Amounts VND. These are placeholders for later economy-balancing and market
demand validation, not final business viability claims. The café helper's
110,000 VND wage is ALREADY included in the 250,000 daily fixed charge.
New types use distinct product-fit segment factors, price bands, market
pools, schedule windows, capacity and career affinity tags.

## Version and code boundary

- `FirstPlayableContentTemplate.Create()/BuildCatalog()` **still returns**
  `first-playable.v1` with exactly four preexisting business IDs.
- `FirstPlayableContentTemplate.CreateBusinessBatchA()/BuildBusinessBatchACatalog()`
  explicitly opts into `first-playable.biz-a.v1` with all original
  definitions plus three new ones. This is a fresh authored source;
  it does not rewrite existing Unity assets or the v1 catalog on disk.
- `M9T02FunctionalEconomy.Create()` remains the exact baseline
  `m9-t02.functional-fixture-v1` with four pools/profiles. The new
  `CreateBusinessBatchA()` explicitly produces `biz-a.v1`
  with seven pools/profiles, retaining the prior four matrices.
- `BusinessEconomicCatalog` continues to require exactly four entries
  for every existing revision, while `biz-a.v1` requires exactly
  seven. All per-pool shares still conserve 10,000 permyriad; every
  profile still requires unique business type and valid pool.
- `BusinessType` existing ordinals 1–4 remain unchanged. Additive
  ordinals 5–7 only. No archived sources edited.
- Existing `BusinessEligibilityEvaluator`, `OwnerDayAllocator`,
  `CustomerDemandCalculator`, `BusinessMarketEpochPlanner`,
  economic settlement and command/receipt engines are reused,
  not duplicated or changed.

## Evidence and testing

- Deterministic C# `ContentCatalogChecks` asserts v1/v2 preview
  separation; exact new type, owner windows, capital, affinity, existing
  definition integrity, employed/founder compatibility, closed weekday
  and authoring defensive copy.
- Deterministic C# `EconomicChecks` asserts complete seven-profile
  roster, unchanged original four profiles, matching new authored
  revisions and capacities, finite/zero-demand market supply,
  deterministic per-segment allocation, wage-in-fixed-cost and
  rejection of incomplete/wrongly versioned catalog.
- New Python source contract `test_business_batch_a_content.py`
  runs in Engine-free CI, covering opt-in-only wiring, enum ordinals,
  authored economics and unique Vietnamese localization.
- CI PASS must be claimed only from the **exact final HEAD** after
  final test finish. Unity Editor, PlayMode, Android/iOS, visual QA and
  full V3/V4 receipt replay across a version activation are **not run**.

## Blockers to live activation

1. Prove new catalog/rules version selection and archive/replay
   compatibility with V3/V4 saves, especially existing persisted
   `first-playable.v1` and `m9-t02.functional-fixture-v1`.
2. Implement an explicit versioned Unity asset regeneration/selection
   path, with migration/restore evidence and UI localization sync.
3. Confirm economic tuning, repeated settlement, time allocation
   conflicts and actual business-scene animation/art evidence.
4. Complete owner-facing Unity visual/device QA gate and asset rights.

This commit deliberately does **not** claim the three new businesses
are live/selectable in the currently serialized Unity scene.

## Session closeout

- **Owner model:** GPT-6 Technical Director
- **Skills used:** startup-life-session-orchestrator,
  startup-life-gameplay-guardian, ui, ui-ugui, localization,
  vietnam-art-direction (text and career affinities)
- **Acceptance criteria:** immutable original four, separate seven-business
  opt-in, distinct economic and time profiles, engine-free regression
- **Tests:** committed C# / Python checks; final CI outcome to report
- **Visual evidence:** none; Unity Editor/device deferred by owner
- **Save impact:** no schema, command ordinal, save migration or
  frozen historical source changes; new BusinessType ordinals additive
- **Known limitations:** new opt-in catalog is not active in runtime;
  market supply and economic profiles are provisional;
  multi-day V3/V4 replay and device QA not yet accepted
- **Files changed:**
  `Assets/StartupLife/Scripts/Core/Business.cs`,
  `Assets/StartupLife/Scripts/Core/EconomicDefinitions.cs`,
  `Assets/StartupLife/Scripts/Content/FirstPlayableContentTemplate.cs`,
  `Assets/StartupLife/Scripts/Content/M9T02FunctionalEconomy.cs`,
  `Assets/StartupLife/Scripts/Presentation/DashboardLocalizationCatalog.cs`,
  `tools/ContentCatalogChecks/Program.cs`,
  `tools/EconomicChecks/Program.cs`,
  `tools/tests/test_business_batch_a_content.py`,
  `.github/workflows/engine-free-ci.yml`,
  `tools/PresentationChecks/Program.cs` (independent prior CI fixture fix),
  this document
- **Commit:** see final main HEAD and exact run
