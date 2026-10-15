# C03-R4 — Visual concept handoff (NOT a native pixel sprite)

**Task:** P03-CHAR-C03-R4  
**Status:** `CONCEPT_ONLY_AWAITING_NATIVE_PIXEL_REAUTHORING`  
**Date:** 2026-10-16  
**Art authority:** Owner feedback on C03-R2 (left) versus C03-R3 (right).  
**Owner verdict:** `APPROVE DIRECTION — REVISE AGAIN` **for direction only**, not a Golden asset approval.

## What exists

One newly generated front-view *concept illustration* depicting the target look:
friendly short-haired young adult, slightly asymmetrical fringe, expressive small
eyes, softened cheeks/jaw, olive jacket, cream shirt, blue-grey trousers,
compact brown cross-body bag and wider dark sneakers. It was shown to the owner
in conversation but **the bitmap is not committed to this repository**.

Concept-only generation evidence:
- Local output basename: `friendly_pixel_messenger_character.png`
- Local PNG SHA-256: `b50fd50492a86126c0652425e9bffaa2820952148cb2877d33c20008b92f58fe`
- Canvas: **1122×1402, RGB**; non-native preview resolution, not a 64×80 RGBA asset.
- No per-joint alpha layers, no pixel-grid reconstruction, no sprite importer or Unity use.
- Do **not** claim that its blocky appearance proves hand-placed native pixels,
  exact nearest-neighbor expansion, originality clearance or commercial usability.

**Important:** This visual is a high-level *reference brief*, not admissible
as the sprite source under `PRODUCTION_PIXEL_STYLE_CONTRACT.md`.
Do **not** downsample, trace, posterize or run a pixel filter on this PNG.
Use it only to communicate desired art direction. Future shipping pixels must be
re-authored from scratch at the fixed native grid with rights/provenance evidence.

## Locked art direction for native C03-R4

Base **R2's sturdier torso/legs/shoes silhouette** while adopting **R3's softer,
friendlier face and hair feel**, without mechanically copying either raster.
Only **one adult character / one FRONT view** until owner review.

1. **Hair:** soft short style, asymmetric left fringe, tidier right side,
   one unmistakable little crown tuft, 3 controlled native colors maximum;
   no black beard-like cheeks, helmet or AI/gloss gradient.
2. **Face:** shorter rounded jaw, kind yet slightly witty small eyes/brows,
   off-center smile, compact visible blush. At 1×, eyes and mouth must remain
   legible, never large anime pupils.
3. **Pose:** gentle weight shift, one shoulder 1–2 native pixels lower,
   hands visibly separate, slight side-to-side rhythm but no unreadable lean.
4. **Clothing:** olive jacket, cream undershirt, blue-grey trousers, and
   single cross-body commuter bag; asymmetric hem/collar only, no logos or
   unreadable text. Keep strap and bag connected correctly in layers.
5. **Feet:** visibly separate short lower legs and wider, slightly outward
   sneakers. Keep feet/forearms/hands ready for genuine joint animation.
6. **Native pixel craftsmanship:** author each useful cluster directly on the
   **64×80 transparent RGBA grid**; retain 21 established semantic rig names,
   reuse the existing `Render-GoldenCharacterC03R1.py` painting logic and
   `PixelPng.py`. No duplicated renderer.

## Deliverables required before the next owner review

- `r4-front-draft.json`: new provenance-rich, hand-authored native pixel commands,
  **not** an edited copy of an AI concept bitmap.
- `C03-R4-native-1x.png`: real 64×80 transparent RGBA.
- `C03-R4-review-8x.png`: proven nearest-neighbor upscale.
- `C03-R4-silhouette.png` and `C03-R4-comparison-R2-R3-R4.png`.
- `C03-R4-review.md`: actual opaque bbox / head ratio (target 2.2–2.4
  heads, 42–45% occupied height), 21 semantic layers, diagnostics, remaining issues.
- Real displayed PNGs and explicit owner `APPROVE_DIRECTION / REVISE / REJECT`
  question. Owner silence does not mean approval.

No further viewpoints, expression batches, outfits, rigs or animations before
the owner evaluates the **native front view**. Final Golden approval additionally
requires Unity articulated animation, mobile BeautyCell and independent
visual/IP rights review.

## Session boundary

**Skills used:** `startup-life-session-orchestrator`,
`vietnam-art-direction`, `startup-life-asset-quality-gate`,
`2d-pixel-perfect`.  
**Tests:** Repository/head inspection and concept file metadata only. No new
native sprite QA, build, Unity or device testing.  
**Visual evidence:** Chat-only concept raster at 1122×1402; not versioned sprite.  
**Save impact:** None.  
**Known limitations:** Image binary absent from repo; native R4 source and 21
layers **NOT CREATED**; independent IP and owner sprite review **NOT RUN**.  
**Files changed:** This document and art-direction status pointer (if updated).
