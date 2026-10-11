# Startup Life — M13 Golden Art Bible v1

**Status:** Production-direction contract drafted, not an approved Golden Asset or completed Unity scene.  
**Source of truth:** `Assets/StartupLife/Art/_ArtDirection/art-spec.yaml`, `composition.yaml`, and `golden-targets.json`. The `.yaml` files use YAML 1.2's JSON subset so their schema can be validated using the stock Python 3 runtime. Never edit a separate competing palette.

## Visual identity and scale

**Direction:** contemporary Vietnam, friendly **non-pixel HD 2D chibi** with genuine jointed cutout animation. Soft matte materials, reduced background detail, clear object silhouettes and a cozy workplace-to-home mood. Not hyperrealistic, not neon-anime and not glossy 3D.

**Provisional numeric locks for first Golden proof:** 2.6 heads total character height; head height approximately 38.5%; master character sheet height 2048px; reference sprite PPU 100; orthographic camera size 5; source portrait canvas 1080×1920. These are intentional production-art defaults **to be verified at phone size**. Changing scale later must update every reference and contract, not silently adjust individual sprites.

The canonical UI palette is grounded in the existing `MobileTheme.asset`, rather than guessed:

| Role | Hex |
|---|---|
| Background | `#F2EBDE` |
| Panel | `#FFFBF2` |
| Ink | `#2D3D39` |
| Accent | `#3A6C58` |

Accent and supporting environment browns/blues must stay quieter than gameplay buttons. `LifeDashboardOverlay` currently uses some variant colors; this difference must be reconciled when deriving a production UI theme, **not** mistaken for a matching asset palette now.

## Golden targets and compositions

1. **Character / turnaround:** contemporary casual-smart outfit for employment and home/founder variants. Front, left three-quarter and side identity sheets. Clean separation for hair back/front, face, torso, upper/lower arms, hands, upper/lower legs, shoes and interchangeable clothing. Feet pivot and stable layer names/IDs. Idle breath, walk with actual bending knees/elbows, keyboard typing, talk gesture, night laptop and kiosk serving are the later rig proof. *Moving one rigid sprite across the screen does not pass.*
2. **Night home:** compact contemporary Vietnamese desk, warm task lamp, simple wall fan or air conditioner, helmet hook. One prominent character workspace with space for animation; dark blue/warm orange lighting but no tourist-aesthetic decor. Story: employee studies or runs a small startup after work.
3. **Software office:** workstation and meeting area with practical supplies and a common helmet rack / water dispenser. Readable keyboard animation and controlled eye path for a working professional; no exaggerated tech symbolism or fake Vietnamese poster text.
4. **Coffee kiosk:** small street-facing stall, takeaway cups, QR stand with **blank** print surface, stainless counter, two-character movement space for founder and fixed helper. Local business plausibility without recreating an existing trademark. Separate signboard text from art.
5. **Life dashboard:** five-tab portrait UI with Noto Sans Vietnamese, VND, 96-reference-unit touch targets, safe-area ownership, world/background negative space and a clear primary decision. Panel/ink/accent colors should derive from the art spec.

Composition follows the same orthographic camera and shared focal path: (1) player/current action, (2) progression or next choice, (3) quiet contextual local detail. Use two to four specific Vietnamese local anchors per scene. Avoid a stereotype collage of nón lá, lanterns, flags or rice paddies.

## Production promotion pipeline

Brief → 8–12 candidates in controlled generation rounds → reject anatomy/context/text/alpha failures → independent review → character turnaround & layer reconstruction → import with shared PPU/rig contracts → genuine Sprite Skin + bone clips → mobile BeautyCell capture → **score at least 15/16, no criterion zero** → explicit approval → registered prefab/sprite.

Score each asset 0–2 across style match, silhouette, palette, anatomy/perspective, Vietnam context, technical cleanliness, mobile readability and reuse. A score without evidence is not approval. Generated source must never be dragged directly into a production scene. No arbitrary Chinese/Japanese/Korean design tokens or baked machine-generated Vietnamese typography.

The registry `Assets/StartupLife/Art/Approved/registry.json` starts **intentionally empty**. Only real reviewed assets may gain approved entries, with source provenance, reference pack, cleanup, import report, BeautyCell, scorecard and reviewer. No Golden character/environment/UI is currently approved.

## Technical and QA handoff

At M13-T01, only contracts and the static validator exist. Run `python3 scripts/Validate-GoldenArt.py` and CI to prove agreement on style ID, palette, camera, PPU, locality, non-approval and score thresholds. It does **not** validate Unity import, real textures, animations, font rasterization, phone screenshots or artistic quality. Those are M13-T02/T03/T04 and end-of-MVP runtime gates.

Later art authors must condition every generation against the selected Golden reference once one exists; at this stage no approved reference image has been selected. **Do not auto-approve** an image produced by the generating model.
