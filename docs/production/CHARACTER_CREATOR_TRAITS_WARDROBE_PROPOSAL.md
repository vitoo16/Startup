# P03-CHAR — Character Creator, Personality Traits & Career Wardrobe

**Date:** 2026-10-15 (Asia/Ho_Chi_Minh)  
**Owner:** GPT-6 (Technical Director / implementation planning)  
**Source baseline:** `be1fb475570a12148c49aac6cf4a0e8a85aa585c`  
**Status:** PROPOSED, NOT AN APPROVED BALANCE/GDD OR SAVE SCHEMA CHANGE. No production visual, animation or asset approval is implied.

**User request:** selectable Male/Female character presentation; personalize face/hair/appearance; different strengths such as stamina, conscientiousness and rapport; dynamically layer authentic profession uniforms/clothes on the *same* character identity.

**Authorities:** `AGENTS.md`, `docs/GDD.md §7`, `docs/ART_DIRECTION_PRODUCTION_BIBLE.md`, `docs/PRODUCTION_PIXEL_STYLE_CONTRACT.md`, `docs/PRODUCTION_ASSET_IP_POLICY.md` and the production roadmap. Preserve existing simulation/save replay contracts.

## 1. Product & fairness decision — awaiting owner acceptance

Keep **Nam / Nữ** appearance selections for v1, with hairstyles, skin palette, clothes and professions available irrespective of selected appearance. Selecting male/female must not inherently grant or subtract stamina, intelligence, meticulousness, customer favor, career opportunities or earning power. The product narrative can still provide meaningful strengths using **separately chosen personal traits** (e.g. Năng động / Chu đáo / Thân thiện / Cân bằng), open to all appearances. This implements differentiated play without claiming men are naturally stronger or women intrinsically more likeable to customers, and matches the anti-discrimination production art directive.

Trait ideas — **not yet balanced or applied**:

| Independent trait | Player-facing meaning | Potential narrow simulation impact only after approved GDD & balancing | Safeguard |
| --- | --- | --- | --- |
| Thể lực / Endurance | Sustain prolonged demanding activities | Reduced chance of fictional fatigue narrative / slower effort deterioration on applicable scheduled work | No hunger bar, medical claim, male-only modifier or forced hourly micromanagement |
| Tỉ mỉ / Carefulness | Check work accurately | Some eligible work-scene quality / fewer avoidable rework cases | Not hardcoded higher for women; not guaranteed salary |
| Giao tiếp / Rapport | Help people feel heard | Some eligible customer-service choice/narrative outcomes | Customer reaction is based on conduct/context, not sex/gender |
| Học hỏi / Learning approach | Build skills effectively | Existing LearningSpeed / Skill projection compatibility *only* through an approved rule migration | GDD already has LearningSpeed; avoid adding a second speed multiplier |

No innate balance advantage for selecting male/female. Any trait bonuses, allocation points, starting package, interaction effect or monetary/career/time impact is a separate **owner-approved rule** and must have deterministic receipt/replay, migration and test coverage. Presentation-only cosmetic rollout can happen independently first.

## 2. Creator UX flow — responsive and low friction

**Page A — Identity:** name, age (current v1 18–40), Nam/Nữ presentation; can preview other variants without losing choices.

**Page B — Look:** live original pixel avatar at fixed PPU with optional safe-position rotation; face template, eyes/brows/mouth/expression, front/back hair, hair shade, skin palette, body/face silhouette options that retain the 2.2–2.4 head adult-chibi target (2.3 nominal); no implication about intelligence/earning/virtue. Current C01 is an unapproved outdated-proportion draft; redesign before relying on it. Color/style options are source-authored, not recolored game sprites.

**Page C — Clothes:** base home outfit and seasonal alternatives; choose shared wardrobe overlays, footwear, bag/glasses/hats; try an occupation uniform preview as **visual only**, no unlocked career or economic award.

**Page D — Background/traits/review:** choose starting background (current `fresh` compatibility), optional freely accessible personality preference; visibly describe effect only when approved gameplay rules exist, and show full-body rotation + edit/back before confirming. Save draft locally in the UI while navigating; commit only through authoritative command.

Desktop-like lists are prohibited on portrait mobile. Use one-handed edit controls, bottom anchored CTA above safe area, back/next, scroll/paginated tabs rather than horizontal overflow. Preview occupies a stable area and stays visible while layers update; reflow 360×640 / 390×844 / 430×932, top notch/gesture safe areas and **200% Vietnamese text**. Disabled states explain reasons, not color alone. Controls and touch size require device DPI testing, not 96 logical reference units by assumption.

## 3. Asset / art contract: one identity, reusable layer stack

Use **one common semantic rig** per approved compatible body geometry, not a prefab for every profession or every male/female combination:

```
AvatarRoot (stable world/pixel pivot)
  Body / Pelvis / Legs / Feet
  Torso / UpperArms / Forearms / Hands
  Neck / Head / Ears / Face / Eyes / Brows / Mouth
  HairBack / HairFront
  OutfitLower / OutfitUpper / Sleeves / Gloves / Footwear
  AccessoryBack / AccessoryFront / Headwear
```

Every authored variant has stable IDs and exact matching pivots/attachment positions for front/three-quarter-left/side/back, with explicit mirrored-side asymmetric accessory rules. Original adult-coded 2.3-head pixel grammar. No whole-body sliding-as-walk; independent planted foot, knees, elbows and face animation must remain functional with new outfits.

Each outfit is a **data recipe** containing:
`outfit_id`, `body_geometry_family`, `required_rig_version`, `compatible_views`, `base_layers`, `top_layers`, `bottom_layers`, `footwear_layers`, `headwear/occlusion_rules`, `profession_context_ids`, `source_provenance_ids`. No layer paths or career IDs buried in game logic. Outfits do **not** directly alter income/time/stats.

Examples:
- Developer/office: casual smart shirt, tee, cardigan, practical shoes.
- Sales/marketing: smart casual, comfortable appointment-ready shoes.
- Coffee/F&B: original apron, simple work top, closed-toe shoes, optional headwear.
- Food preparation: locally plausible practical apron/hair restraint.
- Founder/night-study: original casual home wear, interchangeable messenger bag.
All genders may equip all functionally compatible clothes. Never use company logos/brands unless independently licensed; no fake Vietnamese rendered inside sprites.

When the character changes jobs, `CareerOutfitResolver` **reads** authoritative `CareerId` plus user choice to resolve a visual outfit preset. Job changes never rewrite face/hair identity. Allow retaining personal clothes outside professional activity; workplace safety outfits override *visual slots* only for relevant scene activity. Returning home restores chosen home wardrobe. Changing visual outfit never triggers an economic transaction.

### Performance target architecture
- `CharacterAppearanceProfile`: cosmetic selections, immutable ID/value validation.
- `CharacterVisualCatalog`: ScriptableObject/data asset for authored parts, layers, pivots and allowed combinations.
- `CharacterVisualAssembler`: presentation-only and pooled; rebind changed sprite layers without destroying/recreating 20 GameObjects per wardrobe tap.
- `CareerOutfitResolver`: pure data-driven career/context → outfit recipe and occlusion.
- `CharacterMotionDriver`: shared semantic `idle/walk/talk/type/study/carry/serve` controller and animation/foley hooks, no per-career duplicate AnimatorController.
- `CharacterTraitRules`: **future authoritative simulation only**, NOT a MonoBehaviour or view-side reward calculator, pending owner approval.
- Original layered source and rig assets must pass provenance/license, human independent review, Unity import, BeautyCell and real phone checks before entering `Art/Approved/registry.json`.

Use sprite atlas and material reuse where possible, point filtering, mipmaps disabled; benchmark renderer counts, texture memory and rig CPU on low/mid Android. A 30-character stress scene is a test case, not a claim of achieved performance.

## 4. Historical save & replay impact

**Current repo baseline**: `CharacterCreationViewController` submits `GameCommand(CreateCharacter, backgroundId, age, name, appearanceId)`; `GameState` has `AppearanceId`, `BackgroundId`, `LearningSpeed`; `FirstPlayableFlow` exposes simple create flow. `SimulationEngine.Create` validates allowed appearance IDs from the start definition. Previous V2 and V3 payloads, receipt checks and frozen fixtures must remain byte/semantic compatible.

Do **not** repurpose existing `AppearanceId` into a serialized JSON blob, encode cosmetics in `Name`, embed changing hair/outfit into immutable character-creation receipts or rewrite historical snapshots. Preserve the existing `base.male` / `base.female` identifier meaning for existing runs.

Cosmetic persistence should be developed as an **optional V4-compatible authenticated extension (or a narrowly reviewed successor format)** with default mapping from legacy appearance IDs, versioned catalog references and atomic update commands. New receipt/migration boundaries need independent design, original cold-replay tests, retries, CAS/ambiguous-write proof and deleted/renamed cosmetic-definition fallback. Cosmetics should never change `LearningSpeed` or old replay semantics.

Player-selected traits affecting simulation (if approved) require a separate explicit GDD/economy decision, command canonicalization and audited migration. Deliver the purely visual creator and wardrobe first, without any gameplay rebalancing.

## 5. Verification requirements

- Four views have consistent face/hair identity; all outfit layers align for walk/type/serve and turn transitions without tearing/blur, mirrored bags/straps remain correct.
- 6 expressions neutral/happy/focused/worried/tired/determined; adult-coded inclusive range, 2.2–2.4 head ratio; independent art/IP/cultural review.
- Character viewport readable with text 100/150/200%, 360×640, 390×844, 430×932; safe areas and keyboard; no editor overlay clipping or inaccessible next/back.
- Deterministic visual ID/slot validation and hair-under-helmet, long-hair-behind-shirt, hands-above-sleeves ordering; malformed IDs fall back without avatar disappearance.
- UI authoring doesn't trigger save reads per slider/swipe; draft visual changes remain presentation-only until explicit Confirm.
- Legacy saves load the original presentation even if no optional customization exists. A revoked art asset is never loaded or silently substituted by an unapproved asset.
- Unity Editor/Android real-device comparisons and CPU/GC/GPU/texture memory measured. CI static source checks are *not* enough for Golden sign-off.

## 6. Ordered implementation slices — all unstarted

1. `P03-CHAR-01` — original 2.3-head, 4-view Golden character silhouette & common rig (front C01 prototype remains quarantined); independent review.
2. `P03-CHAR-02` — validated cosmetic data catalog, compatible layer render/preview, editor import/atlas/occlusion tests (no save changes yet).
3. `P03-CHAR-03` — responsive 4-page Character Creator UI, live preview, expression/turn/wardrobe; presentation-only draft and all error states.
4. `P03-CHAR-04` — data-driven profession outfit resolver; scene transition, idle/work/study/serve; no economy-affecting outfits.
5. `P03-CHAR-05` — approved appearance persistence ADR/V4 migration+CAS/cold replay and exact-version run evidence; historical fixtures unchanged.
6. `P03-TRAIT-01` — **optional**, only after owner formally approves personality/trait economy rules and their effect/limits, authoritative deterministic simulation, fair calibration, GDD revision and save migration.
7. Physical Unity/Android/iOS QA, Golden ≥15/16 without zeros, independent originality and commercial-rights clearance before release integration.

## Decision register (owner input required before affecting economy)

- `CHAR-D01`: **Proposed** — Nam/Nữ visual presentation is cosmetic; stamina, meticulousness and rapport are freely selectable/learnable independent traits. Owner's proposal to tie strengths to gender is *not yet implemented* and conflicts with current anti-discrimination art direction.
- `CHAR-D02`: **Proposed** — initial trait effects as small situational probability/narrative modifiers, not direct guaranteed money/salary bonuses; no hunger/stamina bars.
- `CHAR-D03`: **Proposed** — cosmetic customization after starting play remains available; profession workwear auto-preview is temporary visual scene context and does not remove personal choices.
- `CHAR-D04`: **Blocked design gate** — how cosmetics persist without breaking V2/V3/V4 authority requires save-architecture ADR and regression proof before implementation.

## Session closeout

**Owner model:** GPT-6 (Technical Director).  
**Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, startup-life-asset-quality-gate, vietnam-art-direction, ui-ugui, 2d-pixel-perfect.  
**Acceptance criteria:** product proposal identifies current code entry points, responsive creator, true layer/outfit reuse, equitable traits, animation/asset QA, history-safe persistence design, explicit approvals.  
**Tests:** NO new tests; documentation-only proposal. Existing CI status not claimed for this change.  
**Visual evidence:** NONE; no new character art or tested creator.  
**Save impact:** NONE.  
**Known limitations:** all proposed mechanics/UI/persistence unimplemented; explicit owner gameplay decision required.  
**Files changed:** `docs/production/CHARACTER_CREATOR_TRAITS_WARDROBE_PROPOSAL.md` only.  
**Commit:** see file Git history.
