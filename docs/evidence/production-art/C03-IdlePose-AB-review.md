# C03 — Neutral Idle Pose A/B · Owner review

**Project:** Startup Life · **Asset:** Sáng Ngõ Nhỏ · **Task:** C03-NEUTRAL-IDLE-ANATOMY  
**Date:** 2026-10-16 · **Base:** C03-R5.1 front (source Git blob `5e8623de82e580aafc68e4ff7bc4ce5257013289`)  
**Owner R5.1 hand verdict:** `REVISE — HAND POSE REJECTED`. Other character direction remains unchanged.  
**Candidate states:** `A_AND_B_AWAITING_OWNER_REVIEW`; neither approved.

## Anatomy planning and silhouette first

Both drawings are first-party revisions on the *native 64×80 pixel grid*, **not** AI illustration filters. The head, face, hair, torso, clothing, pelvis, legs, shoes, skin colors, full palette and chibi scale are frozen from R5.1. The meaningful differences are limited to screen-right shoulder/upper arm, forearm, wrist, hand, and (for B) minimal hip-bag placement. Left arm remains the relaxed R5.1 contour.

| | A — RELAXED IDLE (preferred) | B — CASUAL IDLE |
| --- | --- | --- |
| Shoulder → elbow | Shoulder anchored near (42,41), elbow descends to right side near (49,56) | Shoulder anchored at same point, elbow stays slightly tucked near (47,54) |
| Elbow → wrist | Forearm hangs near right side to y≈64 | Low gentle bend, wrist near hip at y≈62 |
| Palm | Small closed relaxed contour at native y=63..68, screen-right of bag | Palm at y=61..67, screen-right of bag, closer to hip |
| Bag | R5.1 bag and continuous diagonal strap unchanged | Small bag shape tucked ~1–2 native columns toward hip |
| Strength | More readable limb separation and neutral idle | Compact silhouette; hand–bag negative space narrower |
| Risk | Right arm may feel somewhat away from body | Hand and bag could read as one mass at 1× |

## Source / compositing / reproducibility

- `Assets/StartupLife/Art/Source/Characters/golden-character-c03/C03-IdlePose-A.json`
- `Assets/StartupLife/Art/Source/Characters/golden-character-c03/C03-IdlePose-B.json`
- `scripts/Render-C03IdlePoseAB.py` — reuses the existing `Render-GoldenCharacterC03R1.py` native paint function and R2 sheet helpers; **no duplicate sprite renderer**.
- `scripts/Validate-C03IdlePoseAB.py` — verifies frozen identity, body data, native pixel and basic arm/wrist connectivity.
- Native source provenance is first-party authored replacement polygons/clusters only. Nothing traced from downloaded sprites, processed from concept images, filtered, antialiased or HD-downsampled. Independent commercial similarity/IP review **NOT RUN**.
- Former draft front sort put `hand_r` after `accessory_front`; both A/B reorder the existing 21 semantic names so `hand_r` is drawn **before** the bag overlay instead. The player no longer appears to grip the strap, and the bag is not hidden behind the palm. This is a draft paint order, **not yet validated Unity cutout sorting**.
- Original R5.1 remains immutable; both candidates are self-contained editable native JSON variants rather than R5.2/R5.3 experiments.

Run from repository root:

```bash
python scripts/Render-C03IdlePoseAB.py --output-dir artifacts/C03-IdlePose-AB
python scripts/Validate-C03IdlePoseAB.py
```

Outputs (draft review, never under Art/Approved):

```text
C03-IdlePose-A-1x.png
C03-IdlePose-A-8x.png
C03-IdlePose-B-1x.png
C03-IdlePose-B-8x.png
C03-IdlePose-Silhouette-AB.png
C03-IdlePose-Silhouette-AB-1x.png
C03-IdlePose-HandCrop-AB.png
C03-IdlePose-Comparison.png
C03-IdlePose-technical-evidence.json
layers-A/*.png (21 individual draft parts)
layers-B/*.png (21 individual draft parts)
```

Comparison ordering: **R5.1 / A / B** at the same 8× pixel scale. Silhouette, crop ordering: **A / B**. All raw native sprites use transparent RGBA and an exact pixel grid, with only integer nearest-neighbor enlargement.

## Technical evidence from native source (not canvas height)

| Check | A | B |
| --- | ---: | ---: |
| Native | 64×80 RGBA | 64×80 RGBA |
| Visible bbox xyxy exclusive | (14,7,57,79) | (14,7,56,79) |
| Occupied height | 72 px | 72 px |
| Head height | 32 px | 32 px |
| Actual height / head | 2.25 | 2.25 |
| Layer names / nonempty layers | 21/21 | 21/21 |
| Right-hand colored pixels in source part | 33 | 35 |
| Raw hand/bag part overlap at seam | 2 px | 2 px |
| Upper-arm↔forearm / forearm↔hand part overlap | yes / yes | yes / yes |
| Hand first native row | y=63 | y=61 |
| Palette / head / face / hair / legs / outfit unchanged | yes | yes |
| Binary alpha, exact 8× nearest-neighbor | PASS local | PASS local |

A 2-pixel overlap **in underlying part masks** is not a painted blob: the bag is drawn above that edge, and the relaxed hand itself is to the right. Still inspect the 1× silhouette and palm-to-bag space by eye. Layer overlap and alpha bounds do **not** prove convincing shoulder joints, real rig animation or a sufficiently expressive pose.

**Local QA:** Both native sources reproduce and the dedicated engine-free validator passes. No Unity Editor/import, sprite-pivot validation, actual joint animation, device BeautyCell, owner aesthetic approval or independent IP check performed. No gameplay, save, economy, Creator runtime or approved registry modifications.

## Owner visual gate

**C03 NEUTRAL IDLE — OWNER REVIEW REQUIRED**

Inspect actual **A 1× / B 1×** first, then native-derived **8×**, silhouette A/B, wrist/hand crop A/B and R5.1 comparison. Pick **A**, **B** or **REDESIGN**. Owner silence is not approval.

- If **A/B selected**: front idle *direction* only. Verify Unity import/pivots/real joint animation, device presentation and originality/IP independently before production approval.
- If **both fail**: stop the local pixel-polish loop, re-evaluate high-level arm/bag composition, and wait for owner direction. Do not generate R5.2/R5.3 or other views automatically.

**Review status:** `A_AND_B_AWAITING_OWNER_REVIEW`. Registry remains empty.

## Implementation closeout

**Owner model:** GPT-6 · **Skills:** startup-life-session-orchestrator, vietnam-art-direction, startup-life-asset-quality-gate, 2d-pixel-perfect, sprite-editor (reference-only; live Editor unavailable).  
**Tests:** native PNG bytes, strict NN scaling, 21 painted parts, unchanged face/identity, shoulder-elbow-wrist overlap, hands below chest, seam overlap threshold.  
**Save impact:** None. **Known limitations:** silhouette charm unresolved, Unity layer pivot/sorting and IP unfinished.

## FINAL OWNER SUPERSESSION — EXACT IMAGE SELECTED (2026-10-16)

**A/B selection CANCELLED.** Owner has selected the exact separately uploaded
1122×1402 visual master as C03's preferred appearance: **"thôi xài lại bản này đi"**.
This later decision supersedes the A/B review request and any default preference
for A. Both A/B JSONs stay in repository for historical provenance only; do
**not** automatically use or register either candidate.

Canonical decision: [`C03-OWNER-SELECTED-VISUAL-MASTER.md`](C03-OWNER-SELECTED-VISUAL-MASTER.md)
and [`C03-OWNER-SELECTION.json`](C03-OWNER-SELECTION.json).

**Current owner gate:** Visual identity is **selected**, but no approved
engine-ready image or cutout exists. The selected owner attachment remains
a high-resolution opaque-background concept until original bytes are
committed and a fidelity-preserving production asset passes independent gates.
No other native idle revisions or turnaround begin without renewed instruction.
