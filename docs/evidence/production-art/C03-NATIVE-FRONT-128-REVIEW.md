# C03 — First authored native 128×160 front reconstruction

**Task:** C03 pixel-production continuation · **Date:** 2026-10-16  
**Owner visual reference:** `pixel_art_boy_with_brown_satchel.png` — exact SHA-256
`d2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e`.  
**Existing owner verdict:** selected / approved *high-resolution concept direction*.  
**Native 128×160 raster:** `DRAFT_AWAITING_OWNER_NATIVE_VISUAL_REVIEW`, not production Golden approval.

## Progress made

A **first-party, manually authored native-pixel 128×160 FRONT** reconstruction
now exists. Native pixel colors are explicitly defined on integer grid rows.
The original 1171×1343 opaque illustration is **not automatically downscaled,
filtered or traced into a runtime sprite**. Twenty-one separately painted
semantic RGBA parts match the existing shared character rig naming, including
an intact bag-facing hand behind the separate `accessory_front` bag body/flap.
The bag-body mask is authored separately from the strap.

The exact source is committed to Git as:
- `Assets/StartupLife/Art/Source/Characters/golden-character-c03/C03-native-front-source.json`
- `Assets/StartupLife/Art/Source/Characters/golden-character-c03/C03-native-front-pixel-runs.lzma.b64`
- `scripts/Render-C03OwnerNativeFront.py`

The row data uses a **lossless, SHA-pinned transport format** (stdlib LZMA).
It is not an image-to-pixel conversion method: decompressing produces the
independently authored per-layer `[y,x,length,color_role]` horizontal pixel
runs, with a separate bag mask. The renderer can export this human-readable
source with `--expanded-source-output`. This approach avoids storing
unreviewed PNGs inside `Art/Approved` while keeping the draft reconstructible
in Git, independent of chat file availability.

**Validated source invariants:** packet SHA-256
`1b04022f5f828ba95374cc3e0e875df9597ea89454d3ebdd5730aa46a3254c1f`;
Git blob SHA `49faa526c8c6fcebd47c527ec3961af6249b612e`
matches byte-for-byte local packet. 2,144 colored pixel runs and 20
bag-mask runs, covering 21 native part layers. All alpha values are
0 or 255. All 21 parts have visible native pixels.

## Measured from authored native pixels

| Criterion | Actual result |
| --- | ---: |
| Canvas | 128×160 RGBA |
| Sprite occupied bbox (xyxy, end exclusive) | [29,8,111,155] |
| Visible sprite height | 147 native px |
| Crown-to-chin inspected landmark | 66 native px |
| Height in heads | **2.227** |
| Head share of real visible height | **44.90%** |
| Covered hand pixels under bag body | **191** |
| Hand pixels visible below bag | **24** |
| Hand pixels exposed above or through bag | **0** |

The ratio lies in the required 2.2–2.4 heads and 42–45% ranges. The
chin landmark is visually defined on this native raster, not calculated
from the 160 px canvas. This measurement does not grant aesthetic approval.

## Reproduce on repository checkout

```bash
python scripts/Render-C03OwnerNativeFront.py \
  --output-dir artifacts/C03-native-front \
  --expanded-source-output artifacts/C03-native-front/expanded-native-rows.json

python scripts/Validate-C03OwnerRigHandoff.py \
  --layers-dir artifacts/C03-native-front/layers

python -m unittest discover -s tools/tests -p 'test_c03_native_front_renderer.py'
```

Emitted draft outputs: transparent native sprite `C03-native-front-128x160-DRAFT.png`,
nearest-neighbor `C03-native-front-8x-DRAFT.png`, silhouette at 8×,
21 individual PNGs, `layers/masks/bag_body.png` and a technical
evidence JSON. The images are evidence outputs only, not approved imports.

**Local draft QA:** 6 native-raster test cases PASS including actual pixel
proportions, source roundtrip, RGBA alpha and exact NN scaling. The
lossless packed source was independently decoded and compared to the
full authored row JSON. Actual bag/hand overlap pixel counts above
were measured. Repository test suite additions exist, but no live
Unity Editor or device test has run in this session.

## Visual differences requiring human review

The native version retains dark asymmetric short hair, friendly face,
olive jacket, cream shirt, jeans, chunky footwear and brown messenger
bag in front of the hand. **It is not yet identical to the owner's concept
at 1×.** The face is simpler, the hair/garment detail rhythm is reduced,
and the shoulder and stance could still be judged too stiff. Owner must
inspect the actual native preview against the already selected master:
**APPROVE_NATIVE_DIRECTION / REVISE_NATIVE / REJECT_NATIVE**. Do not infer
native approval from the existing concept approval.

## Still blocked

1. Exact original opaque concept PNG binary has still not been committed to
   Git. Its provenance hash is recorded, but reference art cannot be
   considered fully archived until checked in.
2. Native authored layer PNGs are reproducible, but rig pivots, independent
   articulated hand/elbow animation and Unity sorting are NOT verified.
3. No side/back variants, cosmetics, animator, runtime prefab integration,
   independent commercial IP clearance, mobile BeautyCell or owner Golden
   release approval.
4. `Assets/StartupLife/Art/Approved/registry.json` remains untouched.
   No changes to gameplay/economy/save/runtime character selection.

**Owner model:** GPT-6. **Skills:** startup-life-session-orchestrator,
vietnam-art-direction, startup-life-asset-quality-gate, 2d-pixel-perfect,
sprite-editor (reference only; no live Unity Editor). **Save impact:** None.

## Owner decision and rig continuation — 2026-10-16

**Owner verdict: `APPROVE_NATIVE_DIRECTION`.** The owner's "ok rồi" directly
answered the native-direction visual gate for the 128×160 C03 front
illustration. The previously stated `AWAITING_OWNER_NATIVE_VISUAL_REVIEW`
in the historical section above is now **CLOSED** for visual direction only.
The chosen native character should not undergo further face/hair/outfit redesign
unless the owner explicitly reopens it.

The single FRONT sprite source and palette were **not** repainted. No other
view, expression set or cosmetic variant has been started. Work resumed only
on the authored rig handoff: `C03-front-joint-plan-DRAFT.json`
maps 16 named integer-pixel joints to all 21 existing semantic RGBA parts,
including separate shoulder-elbow-wrist and hip-knee-ankle chains.
The satchel `accessory_front` remains attached to the torso; `hand_r`
stays attached to its wrist and is composited before the bag in the
front-view overlap.

`scripts/Preview-C03FrontRig.py` is a **temporary engine-free cutout
articulation proof**. It reads the same SHA-pinned lossless native pixel
source and performs inverse nearest-neighbor sampling of each
individual sprite **around its own parent-inherited joint**, not sliding
the entire character. Three poses are exported: bind, small left idle
and small right idle; a local experimental render measured 191 hand pixels
covered, 24 pixels below the bag and 0 misexposed on the bag for each
sample. The visual on a cream board had only subtle motion; no claim is
made that this is production animation, suitable timing, or fully
joint-clean. The preview script has a new unit-test suite and requires a
reproducible run from the Git checkout before treating the report as CI
evidence.

Reference local diagnostic image SHA-256:
`bd7680620b3d74208a16639e934aa35b0df68ddcac1110a7b083f90e4e2fd8b7`
(1272×540, integer 3× contact sheet, experimental rig visualization).
These PNG bytes are **not committed to Git**; regenerate review samples
from the source. No Unity Editor was connected or evaluated, so pivots
were *authored in JSON*, not imported or verified by the Unity Sprite
Editor. Do not edit Unity `.meta` files manually.

**Next blocking step:** in a real Unity 6.3 Editor, verify importer PPU,
point filter, joint/pivot offsets, layering, elbow/wrist/hip movement,
occlusion under animation and device BeautyCell. Keep the entire
production rig unregistered until originality/IP and final owner Golden
art approval. Source concept PNG is still not versioned as exact binary.
