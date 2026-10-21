# C03 — Approved front visual → real production rig handoff

> **2026-10-21 supersession:** Owner chose a new 1122×1402 RGBA visual reference (SHA-256 `1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa`). The hash/size below record **historical 2026-10-16** approval only. The current `C03-LATEST-MINOR-POLISH-REFERENCE.json` supersedes this concept; the old mask/cutout, native draft and any screenshot evidence do NOT prove the new image has a Unity rig. The exact new binary is pending Git transfer and import.

**Date:** 2026-10-16  
**Owner decision:** **APPROVE** the latest front-facing C03 illustration with the near-side hand **behind and partly below** the brown satchel.  
**Visual reference:** `pixel_art_boy_with_brown_satchel.png`, SHA-256 `d2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e` (1171×1343 RGB).  
**Art status:** `OWNER_APPROVED_FRONT_VISUAL_REFERENCE`; **production asset status:** `NOT_YET_READY`.

The owner approves the latest visual appearance, not the old R5.1/idle A/B
procedural sprites, and not an independently cleared production-ready rig.
Preserve exactly the friendly face, short asymmetric fringe, softened neck,
tucked ears, green jacket, cream shirt, blue-gray trousers, broad sneakers and
crossbody bag. In front view the satchel front/flap is **in front of** the
hand near the viewer-right hip, with a small exposed skin segment **below**
the lower bag edge. Do not move the bag or put fingers on its front.

## Completed engineering work

- `Assets/StartupLife/Art/Source/Characters/golden-character-c03/C03-owner-front-rig-contract.json`: versioned authoring contract retaining the **canonical 21 rig parts**; front-only, no runtime registration.
- `scripts/Validate-C03OwnerRigHandoff.py`: validates exact semantic part IDs, bag-front draw order, native 64×80 RGBA alpha masks and local pixel overlap. The independent `masks/bag_body.png` excludes strap pixels; no globally hidden hand or substitute for drawing covered hand pixels. Reuses existing PNG decoder.
- `tools/tests/test_c03_owner_rig_handoff.py`: eight positive/negative engine-free checks for coverage, visible hand below the bag, mask containment, binary alpha, missing overlap and prohibited rig-order changes.
- `scripts/Stage-C03HandBagReference.py`: safely stages the **exact unchanged PNG bytes** from an input image or existing handoff ZIP, matching SHA-256, under draft reference art. It does not generate a new sprite, edit the visual identity or run Git itself.
- Existing `scripts/Verify-C03HandBagReference.py`: confirms original PNG hash/size once staged.

**Local checks:** 8/8 synthetic engine-free unit cases PASS. Exact concept PNG
hash verified locally, and the staging script was exercised against the ZIP
and original PNG in a temporary checkout; repeated staging is idempotent.
This is engineering evidence for the contract and file transfer, **not** an
actual 21-layer animation or a Golden art score.

## Binary handoff: still an open repository task

**The exact PNG is not yet committed in Git.** Current GitHub integration can
write text code/docs but has not transferred the 1.32 MB binary from the chat
container. The pre-existing ZIP has the exact PNG with expected path:

`Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-hand-behind-bag-reference.png`

On a local repo checkout:

```bash
python scripts/Stage-C03HandBagReference.py /path/to/C03-hand-behind-bag-upload-pack.zip
python scripts/Verify-C03HandBagReference.py
git add Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-hand-behind-bag-reference.png
git commit -m "art(c03): version owner-approved exact front visual reference"
```

After successful Git commit, update the machine-readable `image_committed`
and `exact_binary_committed_to_git` provenance flags from `false` to
`true` with the actual commit SHA. No fake binary-complete claim.

## Next implementation order — do not skip

1. Ensure the exact reference PNG is committed and hash-verified.
2. **Author** one true native-grid front view from the selected visual with
   controlled pixel clusters. The current 1171×1343 opaque concept cannot be
   downsampled and claimed as native 64×80 pixel art. Resolve required native
   fidelity/grid size with Art Director before changing PPU/canvas authority.
3. Separate 21 independently editable native RGBA layers with authored pivots
   and a local `masks/bag_body.png`. The bag layer must cover hand pixels but
   those hand pixels must remain available for articulated movement.
4. Run `python scripts/Validate-C03OwnerRigHandoff.py --layers-dir <folder>`.
   **Expected to be blocked** until the real native layers exist.
5. Only after visual + IP gates, test a reusable `CharacterVisualAssembler`
   setup in Unity with a real hand, elbow and bag-front sorting animation.
   Capture 1× and phone BeautyCell for owner final Golden review.

**Not done:** PNG committed to Git, native sprite, Unity prefab/pivots,
walk/idle and hand animation, device screenshots, independent rights review,
Golden registry. The existing Approved Registry must stay unchanged.

**Skills:** startup-life-session-orchestrator, vietnam-art-direction,
startup-life-asset-quality-gate, 2d-pixel-perfect, sprite-editor,
unity-cli (reference guidance; no connected local Editor).  
**Owner model:** GPT-6 · **Save/gameplay impact:** none.

## Next-stage cutout evidence (2026-10-16)

An optional offline `scripts/Prepare-C03OwnerConceptCutout.py` now generates
a **DRAFT** transparency test from the exact owner-approved RGB PNG. It
does not trace, resize, re-color or promote that concept into pixel art.
The expected mask has alpha bounding box `[330,156,817,1172]` at its
1171×1343 resolution and 327708 opaque pixels. On the art workstation,
a checker-background preview was inspected for gross silhouette integrity;
this is **not** a passed release-ready edge, native-pixel or Unity gate.

Local generated draft-alpha PNG SHA-256:
`c5fbf21d71954229ed7d765f6217b826e1f038580f02973a8809c1c319b4723b`.
The original owner PNG has not been edited or committed to Git.
See `C03-CONCEPT-ISOLATION-REVIEW.md`.

The draft C03 overlap validator also rejects a hand that visibly pokes
*above* the bag body or through an unsupported transparent gap; the hand
still exists underneath the covering layer. A new synthetic negative
test was added. Actual animation rig, joints and per-layer authored pixels
are still missing; do not claim the 9-case test suite proves them.
