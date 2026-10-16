# C03 — Owner-requested small polish of the selected character

**Date:** 2026-10-16  
**Visual scope:** ONE front-view illustration · no other variants or animation  
**Source of truth for previous owner selection:** [C03-OWNER-SELECTION.json](C03-OWNER-SELECTION.json)  
**Current new image state:** `NEW_GENERATED_REFERENCE_PENDING_OWNER_CONFIRMATION`; **not approved art**.

## This revision is a small edit, not an alternative design

The owner instructed that the softer, friendly full-body C03 illustration be the
character's reference, and that the subsequent pass be restricted to small corrections:
shorten the apparent neck and improve chin-to-collar blending, tuck ears into the
head silhouette, strengthen the jeans/shoes and vary the planted foot, and soften
perfectly symmetric hands/shoulders into a mild weight shift.

Do not alter the signature asymmetrical dark hair, kind eyes and smile, visible
blush, green jacket, cream shirt, blue-grey trousers, broad cream-accent sneakers
or cross-body brown satchel. Do not continue work on rejected R5.1/idle A/B as
the owner-selected character. Do not expand into extra views, variants or
animation from an unreviewed drawing.

## Exact PNG provenance

| Role | Filename | Dimensions | SHA-256 | Stored as Git binary? |
| --- | --- | --- | --- | --- |
| Previous generated visual seen in this conversation | `friendly_pixel_messenger_character.png` | 1122×1402 RGB | `b50fd50492a86126c0652425e9bffaa2820952148cb2877d33c20008b92f58fe` | **NO** |
| Latest small-polish generation | `chibi_pixel_traveler_with_satchel.png` | 1149×1368 RGB | `64bb3520c38537d8b0ebaed1daac9a879215b81564642eee088d9e0cbf98807b` | **NO** |

These are verified conversation sandbox PNGs, **not GitHub assets**. This record
does **not** replace the existing owner-uploaded
`image(20261016-093800).png` provenance record: it preserves that
earlier image and documents a later small-polish pass without falsifying
byte identity. Do not imply that identical appearance entails identical
PNG bytes.

**Expected draft destination for exact bytes, once a binary upload path is
available:**
`Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-minor-polish-visual-reference.png`.
Before publishing this path, compare `sha256sum` with the exact hash above.
Never put an unapproved concept under `Assets/StartupLife/Art/Approved`.

## Technical and release gates

The generated illustration has an **opaque beige background** and is neither
a transparent isolated character sprite nor a native 64×80 image with
21 independently editable layers. It cannot honestly satisfy the
`vn_everyday_pixel_v1` production contract by automatic downscaling or
pixelization. Native source authoring, articulated rigging, independent
similarity/IP review, Unity import and device BeautyCell remain outstanding.

**Art decision:** The original friendly character **design direction is
owner-selected**. This **new minor-polish image version** remains a proposed
visual refinement until explicit owner visual confirmation. The Approved
Registry, simulation, save and runtime character bindings are unchanged.

## Session closeout

- Skills: `startup-life-session-orchestrator`,
  `vietnam-art-direction`, `startup-life-asset-quality-gate`,
  `2d-pixel-perfect`.
- Evidence: PNG dimensions/format/hashes inspected locally; GitHub HEAD read
  before write, new documentation reread after write.
- Tests: No Unity/PlayMode/device/IP tests. No native pixel technical PASS claim.
- Save impact: None.
- Files changed: This reference note and
  `C03-LATEST-MINOR-POLISH-REFERENCE.json`.
- Limitation: Exact binary not uploaded via the current GitHub text-file action.
