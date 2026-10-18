> **OWNER DECISION — 2026-10-18: `APPROVE_MOTION_DIRECTION`.** The user
> approved the stable-body C03 Idle V3 blink direction and its displayed
> review playback, authorizing transition to **Unity integration/QA**.
> This does **not** approve release sprites, Unity playback, mobile BeautyCell,
> independent IP clearance or Golden Asset registration. Status:
> `IDLE_V3_MOTION_DIRECTION_APPROVED_UNITY_QA_PENDING`.
> The initial review and the original pending request below are preserved
> as historical pre-decision evidence.

# C03 — Idle V3 Stable Body & Natural Blink · owner motion review

**Directive:** `C03-IDLE-V3-STABLE-BODY-BLINK`  
**Art Director review of V2.1 Inhale:** `REJECT_INHALE_MOTION` (**14/30**; no legible natural inhale at 1×).  
**Repo baseline:** `a162e5ed94d6e84bc6e5705a47daea1fa4b458b1`.  
**Current deliverable:** `IDLE_V3_BLINK_PLAYBACK_AWAITING_OWNER_REVIEW`.

## Decision and scope

Do not continue V1 cosine, V2 shading-only Inhale or V2.1 shoulder-contour Inhale. They remain historical rejected evidence. Preserve the selected C03 **native front 128×160** visual identity and entire body/props/21 semantic layers. **Only eyelid/iris pixels inside the face-layer eye ROI change.** No head, hair, brows, smile, skin/cheeks, shoulder, chest, hip, foot, hand or bag motion. No additional gesture, camera angle, costume or gameplay change.

**Frozen source** `C03-native-front-source.json` packet SHA-256:
`1b04022f5f828ba95374cc3e0e875df9597ea89454d3ebdd5730aa46a3254c1f`.

## New source and implementation

- `docs/evidence/production-art/source/C03-Idle-V3-stable-blink-DRAFT.json`: versioned pose source, half-eye native pixel removals, closed-eye V2 inheritance, body locks, eye ROI and timing configuration.
- `scripts/Render-C03IdleV3.py`: reuses existing `Render-C03OwnerNativeFront`, `Render-C03IdleV2Keyposes` **only for approved closed-eye keypose**, and `Validate-C03OwnerRigHandoff` for bag/hand overlap. It never imports V2/V2.1 inhale or V1 motion.
- `tools/tests/test_c03_idle_v3.py`: source/alpha/ROI/locked-part, rejection and time-coded GIF checks.
- Existing V2/V2.1 code/evidence retained unchanged except clear supersession notes in production documentation.

### Eyes and native geometry

Three visible states:

1. `open`: pixel-identical original Rest, SHA-256 (raw RGBA)
`efda42e83eb039d31e6ff3cd25dbf68db8d4f65cdc34beab842ee90be96ebe1d`.
2. `half`: deliberately authored 19 iris-pixel removals in **x=48..82, y=47..51** only, preserving underlying head skin, brows/cheeks/smile, 21-layer rig and both feet.
3. `closed`: inherits byte-identical V2 Closed keypose, exactly 35 changed visible pixels versus Rest; SHA-256 (raw RGBA)
`1faf1b8c78e078dd3aed386e22a7932eea5b19d3836bebcb260ae62a3ebd853b`.

Half-Closed is a **review experiment**: if it looks flickery or unflattering at the actual game scale, simplify to Open → Closed → Open. Do not increase animated frames to artificially meet complexity targets.

Native canvas is **128×160 RGBA**, alpha only 0/255, pixel-perfect PNG, silhouette occupied bbox **[29,8,111,155]** unchanged for all poses. No entire-object translation, skeletal rotation, AA, HD resizing or global palette edits.

For each pose, bag covers **191 authored hand pixels**, with **24** visible below the bag. All 20 other layers remain byte-identical.

## True-timed review playback

A **nine-segment GIF** was generated and reopened to verify exact frame durations (millisecond values):

`[3000,50,60,70,5000,50,60,70,4000]`

Total **12,360 ms**, including two independent quick blinks:
- Half-closed approach **50ms**.
- Fully closed **60ms**.
- Reopening half **70ms**.
- Total **180ms per blink**.

The three uneven idle holds (3/5/4 seconds) are an **offline visual evaluation sample only**, **not a hard-coded production schedule**. Future scheduler parameters are independently configurable, 2.8–6.5s as a *proposal*, and must not affect deterministic simulation/gameplay.

Do not infer Unity gameplay-scale QA from `C03-Idle-V3-Gameplay-Scale-MOCK.png`: it is a marked **offline 360×640 portrait composition**, not an actual Unity/phone BeautyCell.

## Output filenames

- `C03-Idle-V3-open-1x.png`
- `C03-Idle-V3-half-1x.png`
- `C03-Idle-V3-closed-1x.png`
- `C03-Idle-V3-Blink-Sheet-4x.png`
- `C03-Idle-V3-Eyes-10x.png`
- `C03-Idle-V3-Timed-Playback.gif`
- `C03-Idle-V3-Gameplay-Scale-MOCK.png`
- `C03-Idle-V3-QA.json`

## Reproduction

```bash
python scripts/Render-C03IdleV3.py --output-dir artifacts/C03-Idle-V3-review
python -m unittest discover -s tools/tests -p 'test_c03_idle_v3.py'
```

Pillow is an **offline reviewer dependency** for GIF/contact sheets only; no dependency added to Unity runtime.

Local reproducibility used the original authored 21 RGBA part PNGs and V2 Closed keypose from previous owner review archives. A lightweight local source mirror, matching native source pixels, passed **6 direct functional checks** for frozen Rest, eye-limited changes, playback timing, rejected out-of-ROI edits, rejected head/hair edits and rejected timing drift. **Remote clean-checkout/Unity test suite results are not verified in this session.** Do not call them PASS. GIF itself was reopened and its nine durations confirmed.

## Required owner review

Play the timed GIF at its **real duration** and inspect open/half/closed at 1×, 4× and 10×. Decide exactly one:

- `APPROVE_MOTION_DIRECTION` — proceed to Unity/phone integration and independent art/IP checks; **not** Golden acceptance.
- `REVISE_MOTION` — cite eye-cluster coordinates or blink timing and desired result.
- `REJECT_MOTION` — stop blink implementation pending art direction.

**No answer is not approval.** Current status remains
`IDLE_V3_BLINK_PLAYBACK_AWAITING_OWNER_REVIEW`.

**Save/gameplay impact:** none. **Approved Registry:** unchanged.  
**Skills used:** startup-life-session-orchestrator, startup-life-asset-quality-gate,
vietnam-art-direction, 2d-pixel-perfect, sprite-editor (read only); no live Unity Editor.  
**Owner model:** GPT-6. **Known blockers:** real Unity playback, phone BeautyCell, independent IP, owner motion/GOLDEN signoff and exact concept PNG archival.

**Unity import restriction:** V3 is an offline authored review source under `docs/evidence/production-art/source` until the live Editor can generate valid `.meta` files. Baseline CI already failed on older C03 source `.meta` gaps; CI static foundation has not been weakened.

## Owner decision record — 2026-10-18

**Verdict:** `APPROVE_MOTION_DIRECTION` (explicit owner reply).

**Scope approved:** C03 Idle V3 native 128×160 eyes-only behavior,
stable planted Rest, fast eyelid sequence and irregular review-only
rest timing. No fixed production blink cadence was approved.

**Frozen inputs:** original Rest; original V2 closed-eye keypose; V3
half-closed authored eye pixels; bag-over-hand and 20 non-face parts
unchanged. Prior Inhale V1/V2/V2.1 remains `REJECT_INHALE_MOTION`
and excluded from all active motion.

**Next gate:** `UNITY_RUNTIME_AND_DEVICE_QA_PENDING`.

Before gameplay binding or registry promotion:

1. Reproduce draft native part PNGs from lossless source.
2. Use a connected Unity Editor to import textures properly with generated
   `.meta`, Point filtering, uncompressed, no mipmaps, 32 PPU and full
   128×160 grid. Never hand-write the importer metadata.
3. Bind source pose frames to the shared runtime character presentation
   or existing rig/prefab only when the original art/source permissions and
   runtime integration contract are satisfied. Avoid adding a separate C03-only
   animation system or replacing gameplay timing.
4. Verify actual Unity Open/Half/Closed/Open frames and true 180ms blink
   timing in a running portrait scene; check pixel edges, occlusion, static
   silhouette, absence of head/torso/feet wobble, and independent blink
   schedule with holds.
5. Capture a real mobile BeautyCell and obtain technical/visual QA. Independently
   clear asset provenance and licensing. Only then consider Golden registration.

**Existing CI blocker:** Engine-free CI previously failed on
`scripts/Test-UnityFoundationStatic.ps1` because several older C03
source items lack Unity `.meta` sidecars, including at baseline
`a162e5ed94d6e84bc6e5705a47daea1fa4b458b1`. The current V3 review JSON
is outside Unity `Assets/` to avoid adding to this debt. This legacy
failure is not marked fixed, ignored or relaxed.

**Owner model:** GPT-6. **Approval recorded in Git:** V3 source JSON
status and `gate.art_review`. **Save impact:** None.
**Approval does not imply completed Unity integration.**
