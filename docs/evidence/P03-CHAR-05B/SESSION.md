# P03-CHAR-05B — Native C03 pixel art source draft

**Date:** 2026-10-16. **Owner:** GPT-6 technical/art implementation.
**Baseline:** `d7c80909f8e582ef3084dcf3c4d2b93fab346739`. All art is **DRAFT_NOT_APPROVED**. Owner accepted
the anti-AI-slop DIRECTION but has NOT approved these specific sprite pixels.

## Deliverable
Created editable deterministic first-party pure Python native **64×80 RGBA**
source for three separate adult Vietnamese daily-life silhouettes, with four
independently authored orientations each (12 turnarounds). Each of the three
directions has three controlled skin/hair palette variations (9 front options)
with separately authored head, hair back/front, face, upper/lower bilateral
limbs, hands/feet and workwear/prop overlays (**21 semantic cutout slots**).
Native visual review uses 4× nearest-neighbor PNG; the source is 64×80 at 1×,
not a smooth render that was blurred/pixelated afterwards. Eyes are minimalist
two-pixel clusters without shiny anime highlights or gradients. Colors are
limited flat clusters. C01 and C02 remain untouched/rejected.

- **Sáng Ngõ Nhỏ:** short uneven bangs, airy button shirt, commuter sling
- **Tan Ca:** side sweep and longer back hair, soft cardigan, canvas tote
- **Quán Đầu Phố:** clustered curls, boxy overshirt, simple backpack

## Fail-closed boundaries
The active style contract's preliminary **128×160** character canvas differs
from this deliberately half-resolution 64×80 exploratory source, so importer
scale/pivots and readability are **UNVERIFIED** and may require manual revision.
The approximate head-to-height proportions need artist verification on exact
silhouettes; this draft is not yet a rigged animation. Side/back geometry is
hand-selected by view; no mirrored front image masquerades as turnaround.

Registry `Assets/StartupLife/Art/Approved/registry.json` remains untouched and
empty. No scene prefab, gameplay stats, gender competence, save schema, Unity
Animator or shipping sprite is changed. Human owner art review and independent
similarity/cultural/rights signoff are mandatory before approval.

## QA
**Skills used:** startup-life-session-orchestrator, vietnam-art-direction,
startup-life-asset-quality-gate, 2d-pixel-perfect, sprite-editor.
**Acceptance:** render source C03 1×, visually clean anti-slop, distinct
monochrome silhouettes, four views, original palette, 21 rig layers, no
external assets, DRAFT-only provenance; actual 15/16 Golden score not claimed.
**Tests:** new Python renderer validator + six negative contract tests, existing
engine-free CI (verdict separately verified). Unity and devices NOT RUN.
**Visual evidence:** engine-free PNG artifact (front variants and turnaround
sheet), plus local inspected draft preview; owner decision still REQUIRED.
**Save impact:** NONE.
**Known limitations:** Still a first pixel draft rather than polished final
art; need silhouette/face differences reviewed at 1×, refine motion,
scene-context Vietnamese authenticity and authentic rig. Not production.
**Files changed:** source renderer, JSON manifest/.meta, provenance, C03
validator/mutation tests, CI, this evidence.
