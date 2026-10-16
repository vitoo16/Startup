# C03 — Idle V2.1 Inhale: one localized native contour revision

**Date:** 2026-10-17 · **Decision being addressed:** owner Art Director `REVISE INHALE`  
**Current status:** `INHALE_V21_DRAFT_AWAITING_OWNER_REVIEW`, **not** animation approval.  
**Approved appearance:** original C03 front direction / 128×160 native image, completely preserved.  
**No full loop. No Unity integration. No registry promotion.**

## Root cause fixed

Earlier `Render-C03IdleV2Keyposes.py` allowed Inhale to recolor only
already-opaque torso pixels; its global alpha-equality requirement prevented
actual upper-body shape change. This made 'breathing' look like flickering
highlights. This is an *authoring contract failure*, not a request to redraw
the character or relax all Golden art validations.

This revision keeps **Rest and Blink as their exact V2 layer keyposes**.
Only Inhale is replaced. `scripts/Render-C03IdleV21Inhale.py` imports the
earlier V2 renderer and the original native pixel/occlusion modules, then
applies a separately authored V2.1 patch list in
`Assets/StartupLife/Art/Source/Characters/golden-character-c03/C03-idle-v2.1-inhale-contour-DRAFT.json`.

### Allowed technical change

- **Left upper arm contour** — x=39–50, y=77–83, only at existing edge
  adjacency. Hand-authored 4 new outline pixels; repaint a small set of
  former dark edge pixels as jacket interior to establish the raised line.
- **Right upper arm contour** — x=85–94, y=79–83. Hand-authored 5 new
  outline pixels, similarly repair the interior contour.
- **Collar/chest jacket** — x=49–85, y=77–94. No alpha change allowed on
  this layer; only a handful of deliberate neckline/cream-shirt boundary
  pixel recolors to express a **localized 1-pixel garment expansion**, not
  an even puffed shirt or general relighting.
- **Frozen** — head, face, both hair layers, both hands, forearms, legs,
  feet, pelvis, torso core, satchel body/strap and bag backing. One
  partially occluded hand remains authored below/behind the satchel.

The new renderer allows `add_opaque` or `erase_opaque` **only on the
two upper-arm exterior contours**, each operation within one native pixel
of the respective original opaque/clear neighboring region. Recoloring is
restricted to the above named semantic parts/ROIs. Both immutable regions
and numerical budgets are hard-checked in code, so editing a JSON limit
alone **cannot authorize broader alpha changes**. The old V2 renderer and
tests remain as historical evidence.

## Measured first V2.1 source-layer experiment

| Check | Observed local result |
| --- | ---: |
| Native canvas | 128×160, binary RGBA |
| Rest vs frozen base | identical |
| Blink | exact V2 keypose preserved |
| Visible composited pixel changes (Inhale vs Rest) | **21** |
| New alpha pixels in two underlying arm layers | **9** (4 left, 5 right) |
| Visible silhouette alpha changes | **7** |
| Removed alpha | **0** |
| Visible change bbox, xyxy exclusive | **[41,78,92,94]** |
| Satchel covers hand | **191 original pixels** |
| Hand exposed beneath satchel | **24 original pixels** |
| Full timed animation | **NOT CREATED** |

A local independent Pillow alpha-composite was used to inspect and compare
the original layer PNGs, V2 Rest/Blink and one new V2.1 Inhale. The renderer
uses only the existing first-party row pixel PNG writer and nearest integer
scale. **Numbers measure source correctness, not motion aesthetics**.
Remote GitHub CI and Unity tests are **not verified** in this session.

## Required visual review — before playback

Supply one `Rest / Inhale V2.1` two-up at true source scale **1×**,
one 4× exact nearest-neighbor two-up, and a 10× shoulder/chest detail.
A separate QA copy may outline the **seven** newly visible silhouette
pixels. The unannotated sprite remains the authoritative source.

Reviewer must look for a real volume change rather than a tiny
shoulder twitch or jacket seam popping. If this 1-pixel trial still reads
as a visual glitch, **REJECT INHALE** and prefer a stable-body idle
with independent fast blink/contextual microgestures. Do not lower test
thresholds or create a loop to hide the problem.

**Decision requested:** `APPROVE_INHALE_KEYPOSE` or `REVISE_INHALE`.

If approved as a keypose only: next separately design short blink timing
and irregular independent schedules; breathing is not to force one blink
per inhale. Neither animation is approved without actual-duration
playback, Unity joint checks and owner motion review.

## Reproduction

```bash
python scripts/Render-C03IdleV21Inhale.py --output-dir artifacts/C03-idle-v21
python -m unittest discover -s tools/tests -p test_c03_idle_v21_inhale.py
```

Review output files include `C03-idle-v2.1-rest-inhale-side-by-side-1x.png`,
`C03-idle-v2.1-rest-inhale-side-by-side-4x.png`,
`C03-idle-v2.1-shoulder-chest-10x.png`, full transparent 1×
Rest/Inhale/Blink PNGs, and an evidence JSON.

**Owner model:** GPT-6 · **Skills:** startup-life-session-orchestrator,
vietnam-art-direction, startup-life-asset-quality-gate, 2d-pixel-perfect,
sprite-editor (read only; live Unity unavailable). **Save impact:** none.
**Known blockers:** actual visual owner verdict, original concept PNG
binary still pending in Git, timed motion, Unity/device/IP/Golden gates.
