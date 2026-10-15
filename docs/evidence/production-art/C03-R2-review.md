# C03-R2 / Sáng Ngõ Nhỏ — front view owner review

**Task:** P03-CHAR-C03-R2  
**Status:** DRAFT_AWAITING_OWNER_REVIEW  
**Owner verdict:** PENDING  
**Target style:** vn_everyday_pixel_v1  
**Date:** 2026-10-16

## Source and provenance

Exactly one hand-composed native 64×80 front view using **79 original polygon/pixel-row commands** and **21 semantic rig layers**. The earlier C03-R1 source is preserved. The artwork uses the original R1 wardrobe *concept* (olive jacket, cream shirt, blue-grey trousers, commuting bag), not the rejected character silhouette or internet sprite assets. No imported illustrations, logos, photo pixelation, image-filtered artwork, smooth gradients, or third-party textures. Independent worldwide IP similarity/rights review remains outstanding.

Sources:
- `Assets/StartupLife/Art/Source/Characters/golden-character-c03/r2-front-draft.json` — authored pixel strokes, palettes, rig semantics and provenance.
- `scripts/Render-GoldenCharacterC03R2.py` — review-export orchestrator reusing R1's existing native RGBA painter and PixelPng utility; intentionally does not replace the renderer.

Reproduce the 1×, 8×, silhouette, side-by-side and 21 layer PNGs without registering assets:

```bash
python scripts/Render-GoldenCharacterC03R2.py --output-dir artifacts/C03-R2-review
```

The PNGs in this review are temporary **draft evidence**, never `Assets/StartupLife/Art/Approved` files and are not referenced by gameplay scenes.

## Real-pixel proportions (not canvas height)

| Measurement | Value |
| --- | ---: |
| Native canvas | 64×80 px |
| Occupied alpha bbox (x0,y0,x1-exclusive,y1-exclusive) | (15,7,54,79) |
| Actual character height: y=7..78 | 72 px |
| Hair crown to chin: y=7..38 (inclusive) | 32 px |
| Ratio | **2.250 heads tall** |
| Head share of actual character height | **44.44%** |

The crown/chin are *art-directed visual landmarks checked on the actual pixels*, and do not include the neck; automated alpha bounding-box calculation independently verifies the body height. Meets owner-specified ratio 2.2–2.4 and head fraction 42–45%. Passing this ratio alone never means Golden art approval.

## Changes versus rejected R1

1. **Hair** — shortened and made asymmetric, 3 purpose-specific tones, no box highlights and no black side masses continuing down the cheeks or jaw (hair pixels at or above native y=25).
2. **Face** — small uneven eyes, slight brow height offset with separation from fringe, softer cheeks and a subtly asymmetric smile. No anime pupils, sparkle or heavy nose/chin shading.
3. **Body** — visible neckline and shoulder break; arms and individual hands separate from torso; distinct shorter trouser legs and slightly splayed shoes.
4. **Outfit** — recognizable uneven jacket lapels over cream tee and corrected continuous diagonal bag strap, with flap on commuting bag.
5. **Rendering** — original alpha binary pixels, 8× exact nearest-neighbor without filters. Same 21 semantic rig parts, but this does **not** prove animation or compatible verified joint pivots.

## Verification and limitations

**Technical PASS (local engine-free evidence):** source renders correctly, true 64×80 RGBA, all 21 layers populated, zero partial-alpha edge pixels, visible bbox and head ratio measured from image, 8× output checked pixel-for-pixel.

**No Golden PASS:** owner's aesthetic view not received; independent artistic/originality/IP review not run; no Unity import/animations/device BeautyCell; no approval registry entry. An appearance or silhouette that technically satisfies sizes may still be visually rejected.

**Known concerns for human review:** bag/hand overlap, facial nuance at 1×, and how strongly the new silhouette communicates a friendly working adult. Environment will later provide specifically Vietnamese context; ethnicity must not be inferred from facial features.

Out of scope: other views, expressions, outfit variants, animation, second character, Character Creator runtime, gameplay/economy/save. All remain unchanged.

## OWNER VISUAL REVIEW REQUIRED

Inspect actual PNGs:
- C03-R2-native-1x.png (transparent 64×80).
- C03-R2-review-8x.png (exact 8× nearest-neighbor).
- C03-R2-silhouette.png (dark silhouette on cream).
- C03-R2-comparison.png (**left R1**, **right R2**, same 8× source-pixel scale).

**Decision: `APPROVE_DIRECTION` / `REVISE` / `REJECT`.** Silence is not approval. APPROVE_DIRECTION allows further view *exploration* only; production clearance requires independent originality, rig + mobile BeautyCell, and owner Golden approval.
