# C03-R4 — Final Front Polish Redline (Art Director)

**Task:** P03-CHAR-C03-R4-FINAL-POLISH  
**Owner review date:** 2026-10-16  
**Style:** `vn_everyday_pixel_v1`  
**Verdict:** `APPROVE_DIRECTION — FINAL_POLISH_REQUIRED`  
**Scope:** One youthful adult chibi, **front view only**, no other character or animation.  
**Art status:** `CONCEPT_ONLY_AWAITING_NATIVE_PIXEL_REAUTHORING`.  
**Authority:** Latest owner review of the polished concept after C03-R2/R3 comparison.

This decision approves the *direction*, **not** any R3 or R4 image for production. The latest concept artwork (chat-only, high-resolution RGB) is not a native 64×80 RGBA layered sprite; do not downsample, auto-trace, filter, or derive native sprite contours from it. The artist must author the shipping pixel source from scratch. R1/R2/R3 evidence stays immutable.

## Lock / must not change

- Character identity, friendly young-adult demeanor, slightly playful small eyes, uneven soft short hair and asymmetrical fringe; **do not redesign face or hair**.
- Modern Vietnamese commuter/work-life context; avoid ethnic facial stereotypes, national-symbol decorations and fake text.
- Muted olive jacket, cream undershirt, blue-grey trousers, dark low sneakers, single crossbody everyday bag.
- Native-source grammar: 64×80 transparent grid, non-blurred RGBA, nearest-neighbor review at integer scale, existing **21 semantic rig layers**. No new renderer; reuse `Render-GoldenCharacterC03R1.py` and `PixelPng.py`.
- Size target measured from **actual occupied opaque pixels**, not the canvas: **2.2–2.4 heads tall** and **head share 42–45%**; don't change only labels or meta to satisfy ratio.

## Mandatory micro-redlines

| Priority | Region | Change | Reject when |
| --- | --- | --- | --- |
| P0 | Neck + head-to-body join | Shorten visually by **1–2 native rows only if proportions remain valid**. Tuck lower jaw toward jacket collar; preserve a clear chin/neck/cream-shirt transition without a straight neck column | Head looks perched on a thick post; chin/collar collides or joint seam is exposed |
| P0 | Ears | Reduce lateral protrusion conservatively (aim 1 native pixel per ear where possible), keep visible but blend into cheek contour. Retain asymmetry from the portrait | Ears dominate width at 1× or appear detached |
| P0 | Hips/legs/feet | Preserve **R2's stable lower-body foundation**. Offset one lower leg/foot slightly to convey weight shift; define separate thigh, shin, ankle and shoes. Make toe caps and sole read, with one sneaker subtly more outward | Both legs merge; floating shoes, broken footplant, oversized rounded boots |
| P1 | Posture/arms | Move just one shoulder ~1 native row, tilt torso very slightly (not a global layer slide), relax one wrist/hand while preserving all joint separations | Front actor becomes crooked, puppet-like, or symmetrically stiff |
| P1 | Strap + bag | Draw connected diagonal strap with deliberate pixel rhythm and layer occlusion; make bag a small rounded-square functional silhouette with a clean flap or seam, not extra decor | Strap floats/breaks at the torso; bag obscures hand/leg or turns into large shapeless block |

**Craft rules:** Pixel movement must be hand-authored in the semantic layer; no auto symmetry, gradients, blur, anti-aliasing, faux pixel overlays, excessive dithering, generic highlights or added logos. Do not add more than 1–2 clothing asymmetries. Palette reuse first; do not introduce colors without explaining their role.

## Before → After review protocol

Produce exactly one R4 **native front** candidate for the owner to inspect:

1. `r4-front-draft.json` source with provenance, native contours, every semantic part, and copyright/IP TODO; don't overwrite R1/R2/R3.
2. `C03-R4-native-1x.png` as true 64×80 RGBA.
3. `C03-R4-review-8x.png` as exact nearest-neighbor enlargement of native pixels (no glossy artist rendering substituted).
4. `C03-R4-silhouette.png` monochrome; one view only.
5. `C03-R4-comparison-R2-R3-R4.png` at equal native scale.
6. `C03-R4-review.md` listing actual alpha occupied bounds, head-vs-total pixel ratios, 21 nonempty layer count, remaining concerns and independent IP status.

**Visual checks at 1×:** eyes and small smile still show personality; shorter collar-neck appears natural; ear outline does not distract; feet visibly grounded and slightly offset; strap connects correctly; 1–2 subtle asymmetric accents, not more noise. Compare face/hair directly with owner-approved *direction* (not copying pixels from AI concepts).

**Technical checks:** exact 8× pixel-to-pixel equality after alpha compositing; strict grid bounds, only alpha 0/255, 21 semantic layers, no missing controls or changed thresholds; actual proportion measured from occupied image. If a checker passes despite bad aesthetics, retain the failure in the human visual review.

## Scope and gate

- **Allowed:** refine *only* this front view and its source layers.
- **Forbidden for now:** side, three-quarter, back, expression batches, outfits, second actor, rig animation, Unity scene binding, registry promotion.
- **Owner review:** display all four actual native-derived PNG views/comparison in chat. Explicitly request `APPROVE_DIRECTION / REVISE / REJECT`; silence leaves status pending.
- **Production approval:** separately requires integrated articulated joint motion, independent originality/IP clearance, Unity/mobile BeautyCell, owner Golden signoff.
- **Tests:** not run for any native R4 source: it does not yet exist. No art PASS inferred from the concept or this written redline.
- **Save/gameplay/economy:** no changes.

## Session closeout (documentation-only)

**Owner:** GPT-6, Art Direction / Technical Direction  
**Skills used:** startup-life-session-orchestrator; vietnam-art-direction; startup-life-asset-quality-gate; 2d-pixel-perfect.  
**Acceptance:** Owner's direction and precise final-polish criteria captured. No raster acceptance claimed.  
**Visual evidence:** Concept/redline illustration in chat only; not 64×80 source nor committed as shipping asset.  
**Tests:** GitHub file/head checks only; Unity, device, independent IP and R4-native validator **NOT RUN**.  
**Save impact:** None.  
**Known limitations:** Front native source R4 and layered PNGs are pending creation and owner review.  
**Files changed:** This redline and linked C03-R4 concept handoff.
