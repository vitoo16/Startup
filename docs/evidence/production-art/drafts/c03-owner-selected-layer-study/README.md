# C03 owner-selected visual — 21-layer cutout proof

**Status:** `DRAFT / ART CORRECTION REQUIRED / NOT UNITY-ANIMATION-READY`

This directory contains real **21 RGBA PNG source-cutout layers**, a
`layers/masks/bag_body.png` mask, a 16-joint *estimate* and visual previews,
all derived from the **exact owner-selected October 21 C03 PNG in Git LFS**,
SHA-256 `1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa`.
It uses source pixel colors without recoloring or changing C03's identity.
The 21 semantic names/order and 16-joint topology match the old rig.

**Important:** This is an extraction of the **visible pixels from a flattened,
opaque 1122×1402 source**, not hand-authored transparent 128×160 native pixel
art and not a production rig. Some layer boundaries contain bits of adjoining
clothes, face, hair or satchel, because source-hidden portions do not exist in
the original PNG. The composite deliberately matches the extracted visible
source, but the layers WILL visibly tear if rotated before being cleaned up.
Neither the guide's provisional pivots nor any animation are approved.

## Files

- `layers/*.png` — 21 full-canvas transparent RGBA study layers
- `layers/masks/bag_body.png` — independent draft front bag-body region
- `C03-selected-cutout-composite-DRAFT.png` — original-visible C03 composite
- `C03-selected-cutout-preview-DRAFT.png` — image preview for human review
- `C03-selected-21-layer-contact-sheet-DRAFT.png` — identifies seams to repair
- `C03-selected-16-joint-guide-DRAFT.json` — 16 bone IDs, parent links, rough pivots, slot ownership
- `C03-selected-cutout-receipt.json` — provenance, every PNG SHA-256 and remaining gates

The image study is reproducible by
`python scripts/Stage-C03SelectedOwnerCutout.py --output-dir <new-empty-folder>`
after `git lfs pull`. The one-shot artifact generator is
`.github/workflows/c03-selected-layer-draft.yml`.

## Unity QA only

After Unity 6000.3.25f1 has indexed the repository, run:

`Startup Life → Art → C03 → Build selected-owner cutout QA rig (NOT motion ready)`

The builder in `GoldenCharacterDraftBuilder.cs` verifies exact LFS bytes and
22 draft-file checksums, imports through **Unity's** TextureImporter, and
creates a separate
`Assets/StartupLife/Art/Source/Characters/golden-character-c03/Generated-Owner-Cutout-QA/C03_OWNER_SELECTED_CUTOUT_QA.prefab`.
It preserves the same 16-joint / 21-renderer topology and 0–20 sorting slots.
It refuses overwrites and creates **no animation controller or clips** for
these as-yet-uncleaned layers. This command has NOT been executed in Unity
in this session, so there is no verified Editor prefab or screenshot evidence.

### Must finish before runtime integration

1. Refine all 21 layer boundaries by hand, especially hair/face split,
   sleeves/hands, shirt-jacket seams, bag-occluded hand and leg/foot overlap;
   paint originally occluded anatomy needed for rotating each joint.
2. Redraw at authored native 128×160 RGBA/32 PPU from C03 selected visual
   rather than treating the full-resolution opaque reference as native art.
3. Review, update and animate true 16 joint pivots; compare against the
   selected C03 at 1×/4× and on device.
4. Run independent originality/IP and final owner art approval before
   `Art/Approved`, Creator/Life, save profiles or FirstPlayable.

No shipping asset is authorized by the presence of these files.
The rejected prior substitute art is NOT used. The previous
`Generated-V3-Quarantine` prefab, animation, controller and imported sprites
remain untouched.

**Owner model:** GPT-6.
**Skills:** startup-life-session-orchestrator, vietnam-art-direction,
2d-pixel-perfect, sprite-editor, startup-life-asset-quality-gate, unity-cli.
**Save impact:** NONE. **Visual evidence:** Git LFS image/composite and
contact sheet; **Unity Editor/PlayMode/device:** NOT RUN.
