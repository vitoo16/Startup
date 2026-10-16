# C03 — Engine-free 16-frame Idle articulation proof and Unity handoff

**Date:** 2026-10-17  
**Character:** C03 / Sáng Ngõ Nhỏ  
**Owner-approved scope:** exact front visual and native front **direction** only  
**Status:** `ARTICULATION_PROOF_PENDING_UNITY_AND_DEVICE` (NOT Golden/shipping approval).

## Progress, without redesign

The native 128×160 C03 pixel identity, 21 original semantic layers, 16-joint
hierarchy, approved mild asymmetrical front pose, and viewer-right hand behind
the brown satchel remain unchanged. No alternate character, outfit, side/back
view, expressions or prefab were created.

`scripts/Preview-C03IdleLoop.py` now interpolates the two existing limited
front idle-pose studies using a periodic cosine oscillator into **16 distinct
pixel-perfect frames**, while preserving the root pivot and inherited joint
rotations. It reuses `Preview-C03FrontRig.py` and the existing native source
and compositor. No raster translation of the complete character is substituted
for a joint animation.

A strictly declarative `idle_loop_proof` section in
`C03-front-joint-plan-DRAFT.json` specifies eight adjoining elbow, wrist,
knee and ankle bone pairs. For each sampled frame, the new script requires:

- Both segment layers contribute native pixels near the current world pivot,
  and those layer pixels touch within **1 native pixel** (10px local radius).
- The bag-body mask still covers the relevant hand's upper pixels, with a
  natural skin segment below it; no exposed hand above/through the satchel.
- Visible alpha remains inside the **128×160** canvas and no frame is empty.
- Pixel output changes between every pair of neighboring frames **including
  the loop seam**, while the root is held fixed.
- The 21 parts remain individually authored in the source, never flattened
  into the art registry.

`scripts/Export-C03UnityRigHandoff.py` produces a separate machine-readable
draft Unity authoring manifest mapping the same **16 joints and 21 parts**
to an absolute source pivot, per-bone parent-relative local position, and
each full-canvas sprite's center-relative local offset in Unity world units.
Sprite importer settings are recorded (Point filter, uncompressed, no mipmap,
128×160 and 32 PPU). Do **not** edit Unity `.meta` directly. This JSON is
an **Editor handoff**, not a created prefab or a live Unity validation.

## Reproduce

```bash
# stdlib-only native PNG frames, 1× and 4× nearest-neighbor contact sheet
python scripts/Preview-C03IdleLoop.py --output-dir artifacts/C03-idle

# optional: GIF is a convenience preview, not authoritative pixel evidence
python scripts/Preview-C03IdleLoop.py --output-dir artifacts/C03-idle --gif

# C03 full-canvas centered-sprite Unity authoring offsets + sort orders
python scripts/Export-C03UnityRigHandoff.py \
  --output artifacts/C03-idle/C03-unity-rig-authoring-manifest.json

python -m unittest discover -s tools/tests -p 'test_c03_idle_loop.py'
python -m unittest discover -s tools/tests -p 'test_c03_unity_rig_handoff.py'
```

The 1× and 4× sheet, 16 individual RGBA PNG frames, optional 4× GIF and
`C03-idle-loop-technical-evidence.json` are **draft evidence only** and
must stay outside `Art/Approved`. The GIF may use indexed colors, so inspect
the PNG sheet at native grid for authoritative color/pixel QA.

## Independently examined local motion sample

A standalone local pixel-probe rendered the exact extracted 21 native PNG
layers and current joint pivot coordinates with the same rotation/nearest
sampling equations. Its output was manually inspected as a 4× contact sheet.

| Measurement | Local proof |
| --- | ---: |
| Distinct loop samples | 16 |
| Per-frame rig hierarchy | 16 joints / 21 sprite layers |
| Adjoining seams checked | **8 × 16 = 128** |
| Joint seam failures | **0** |
| Adjacent changed native RGBA pixels (including wrap) | **min 82 / max 466** |
| Native visible alpha union bbox throughout loop | x=28..110, y=8..154 |
| Hand-under-bag masked overlap, all frames | 191 pixels |
| Hand visible below bag, all frames | 24 pixels |
| Hand wrongly visible above/through bag, all frames | 0 |

The same 16-frame probe is **not a substitute** for checking Unity output:
integer rasterized micro-rotations can still create pixel snapping and
cutout seams perceptible on a phone. The 16-frame poses show very subtle
idle motion, not verified walk/run/work animations. The new committed
repository test files have **not** been executed in a clean full repo checkout
or Unity Editor in this session. Report local probe measurements separately
from pending remote suite results.

## Required next production steps

1. Import the exact native layer PNGs in the live Unity Editor using proper
   sprite importer capability checks. Attach them to the 16 transforms.
2. Verify native C03 manifest offsets, parented rotation, bag/hand sort,
   visible seams, and movement in the Unity running game, not just a renderer.
3. Capture portrait mobile BeautyCell (native pixels + integer nearest
   scale) and owner motion-specific review before promoting the rig.
4. Commit the exact original high-resolution owner-selected PNG binary
   with its existing source SHA, and finish independent originality/IP checks.
5. Approved Asset Registry, actual game runtime and save/economy stay untouched.

**Owner model:** GPT-6. **Skills loaded:** startup-life-session-orchestrator,
vietnam-art-direction, startup-life-asset-quality-gate, 2d-pixel-perfect,
sprite-editor and unity-cli (documentation/read-only; no connected Editor).
**Tests:** 16-frame local independent pixel probe; tests committed, full clean
repo/Unity run pending. **Visual evidence:** 16-frame PNG contact sheet + GIF.
**Save impact:** None. **Known blocker:** Unity Editor not connected, and exact
opaque concept image has not yet been committed as Git binary.
