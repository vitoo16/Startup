# P03-CHAR-04B — Original C02 pixel chibi exploratory source, NOT approved

**Date:** 2026-10-15; **Owner:** GPT-6 Art + Technical Director  
**Baseline:** `c7196989899d47e02eb8413ba27298a9c681021e`; engine-free CI SUCCESS.  
**Asset status:** `DRAFT_NOT_APPROVED` (all candidates). Not a Golden art approval, not a shipping character, not a Unity scene or an animated sprite.

## First-party art exploration
Created `golden-character-c02/explorations.json` with **three independently conceived silhouette families** and **nine controlled palette/expression iterations**, four separate projection recipes each (`front`, `three_quarter_left`, `side`, `back`). Source is first-party pixel rectangles and semantic components, not a converted photo, copied game/app sprite or internet reference. First explorations:
- **Nếp Gấp:** angular irregular fringe, layered cardigan, slim diagonal commuter satchel.
- **Sương Sớm:** looped small clustered curl masses, rolled practical sleeves, simple canvas tote.
- **Góc Phố:** lifted off-center hair block, cropped zip outerwear, compact backpack.

Neutral adult workplace proportions and clothes, not child-coded and not assigning skills, pay, friendliness, morality or competence by appearance. Contemporary Vietnamese commuting cues from practical bags/casual clothes; no logo/text, flag, stereotype, copyrighted character or commercial reference. Multiple perspectives are independently painted view cases, not automatic flip. Intended current pixel proportions **2.303 heads**, source bounds 128×160 / 32 PPU. Root and **21 semantic joint/layer IDs** align with future reusable rig (face, hair front/back, torso, pelvis, bilateral upper/lower limbs, hands, feet and detached outfit/accessory overlays). This is an editable procedural source; not yet a Unity bone-animated/PixelPerfectCamera verified prefab.

`Render-GoldenCharacterC02.py` emits a pure-stdlib 9×4 color contact sheet, source/render geometry stats and optionally per-part PNG layers per view. C01 remains archived in quarantine unchanged; only its PNG writer was extracted to a reusable `PixelPng.py` without altering C01 artwork. A strict C02 validator and negative mutations run alongside old immutable C01 tests. The contact sheet will be uploaded as engine-free CI artifact for visual candidate review.

## Production gates still required
- Independent human similarity, copyright/license and cultural/accessibility review; nine program variants are **not** a certified art shortlist.
- Hands-on anatomy/perspective and silhouette aesthetics, any overlap and coherence corrections after actual sheet viewing.
- Unity sprite import, pivots, four-view rig/real joint motion idle/walk/type/talk, device BeautyCell; **all NOT TESTED**.
- Explicit owner review of shortlist/render; silence never means approval.
- Leave `Assets/StartupLife/Art/Approved/registry.json` **EMPTY**. No scene, approved resolver, save schema, player stats or runtime preview is altered.

## Closeout
**Skills used:** startup-life-session-orchestrator, vietnam-art-direction, startup-life-asset-quality-gate, sprite-editor, 2d-pixel-perfect, unity-cli.  
**Acceptance:** source 3 silhouette directions × 3 iterations × 4 separate projected views; original editable layers; clean pivots; 2.2–2.4 head target; no unapproved sprite release.  
**Tests:** source validator + mutation cases and existing C01 renderer regression in engine-free CI (verdict inspect run).  
**Visual evidence:** CI contact sheet is code-generated preview only, not Unity/device/artist signoff.  
**Save impact:** NONE.  
**Known limitations:** concepts may require manual silhouette/angle/anatomy correction, no fully authored animation frames yet; no independent art/IP approval.  
**Files changed:** new C02 data/source renderer/validator/tests, shared PNG writer refactor, old C01 source preview renderer refactor, workflow artifact, this evidence document.  
**Commit:** see Git history.

## Source preflight correction
The C02 record has now been added to the canonical provenance manifest as **DRAFT** (no fake rights or approval dates). The C02 validator checks the six boolean approval flags explicitly, rather than mistakenly treating the nonempty path of the approval registry as an approval. The RGBA rasterizer validates bounds even on externally mutated layer clusters, preventing clipping/silent data loss. This closes the source gate and retains all previous independent review restrictions.

## Owner visual direction feedback — 2026-10-16

**REVISE_REQUIRED — do not promote C02 to a Golden character.** The owner
explicitly rejected the C02 silhouette as mature-looking and Euro-American
rather than contemporary Vietnamese/SEA-feeling pixel chibi. This supersedes
earlier assistant recommendations to shortlist "Sương Sớm". The art is not
suitable for human approval as-is. `owner_visual_review` is recorded in the
C02 quarantined source manifest; validator and mutation checks enforce that
the visual rejection remains release-blocking. No stats, gender balance,
approved registry or production asset was changed.

Art team next works only from fresh C03 first-party source and the
`docs/production/CHARACTER_C03_VIETNAMESE_CHIBI_REDIRECTION.md` direction,
subject to owner review of actual rendered previews; no ethnic caricature.
