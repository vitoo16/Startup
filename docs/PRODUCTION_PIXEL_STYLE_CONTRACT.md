# Startup Life — Original Cute Pixel Production Style Contract

**Status:** Active *technical* pixel contract after P02-01 migration. Unity sprite render, rig, mobile and independent IP/visual approval remain NOT RUN. **Effective planning:** 2026-10-14. **Owner:** GPT-6 Technical Director, subject to independent artistic originality/clearance review.

## 1. Visual promise

An **original, soft and playful pixel-art depiction of everyday contemporary Vietnam**: warm human micro-stories, confident readable UI, delightful subtle movements, and a character who actually works, studies, meets people, and operates a small business. The game's visual recognition should come from its own shapes, architecture, interaction rhythm and type hierarchy, **not a borrowed game's palette, border, avatars, logo, icons or screen composition**.

`docs/ART_BIBLE_M13.md` and `Assets/StartupLife/Art/_ArtDirection/*` formerly described non-pixel HD 2D chibi under style ID `vietnam_cozy_chibi_cutout_v1`. The P02-01 technical spec migration has occurred, but does **not** certify live Unity gameplay as pixel art. The prior M13 contract and any frozen technical expectations remain historical evidence; do not accidentally claim the active Unity scene now renders pixel graphics.

**Active technical style ID (not visual approval):** `vn_everyday_pixel_v1` (internal identifier, not a market-tested trademark). Brand names, icons and mascots require separate trademark clearance.

## Owner Art Director decision — 2026-10-15

**ACTIVE DESIGN TARGET:** Short, softly rounded, expressive **adult** chibi at **2.2–2.4 total heads, nominal 2.3**. This supersedes the previous 2.5–2.8 direction. Keep provisional source canvas, PPU and camera constants unchanged until rig/phone tests. Existing C01 first-party front-facing pixel draft was authored for the earlier proportions; it remains **DRAFT_NOT_APPROVED** and must be redesigned/verified. No automatic retroactive acceptance.

Native contemporary Vietnamese environments must be composed as **original pixel clusters and tile geometry**, not photo pixelization of online photographs, re-colored stock/game sprites, traced screenshots or derivative branded shop facades. Write a locality sheet for each location with a purpose, socioeconomic context, 2–4 plausible anchors, excluded stereotypes, blank signage/TMP text and gameplay action zones. Architectural details, scooter helmets, compact shops, practical QR stands and apartment routines convey locality more authentically than a tourism-symbol collage.

**Anti-discrimination:** Do not encode salary, competence, intelligence, profession, virtue or comic inferiority by skin tone, gender presentation, age, body size, disability, class or region. Chibi size is a visual treatment of adults, not a representation of children. Ensure appearance variants, NPC job depictions and marketing are reviewed respectfully by an independent human reviewer.

The detailed design handoff lives in [ART_DIRECTION_PRODUCTION_BIBLE.md](ART_DIRECTION_PRODUCTION_BIBLE.md); the existing independent legal/IP approval and real Unity/phone Golden gates remain intact.

## 2. Distinctive graphic grammar — original-first

| Dimension | Proposal to prototype and lock | Rejection trigger |
| --- | --- | --- |
| Silhouette | Chibi actor proportions around 2.2–2.4 heads (nominal 2.3; pending real Unity/device verification), expressive square-rounded hair masses, distinctive asymmetrical bag/jacket/shoe accessories drawn from original sketches | Generic character clones, near-identical costumes or trademarked props |
| Faces | Sparse readable pixel clusters; emotional change through brows/eyes/body pose; identity survives mirrored/equipment variants | Same face/hair outline as known market character |
| Environment | Three original room silhouettes: compact shared apartment desk, workplace workstation cluster, practical sidewalk kiosk. Two-to-four locally plausible anchors/room | Reproducing a recognisable screenshot/interior from a commercial title |
| UI | Unique "receipt-slip + notebook-edge" inspired modular shapes: short corner notches, deliberate off-center tabs, stitched-like 1px dividers and simple grid icons | Copying any app's full tab bar, mascot iconography or panel hierarchy |
| Props | Real utility forms: scooter helmet, QR stand with blank face, delivery crate, compact fan, filing tray, takeaway cup; consistent mini-perspective | Third-party trade dress, logos, branded cup/packaging shapes |
| Palette | Small **purpose-authored** family: warm paper neutral, deep green-ink, calm slate shadow, amber action highlight, muted tile accent. Document actual numeric swatches only after originality/comprehension study; single design-token authority | Sampling an existing game's swatches as its identity |
| Humor/motion | Micro-bob, paper tab flip, receipt stamp feedback, small face/arm changes, expressive handovers; no excessive particle effects | Unresponsive still images or whole-body slide pretending to walk |

The geometric idea ("receipt-slip + notebook-edge") is a fresh design prompt, not a claim that ordinary receipts/notebooks are protectable or unprecedented. Final silhouette, UI composition and approved asset set must pass the separate similarity review in `PRODUCTION_ASSET_IP_POLICY.md`. Do not use prompts invoking living artists or titles of games/apps as a style recipe.

## 3. Pixel scale proposal — measured proof required

- **Scene/world prototype:** logical 360×640 portrait for visual composition; select a *single* sprite family PPU after testing (candidate: **32 PPU**) and character native size (candidate: **64×80-ish pixels**). Candidate values are **not production locks** until one mobile BeautyCell and animated walk verify readability.
- **Tiles/grid:** candidate **32×32 pixels** for room construction; choose actual floor/wall grid via Golden environment test. No fractional repositioning to hide seams.
- **Camera:** project is URP/Universal 2D, so use the appropriate **URP PixelPerfectCamera**, not a duplicate Built-in pixel-perfect implementation. Detect actual URP renderer type before writing code. Verify point sampling, sprite pivots, snapping, bilinear/mipmap settings and atlas padding on target hardware.
- **Aspect ratios:** 9:16 base, plus 360×640, 390×844, 430×932 notched examples. Avoid stretching integer-pixel artwork non-uniformly; decide between crop/extra layout fields after captures, not blind scaling.
- **UI is different from scene:** keep TextMeshPro and critical interface at **native display resolution**, with appropriate CanvasScaler/safe-area, 96 reference-pixel touch target baseline, content-driven wrapping and Vietnamese diacritics. Do not render body text into a 360×640 pixel buffer and upscale it as unreadable blocks.
- **Icon/UI textures:** draw original crisp pixel frames and 9-slice safe regions; control rendered size/edge crispness. Responsive layouts must adapt, not merely magnify tiny decorative typography.
- **Color tokens:** prototype semantic `background, surface, ink, accent, warning, disabled, focus` and derived pressed/hover states. Convert to hex **once** in `MobileTheme.asset` and the art-spec palette together; do not hardcode the same value across scene scripts.

These choices must be evaluated alongside the locked GDD requirements: the character stays visible, choices are simple, time is main constraint, Vietnamese contemporary setting, five primary tabs, Career separate from Skill, and no unnecessary micromanagement.

## 4. Genuine character animation under pixel constraints

**Non-negotiable:** a sprite translated rigidly from A to B is not a walk, typing or service action. The approved character must include animated **legs/knees, arms/hands, face/eye expression and body weight** in appropriate scenes.

Planned animation set for Golden proof: `idle` (breathing/weight shift), `walk` (distinct foot plant and limb arcs), `talk`, `type`, `study`, `carry`, `kiosk-serve`, and **transition** states. Distinct clip names must connect to a single reusable rig/controller, not per-career copy-pasted controllers.

Pipeline: original turnaround sketches -> clustered pixel source layers with versioned IDs -> rig-ready sprite import / Sprite Skin bones (when quality supports it) -> articulated poses and clips -> fixed-PPU playback -> on-device capture. Rotation and deformation often introduce blurred or uneven pixel edges; design limited-pose transitions and snapped limb attachments. If rigid pixel aesthetic conflicts with skeletal deformation, use pose-authored replacement limbs/frames **with demonstrably moving joints**, retaining one common semantic animation interface. Do **not** accept slide-only motion as an alternative.

Outfit and skin/hair variants should reuse layout conventions. Reimport must preserve pivots/bone mappings. Inventory of required poses and limb state ownership belongs in the art approval record.

## 5. Golden proof scenes

1. **The player:** front/three-quarter/side with expressive idle and genuine walk/typing.
2. **Night room:** small workspace, practical helmet hook, desk lamp and focused character actions.
3. **Modern workplace:** shared workstation, water dispenser, practical career scene props.
4. **Coffee kiosk:** two actors, takeaway counter and blank QR/signage surfaces.
5. **Life UI:** original five-tab pixel chrome plus real font: cash, current activity, timeline and one clear next decision.

No illegible autogenerated Vietnamese lettering baked into textures. TMP supplies signs and labels where possible; screenshot/text legal clearance reviewed separately. Contemporary context should feel lived-in, not an exoticized tourist collage.

## 6. Migration strategy from previous M13 contract

Do not edit a single YAML field and claim style conversion. Make the W03 change as a **coherent atomic code slice**:

1. Archive/checkpoint old `ART_BIBLE_M13.md` and confirm Git identity of original spec; never delete the historical release proof.
2. Update `Assets/StartupLife/Art/_ArtDirection/art-spec.yaml` with `style_id=vn_everyday_pixel_v1`, actual approved palette, pixel sizing/import and rig contracts.
3. Update `composition.yaml`, `golden-targets.json`, `Assets/StartupLife/Data/MobileTheme.asset`, plus importer presets. Reconcile UI readability requirements and pixel-world reference scale.
4. Change `scripts/Validate-GoldenArt.py` to validate the new **specific** schema and reject old/new style ID mixing. Do not loosen the score threshold; keep the existing independent approval gate.
5. Update `Art/Approved/registry.json` style ID **without adding approved entries**. Only actual approved artifacts can later populate the registry.
6. Update `docs/ART_BIBLE_M13.md` status/index to `superseded historical contract`, then add visual and screenshot references to the production style guide/asset IDs.
7. Run exact-head static art validator; then real Unity importer, rig and device BeautyCell. If physical Unity proof is unavailable, mark status `IMPLEMENTED_UNVERIFIED`, do not call the style production-ready.

## 7. Originality approval

Each Golden category requires three independent design explorations that do **not** start from named references. Each must provide original construction/source layers, layout/shape decisions, and a provenance record. Reject near-derivative designs; choose based on distinctiveness, phone readability and consistency.

A final asset needs: license/provenance `CLEAR`; independent visual review; style score **>=15/16** and no zero criterion; clean importer/atlas; relevant motion; tested Vietnamese typography; a real Unity/mobile BeautyCell; approved registry entry. Studio production may reuse a custom-made component/shape within this game, but cannot appropriate third-party expression.

**No one can guarantee global uniqueness against every app/game in existence.** The feasible promise is original creation, meaningful independent review, transparent evidence and documented replacement/remediation for suspected infringement.

### P02-01 synchronized technical migration

Canonical specs and empty approval registry now use `vn_everyday_pixel_v1`, with provisional 32 PPU / 32px tiles / 128×160 character / 360×640 world and screen-space native TMP. The new authored UI palette is synchronized to `MobileTheme.asset` and its C# builder defaults. Actual URP PixelPerfectCamera/texture import, human originality approval and physical device visual proof remain separate mandatory production tasks. Historical M13 source is available at commit `9d1a3d3`.

### P02-02 scene-builder code follow-up

Both Editor scene builders now delegate pixel camera configuration to a single `PixelWorldCameraSetup` (URP-only `PixelPerfectCamera`, 32 PPU, 360×640, pixel snapping, Windowbox; MSAA/HDR/dynamic resolution disabled). C# numerics are cross-checked against the active pixel art spec by the Golden validator. These scripts affect *future Editor-generated scenes only*; already committed `.unity` scenes are not automatically patched. An actual Unity Editor must inspect, migrate, save and capture them. No Unity/runtime PASS implied.
