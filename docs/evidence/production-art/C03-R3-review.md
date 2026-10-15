# C03-R3 — Sáng Ngõ Nhỏ, front-only revision

**Project:** Startup Life · **Task:** P03-CHAR-C03-R3 (continuation of R2 REVISE)  
**Target:** `vn_everyday_pixel_v1` · **Date:** 2026-10-16  
**R2 owner verdict:** `REVISE` (explicit message, no supplementary symptom list)  
**R3 status:** `DRAFT_AWAITING_OWNER_REVIEW`; **owner verdict:** `PENDING`.

## Provenance

First-party native-grid authored geometry; 85 manually specified contour/cluster commands.
No photography, third-party sprites, image-to-pixel filters, AI HD downsampling,
external illustration tracing, baked Vietnamese typography or brand marks.
Only the original wardrobe *idea* survives: muted green jacket, cream T-shirt,
blue-gray trousers, sneakers and commuter sling bag. Both R1 and R2 sources are retained.
Independent visual originality/commercial-rights clearance **NOT RUN**.

## What changed since R2

- Cropped and re-clustered the asymmetric dark hair with three restrained tones.
  Native hair clusters stop above the cheeks; chin outline remains skin only.
- Adjusted small eyes and separate asymmetrical brows; minimized nose shading.
- Redrew soft chin/neck connection and asymmetric jacket openings.
- Reduced the brown messenger bag bulk, kept a continuous diagonal strap.
- Individually authored thigh/shin/foot and arm/forearm/hand pieces using
  the existing 21 semantic layer names. No animation/rig-pivot proof is asserted.

**Caveat:** R3 remains aesthetically close to R2 in overall pose and silhouette.
The technical ratio and pixel checks cannot establish that the desired personal,
non-generic pixel art style is fully achieved. This remains an explicit owner gate.

## Measured from actual opaque pixel raster

| Item | Native result |
| --- | ---: |
| Canvas | 64×80 RGBA |
| Character alpha bounding box (xyxy, exclusive end) | (15, 7, 53, 79) |
| Hair crown to shoes (y=7..78) | **72 px** |
| Crown to chin, inspected native landmark (y=7..38) | **32 px** |
| Total / head | **2.25 heads** |
| Head share | **44.44%** |
| Semantic source layers | **21 nonempty** |
| Authored native contour/cluster commands | **85** |

The chin is a manually inspected art landmark, not an assumed 80px canvas ratio.
Visible alpha bounds are determined from the actual RGBA compositor. Hair/mouth
legibility and pleasant silhouette require human visual review.

## Reproduce and verification

```bash
python scripts/Render-GoldenCharacterC03R3.py --output-dir artifacts/C03-R3-review
python scripts/Validate-GoldenCharacterC03R3.py
```

Review outputs: `C03-R3-native-1x.png` (transparent), `C03-R3-review-8x.png`
(exact NN), `C03-R3-silhouette.png` and `C03-R3-comparison-R2-R3.png`
(left R2, right R3). The exporter also emits 21 separate 64×80 RGBA layers
and `C03-R3-technical-evidence.json` with hashes and real bbox.
These generated files belong to draft/evidence, **not production approved art**.

**Local technical checks PASS:** Native RGBA 64×80, alpha only 0/255, 21
nonempty layers, nearest-neighbor 8× byte-level pixel equivalence (background
applied after compositing), measured ratio in specified range.

**NOT RUN:** Unity runtime, real joint animation, device BeautyCell, independent
commercial IP review, mobile accessibility, final artistic signoff. No edits to
approved registry, runtime Character Creator, gameplay, economy, save, prior C03
revisions, variants, side/back views, outfits or animation.

## C03-R3 — OWNER VISUAL REVIEW REQUIRED

Please inspect actual 1×, 8×, silhouette and R2/R3 comparison. Select:
`APPROVE_DIRECTION` (front *direction only*), `REVISE` or `REJECT`.
Silence is not approval. A direction approval does not qualify a shipping asset.
