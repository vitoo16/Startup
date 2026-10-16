# C03-R4 — Native front-only final polish review

**Task:** P03-CHAR-C03-R4 / Sáng Ngõ Nhỏ  
**Style:** `vn_everyday_pixel_v1`  
**State:** `DRAFT_AWAITING_OWNER_REVIEW`  
**Owner art direction:** `APPROVE_DIRECTION — FINAL_POLISH_REQUIRED`; **R4 sprite approval has not been granted.**  
**Date:** 2026-10-16

## Provenance and files

- First-party authored native-pixel changes to the existing C03-R3 layered JSON. R3 is preserved, and **R3's hair and facial cluster commands are byte-for-byte identical** in R4.
- Native R4 source: `Assets/StartupLife/Art/Source/Characters/golden-character-c03/r4-front-draft.json` (87 commands, 21 semantic layers).
- Renderer: `scripts/Render-GoldenCharacterC03R4.py` reuses the R1 native painter and R2 comparison/export helpers. No duplicate rendering engine.
- Validator: `scripts/Validate-GoldenCharacterC03R4.py`. Structural checks alone **never constitute art approval**.
- No stock sprites, downloaded source images, HD image-to-pixel filters, gradients, tracing or external brand marks. Earlier high-resolution R4 concept is only art-direction context; the native sprite is generated from manually revised JSON commands, **not** resized from that concept. Independent IP similarity/rights clearance is still required.

## Art Director redline execution

| Region | Change in R4 | Remaining reviewer question |
| --- | --- | --- |
| Neck | Native skin neck contour shortened 2 rows, collar shape adjusted | At 1×, does head now sit naturally on shoulders? |
| Ears | Left/right outer ear outlines tucked in conservatively | Ears should remain recognizable without dominating head width |
| Pose | Left shoulder, forearm and hand lowered one row; right hand reshaped; jacket hem staggered | Is asymmetry enough to feel alive without looking crooked? |
| Legs/shoes | Right shin shifted outward one column, both sneakers redesigned with defined toe and cream sole | Avoid oversize or flat platform-shaped soles |
| Strap/bag | Cross-body diagonal strap rebuilt; bag made smaller and closer to body, with compact flap | Ensure bag/hand do not visually collide |
| Hair/face | **No changes** to R3 hair and face source commands | Existing owner-approved direction should remain recognizable |

These are intentionally localized front-view refinements, not a new persona or costume. Comparison is **R2 (left), R3 (middle), R4 (right)** at the same actual native-to-8× scale.

## Measurements from composited alpha, not canvas height

| Quantity | Verified local value |
| --- | ---: |
| Native transparent RGBA | 64×80 px |
| Occupied bounds, xyxy exclusive | (15,7,53,79) |
| Actual visible height | 72 px |
| Hair top and inspected chin bottom (inclusive) | 7, 38 |
| Head height | 32 px |
| Real height / head | **2.25 heads** |
| Head fraction | **44.44%** |
| Semantic layers with native pixels | **21 / 21** |
| Commands | **87** |
| Occupied-pixel changes versus R3 | **305 of 1967 (15.51%)** |
| Hair/face command changes versus R3 | **0** |

The chin boundary is checked as a visual landmark. This measurement proves only ratios, not beauty, comfort, animated joint placement or Golden style.

## Reproduction and QA

```bash
python scripts/Render-GoldenCharacterC03R4.py --output-dir artifacts/C03-R4-review
python scripts/Validate-GoldenCharacterC03R4.py
```

Generated review artifacts (quarantined; not approved sprites):
- `C03-R4-native-1x.png` — actual 64×80 transparent RGBA
- `C03-R4-review-8x.png` — exact integer nearest-neighbor on neutral review background
- `C03-R4-silhouette.png` — monotone shadow derived from opaque R4 pixels
- `C03-R4-comparison-R2-R3-R4.png` — equal native-scale comparison
- `layers/*.png` — 21 individual 64×80 semantic layers
- `C03-R4-technical-evidence.json` — local output SHA-256 and measurements

**Local engine-free test:** PASS for RGBA dimensions, strict 0/255 alpha, 21 semantic layers, R3 unchanged face/hair, pixel-by-pixel exact 8×, and real pixel head/body ratio. Python source compilation also passed locally.

**NOT RUN:** Unity importing and Sprite Editor pivots, articulated cutout animation, Unity PlayMode, Android/iOS BeautyCell, independent originality/IP clearance and release scorecard. No changes to gameplay, economy, save, Character Creator, older revisions or the approved registry.

**Visual concern still open:** The overall R4 silhouette remains close to R3 on purpose; facial acting and shoe-to-leg harmony need owner judgment at 1×. Do not claim mascot/Golden readiness from this technical pass.

## C03-R4 — OWNER VISUAL REVIEW REQUIRED

Owner should inspect all four actual PNGs and answer:

**`APPROVE_DIRECTION` / `REVISE` / `REJECT`.**

Silence means pending. Even `APPROVE_DIRECTION` is a front-view *direction* decision only, not production approval. Additional views and animation remain paused until this review is resolved.

### Implementation session closeout

- **Owner:** GPT-6, Technical Director / asset author.
- **Skills used:** `startup-life-session-orchestrator`, `vietnam-art-direction`, `startup-life-asset-quality-gate`, `2d-pixel-perfect`, `sprite-editor` (Unity importer guidance only, no live editor).
- **Tests:** Local Python R4 render, dedicated structural validator, source compilation.
- **Visual evidence:** Actual 1× RGBA, 8× NN, silhouette and equal-scale R2/R3/R4 artifacts.
- **Save impact:** None.
- **Known limitations:** No Unity/phone evidence, no IP/legal clearance, no owner sprite verdict, no validated working rig.
- **Files changed:** R4 JSON + text meta, R4 renderer, R4 validator, this review and technical-evidence document.

## Owner verdict update — R4 REVISE REQUIRED (2026-10-16)

**Supersedes R4's previous `AWAITING_OWNER_REVIEW` state.**
The product owner explicitly rejected R4 as a final front-view Golden direction:
the face became too neutral, the haircut too cap-like and generic, and the
head-to-torso and rigid standing pose failed the personality/silhouette gate.
A numerically compliant 2.25-head ratio and pixel-perfect output do NOT
override this art-direction rejection.

**Disposition:** R4 native source remains immutable historical DRAFT; it is
**not** production approved and must not be used as the final personality
anchor. The next front-only investigation is
[`C03-R5-review.md`](C03-R5-review.md) with three separate **silhouette,
face/hair and full-character** review stages, explicitly restoring the playful
character of R2 without tracing/repixelating any R2 bitmap. This is not
permission to start other views, expressions, a rig or Unity animations.
