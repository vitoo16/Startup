# P03-CHAR-02B — Approved-only character layer composition

**Date:** 2026-10-15 (Asia/Ho_Chi_Minh)  
**Owner:** GPT-6 Technical Director  
**Starting main:** `bc198f43331b0b9ae997b6ef57d988fd2f922707`; prior engine-free CI SUCCESS.  
**Status:** CODE ADDED; exact-commit .NET / engine-free CI required. No Unity Editor/playback/device/Art Director signoff claimed.

## Scope
Added a pure `CharacterVisualComposer` over the existing cosmetic `CharacterLookCatalog` + `CharacterWardrobeResolver`. Four explicit `CharacterRigView` variants (front, 3/4 left, side, back) require independently authored Face/Hair/Outfit bindings before composition. A plan exposes stable rig-part → preapproved sprite-id entries sorted by layer priority, with transparent occlusion (hat can cover hair). Work career overlays do not change saved personal clothing. SkinTone/HairColor remain *semantic palette tokens*, not prematurely recolored sprites. No visual layer is associated with trait or changes gameplay.

The composer accepts an explicit asset allowlist from **independently approved sprite IDs**, fails closed for any unapproved sprite/unknown catalog option/duplicate rig part/unsupported view. The .NET tests use `fixture.*` mock IDs, NOT actual production approvals. The real approved registry remains empty, and the new planner has no wiring into scenes or generated prefabs. Do not claim any avatar art is available in shipping builds.

## Clean code/performance
All new code is pure and Unity-engine-free (existing Presentation asmdef), with small indexed lookups. No filesystem/save reads or allocations during idle frames; per user preview composition creates only a bounded new immutable plan. No cloned rig/controllers, no new authority over economy/receipt/appearance save.

## Verification and closeout
- **Skills used:** startup-life-session-orchestrator, gameplay-guardian, asset-quality-gate, vietnam-art-direction, ui-ugui, sprite-editor, 2d-pixel-perfect.
- **Acceptance:** no unapproved sprite exposure; deterministic front/side/3/4/back mapping, stable sorting, hide covered hair, all cosmetic traits equal across genders, contextual wardrobe reuse.
- **Tests:** seven new `tools/PresentationChecks/Program.cs` cases, full engine-free regression via existing workflow; status requires exact HEAD CI run.
- **Visual evidence:** NONE; no runtime layer sprite assets passed approval, no Unity scene/prefab/BeautyCell changed.
- **Save impact:** NONE. Old `AppearanceId`, Command/receipt schema and frozen archive untouched.
- **Limitations:** No actual avatar view can render until independently approved assets exist and a Unity adapter binds them. Layered sprite atlas/material palette and UI are follow-up P03-CHAR work. Legal/IP requires independent signoff.
- **Files changed:** `Assets/StartupLife/Scripts/Presentation/CharacterVisualComposition.cs`, new Unity .meta, `tools/PresentationChecks/StartupLife.PresentationModel.csproj`, `tools/PresentationChecks/Program.cs`, this evidence.
