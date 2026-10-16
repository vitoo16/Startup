# C03 Idle — owner REJECTED v1, fresh native-keypose workflow required

**Date:** 2026-10-17 · **Art Director verdict:** **REJECT ANIMATION**
**Character:** C03 / Sáng Ngõ Nhỏ · **Accepted base:** front native 128×160 appearance direction remains unchanged.
**Supersedes:** all prior language treating `C03-idle-loop-debug.gif` or `Preview-C03IdleLoop.py` as a usable Idle animation.

## What failed visually

The 16-frame idle study oscillates joint rotations with a single cosine wave,
while independently inverse-sampling the full 128×160 contents of each layer
through nearest pixel centers. The result is conspicuous hair/face contour
crawling and near-uniform left-right head wobble, not purposeful breathing.
Small integer-pixel shifts also introduce transient changes at the collar,
arms and lower body despite mathematical joint adjacency being satisfied.
The older "all neighboring frames must be different" gate **incorrectly
rewarded jitter instead of held beats**. The former 128/128 seam result and
191/24 bag-hand overlap result are structural checks, NOT aesthetic PASS.

Review of the exact 16 local rendered frames from the same authored
21-layer native source (pixel-for-pixel probe):

| Objective region delta vs frame 0 | Worst observed |
| --- | ---: |
| Head/hair/face y=8..75 | **1,317 changed pixels** |
| Upper body y=76..114 | **130 changed pixels** |
| Planted shoes y=140..159 | **0 changed pixels** |
| Head/body change normalized per ROI scanline | **5.81×** |

The large head-to-body imbalance corroborates the obvious wobble.
It is diagnostic evidence for this rejected v1, **not a universal animation
quality standard**.

## Immediate containment — done

- `C03-front-joint-plan-DRAFT.json` records owner verdict `REJECTED`,
  `legacy_idle_loop_allowed_for_production=false`, and links to v2.
- `Preview-C03IdleLoop.py` now **refuses to produce the legacy frames by
  default**. `--archive-rejected` is a deliberate, forensic reproduction
  switch, never an authorization for production.
- The legacy renderer's evidence status now reads
  `LEGACY_C03_IDLE_REJECTED_OWNER_VISUAL_ARCHIVE_ONLY` and its production
  permission is **false**. The historical test retains the old structural
  assertions only in explicit archive mode.
- No production prefab, animation clip, character selection or registry
  must depend on the rejected loop.

## Replacement art direction: v2 keyposes

The next attempt is a **separately authored native sprite-layer keypose**
workflow, *not* a parameter adjustment of the cosine algorithm.
Contract: `C03-idle-v2-art-direction.json`.

The main concept has three short authored beats rather than constant
oscillation: ① grounded rest, ② gentle inhale/exhale with at most
single-pixel intentionally redrawn collar/jacket volume, ③ one quick eye
blink; add **held poses** and irregular natural timing. Root and both
planted feet remain fixed for this front idle, with no knee/ankle wobble.
The brown satchel follows the torso and must keep covering the hand.
A blink edits eyelid/eye clusters, not the position or rotation of the
whole head. Deliberately repair any exposed joint seam at each native pose.

**Review workflow:** First show owner a small sheet of original bind pose,
inhale keypose and closed-blink keypose at native 1× and 4× nearest-neighbor.
Only after those keyposes are accepted, interpolate timing using deliberate
sprite holds/durations and present a played loop for motion review.
A static image direction approval does NOT automatically approve its motion.

## New engine-free motion preflight (NOT artistic approval)

Run `python scripts/Review-C03NativeMotion.py --frames-dir artifacts/.../native-frames`.
It evaluates native 128×160 RGBA frames and checks: no 1× alpha antialias,
planted shoe pixel stability, head/torso contour-change budgets, clipping,
presence of deliberate art changes. **Held adjacent frames are permitted**.
The guard must diagnose the archived 16-frame v1 as `REJECTED_MOTION_PREFLIGHT`;
new keyposes still need owner review, layer overlap checking and Unity play.

The initial v1 measured max 1,317 changed head pixels vs a v2 preflight
guard of 128, and 5.81× head/body normalized-row motion vs guard 2.0×.
These budgets are intentionally conservative for this chosen *subtle* Idle,
not a relaxation of older Golden QA and never a substitute for visual
evaluation. Do not modify values to force a PASS.

**Remaining blockers:** manually authored v2 keyframe layers, independent
motion reviewer, Unity real animation and pivots, device BeautyCell,
independent IP, exact approved source binary in Git and Golden registry.

**Owner model:** GPT-6. **Skills used:** startup-life-session-orchestrator,
vietnam-art-direction, startup-life-asset-quality-gate, 2d-pixel-perfect,
sprite-editor and unity-cli (instruction only; no connected live Editor).
**Tests:** existing 16-frame local forensic measurement; new v2 review unit
tests authored but require checkout/CI execution. **Visual evidence:**
rejected contact sheet `C03-idle-loop-contact-4x.png` (historical, not a
recommended animation). **Save impact:** none.
