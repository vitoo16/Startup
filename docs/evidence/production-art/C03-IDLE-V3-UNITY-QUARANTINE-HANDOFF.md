# C03 Idle V3 — Unity quarantine integration handoff (Editor not yet executed)

**Owner-approved motion direction:** `APPROVE_MOTION_DIRECTION` (2026-10-18)  
**Base:** `00f40988475fd697cf4c536d0f1bb7a9a7bd2e65`  
**Stage:** `UNITY_EDITOR_IMPORT_PREPARED_BUT_NOT_RUN`.  
**Character:** C03 / Sáng Ngõ Nhỏ, 128×160 native RGBA at 32 pixels/unit.  
**Release permission:** **NONE**. Only an isolated proof prefab may be authored in a live Editor.

## Work implemented on main

This stage deliberately uses the existing native art pipeline and C01 Editor
draft-building assembly instead of creating a new gameplay renderer:

- `scripts/Stage-C03IdleV3UnityProof.py` — reads 21 already-generated
  native semantic part PNGs, the exact V3 Open/Half/Closed full-body
  composite PNGs and the existing
  `scripts/Export-C03UnityRigHandoff.py` output.
- The staging tool derives **three face-only native sprite variants**
  without moving any of the 20 non-face layers. It independently
  recomposites all 21 layers for each pose and requires **byte-for-byte
  RGBA parity** against the approved-direction V3 composite.
- Hard source gates include original Rest SHA256
  `efda42e83eb039d31e6ff3cd25dbf68db8d4f65cdc34beab842ee90be96ebe1d`,
  inherited Closed SHA256
  `1faf1b8c78e078dd3aed386e22a7932eea5b19d3836bebcb260ae62a3ebd853b`,
  half pose **19**, closed **35** visible eye pixel changes, exact native
  eye ROI, 21 part IDs, 16 bones, bag-front sorting and zero alpha
  antialias. It emits 24 PNGs, each with SHA-256, outside Unity `Assets`.
- `Assets/StartupLife/Scripts/Editor/GoldenCharacterDraftBuilder.cs`
  now also contains a separate `C03IdleV3QuarantineBuilder` and an Editor
  menu item, reusing the **existing Editor assembly/meta**. It refuses
  overwrite, checks staged input file hashes, verifies import contracts,
  imports using Unity `AssetDatabase`/`TextureImporter` (not hand-edited
  `.meta`) with Point filter, 32 PPU, no mipmap, uncompressed/center pivot.
- The Editor builder creates a 16-joint hierarchy, places 21
  original native `SpriteRenderer` layers at the authored offsets,
  and creates a 180ms proof `AnimationClip` with object-reference
  keyframes on **the `face` renderer only**. It provides a quarantined
  Animator `Blink` trigger; no steady 3s/5s/4s schedule or automatic
  gameplay scheduler is wired. No changes to save/economy/runtime resolver.
- `tools/tests/test_c03_idle_v3_unity_staging.py` adds engine-free
  regression coverage. No new `.meta` was manually authored.

## Reproduce offline staging

From the repository root (Pillow required on authoring workstation):

```bash
python scripts/Render-C03OwnerNativeFront.py --output-dir artifacts/C03-native
python scripts/Render-C03IdleV3.py --output-dir artifacts/C03-v3-frames
python scripts/Export-C03UnityRigHandoff.py --output artifacts/C03-rig.json

python scripts/Stage-C03IdleV3UnityProof.py \
  --native-layers artifacts/C03-native/layers \
  --v3-frames artifacts/C03-v3-frames \
  --rig-manifest artifacts/C03-rig.json \
  --output artifacts/C03-Idle-V3-Unity-Quarantine/input

python -m unittest discover -s tools/tests -p 'test_c03_idle_v3_unity_staging.py'
```

Alternatively, the owner handoff ZIP contains the exact
`artifacts/C03-Idle-V3-Unity-Quarantine/input/` tree; unpack at the
repository root before opening the Editor.

## Only in a live Unity 6.3 LTS Editor

After checking the relevant Unity CLI and Sprite Editor capabilities:

1. Select menu: **Startup Life → Art → C03 → Build quarantined Idle V3 blink preview**.
2. The Editor must create actual `.meta` sidecars during import. The tool
   makes a new folder only, and fails closed if it already exists.
3. Inspect `Assets/StartupLife/Art/Source/Characters/golden-character-c03/Generated-V3-Quarantine`.
   Inspect all 21 `SpriteRenderer` parts and 16 transforms, imported textures
   and per-part sorting, especially bag front over the affected hand.
4. Open the quarantined prefab/scene in Play Mode, manually send Animator
   **Blink** trigger. Check Open → Half → Closed → Half → Open at 180ms,
   verifying zero body/head translation, no visible seam or eye crawl, and
   a static held Rest. No automatic blink scheduler was added.
5. Capture actual Unity portrait 1×, actual native 4×, and mobile BeautyCell,
   with independent Art Director review. Validate real runtime motion and
   source provenance/IP before ANY production integration or Golden registry.

This **has not** been executed in a live Editor or device in this session.
The prepared importer cannot be called an imported prefab, a working Unity
animation, or shipping asset until it is run and inspected.

## Verified local evidence

Using the owner-reviewed native source layer pack and V3 PNG pack:

| Check | Local result |
| --- | --- |
| Quarantined input RGBA PNG count | **24** (21 original parts + 3 face variants) |
| Native canvas, alpha, PPU contract | 128×160, binary alpha; declared 32 PPU |
| Composited Rest / Half / Closed versus V3 approved-direction PNGs | **Exact per-pixel match, all three** |
| Rest altered | **No** |
| Body/hand/bag/feet modified | **No** |
| Staging SHA-256 for 24 images | Verified |
| Refuse overwrite / out-of-eye edits / bad bag sorting / tampered Rest | **4 negative checks PASS local** |
| Live Unity Editor importer and prefab runtime | **NOT RUN** |
| Unity device BeautyCell, IP, Golden | **NOT RUN** |

**Local test note:** The standalone staging logic and five test scenarios
were run against the known C03 artifact ZIPs on this workstation.
The newly committed repository test has not been run in a clean GitHub
checkout yet, and Unity C# has not been compiled or executed by Unity.
Do not conflate that local evidence with remote CI/runtime success.

**Existing CI issue:** The engine-free static Unity foundation check at
the previous baseline `a162e5ed...` failed because older C03 authoring
files inside `Assets` lack Unity-generated `.meta` files. This was not
introduced solely by V3. No validator thresholds or .meta rules are
changed in this task; those legacy assets still require an Editor-controlled
reconciliation separately.

**Owner model:** GPT-6.  
**Skills:** startup-life-session-orchestrator, startup-life-asset-quality-gate,
vietnam-art-direction, 2d-pixel-perfect, sprite-editor and Unity CLI guidance.  
**Save/gameplay impact:** None.  
**Known blockers:** connected Unity Editor, actual PlayMode/Blink playback,
phone screen evidence, independent IP clearance, source PNG provenance
and final Golden asset approval.


## R2 hardening — Unity import/persisted proof (2026-10-19)

The quarantined authoring pipeline has been strengthened following the
owner-approved Idle V3 direction. Previously, the Editor only checked
hashes for 24 native PNGs, not the rig manifest, and generated a prefab
without reloading and inspecting the persisted result.

**New source gates** in `scripts/Stage-C03IdleV3UnityProof.py`:

- Pin the canonical 21-layer order, 16-bone identity and the frozen native
  art packet SHA. Reject moved joint pivots and nonmatching sprite local
  offsets, rather than relying solely on correct flattened previews.
- Verify all face changes belong to the exact eye ROI before pixel-count
  bounds, producing an actionable error instead of generic failure.
- Store `rig_manifest_sha256` in the offline staging receipt, in addition
  to the 24 per-PNG SHA-256 checks.

**New Editor proof gates** in the existing
`C03IdleV3QuarantineBuilder` (part of
`Assets/StartupLife/Scripts/Editor/GoldenCharacterDraftBuilder.cs`):

- Refuse staging if `rig_manifest_sha256` does not match the full
  rig-authoring JSON input bytes.
- Save/reimport Unity assets and verify the reloaded prefab has exactly
  16 authored bone transforms and 21 SpriteRenderers, the intended
  parent-local transforms and stable bag-over-hand rendering order.
- Confirm imported textures are full-size Single Sprite, Point,
  uncompressed, no mipmaps, 32 PPU and centered.
- Confirm the persisted AnimationClip has exactly **one**
  object-reference animation binding: the face
  `SpriteRenderer.m_Sprite`. Assert exact
  Open/Half/Closed/Half/Open sprite references at
  `[0, 20, 50, 110, 180]` milliseconds; no transform curves.
- Expose Unity Editor menu
  **Startup Life → Art → C03 → Verify quarantined Idle V3 blink preview**
  to repeat persisted serialized asset verification after generation.
  This is not a live PlayMode/BeautyCell test.

The previous handoff ZIP's rig descriptor predated the current
rig exporter and did not carry the new hash. Use the regenerated
**C03-Idle-V3-Unity-Quarantine-Handoff-R2.zip**, whose 24 native PNG
bytes are unchanged from R1, with updated non-shipping rig metadata
and a SHA-pinned staging receipt. The R2 ZIP was rechecked locally:
24/24 PNG hashes match; all 16 joint and 21 renderer offsets follow the
original authoring coordinates; Open and Closed recomposites match the
approved-direction native image RGBA SHA-256.

**Automated CI gate:** Engine-free CI now executes the actual C03
source migration, V3 blink and quarantined staging Python regression
suites with a pinned authoring-only Pillow version. The code+CI
commit `d848842d656b530ce4cde1dea95afee8bc6b23ec` recorded a
successful run. This checks engine-free art contracts only; it is
not an Editor compilation or PlayMode proof.

**Still blocked until a real Unity Editor/device is available:**
C# script compilation in Unity, quarantine import execution,
reloaded prefab in actual Editor, PlayMode eye-frame timing,
real pixel-perfect portrait screenshot, phone BeautyCell,
independent originality/IP and Golden approval. None are
automatically promoted by this change.

**Owner model:** GPT-6 · **Skills:** session-orchestrator,
startup-life-asset-quality-gate, unity-cli, sprite-editor,
2d-pixel-perfect. **Save impact:** none. **Runtime changes:** none.
