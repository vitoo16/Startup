# Why the selected C03 character is still invisible in Startup Life

**2026-10-21 · P03 character evidence / actual production integration BLOCKED.**

This is not a texture missing randomly at runtime. The active art registry
`Assets/StartupLife/Art/Approved/registry.json` has **zero approved entries**,
`CharacterCreatorVisualPreviewBridge` has **no rig/configuration attached** in
`FirstPlayable`. The owner's exact current visual reference is now committed as a
**Git LFS-tracked source PNG**; however, the source is opaque and is not a game-ready
articulated character. The existing C03 native 21-part source and Idle V3 blink
are deliberately quarantined proofs, **not the selected shipping visual**.

## Which original image?

The latest owner-approved *visual reference* is
`image(20261021-124610).png`, **1122×1402 RGBA (fully opaque)**,
SHA-256 `1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa`,
with the satchel in front of the near-side hand. This version supersedes the
older chosen illustration and the C03 procedural draft look. Source of truth:
`docs/evidence/production-art/C03-LATEST-MINOR-POLISH-REFERENCE.json` and
`C03-OWNER-APPROVED-FRONT-RIG-HANDOFF.md`.

## Reproduce exact-reference diagnosis

Run in the local Git checkout with Python 3:

```powershell
cd C:\Users\viett\Startup
python scripts/Check-C03PlayerArtReadiness.py
```

With Git LFS hydrated and the exact owner PNG present, expected status is
`REFERENCE_VERIFIED_NOT_GAMEPLAY_READY` and exit code **0**. A pointer-only
checkout (without LFS hydration) or missing PNG deliberately returns
`MISSING_OR_INCORRECT_REFERENCE` (exit code **2**). These are input-integrity
results, not a Unity gameplay acceptance gate. The tool makes no
changes to files, saves, Unity scenes or the approval registry.

If the original owner-selected PNG or handoff ZIP is available, use the
existing SHA-checked staging action (NEVER bypass its hash assertion):

```powershell
python scripts/Stage-C03HandBagReference.py "C:\path\to\pixel_art_boy_with_brown_satchel.png"
python scripts/Verify-C03HandBagReference.py
python scripts/Check-C03PlayerArtReadiness.py
```

The staged copy goes under
`Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-hand-behind-bag-reference.png`.
It is **draft reference material only**, not an approved sprite. These
commands refuse a wrong visual binary or silently overwriting a different
file. Do not put it under `Art/Approved`.

## Show the exact selected reference in Unity (isolated)

1. Stop Play Mode; after source compilation succeeds, open Unity Editor.
2. Use **Startup Life → Art → C03 → Open selected visual reference in Game View**.
3. The menu validates SHA-256 and dimensions; if absent/mismatched, it refuses.
4. Unity asks whether to save any current modified scene before replacing it.
   Handle that confirmation yourself; **never discard unsaved scene changes**.
5. Switch to the **Game** tab. The isolated QA scene displays the **opaque,
   non-articulated original illustration** at a portrait-compatible scale.
   Entering Play Mode does not magically add joints or animations. This stage
   does not save a scene or modify FirstPlayable.
6. Record a screenshot, compare the visible bag/hand and face against the
   owner's selected image, and report Unity Console errors if any.

This **is not gameplay integration**. The image is a flat high-resolution RGBA (fully opaque)
reference with its background baked in; putting it directly in
`FirstPlayable` would violate the production pixel-art, cutout and rig gates.

## C03 reference importer gate (2026-10-22)

The selected LFS-tracked 1122×1402 image has a **minimal identity-only
`.meta`** on `main`; the current shell/GitHub connector did **not** run
Unity 6000.3.25f1 or record an imported `TextureImporter` from the Editor.
Do not mark this gate PASS based on a script review or GitHub CI alone.

After fetching the exact image through Git LFS, in the **actual Unity Editor**:

1. Run `Startup Life → Art → C03 → Open selected visual reference in Game View`.
   The command verifies the owner-selected source SHA before showing a
   save/discard/cancel prompt for the current scene. **Cancel** makes no
   importer or scene changes; continuing lets Unity serialize the source
   `TextureImporter` while retaining GUID
   `0eb68713039bb3f40d6ad5c2a6bb0309`.
2. Run `Startup Life → Art → C03 → Verify imported selected reference (read only)`.
   The verifier fails closed on a wrong image/path/hash, changed GUID,
   missing Sprite, incorrect dimensions, or mismatched reference-only
   Sprite/Point/350 PPU/no-mips/uncompressed import settings.
3. Inspect `git diff -- <reference>.meta` and commit **the Unity-authored
   serialized importer file**, preserving the tracked GUID. Do not manually
   fabricate importer YAML. Record the Unity console result and Game View
   screenshot at an actual portrait viewport.
4. Keep `Art/Approved/registry.json`, `FirstPlayable.unity`, production
   character bindings and saved player profiles untouched. These checks
   concern the **opaque QA reference**, not a native sprite rig, gameplay
   character, device BeautyCell or independent IP approval.

Engine-free structural test coverage:
`tools/tests/test_c03_player_art_readiness.py` checks the new menu's
read-only behavior, GUID guard, importer settings and the requirement that
the scene-save prompt precede reimport. Such tests **cannot** confirm that
the code compiled or ran inside the Unity Editor.

**Owner:** GPT-6 · **Skills used:** startup-life-session-orchestrator,
unity-cli, 2d-pixel-perfect, sprite-editor, startup-life-asset-quality-gate,
vietnam-art-direction. **Visual evidence:** NOT RUN in this session.
**Save impact:** NONE. **Known limitations:** native 21-part reconstruction,
16-joint motion, Creator/Life integration, independent review and device
proof remain pending. **Files changed:** C03SelectedVisualPreviewEditor.cs,
test_c03_player_art_readiness.py, this diagnosis.

## Native 21-layer current-image handoff (2026-10-22)

The archived October 16 rig contract remains immutable. The **active**
2026-10-21 handoff contract is
`docs/evidence/production-art/C03-ACTIVE-OWNER-FRONT-RIG-CONTRACT.json`.
Its `selected_concept.native_rig_from_selected_image` is `NOT_AUTHORED`.
The previous experimental 21-layer V3 sprite pack is **not** acceptable as
the newer image's production rig, regardless of whether its silhouette
and synthetic joint tests pass.

After an artist has genuinely reconstructed native pixel layers from the
**1122×1402** owner-selected reference (not a resized raster), stage the
following in one **draft-only** directory outside `Assets`:

- All **21 canonical part IDs** named `<part_id>.png` and an independent
  `masks/bag_body.png`, each on the same 128×160 native RGBA grid.
- A `C03-selected-native-source-manifest.json` recording
  `schema: startup-life.c03.selected-native-source-handoff.v1`,
  `status: DRAFT_PENDING_VISUAL_AND_IP_REVIEW`,
  `selected_reference_sha256`, `selected_reference_path`,
  `native_canvas_px: [128,160]`, `pixels_per_unit: 32`,
  `file_sha256` mapping all 22 relative file paths to their exact
  lowercase SHA-256, and `source_packet_sha256`.
- Compute the packet digest as SHA-256 over UTF-8 concatenation, for
  each sorted filename, of `filename` + NUL + `file_sha256` + LF.
  This checksum binds the entire proposed layer set rather than merely
  labeling an arbitrary source packet as new art. Never reuse the frozen
  October 16 `native_front_source_packet_sha256`.

Only **after actual art is submitted**, update the *active contract's*
`native_rig_from_selected_image` to `ARTIST_AUTHORED_LAYERS_SUBMITTED`
and run:

```bash
git lfs pull
python scripts/Verify-C03HandBagReference.py
python scripts/Validate-C03OwnerRigHandoff.py --layers-dir /path/to/artist/current-c03-native-layers
python -m unittest discover -s tools/tests -p 'test_c03_owner_rig_handoff.py' -v
```

The validator checks exact reference SHA, manifest completeness, current
reference identity, 22 file hashes + canonical packet digest, nonempty
binary-alpha layer pixels and hand-behind-bag mask overlap. It never
manufactures sprites, trusts metadata as evidence of visual likeness,
imports an Editor prefab, approves IP or modifies gameplay. Image-specific
layer artwork, authored joint pivots, actual animation, Unity graphical
and mobile BeautyCell, and human owner/IP review remain **PENDING**.

## Build image-matched C03 articulated QA rig (new pipeline, 2026-10-22)

This is a **new independent staging/Editor import path** for art reconstructed
from the owner-selected **1122×1402 RGBA image**. It is not a rename of the
October 16 V3 sprites, rig, rejected sinusoidal Idle or masked concept.
The stage deliberately fails until current-image artist-authored layers exist.

**Source art prerequisites:** 21 transparent 128×160 native-grid semantic PNG
layers, `masks/bag_body.png`, the SHA-256 manifest specified above and the
hydrated owner reference. Do not downsample the opaque illustration into
fake native pixel source. The 128×160 / 32 PPU values are QA authoring
assumptions; any later approved production style migration must update the
contract and importer together, not silently diverge.

A separately **artist-authored** joint/motion file is required, using
`startup-life.c03.selected-native-joint-motion.v1` with
`status: ARTIST_AUTHORED_PENDING_UNITY_REVIEW`,
`character_id: C03`, `view: front`, `selected_reference_sha256`,
`source_packet_sha256`, `native_grid: [128,160]`,
`pixels_per_unit: 32`, and:

- `joints`: exactly 16 entries `{id, parent, pivot_px:[x,y], parts:[...]}`.
  Joint IDs/parent hierarchy follow the existing front rig topology; each
  of the 21 semantic parts belongs to exactly one joint. **Pivots are
  newly authored for the current image**; reusing the historical 16-pivot
  set verbatim is rejected.
- `motions`: authored `idle` plus optional `walk`, `work`, `study`
  clips, each with `id`, `duration_seconds` and `tracks`.
  A track is `{joint_id, keys:[[time_seconds,angle_degrees],...]}`.
  A clip needs at least two genuinely moving non-root joints; the root
  cannot move, loop start/end return to bind pose, and `walk` must move
  at least two leg joints. No automatic cosine keyframes, whole-body
  translation or generic procedural motion are generated.

Once source art and pivot/animation authorship are real and reviewed as
a draft, update **only the active contract** `native_rig_from_selected_image`
to `ARTIST_AUTHORED_LAYERS_SUBMITTED`. Do not modify the frozen archive,
set registry approval, or claim this metadata alone proves image likeness.

```bash
git lfs pull
python scripts/Stage-C03SelectedNativeRig.py \
  --layers-dir /path/to/artist/current-native-layers \
  --rig-plan /path/to/artist/C03-selected-joint-motion.json \
  --output artifacts/C03-selected-native-rig/input
python -m unittest discover -s tools/tests -p 'test_c03_selected_native_stage.py' -v
```

Staging revalidates the actual owner PNG, 22-file native packet,
hand-behind-bag overlap, new joints and authored motion **before** copying
any of the 21 sprite layers. It writes an Editor-readable SHA-checked
`C03-selected-native-unity-handoff.json`; refuse overwriting an existing
stage to protect artist changes.

In **Unity 6000.3.25f1 with no Play Mode active**, select:

`Startup Life → Art → C03 → Build selected native articulated rig (quarantine)`

The Editor validates the selected source again, imports the 21 PNGs as
full-resolution Single Sprite, Point/no-mip/uncompressed with 32 PPU and
center pivot, generates authored 16-joint hierarchy/21 renderers,
creates actual keyframed `localEulerAnglesRaw.z` AnimationClips and a
manually reviewable Animator, then saves:

`Assets/StartupLife/Art/Source/Characters/golden-character-c03/Generated-Selected-Quarantine/C03_SELECTED_NATIVE_QA.prefab`

It does **not** approve, register, or bind anything to a production scene,
and does **not** overwrite previous Quarantine or artist outputs.
Review the real Unity Console, hierarchy, sprite imports, independent
hand-bag overlap, limb articulation, visible pixel seams and mobile
BeautyCell. Actual Game View / PlayMode/device testing is still required;
Engine-free CI source checks are not proof of Unity compilation or art
quality. Existing runtime `CharacterVisualAssembler` and creator bridge
remain the planned integration boundary **after** human visual/IP approval.

**Implementation owner:** GPT-6; **skills:** startup-life-session-orchestrator,
unity-cli, sprite-editor, 2d-pixel-perfect, vietnam-art-direction and
startup-life-asset-quality-gate. **Save/economy impact:** none.
**Art/device/IP evidence:** pending. **Files:** staging script,
GoldenCharacterDraftBuilder.cs, source contract tests and CI workflow.

## Separately test the existing native C03 Idle V3 draft

The original 21-layer C03 native experimental rig can be staged and imported
with the preexisting commands in
`docs/evidence/production-art/C03-IDLE-V3-UNITY-QUARANTINE-HANDOFF.md`.
Its Unity menu is
**Startup Life → Art → C03 → Build quarantined Idle V3 blink preview**.
That tests the *approved motion direction* only, and is not proof that the
owner-selected PNG has been reconstructed into a game-ready rig. Never treat
this quarantined prefab as a new approved gameplay asset.

## Acceptance/remaining work

- Exact new selected image in repository: **COMMITTED AS GIT LFS** since `20aad88` (pointer SHA-256 `1999322a...`, 1,406,695 bytes); engine-free C03 reference identity check passes with LFS checkout enabled.
- Selected image SHA-256/dimensions: **PASS** for exact owner-uploaded source checked on an LFS-hydrated CI checkout; graphical Unity asset import remains **NOT VERIFIED**.
- Isolated Unity original-image Game View menu: implementation committed;
  actual local Unity Editor/PlayMode **NOT RUN HERE**.
- Original transparency + 21 articulated native parts derived from **this
  exact approved front visual**: **NOT PRODUCED/VERIFIED**.
- Independent originality/IP, sprite import/rig, phone BeautyCell, character
  choice mapping, outfit variants, persistent runtime resolver and
  `FirstPlayable` actor binding: **BLOCKED**.
- Engine-free unit tests check source identity, incorrect/missing imagery,
  guard rails and script wiring. These tests cannot establish Unity rendering.
- Save/gameplay/economics changes: **NONE**.

**Skills:** startup-life-session-orchestrator, unity-cli, 2d-pixel-perfect,
sprite-editor, startup-life-asset-quality-gate, vietnam-art-direction.
**Visual evidence:** existing references only, no current Unity screenshot.
**Known limitation:** this chat runner cannot run the user's local Unity Editor.
The committed PNG has minimal identity `.meta` sidecars; actual Unity 6000.3.25f1
must import and fully serialize the TextureImporter without changing GUID. This
is still not a real rig, animation or phone proof.

## New owner selection (2026-10-21)

Owner selected a NEW canonical image, `image(20261021-124610).png`, 1122×1402 RGBA
(all alpha = 255), SHA-256 `1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa`. This replaces the earlier 2026-10-16 concept
whose SHA-256 was `d2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e`. Former concepts and the native Idle V3 are
historical drafts, not substitutes. The exact binary has since been committed via Git LFS and source-checked in CI;
that does NOT mean Unity has imported it or it is approved for gameplay.
