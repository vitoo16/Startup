# C03 — Transparent concept isolation (draft only)

**Date:** 2026-10-16  
**Owner-approved visual:** C03 / Sáng Ngõ Nhỏ, front view, satchel covers the relevant hand.  
**Original exact source SHA-256:** `d2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e`.  
**State:** `DRAFT_ALPHA_CUTOUT_FOR_EDGE_REVIEW`; NOT a native-pixel Golden sprite.

## Achieved

The source concept has a 1171×1343 opaque cream backdrop. A **non-destructive**
offline isolation script now exists:
`scripts/Prepare-C03OwnerConceptCutout.py`.
It explicitly checks the original PNG SHA-256 and format before creating:

- `C03-owner-concept-alpha-DRAFT.png`: original RGB pixels with a separate,
  binary, automatically estimated transparency mask.
- `C03-owner-concept-background-check-DRAFT.png`: an edge/halo inspection
  composited against a contrasting background.
- `C03-concept-isolation-report.json`: source hash, mask bounding box, cutout
  hash and unresolved gates.

**Observed local silhouette bounds:** x=330..816 and y=156..1171 of the
original 1171×1343 canvas (xyxy exclusive `[330,156,817,1172]`).
The mask had **327708 opaque pixels**, obtained by finding the connected
foreground containing the original dark hair at the image center.
The color clusters and face remain unchanged by the isolation operation.
The processed PNG was visually inspected on a checker-style background in the
art workstation; it is an inspection artifact, not a signed-off layered sprite.

## Reproduction after exact source PNG is staged into a local repo

```bash
python scripts/Stage-C03HandBagReference.py /path/to/C03-hand-behind-bag-upload-pack.zip
python scripts/Verify-C03HandBagReference.py
python scripts/Prepare-C03OwnerConceptCutout.py --output-dir artifacts/C03-cutout-review
python -m unittest discover -s tools/tests -p 'test_c03_*'
```

`Prepare-C03OwnerConceptCutout.py` requires Pillow on the **art workstation
only**; the game runtime has no Pillow dependency. This script is deliberately
limited to the exact owner-selected concept. It is **not** a sprite tracing,
downsampling, quantization, rig generation or production import tool. In
particular, hard-edge masking can discard detached detail or expose edge
halos, so the underlying reference PNG remains immutable.

## Remaining blocking production work

1. Commit the exact **original** PNG binary to the draft reference path and
   verify the Git blob against the SHA-256. The code connector still cannot
   transfer that binary, so **the image is not versioned in Git**.
2. Author actual native pixel clusters from the approved design and separate
   correct 21 RGBA rig parts with pivots. Do not use opaque concept export as
   the frame-by-frame runtime sprite or claim this cutout is native 64×80.
3. Author bag-body mask and hand pixels *under* `accessory_front` and pass
   the occlusion checker. It now explicitly rejects hand-skin peeking above
   or through the bag front in addition to rejecting zero overlap.
4. Verify Unity sorting, correct anatomical left/right binding, real joint
   motion, mobile BeautyCell and independent originality/IP clearance.

**Owner approval of appearance is preserved.** These remaining tasks concern
reproducibility and engine production only; no request to revise the chosen
hair, face, outfit or stance. Approved Registry and gameplay/save code remain
unchanged.
