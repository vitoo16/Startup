# P00-01 — Production launch-scope reconciliation (decision draft)

**Date:** 2026-10-14  
**Authority:** `docs/PRODUCTION_EXECUTION_ROADMAP.md` for production sequencing; `docs/GDD.md` for product rules.  
**Status:** IN_PROGRESS — technical baseline proposal, **owner scope approval pending**. This document does not close a milestone, certify a feature or modify an existing gameplay rule.

## 1. Product contract that must survive production

- Portrait-first offline Unity 6.3 LTS / URP / C#; Vietnamese-first uGUI/TMP at native-resolution UI.
- Career progression and transferable Skills are distinct; career history persists.
- Employed days fast-forward work and reserve evenings for meaningful study/startup choices. Unemployed/founder days have daytime availability.
- Profession scenes use deterministic quota scheduling. Time, cash, XP and event decisions are awarded by authoritative simulation/receipts, not animations.
- Business operation compatibility is data-driven; managers can later make some physical businesses operable without player attendance.
- Low interaction burden, nonnegative player cash, recoverable arrears and no mandatory minigame/micromanagement.
- Original contemporary Vietnamese **cute pixel art** is the production target. M13 HD-chibi implementation stays technically active until the coherent W03 migration, and no generated art is shipping-approved automatically.
- Offline progress pauses on application suspension. Historic saves/receipts remain replay-compatible; upgrading art must not mutate save semantics.

## 2. Scope mapping: GDD vs historical MVP vs production candidate

| Domain | GDD initial six/four or later candidates | Historical MVP baseline | Proposed production launch target | Evidence/status at this review |
| --- | --- | --- | --- | --- |
| Careers | 6 initial; 8 EA; 7 later candidates | 6 | **14 = initial 6 + EA 8**, each with distinct scene distribution, skill and business affinity. GDD later 7 deferred pending capacity proof. | Developer backend and M15 progress documented. **Live implemented catalog counts not yet audited**; no 14-career completion claim. |
| Transferable skills | 10 recommended v1 candidates; first 6 MVP | 6 | **6** thoroughly supported skills at launch; remaining 4 separately scope-reviewed | Do not confuse careers with skills or silently add Creativity just for Marketing descriptions. |
| Businesses | 4 initial; 5 EA; 6 later candidates | 4 | **9 = initial 4 + EA 5** with differentiated costs, schedules, affinity and scene/capacity. GDD later 6 deferred. | Existing finance/eligibility architecture exists; actual nine-business integration **unverified**. |
| Customers | Six GDD segment candidates | 4 aggregate segments | **4 first**, expand only after determinism/performance and meaningful differentiated demand proof | Do not equate six suggested segment personas with a requirement for six launched AI agents. |
| Curated events | 9 thematic categories; 60/30/10 severity philosophy | 20 | **60 curated events**, independently validated/replay-safe, category matrix and severity editorial review | M15 catalog exists; category/count/recovery audit pending. |
| Employees/managers | EA roles, reliability/morale, manager-operated ownership | One kiosk helper; no advanced employees | **Manager-operation and meaningful simplified staff lifecycle** behind explicit costs, authority and archive guarantees | New rules/UX must be approved before implementation; do not imply implemented. |
| Character creation | 18–40 initial, older later; hair/outfit/background | 18–40, two visual presentations | Retain 18–40 contract; pixel modular rig/appearance options, native Vietnamese localization; older variant only if art production permits | Active visuals still pre-migration; zero independently approved pixel Golden assets asserted. |
| World/visual scenes | Career, home and business stories; character always visible | 33 visual recipes across nine reused environments | Original pixel home / workplace / kiosk Golden cells first; expand reuse and distinct career/business payoff | Unity device BeautyCell and genuine articulated character motion pending. |
| Navigation/accessibility | Career, Skills, Business, Life, Settings and histories | Five tabs / mobile shell | Full readable five-tab navigation, safe-area, 200% text review, focus and reduced-motion options | Runtime/device acceptance pending. |
| Platforms/release | iOS and Android | Both release candidates | **Android + iOS target**, each requiring independent native build, physical device, signing and store compliance | Mac/Xcode/iPhone missing; **iOS BLOCKED**, cannot call cross-platform release approved. |

**Counts in bold are proposed planning capacities, not approved release commitments.** The GDD's 1.0 candidates have not been cancelled. A later owner-approved scope adjustment may move them to a post-launch wave without redefining GDD invariants.

## 3. Feature dependency matrix and sequencing

| Work item | Depends on | Independent acceptance proof | Risk if skipped |
| --- | --- | --- | --- |
| Exact-head simulation/save hardening (P01) | P00 scope baseline, historical receipts and catalog | .NET deterministic cases + ambiguous writes/retry/cold replay; Unity runtime separately | Corrupt/duplicated money, time or history |
| Pixel spec and provenance migration (P02) | Proposed style grammar, existing M13 files, IP rights pipeline | One active style ID across YAML, asset, validator and empty approved registry; no mixed-style assets | Hybrid look, licensing gaps, validator false PASS |
| Golden rig/UI/rooms (P03) | P02, independent asset clearance, URP project import | Limb/joint animation; actual device capture at relevant portrait sizes; > =15/16 Golden score and no zero | Unreadable or unoriginal product; fake animation |
| Catalog expansion (P04) | P01 simulation guarantees, scope lock, reusable scenes | Distinct career/business/employee/event gameplay and save regression cases | Reskins, balance drift, duplicate logic |
| Integration/accessibility/performance (P05) | P03 visuals, P04 systems, release device inventory | Continuous playthrough, accessibility captures, profiling, physical-device recovery | Inaccessible UI, perf crash |
| Release candidate (P06) | Every prior gate plus asset BOM, compliance, signing | Device build IDs, permissions/privacy/store evidence, rollback, signed go/no-go | Unshippable or legally risky release |

## 4. Decision register — open owner approvals

| ID | Decision required | Technical Director recommendation | State |
| --- | --- | --- | --- |
| SCOPE-01 | Target 14 careers / 9 businesses / 6 skills / 60 events? | Use as **capacity target**, prioritize differentiated content over raw counts; adjust only after implementation audit | PROPOSED |
| SCOPE-02 | Manager/employee depth for launch | Implement manager-operated physical businesses only with full wage/attendance/save contracts; avoid manual shift scheduling | PROPOSED |
| SCOPE-03 | Six vs four customer segments | Begin with current four; add two only if genuinely differentiated | PROPOSED |
| SCOPE-04 | Android/iOS release policy | Keep both as targets; if iOS provisioning stays unavailable, request separate explicit Android-first release approval | BLOCKED |
| ART-01 | Replace historic HD visual contract | Migrate in W03 atomically; preserve historic baseline; require independent provenance, visual and IP review | PROPOSED |
| QA-01 | Release proof versus code status | Engine-free green is necessary, **not** sufficient; require Unity and device evidence before milestone closure | LOCKED by AGENTS.md |

## 5. Current risks and next executable tasks

1. **Inventory risk:** an exact-HEAD list of implemented career/business/skill/event IDs and approved registry entries is required before confirming production content gap numbers. This document explicitly avoids inventing current implementation counts.
2. **Save risk:** M15 V4 receipts, event archives, CAS/ambiguous retry and overtime must be verified on one current source HEAD.
3. **Visual/IP risk:** original pixel palette/rig/pivots are still proposed, not integrated. Independent third-party review is required for shipping assets.
4. **Platform risk:** Unity runtime and physical Android/iOS evidence is separate from CI. iOS toolchain unprovisioned.
5. **Governance risk:** historical MVP plan and art bible remain intact. Production roadmap is the only forward schedule.

**Next:** P00-02 style grammar and provenance contract check; then P01-01 deterministic runtime verification; P01-02 Unity workflow proof. W03 atomic pixel contract migration is deliberately not started by this planning document.
