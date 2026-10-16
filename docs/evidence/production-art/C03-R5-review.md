# C03-R5 — Character personality and silhouette review

**Task:** P03-CHAR-C03-R5  
**Style ID:** `vn_everyday_pixel_v1`  
**Owner decision on R4:** **REVISE REQUIRED** — R4 was technically valid but too generic, rigid, and insufficiently expressive.  
**R5 status:** `DRAFT_AWAITING_OWNER_REVIEW` — NOT Golden approved.  
**Date:** 2026-10-16

## Revised production gate

Review **one actor, one FRONT** through **three separately visible states**, in order, at **true native 1×**, with integer nearest-neighbor enlargement only:

1. **Silhouette:** monochrome of actual occupied native pixels. Does the shape communicate a distinctive person, weight distribution, asymmetric hair and readable feet? A technically different mask is not automatically a memorable one.
2. **Face & Hair:** isolate the `head`, `face`, `hair_front` artwork. Can the tiny face show warmth and playful wit, rather than two neutral dots? Can the off-center fringe be recognized?
3. **Full Character:** evaluate clothing, bent/right hand gripping the strap, relaxed/left hand, off-axis leg/footplant and the combined shape language. Only after the owner accepts all three stages can we lock pivots and separately approve rig-ready production layers.

All three states are **different views of the same front-view draft, not different characters or additional camera views**. The final full-character stage is still an *art study*. The 21 established semantic part names are preserved in the drawing source but **independent pivots, limb deformation, sprite slicing, reliable runtime sorting, genuine walking animation and Unity import are NOT implemented or approved**. In the preview, `hand_r` is deliberately composited last to show the fingers in front of the strap. This changed presentation stack must be reconciled with Unity/rig sorting if the artwork is ever approved.

## Authentic, original source / reproducibility

- Source: `Assets/StartupLife/Art/Source/Characters/golden-character-c03/r5-personality-front-review.json`
- Painter: **unchanged, reused** `scripts/Render-GoldenCharacterC03R1.py` + `scripts/PixelPng.py`
- Reviewer exporter: `scripts/Render-GoldenCharacterC03R5.py`, reusing R1 native painting and existing R2 sheet utility
- Stage validator: `scripts/Validate-GoldenCharacterC03R5.py`
- 78 explicitly authored first-party native pixel polygon/row commands, without pixelating, downsampling, tracing or recoloring an external/earlier asset. The R2 *expressive intent* and wardrobe concept were reviewed as art direction only. No third-party logos or identifiable marks. Independent IP/originality review **NOT RUN**.
- R1–R4 historical sources and tests are unchanged. No edits to `Assets/StartupLife/Art/Approved/registry.json`.

Reproduction from repo root:

```bash
python scripts/Render-GoldenCharacterC03R5.py --output-dir artifacts/C03-R5-review
python scripts/Validate-GoldenCharacterC03R5.py
```

## What the personality pass attempts

- **Hair:** a single asymmetric low swept fringe on screen-left, tidy short screen-right, three distinct dark hair roles rather than equal helmet-like bangs. No beard-like dark side masses touching the chin.
- **Face:** left eye slightly narrower/lower, right eye more awake, eyebrows with differing heights, a smile raised at screen-right, two compact cheek marks; no anime highlights.
- **Anatomy and stance:** torso is subtly off-center over the supporting left leg; relaxed right leg opens outward; nonmatching sneaker toes and sole lengths. No global sprite translate used as false pose.
- **Gesture:** left arm loose, right forearm bends inward to a clearly visible foreground hand at the strap.
- **Clothing and bag:** retain muted green/cream/blue-grey outfit and small brown commuter bag without textures/logos/visual noise.

**Known unresolved art concern:** despite having changed hair and face, this R5 draft might still read as too neat/generic at actual gameplay pixel scale; silhouette remains nearly static through the torso. The strap/hand overlap and prominent bag shape require close owner inspection. No internal self-score should count as aesthetic approval.

## Pixel evidence (real opaque area, NOT 80px canvas)

| Measurement | Result |
| --- | ---: |
| Native source canvas | 64×80 transparent RGBA |
| Occupied bounding box xyxy exclusive | (14, 7, 53, 79) |
| Actual occupied height | 72px |
| Hair crown → visual chin inclusive | y=7..38 = 32px |
| Heads tall | **2.25** |
| Head fraction | **44.44%** |
| Alpha | 0/255 only |
| Layer namespaces | 21, reservation only — not a validated animation rig |
| Hand-authored polygon/cluster commands | 78 |

The exact face landmark is visually inspected, and the occupied pixel extrema
are computed from actual native RGBA. 2.25 heads satisfies the numerical
contract but **does not** establish charm, character identity or Golden readiness.

## Review files (draft only)

- `C03-R5-silhouette-1x.png`, `C03-R5-silhouette-8x.png`
- `C03-R5-face-hair-1x.png`, `C03-R5-face-hair-8x.png`
- `C03-R5-full-character-1x.png`, `C03-R5-full-character-8x.png`
- `C03-R5-stages-1x.png`, `C03-R5-stages-8x.png` — common visual review board
- `C03-R5-comparison-R2-R4-R5-8x.png` — equally scaled history
- `C03-R5-technical-evidence.json` — SHA256 of all native generated images

**Local technical gate:** Python script compilation and stage validator PASS: 64×80, all three stages nonempty, 8× checks pixel-for-pixel after alpha compositing, binary alpha, chibi height from actual RGBA, no owner/IP/Unity claim. **Not run:** Unity editor, actual rig, animation, devices/BeautyCell, independent IP comparison.

## OWNER REVIEW REQUIRED — three separate questions

1. **Silhouette:** Does the haircut and standing weight have distinctive, convincing shape at 1×?
2. **Face & Hair:** Does the face now convey gentle, slightly playful personality, with a signature asymmetric fringe?
3. **Full Character:** Is the strap-grip/arm and supporting/relaxed leg believable and cute without clutter?

Record overall `APPROVE_DIRECTION`, `REVISE`, or `REJECT`, optionally with stage-specific veto. Owner silence never grants approval. All views, expression sheets, outfits and real animation remain blocked until this gate is resolved.

## Session closeout

**Owner model:** GPT-6.  
**Skills:** startup-life-session-orchestrator, vietnam-art-direction, startup-life-asset-quality-gate, 2d-pixel-perfect, sprite-editor (reference-only; no live Editor).  
**Save impact:** None.  
**Files:** R5 review JSON, renderer/exporter, validator, this review, R4 owner decision note and technical evidence hash manifest.  
**Known limitations:** Aesthetic readiness pending, rig drawing order is prototype-only, Unity/mobile & IP unverified.
