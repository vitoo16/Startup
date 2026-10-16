> **SUPERSEDED FOR INHALE — 2026-10-17:** Owner requested `REVISE INHALE`. The shading-only Inhale described here is an
> archived, **not approved** study. Rest and closed-eye Blink directions
> stay as reviewed. The new ONE-pose trial with local 1-pixel shoulder
> contour authoring is documented in
> [C03-IDLE-V21-INHALE-REVIEW.md](C03-IDLE-V21-INHALE-REVIEW.md).
> No full loop or runtime approval follows from either document.

# C03 — Idle V2: first native keyposes (owner visual review)

**Date:** 2026-10-17  
**Character:** Sáng Ngõ Nhỏ / C03 · FRONT only · Native 128×160 RGBA  
**Owner verdict:** C03 front visual APPROVED_DIRECTION; Idle V1 cosine animation **REJECTED**.  
**V2 state:** `KEYPOSES_DRAFT_AWAITING_OWNER_VISUAL_REVIEW`. No animation approval or runtime integration.

## This is NOT the rejected V1 motion with smaller angles

V1 used continuous layer rotations around pivots, creating wobbling hair, snapping edges and unnatural head movement. V2 is a different mechanism: *three independent authored native pixel keyposes* built as small semantic-layer pixel edits to the frozen C03 native artwork. No rotation, frame interpolation, image resampling, whole-body translation, animator creation or generated Unity clip occurs here.

This first controlled visual study contains exactly:

- **Rest**: original native front, byte-for-byte unchanged
- **Inhale**: manually adjusted olive-jacket/shoulder/chest brightness clusters while retaining their original pixels and contour; 27 visible pixels differ. **Caution: this is only a shading keypose, not yet confirmed convincing physical breathing**
- **Blink**: short native eyelid shapes on the separate `face` layer; 35 visible pixels differ, eyebrows/smile/face silhouette preserved

No keypose changes hair mass, head skin geometry, shoe placement, knees, feet, bag, strap, hand or pixel canvas. This passes the *lock invariants* but does not prove the animation will feel alive.

## Exact source (versioned on main)

- `Assets/StartupLife/Art/Source/Characters/golden-character-c03/C03-idle-v2-keyposes-DRAFT.json`
- `scripts/Render-C03IdleV2Keyposes.py`
- `tools/tests/test_c03_idle_v2_keyposes.py`
- Frozen 21-layer native source remains `C03-native-front-source.json`, packet SHA-256 `1b04022f5f828ba95374cc3e0e875df9597ea89454d3ebdd5730aa46a3254c1f`.
- Motion art direction: `C03-idle-v2-art-direction.json`.
- V1 `Preview-C03IdleLoop.py` still fails closed by default; explicitly archived only.

Recreate keyposes in a complete repository checkout with:

```bash
python scripts/Render-C03IdleV2Keyposes.py --output-dir artifacts/C03-idle-v2
python -m unittest discover -s tools/tests -p 'test_c03_idle_v2_keyposes.py'
```

Native sprite sources, point-nearest 4× review and the evidence JSON are generated into the given output folder, **not** into the Approved Registry. The prior native authoring decoder, semantic painter and bag-occlusion validator are reused; no duplicated character engine.

## Evidence from local independent source-layer inspection

| Measured invariant | Result |
| --- | ---: |
| Keyposes | 3 (rest, inhale, blink) |
| Native canvas | 128×160 |
| Rest vs locked source | 0 changed pixels |
| Inhale vs rest | 27 changed visible pixels, all y=85–93 |
| Blink vs rest | 35 changed visible pixels, all y=47–51 |
| Hair / head shape / feet pixels | Unchanged |
| Composited silhouette/alpha | Unchanged |
| Hand pixels behind bag front | 191 for each keypose |
| Hand pixels still showing beneath bag | 24 for each keypose |
| Hand wrongly poking above/through bag | 0 |
| Discrete keyposes reviewed at native 1× and integer 4× | Local visual proof generated |
| Full timed Idle animation | **NOT CREATED** |
| Unity + device + independent IP tests | **NOT RUN** |

Local pixel inspection was executed against the exact authored native 21-part sources contained in the C03 native reference pack. The new GitHub renderer/test code has been committed, but a clean repo CI/Unity run has **not** been verified and must not be called PASS.

## Human art gate — required before more animation

Present the owner **Rest → Inhale → Blink** at actual 1× and exact nearest 4×, plus face/chest closeups. The owner must decide `APPROVE_KEYPOSE_DIRECTION` or `REVISE_KEYPOSES`. **A breathing pose that reads merely as a highlight flicker should be revised, not automatically repeated into a loop**.

Only after that gate: design proper timing and still moments from distinct authored keyposes, inspect genuine playback, then run Unity/import/pivot/device testing. Keep cosmetics, extra views, full body walk cycles and the Approved Registry blocked until relevant independent gates.

**Owner model:** GPT-6. **Skills:** startup-life-session-orchestrator, vietnam-art-direction, startup-life-asset-quality-gate, 2d-pixel-perfect, sprite-editor (reference-only, no live Unity). **Tests:** local native pixel-diff + alpha + bag-over-hand; repository test added, full CI pending. **Save impact:** none. **Known limitations:** inhale is currently shading-only and may lack believable chest volume, not a finished animation.
