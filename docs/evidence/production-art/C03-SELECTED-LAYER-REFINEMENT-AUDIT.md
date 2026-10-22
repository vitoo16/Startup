# C03 selected-source 21-layer cutout — motion and ownership audit

**Date:** 2026-10-22
**Canonical owner-selected C03:** `C03-hand-behind-bag-reference.png`,
1122×1402 RGBA, Git LFS SHA-256
`1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa`.

**Verdict:** **REVISE ART / NOT ANIMATABLE / NOT SHIPPING APPROVED.**
This does not rescind the owner's approval of the C03 character's
appearance. The source image itself remains authoritative; the split layers
are incomplete production source.

## Delivered and independently reproducible

- Original Git LFS 21-layer cutout + `masks/bag_body.png` exists under
  `docs/evidence/production-art/drafts/c03-owner-selected-layer-study/`.
  The 16-joint topology guide and original 21 semantic ordering match
  existing `Generated-V3-Quarantine` interface. The old V3 assets remain
  untouched and have NOT been promoted.
- The read-only joint stress script
  `scripts/Preview-C03OwnerCutoutArticulation.py` renders bind pose,
  left/right arm bend and two alternating leg-bend diagnostic poses from the
  actual tracked PNGs. Run
  [39744890834](https://github.com/vitoo16/Startup/actions/runs/39744890834)
  passed five joint-affine unit tests plus SHA-verified source, and produced
  artifact `c03-selected-cutout-pose-stress-NOT-APPROVED`
  (ID `12411266058`). The root does not slide.
- Found an actual ownership defect in the original cutout: **14,025**
  source-visible pixels outside the authored torso polygon were attributed
  to `torso` as a fail-safe fallback. The original `torso` RGBA bbox was
  **[308,481,790,1256]**, incorrectly reaching into the shoes.
- Implemented
  `scripts/Refine-C03SelectedLayerOwnership.py`: 21 layers are read
  from the checked-in draft and all pixel assignments are derived from
  exact current owner-image RGB/alpha, with deterministic nearest existing
  semantic-mask assignment. **14,025 / 14,025** erroneous fallback pixels
  were redistributed without any recoloring or geometry changes to the
  **composite**. The corrected `torso` bbox is **[438,661,790,947]**.
  The bag-body mask and the 16-joint hierarchy are unchanged.
- The refinement's read-only
  [Actions run 39745979598](https://github.com/vitoo16/Startup/actions/runs/39745979598)
  passed per-PNG SHA-256 validation (21 RGBA + 1 mask), exact bind-pose
  pixel-composite identity and positive error-preservation assertions.
  Download artifact `c03-source-owner-layer-refinement-NOT-UNITY-READY`
  (ID `12410733807`) for all revised PNGs, receipt and paired
  arm/walk diagnostic previews. The improved files are **review
  artifacts**, NOT replacements for the V1 source PNGs on main.

## Visual defects blocking actual animation

| Parts / joints | What the real diagnostic shows | Required next edit |
| --- | --- | --- |
| `upper_arm_l`, `forearm_l`, `hand_l` / shoulder–elbow–wrist | Thin bright break at left jacket cuff in arm-bend pose; blend is imperfect even after torso reassignment | Paint genuine sleeve overlap and concealed hand/forearm geometry, not a fake slide |
| `upper_arm_r`, `forearm_r`, `hand_r`, `accessory_front` / shoulder–wrist | Brown satchel-region pixels remain attached to the near hand in an authored cutout | Separate skin, cuff, bag body, strap; paint hand overlap behind bag and validate bag-only mask |
| `lower_leg_l/r`, `foot_l/r` / knees and ankles | Disconnected outline and thin gaps at shoe seams appear under diagnostic walk rotations | Author hidden sock/ankle/cuff pixels and revise foot-to-leg attachment |
| `head`, `face`, `hair_front/back` / neck | Layer names exist, but the flattened illustration's hair/face overlap is not independently painted for free rotation | Rework hidden fringe/ear/neck contour while preserving selected hair silhouette and expression |
| `outfit_torso`, `torso` / spine | Static matches selected shirt/jacket color but source can produce clipped jacket/collar edges on body articulation | Original native transparent jacket/shirt mask cleanup |

**Visual reality:** The new pipeline is still a high-resolution
1122×1402 opaque-source cutout. It is **not** manually authored native
128×160/32 PPU pixel art; no hidden joints have been reconstructed, and
the draft's 16 pivots are estimates. Static pixel identity to the selected
picture does not prove correct articulation, cleanliness at mobile scale,
or an accepted animation.

## Production acceptance gates not met

- Artist-authored C03-matched **native** 21 separate RGBA layer sources at
  the agreed grid; separately painted occluded anatomy, including the
  hand behind the satchel. **OPEN**
- Current-image 16-pivot rig and seam-free real multi-joint idle/walk/work
  motion, tested in Unity Editor. **OPEN**
- Portrait device BeautyCell, independent IP/originality review, explicit
  owner approval and approved registry. **OPEN**

Do not mutate `Art/Approved`, `FirstPlayable.unity`, save schema or
old `Generated-V3-Quarantine` based on these proofs.

**Technical owner:** GPT-6.
**Skills used:** startup-life-session-orchestrator, unity-cli,
sprite-editor, 2d-pixel-perfect, vietnam-art-direction,
startup-life-asset-quality-gate.
**Tests:** workflow 39744890834 PASS, 39745979598 PASS.
**Visual evidence:** real PNG diagnostic contact sheets and SHA-pinned
artifact files. **Unity/device evidence:** NOT RUN.
**Save impact:** NONE.
**Known limitations:** no native repaint, no legitimate production
animation, no human IP or owner approval for this split.
