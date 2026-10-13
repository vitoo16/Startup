# Startup Life — Production Execution Roadmap

**Status:** ACTIVE planning baseline, not a release approval. **Owner:** GPT-6 Technical Director. **Effective:** 2026-10-14. **Repository lane:** `main` (owner-authorized). **Cadence:** 16 planning weeks; extend by unmet quality gates, never ship merely because a week elapsed.

## 0. Decision and document precedence

We are building toward **a shippable commercial game**, not running a second "finish the MVP, then start production" phase. The product pillars and authoritative gameplay invariants in `docs/GDD.md` remain intact. `docs/MVP_PLAN.md`, `docs/IMPLEMENTATION_PLAN_MVP.md`, `docs/ART_BIBLE_M13.md`, existing evidence and historical frozen content remain as **historic design and engineering references**, NOT active final-release acceptance criteria.

Production planning authority: this roadmap for sequencing/gates; `docs/PRODUCTION_PIXEL_STYLE_CONTRACT.md` for the proposed new visual contract and migration; `docs/PRODUCTION_ASSET_IP_POLICY.md` for provenance and clearance; GDD for rules and product identity; committed save/receipt contracts for historical technical behavior. Contradictions require a documented ADR and owner review, not silently editing archived evidence.

The old M13 art-spec/Unity assets are still non-pixel until the style-migration implementation task passes. Do not label pixel art integrated or approved because these documents exist.

## 1. Verified starting inventory and gap classification

Inspected `main` as of 2026-10-14, with a later M15 post-shift overtime change than the previously green 329/329 CI report. **Treat exact-head test/Unity statuses as unknown until checked at the exact new code SHA**; journal entries from older SHAs are not transfer certificates.

| Stream | Exists in repo | Production gap |
| --- | --- | --- |
| Simulation/Save | `SimulationEngine`, quota scheduler, V2/V3 history, V4 journal/CAS/catalog archives | exact-head end-to-end replay, failover, time/economy proof, profiling, stable release catalog retention |
| Events | M15 content/authoritative shift-time offers, replay + localized modal | full matrix including course/career/customer/supplier/overtime, Unity screenshots, edge-case device recovery |
| UI | `MobileTheme`, `MobileSafeArea`, `MobileScrollViewFactory`, existing overlays | production pixel UI kit, gesture ergonomics, native-resolution TMP/Vietnamese, accessibility |
| Visuals | `docs/ART_BIBLE_M13.md`, `Art/_ArtDirection/*`, empty `Art/Approved/registry.json` | approved original pixel visual grammar, real authored/layered assets, articulated animation, composited scenes |
| Content | original GDD + current `FirstPlayableContentTemplate`, M15 events | scale careers/courses/businesses/employees using existing simulation, narrative and balancing |
| QA | engine-free CI and `unity-runtime-smoke.yml` | provisioned Unity runners, actual EditMode/PlayMode, Android/iOS IL2CPP, crash/perf/device evidence |
| Release ops | build/test scripts, Unity project | privacy/consent, crash diagnostics, app-store listings, signing/secrets, QA-supported release channels |

**Not yet approved as final production scope:** the GDD distinguishes original six careers/four businesses, eight Early Access career additions/five businesses, and additional 1.0 candidates. Provisional content planning target: **14 distinct careers / 9 distinct businesses / 6 skills**, with production-ready course curricula, employees/managers and meaningful scene variance; **minimum 60 curated events** with the GDD's 60/30/10 severity philosophy. These are backlog capacity assumptions, NOT new economic rules or a claim that 1.0 GDD candidates are cancelled. Lock final catalog counts in W1 after auditing uniqueness and mobile production capacity. Anything added must have a distinct simulation/scene/affinity payoff; no reskins sold as features.

## 2. Critical path and independent workstreams

`P-00 Scope + contracts` -> `P-01 Save/runtime hardening` + `P-02 Pixel grammar/IP approval` -> `P-03 Golden rig/UI/scene slice` -> `P-04 Production systems/content` -> `P-05 Full integration/optimization` -> `P-06 RC, stores, release`.

Parallel lanes with one technical owner: **ENG** deterministic gameplay/save/scheduler; **ART** asset production & animation; **UX** native-resolution uGUI/TMP; **CONTENT** careers/businesses/courses/events; **QA** CI/Unity/device/save; **OPS** store/telemetry/privacy. Shared scene/prefab owners must not edit the same file concurrently. On `main`, read HEAD first and use blob SHA optimistic writes. Run relevant CI on each code revision; art output cannot bypass approval gates.

## 3. Week-by-week executable schedule

Weeks are relative to 2026-10-14 and are scheduling targets rather than estimates of guaranteed completion. A blocked gate takes priority over the next calendar item.

| Week | Milestone / owner lane | Specific work & source-edit order | Exit evidence |
| --- | --- | --- | --- |
| **W01 (Oct 14–20)** | **P00 Production re-baseline** — TD/ENG/ART | (1) `AGENTS.md`, this roadmap, IP/style contracts; (2) compare `docs/GDD.md` against `IMPLEMENTATION_PLAN_MVP.md`, create scope/status matrix; (3) enumerate approved assets, source licensing and current CI; (4) draft ADR for incompatible non-pixel M13 replacement | Approved product scope, feature dependency matrix, brand/originality risk register, immutable old docs retained; no false closed milestones |
| **W02 (Oct 21–27)** | **P01 Runtime & save verification** — ENG/QA | (1) `EventV4AdvanceCoordinator.cs`, `EventV4CommitGateway.cs`, `EventDecisionJournal.cs`; (2) `VersionedReceiptReplay.cs` & V4 archive; (3) `tools/{SimulationChecks,EconomicChecks}` new cases; (4) `.github/workflows/unity-runtime-smoke.yml` actual provisioned runner | Replayed save transitions, failed write/ambiguous retry, overtime after-shift minutes, cold restore, exact-code-head 0-warning build + deterministic CI; Unity runner proof separately |
| **W03 (Oct 28–Nov 03)** | **P02 Pixel render/asset migration** — ART/ENG/QA | (1) lock pixel grammar/IP review; (2) migrate together `Art/_ArtDirection/{art-spec.yaml,composition.yaml,golden-targets.json}`; (3) `Assets/StartupLife/Data/MobileTheme.asset`; (4) `scripts/Validate-GoldenArt.py`; (5) original approval registry style schema migration and import presets | Exactly one active style ID and palette authority; validator green; no unapproved runtime asset or old-HD/new-pixel hybrid |
| **W04 (Nov 04–10)** | **P03 Pixel Golden character** — ART/ANIM | (1) original character front/side/three-quarter sheets + provenance; (2) rig-ready layers and stable pivots; (3) 2D Animation rig + actual joint motion, idle/walk/typing/talk; (4) `Art/Approved/registry.json` only after BeautyCell & independent approval | True limb movement at mobile pixel scale; no rigid-body sprite slide masquerading as walk; IP/style + rig evidence |
| **W05 (Nov 11–17)** | **P03 UI visual system** — UX/ART | (1) pixel token bridge through `MobileTheme`; (2) reusable controls for `LifeDashboardOverlay`, `BusinessManagementOverlay`, `EventChoiceOverlay`; (3) `MobileSafeArea`, `MobileScrollViewFactory`; (4) TMP diacritics and dynamic layout | One-handed 9:16 design at 360x640/390x844/430x932, readable 200% text, 96 reference-pixel tap target baseline (also validate physical touch metrics), focus & contrast |
| **W06 (Nov 18–24)** | **P03 Golden rooms/animation** — ART/ANIM/ENG | Original night-home, office, kiosk, props; align scene camera, sorting layers, Pixel Perfect Camera under URP, sprite atlas, animation binding and state transitions; capture `FirstPlayable.unity` | Coherent crisp art/rig/lighting, no pixel crawling at target scales, character interaction loop and on-device scene captures |
| **W07 (Nov 25–Dec 01)** | **P03 Commercial-quality vertical slice gate** — ENG/UX/QA | Integrate onboarding -> job -> study -> optional business -> event -> save/reload using `StartupLifeBootstrapper.cs`, `LifeScreenViewController.cs`, `WorkShiftPlaybackController.cs` | 20–30 min continuous **representative** playthrough; strong visual identity, validated save/retry, performance baseline on physical Android; gate to scale, not MVP release |
| **W08 (Dec 02–08)** | **P04 Careers production batch A** — CONTENT/ENG | Add Marketing, Sales, Office/Admin to `ContentCatalogSource.cs` / `FirstPlayableContentTemplate.cs` as appropriate; quota-deck patterns, affinity, animations and cases | Distinct job scenes/quota tests, career-vs-skill separation, salary progression, UI and save scenarios |
| **W09 (Dec 09–15)** | **P04 Careers batch B + courses** — CONTENT/ENG | Chef, Barista + course design in existing catalog; then additional distinct careers prioritized against approved W1 scope; reuse existing `QuotaScheduler`, `SimulationEngine`, and existing Study receipts | Career scene quotas, meaningful course progression, correct study time vs work, no fake narrative XP |
| **W10 (Dec 16–22)** | **P04 Business batch A** — CONTENT/ENG | Full operating scenes/capacity for original four and agency/café/fashion; `M9T02FunctionalEconomy.cs`, `BusinessEligibilityEvaluator.cs`, `OwnerDayAllocator.cs`, business overlays | Multi-business affordability/time/market/settlement deterministic; no phantom revenue or time |
| **W11 (Dec 23–29)** | **P04 Business batch B + employees** — ENG/CONTENT | Digital service/beauty business additions and manager/employee architecture where GDD design is approved; extend existing business services, do not fork ledger or parallel simulator | Manager independence rules, wages/arrears/replay, life history, employee scene UI, explicit unimplemented scope if blocked |
| **W12 (Dec 30–Jan 05)** | **P04 Narrative + animation scale** — CONTENT/ART | Curated event matrix toward approved target; freeze catalog before each release-altering revision via `RetainedM15DevelopmentEventCatalogs`; update localized source and archive, add reusable true-motion activity clips | Every event choice legal/affordable/recoverable, category frequency correct; archived fingerprints retained; 60/30/10 severity review |
| **W13 (Jan 06–12)** | **P05 Whole-game UX & accessibility** — UX/QA | Full five-tab navigation, history/profile/settings, dynamic font, localization, safe-area, Android back/focus, reduced motion, screen-reader feasibility | Real-device portrait screenshots across tabs, no obscured choice/overflow, all critical paths actionable |
| **W14 (Jan 13–19)** | **P05 Performance/audio/telemetry** — ENG/ART/OPS | Sprite atlases, RAM/GPU/CPU profiling, audio mixer, licensed original loops/SFX, minimal privacy-conscious event instrumentation, diagnostic toggles | Evidence-backed memory/FPS/load time targets (device tiers set in W1), telemetry not collecting user secrets, audio provenance |
| **W15 (Jan 20–26)** | **P06 Release candidate & compliance** — QA/OPS | Android IL2CPP build + signing, iOS exported Xcode project/signing (blocked unless Mac/iPhone provisioned), store art from approved assets, disclosures, data safety, migration matrix, rollback | RC build hashes, permissions/privacy evidence, saved checkpoint upgrade test, issue triage, accessible language reviews |
| **W16 (Jan 27–Feb 02)** | **P06 Soft launch / release decision** — TD/QA/OPS | Full regression, device matrix, crash/retention/performance review, reject/hold/go decision, build channel and patch runbook | Release decision signed from evidence. No iOS shipping claim without hardware/signing or unresolved high-severity issues. |

## 4. First 10 implementation sessions — required file edit order

Treat each entry as a **small code-reviewed session** on `main`, with exact HEAD and evidence. The order prevents a half-migrated visual style from entering scenes.

| Session | Task ID & first files to read/edit | Product outcome / acceptance |
| --- | --- | --- |
| **S01** | `P00-01`: `AGENTS.md`, `PRODUCTION_EXECUTION_ROADMAP.md`, `GDD.md`, historical `IMPLEMENTATION_PLAN_MVP.md` | Adopt production authority, create approved launch-scope matrix; do not erase historical planning |
| **S02** | `P00-02`: `PRODUCTION_PIXEL_STYLE_CONTRACT.md`, `PRODUCTION_ASSET_IP_POLICY.md`, existing M13 art YAML/registry | Pixel style grammar and licensing/provenance gate locked as a written proposed contract |
| **S03** | `P01-01`: M15 V4 core gateway/replay/planner and `tools/EconomicChecks/Program.cs` | Revalidate overtime/post-shift, checked time/XP/money and CAS from exact current HEAD |
| **S04** | `P01-02`: `scripts/Test-UnityHeadless.ps1`, `.github/workflows/unity-runtime-smoke.yml`, `Assets/StartupLife/Tests/{EditMode,PlayMode}` | Actual Unity runner evidence; no substitute for engine-free CI |
| **S05** | `P02-01`: `Art/_ArtDirection/art-spec.yaml` then `composition.yaml`, `golden-targets.json`, `Validate-GoldenArt.py` | Atomic style-v2 migration with archived non-pixel v1 held as historical source |
| **S06** | `P02-02`: `MobileTheme.asset`, `MobileTheme.cs`, approved-registry schema/validator | One set of tokens for pixel UI+environment; no hardcoded new palette scattered through overlays |
| **S07** | `P03-01`: `Art/Source/Characters/` (new), `Art/Approved/registry.json`, Unity importer/editor gate | First licensed/original layered character & clean pivot/rig source; independent approval required |
| **S08** | `P03-02`: new reusable character rig/animation prefabs/clips, `FirstPlayable.unity` | Genuine pose/limb animation; pixel crispness tested on actual render |
| **S09** | `P03-03`: `MobileTheme`, `MobileSafeArea`, `LifeDashboardOverlay`, `EventChoiceOverlay` | Production UI preview with Vietnamese TMP, readable disabled choices and accessible tap areas |
| **S10** | `P03-04`: `StartupLifeBootstrapper`, `LifeScreenViewController`, `WorkShiftPlaybackController`, EditMode/PlayMode tests | Vertical slice end-to-end, deterministic persisted state and displayed character animation |

For sessions after S10, use the same ticket naming `P04-CAR-*`, `P04-BIZ-*`, `P05-UX-*`, `P06-RC-*` and record `owner, dependencies, exact files, before/after SHA, CI run, visual evidence, approved asset IDs, save impact, rollback`.

## 5. Workstream Definition of Done

**ENG:** pure simulation as source of authority; no UI economy or duplicate save engine; exact-head build 0 warnings/errors, deterministic replay, failed write + retry, no historical fixture relaxation, frame-time impact known.

**ART/ANIM:** original asset & license/provenance evidence; pixel grammar matches approved version, separate layered sources, limbs/poses actually move, clean sprite importer/atlases, real phone BeautyCell, independent 15/16 Golden gate with no zeros, approved registry ID. Image generator cannot approve its own output.

**UX/LOCALIZATION:** native-resolution TMP separate from low-res pixel world; proper diacritics, 9:16 and safe-area states, scalable font, visible focus/errors, alternate motion options, no copyrighted icon traces.

**QA:** Unity EditMode/PlayMode (including scene references), deterministic .NET, Android lifecycle, iOS with actual provisioning, IL2CPP, save migration/bad-disk and performance measurement. Engine-free PASS never implies on-device PASS.

**RELEASE:** final scope and asset-cleared BOM, commercial-use source licenses and audits, consent/privacy/analytics, app-store media from approved production art, notarized/signed deliverables as applicable, incident and patch procedures.

## 6. Blocking risks and release rules

| Blocker | Required intervention |
| --- | --- |
| Pixel migration conflicts with existing non-pixel M13 validator/asset IDs | migrate JSON specs and checker in one verified slice; do not delete art baseline or silently overwrite registry |
| No licensed Mac + iPhone provisioning | iOS release gate remains red; Android-first beta is a possible independently approved release decision, not an assumed workaround |
| Career/business count increases simulation scope | scope-lock wave 1 from existing GDD, phase lower-value 1.0 candidates only after their distinct gameplay contracts are proven |
| Asset similarity/license uncertain | quarantine candidate, get documented independent human/legal review or replace with clean first-party work; no automated guarantee of global uniqueness |
| Art/animation causes blur or memory spikes | URP Pixel Perfect diagnostics and mobile atlas/perf budgets; reduce decoration, not text readability or source-quality policy |
| Product QA behind schedule | hold release and reduce nonessential scope via approval; do not mark missing QA PASSED |

**Hard stop for release:** corrupt or unrecoverable new save; money/time duplication; accidental overdraw/overflow; unsupported historical versions rewritten; inaccessible critical UX; unlicensed/high-similarity product art; untested target platform builds; unresolved blocking crash or privacy requirements.

## 7. Production status convention and governance

Status is one of `NOT_STARTED`, `IN_PROGRESS`, `BLOCKED`, `IMPLEMENTED_UNVERIFIED`, `VERIFIED`, `RELEASE_APPROVED`. A commit, a code test, a screenshot and a device test are different types of evidence. Do not mislabel one as the other. Weekly gate summary must list exact SHA and failed/deferred checks; update `docs/IMPLEMENTATION_PROGRESS.md` as an evidence index, not a second authoritative production checklist.

Art approval is **not** automatic from CI; separate visual/IP clearance remains mandatory. Financial, time and career semantics are never changed to meet an illustrative roadmap number. Only the owner can approve materially different product scope, monetization, brand/IP risk acceptance or release-platform changes.
