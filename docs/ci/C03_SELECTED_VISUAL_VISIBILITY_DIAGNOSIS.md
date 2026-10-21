# Why the selected C03 character is still invisible in Startup Life

**2026-10-21 · P03 character evidence / actual production integration BLOCKED.**

This is not a texture missing randomly at runtime. The active art registry
`Assets/StartupLife/Art/Approved/registry.json` has **zero approved entries**,
`CharacterCreatorVisualPreviewBridge` has **no rig/configuration attached** in
`FirstPlayable`, and the exact owner-approved final C03 front visual PNG is
**not committed to GitHub**. The existing C03 native 21-part source and Idle V3
blink are deliberately quarantined proofs, **not the selected shipping visual**.

## Which original image?

The latest owner-approved *visual reference* is
`pixel_art_boy_with_brown_satchel.png`, **1171×1343 RGB**,
SHA-256 `d2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e`,
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

Before staging the original PNG, expected status is
`MISSING_OR_INCORRECT_REFERENCE` and process exit code **2**. This means
**art input is blocked**, not a Unity compiler failure. The tool makes no
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

This **is not gameplay integration**. The image is a flat high-resolution RGB
reference with its background baked in; putting it directly in
`FirstPlayable` would violate the production pixel-art, cutout and rig gates.

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

- Exact selected original image in repository: **MISSING** (until user stages it).
- Selected image SHA-256 / dimension check: source tool implemented; actual
  user's PNG **NOT YET TESTED**.
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
**Known limitation:** current GitHub connector cannot run the user's local
Unity Editor or read an unuploaded binary PNG on their computer.
