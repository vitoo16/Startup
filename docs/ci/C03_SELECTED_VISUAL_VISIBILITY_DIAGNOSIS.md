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
