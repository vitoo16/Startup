# P03-01 — Quarantined layered Golden Character C01 (front pose)

**Date:** 2026-10-14  
**Owner model:** GPT-6 Technical Director  
**Baseline:** `f63f92c52179ab178e82aad6baf4e657fffe94e9` (engine-free CI PASS).  
**Status:** EDITABLE_SOURCE_AND_EDITOR_GENERATOR_ONLY; not imported/rendered/approved.

## Deliverable
- Hand-authored, first-party-designed (AI-assisted) JSON pixel rectangle source at `Assets/StartupLife/Art/Source/Characters/golden-character-c01/layers.json`, with 20 independently movable, stable-named layers, integer logical joints, sorting order, 128×160 canvas and 32 PPU, no externally sampled game sprite.
- Editor-only authoring tool `GoldenCharacterDraftBuilder.cs` imports tightly cropped sprites with point filtering, no mipmaps, uncompressed textures and pixel-aligned custom pivots via `TextureImporter` (not hand-authored sprite .meta). Creates an isolated, **unapproved** proof prefab, one AnimatorController and four genuine joint rotation animations: idle/walk/type/talk. Forearm/knee/upper leg are separate animated joints. Authoring output is restricted to `Art/Source/Characters/golden-character-c01/Generated`; no shipping scene/BuildSettings/Approved registry edits.
- Provenance manifest records one DRAFT candidate with no fabricated rights, legal approval, Unity import proof, screenshot or scorecard. Originality cannot be guaranteed purely by new code or artwork.
- CI checks layer graph, pivots, bounds, palette, mandatory layers, camera/PPU consistency, pending views, separate approval and existence of motion-authoring code, plus ten deterministic fixture cases.
- Only front view is authored; side and 3/4 remain pending. No best-of-8 art review claim. **No Golden asset has passed the 15/16 gate**.

## QA / trace
- **Skills used:** `startup-life-session-orchestrator`, `startup-life-asset-quality-gate`, `vietnam-art-direction`, `2d-pixel-perfect`, `sprite-editor`; implementation follows Unity Editor TextureImporter and animation pattern; the sprite-editor skill requires an actual Unity Editor to confirm generated import metadata.
- **Acceptance criteria:** editable unapproved layered source; actual separate arm/knee animation paths in authoring code; strict quarantine; no existing production scene refs; no duplicate rig logic.
- **Tests:** engine-free validator and 10 fixture tests added; actual head CI verdict pending; Unity Editor compilation and visual inspection **NOT RUN** in this environment.
- **Visual evidence:** NONE. Generated PNG/Animator prefab requires execution within Unity 6000.3.25f1 and phone BeautyCell. The rendered avatar has not been examined yet.
- **Save impact:** NONE. This slice adds no game progression, save or simulation changes.
- **Known limitations:** Prototype rig curves are authored but not verified at runtime, no Unity-generated assets committed, no side/3/4 turnaround, no lip/face expression animation, no final IP chain-of-title approval. Complete 8–12 actual distinct Golden candidates and independent scorecards before production registry approval.
- **Files changed:** C01 source JSON/.meta + folder .meta, art-spec prototype source ref, unapproved provenance draft, Editor builder and .meta, character validator/fixture suite, CI and this record.
- **Commit:** See Git commit containing this session record.

## Follow-up — Data-driven joint animation

The prototype initially encoded per-motion joint amplitudes in the Editor script. This was replaced within the same P03-01 slice by four authored motion clip definitions in `layers.json`, each with editable duration and five-angle joint tracks. The generic Unity clip generator has no hand-coded walk/typing angles and uses one reusable motion path. Static validator refuses clipped curves, duplicate/unknown bones, >45-degree amplitudes, walking without independent knees/opposing legs, and typing without moving forearms. Added four regression mutations. This is static authoring evidence only; actual Unity playback remains unverified.
