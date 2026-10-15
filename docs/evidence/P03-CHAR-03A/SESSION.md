# P03-CHAR-03A — Character Creator UI foundation

2026-10-15. Owner GPT-6 Technical Director. Baseline `b14adcc8ceda258239be7909363087b76ca0e7b4`.
**Scope:** data-only 19 localized cosmetic options for Nam/Nữ, face, hair, hair color, skin, accessory, outfit and gender-independent trait, with immutable default identifiers and four-step in-memory navigation. No sprite, external source asset, attribute bonus or save write. The active catalog is JSON, not hardcoded inside Unity controller. Pure CharacterCreatorDraftSession reuses existing CharacterLookCatalog and validates all localized choices.
**Skills used:** startup-life-session-orchestrator, ui-ugui, vietnam-art-direction, asset-quality-gate, gameplay-guardian.
**Acceptance:** no cosmetic rebalancing; canonical legacy AppearanceId, independent traits, in-memory draft, four bounded UI pages, localized menu choices.
**Tests:** two new engine-free PresentationChecks and JSON validator. Exact HEAD CI required. Unity Editor/device not run.
**Visual evidence:** NONE. Real UI binding will follow separately; art remains unapproved and C01 draft is not Art Bible-compliant.
**Save impact:** NONE; no persistence introduced.
**Known limitations:** textual choices only, no Unity scene preview, no 200% typography proof or responsive device captures.
**Files changed:** pure session C#/.meta, JSON option catalog/.meta, PresentationChecks project/tests, validator, CI, evidence.

## UI builder and runtime controller integration

Follow-up adds a scroll-clipped, four-page character creator and reusable selector rows to `FirstPlayableShellBuilder`. It **reuses** the existing name/age/Nam-Nữ controls and associated serialized UnityEvents by reparenting, not recreating. New text-only option controls stay under `CharacterCreatorScroll`; Back/Next and the historic Create button remain reachable outside that viewport within `MobileSafeArea`. Every option label and localized Vietnamese copy is sourced from `CharacterCreatorDraftCatalog.json`, and the serialized TextAsset is bound on both Create and the explicit idempotent `RepairGeneratedAssetReferences` path. Runtime `CharacterCreationViewController` applies draft changes in memory and sends *only* the original appearance ID in `CreateCharacter`, preserving receipt/save semantics. Old, not-yet-repaired scenes keep the legacy identity-only flow instead of loading unapproved sprites. A pending-preview label explicitly discloses absent approved art.

Added an engine-free UI wiring validator plus 3 mutation cases. **Actual existing .unity scene has NOT been repaired or imported**; required next action is Unity Editor `RepairGeneratedAssetReferences()` and visual/mobile verification. No real responsive screenshot or dynamically-scaled font at 200% is claimed. Art/IP registry remains unchanged and no economy traits applied.

## Follow-up — safe Editor migration and transparent UX

The explicit Unity Editor menu `Startup Life/First Playable/Repair assets and Character Creator` upgrades an existing serialized scene without wholesale recreation and preserves the existing character actions. A repair of horizontal rows now preserves `theme.touchTarget` for newly authored `Creator*` selector/navigation controls instead of downgrading them to 90. A persistent user-visible text note clarifies that only identity fields persist, while cosmetic/trait choices remain an **unpersisted** in-memory prototype pending the separate save/CAS migration. This is not a 48dp device claim; device measurements are still mandatory.
