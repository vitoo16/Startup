# C03 — Modern Vietnamese pixel chibi: corrective art direction

**Status:** ART REDIRECTION / PROPOSAL; **no asset approved**.  
**Date:** 2026-10-16. **Authority:** product owner feedback.  
**Supersedes:** C02 character silhouette recommendation. C01 and C02 are quarantined draft history.

## Problem observed

C02 reads as older, angular and Euro-American rather than the requested
Vietnamese young-adult, cute native-pixel visual identity. This is a failed
aesthetic and cultural-context gate even if C02 technical validator passes.
Never report `CI SUCCESS` as art acceptance. Avoid anthropological facial
checklists or claiming ethnic identity is identifiable from fixed facial features.

## New C03 non-negotiable visual contract

- Young working **adults** (approximately 20s–early 30s, without assigning
  abilities to age), **2.2–2.4 heads tall; target 2.3**, shorter arms/legs;
  friendly rounded and expressive faces, soft full cheeks, clear small
  brows/eyes and distinct friendly mouth shapes, no severe angular
  middle-aged masculine jowls or gray/white hair unless intentionally
  designing an older NPC.
- Read as contemporary Vietnamese *daily-life game characters* through
  hair, streetwear, work bags and **contextual environments**. Offer natural
  Vietnamese skin-tone range without treating any tone or eye contour
  as a mandatory racial marker. Varied dark hair (black, dark brown),
  practical youthful styling, gender-neutral careers and shared rig.
- Native hand-authored **clustered pixel shapes** at a fixed grid: sharp
  pixels, no anti-alias, smooth blur, pixelated photo, AI upscale,
  fake pixel noise or pseudo-3D airbrush. Pixel silhouette must still
  look good at 1× and tiny portrait-phone viewing.
- Contemporary Vietnam city/context cues: a small `hẻm` apartment
  threshold, neighborhood coffee cart, university/work laptop tote,
  understated local commuting items. No mandatory `nón lá`, dragons,
  rice paddies, flags, stereotyped eyes, faux ethnic costume or
  garbled Vietnamese writing. Any typography stays in TMP.
- Expression sheet at minimum neutral / small smile / focused /
  delighted. Four independently drawn projections: front,
  three-quarter-left, true profile and back (never flip front).
  Hands, elbows, knees, face and clothing overlays follow
  shared pixel-snapped semantic rig. Footplant/real knee motion,
  no rigid sliding.
- At least **3 clearly distinct C03 silhouettes**, 8–12 controlled
  variations, diverse gender presentations and skin tones, while
  maintaining a coherent single family style. Do not merely recolor C02.

## Explorations for the *next* C03 actual preview

- **Sáng Ngõ Nhỏ:** soft short dark hair with uneven clustered fringe;
  airy short-sleeve shirt, relaxed pants, commuter sling, warm smiling face.
- **Tan Ca:** neat dark ponytail / shoulder-length layered hairstyle,
  comfortable linen-like shirt and canvas tote, approachable small smile.
- **Quán Đầu Phố:** dark wavy undercut or rounded curls, understated
  overshirt with sneakers and compact backpack; never caricature an
  ethnic or occupational identity.

These are work directions, not approved names, gameplay gender roles or
automatic registrations. The concept art must visibly differ.

## Owner-approved design DIRECTION — 2026-10-16 (not asset approval)

Product owner accepted the revision: use the *daily-life, casual-work* silhouette
family as the starting language, merge the softer, youthful hair/face treatment
of the first exploratory concept, and REMOVE repetitive AI gloss. The earlier
assistant estimate of `40–50%` reduction is qualitative only, **not** a
technical percentage or acceptance threshold. Do not trace, upscale or
pixelate previous AI image samples as sprite sources.

**Specific anti-slop design gates:**

1. Native hand-authored pixels only. At true 1× resolution,
   silhouette, hair and eye/mouth expressions must be clear. Export may
   be nearest-neighbor integer scaled for review; never bilinear or
   fake pixel overlay.
2. No large generic anime pupils, bloom, airbrush, faux 3-D, shiny
   gradient hair or heavily repeated symmetrical face templates.
3. At least three **meaningfully distinct** body/hair/clothing silhouettes
   evaluated in monochrome; recoloring the same body does not count.
4. Ensure a neutral/soft smile/focused/delighted expression sheet while
   remaining young **adult**; appearance choices and gender never encode
   abilities or character value.
5. Everyday contemporary Vietnamese life is communicated by believable
   clothes, bags, spaces, routines and later environment composition;
   not stereotyped eye shapes, forced nón lá or decorative fake signage.
6. Separate clean hair/head/torso/forearm/hand/thigh/shin/foot layers
   and pivots, no whole-body slide as walk, with four authored views.
7. Human art-direction approval requires **the actual PNG at 1× and
   enlarged nearest-neighbor**, not only a style prompt, CI PASS or a
   colorful concept sheet. Missing approval leaves the registry empty.

A quick first-party procedural low-res sketch was inspected in this
session and **rejected internally** as too crude and insufficiently
differentiated; it must not be committed as a Golden candidate or
mistaken for art approval. The C03 *direction* is approved, **not** any
specific sprite or art sheet.

## Review acceptance / veto

Art Director/owner receives an actual PNG contact sheet first (not just JSON).
Reject if older Euro-American styling returns, if any design is a close
lookalike of online game characters, or if recognizably Vietnamese daily-life
context is replaced by pan-Asian decoration.

Use existing scorecard and an independent IP/cultural reviewer. C03 only
becomes `APPROVED` after owner aesthetic signoff, independent originality/
rights review, authentic Unity layered animation/PPU import, and on-device
BeautyCell. No auto promotion, no modification of
`Assets/StartupLife/Art/Approved/registry.json`.

## Existing project protections

Preserve `AppearanceId`, traits independent of sex, receipts/save
replay and old C02 frozen evidence. Use the existing 21 semantic joint
names and `CharacterVisualComposer` and `CharacterVisualAssembler`
instead of a competing character renderer. No copy/paste external
sprites, no brand/logo, no new economy effects.

## Native-resolution C03 handoff (2026-10-16)

The first-party renderer supports paired inspection outputs without touching the approved registry:

```bash
python scripts/Render-GoldenCharacterC03.py --output artifacts/C03-turnaround-4x.png --native-output artifacts/C03-turnaround-1x.png --fronts artifacts/C03-front-variants-4x.png --report artifacts/C03-report.json
```

The `--native-output` sheet contains the same twelve 64×80 native canvases at **exactly 1×**, surrounded only by sheet gutters. Compare it against the default integer nearest-neighbor 4× sheet; review each character's hair silhouette, adult proportions and distinct outfit geometry at 1×. Rendering, source generation or static validation alone **does not imply visual approval**. Ask owner `APPROVE / REVISE / REJECT` only with actual visible PNG previews. The production registry remains untouched until independent IP review, rig animation and device BeautyCell gates are met.

## C03 source-render review — internal technical/art QA, 2026-10-16

**Source inspected:** `scripts/Render-GoldenCharacterC03.py` blob `72ff442e8799d8546b1e08087b18dd322953967e`. Recreated twelve 64×80 native-grid turnarounds, twelve face-expression previews (three styles × four expressions), nine appearance front variants and **252 layered PNGs** (three styles × four views × 21 semantic parts). Generated report remains `DRAFT_NOT_APPROVED`.

- **Technical pixel evidence:** 1× RGBA contact sheet SHA-256 `3162dc82bb909ddb4e8d0a7122dbabe0fbbf05288c561debd95d23d4888027de`, 4× SHA-256 `9b9057ce89a6e836825b45c73ea203d300f080067527f364171e7e32aa2d0e15`. Per-sprite 4× nearest-neighbor pixel blocks were checked against exact 1× and matched. Asset generation and this pixel check are **not** Unity, device, independent IP or aesthetic approval.
- **Internal art QA: REVISE RECOMMENDED (not the owner's decision).** The front/three-quarter/profile views are visually under-differentiated; prominent square forehead/hair highlights can read as overlarge block eyes; the three body silhouettes still look too alike, especially at 1×. Facial acting lacks clean separation across all expressions. Further source-native silhouette, true-profile, fringe/face and expression re-authoring is required **before** rig animation or production registration.
- **Owner review state: AWAITING_OWNER_REVIEW.** Actual PNGs were rendered for conversation review; these outputs are not committed as approved Unity assets or shipped. The approved registry and save schema remain unchanged. No Unity playback/BeautyCell or independent rights clearance was executed.

Reproduction uses the `--native-output`/`--output` arguments recorded above. The generated previews may be shared for **REVISE / REJECT / direction feedback**; do not infer owner approval from silence.

## OWNER VETO — C03 native-grid render (2026-10-16)

**Decision: REJECT CURRENT C03 RASTER.** The owner's feedback is that the current
pixel-character render is stylistically incorrect. This supersedes the earlier
internal `REVISE RECOMMENDED / AWAITING_OWNER_REVIEW` on this specific rendered
source. It does **not** revoke the approved overall modern-Vietnamese daily-life
character direction.

Visual verification of the existing first-party `64×80` assets finds:
- Dark, square hair/fringe clusters invade the facial read and can resemble
  extremely oversized eyes. This fails the requested softer, deliberately
  pixel-authored charming adult chibi shape language.
- `back` views are derived from nearly the same front head, fringe and garment
  geometry; visually comparing total canvas RGBA yields only ~0.6–2.1% changed
  pixels between front/rear for all three silhouettes.
- A separate occupied-pixel diagnostic reports front/rear differences of
  **5.86%, 1.55%, 1.60%** respectively. A native raster, rectangular pixels,
  distinct file names or 21 populated layers does **not** prove independently
  authored views, visual craftsmanship or good sprite animation.
- `side` and `three_quarter_left` reuse near-front anatomy rather than
  a credible profile, while facial expressions and family silhouettes are
  insufficiently distinct at 1×.

**Engineering follow-up:** `python scripts/Inspect-GoldenCharacterC03Views.py`
returns a nonzero status on these invalid turnarounds. This extra structural
heuristic is intentionally **not** a substitute for the human art gate; even
a passing report is not asset approval. The older native-source validator
confirms only low-level RGBA/grid/provenance invariants, not art quality.

**Next visual production gate:** Do not create more palette/face variants or
start rigging this C03 shape. Re-author **one carefully composed front-view
character at native 1×** first, with readable hair/eyes, distinct uneven
pixel clusters, believable short-adult posture and recognizable everyday
workwear. Show the exact native PNG and pixel-precise nearest-neighbor
enlargement to the owner. After feedback, independently draw a genuine side
profile and rear (new head/hair/garment silhouettes, not front projections
with facial features removed); then expand to three differentiated characters
and expressions. Do not use AI pixel-filtered photos, traced internet sprites,
fake pixel overlays or stereotype-based facial markers.

**Approval / shipping state:** `DRAFT_NOT_APPROVED`, owner visual verdict
`REJECTED`, independent IP clearance not performed, Unity animation/device
BeautyCell not performed. `Assets/StartupLife/Art/Approved/registry.json`
must remain unchanged.

## C03-R1 — first native-pixel re-authoring (2026-10-16)

**Owner verdict on the previous C03 raster remains REJECTED.** This is an
independent, **front-facing only** first revision; it does not rehabilitate
the rejected procedural turnarounds or imply approval of the new art.

Source of truth: `Assets/StartupLife/Art/Source/Characters/golden-character-c03/r1-front-draft.json`.
The `commands` list records **74 authored native pixel contours and hand-placed
horizontal pixel clusters**, each with an explicit palette role and semantic
rig-layer destination. The generic renderer does not use the rejected
oval/rectangle character generator. No photographs, network sprites or
image-to-pixel filters were used. This design is still subject to independent
IP similarity review before shipping.

Export exact native 64×80 RGBA, owner-review 1×, 4×, and 8× nearest-neighbor:

```bash
python scripts/Render-GoldenCharacterC03R1.py --output-dir artifacts/c03-r1
```

Native PNG SHA-256 of the currently reviewed R1 source:
`2aa58b165ff6183a0c288340dd53783ce286eba75dfcd6004ed6f4f8f0e680e6`.
At this revision all 21 semantic rig layers have authored pixels;
this **does not establish working motion** or that pivots are approved.
Review enlarged and true-size PNG together. Technical pixel/scale diagnostics
are not a proxy for visual quality.

**Required human review now:** Character looks appropriately youthful and
adult, actually cute at 1×, pixel-cluster silhouette reads cleanly, brows and
eyes are not covered by bangs, chin does not look bearded, clothing feels
contemporary Vietnamese daily life without ethnic stereotypes. Ask for
**REVISE / REJECT / APPROVE_DIRECTION** on this *single* front view, not approval
of a shippable rig. No extra views, palette recolors, animation or registry
promotion until owner direction is resolved.

**Open:** owner visual decision, true side and independent rear geometry,
four expressions, at least two further distinct personalities,
Unity 2D articulated motion, device BeautyCell, originality/IP clearance,
full 128×160 production-grid/pivot alignment. Unity/phone tests **NOT RUN**.
Gameplay balance, save schema and approved registry unchanged.

## C03-R4 concept-only handoff — 2026-10-16

Owner feedback on R2 (left) / R3 (right): **APPROVE DIRECTION — REVISE AGAIN**.
Keep R2's sturdier body/legs/shoes and R3's softer friendly face/hair; further
polish signature asymmetric fringe, rounded jaw, eyes/smile, slight weight-shift,
jacket hem, compact sling bag and wider shoes. **This is NOT approval of a sprite
or permission to expand to turnaround/animation.**

The subsequent generated illustration is a 1122×1402 RGB **concept visual only**,
not an authored 64×80 RGBA sprite and not committed as pixel artwork. See
[`docs/evidence/production-art/C03-R4-CONCEPT-HANDOFF.md`](../evidence/production-art/C03-R4-CONCEPT-HANDOFF.md)
for concept provenance and the native-source acceptance checklist.

**Current gate:** `CONCEPT_ONLY_AWAITING_NATIVE_PIXEL_REAUTHORING`.
R1–R3 history preserved. No R4 front sprite source, approved-registry entry,
Unity/device evidence or independent IP clearance yet.

## Native C03-R4 front polish ready for owner review — 2026-10-16

**Update, superseding the earlier "concept-only" status above:** C03-R4 now
has one independently authored native **64×80 RGBA front** sprite source at
`Assets/StartupLife/Art/Source/Characters/golden-character-c03/r4-front-draft.json`
and a deterministic script `scripts/Render-GoldenCharacterC03R4.py` to
export exact 1×/8× previews, silhouette, R2/R3/R4 comparison and all 21 parts.

Technical local validation: **PASS** for actual height 72px, head 32px,
2.25 heads total, binary alpha, 21 nonempty layers, exact NN 8×;
older R3 front-face/hair authoring remains unchanged. Details and hashes:
`docs/evidence/production-art/C03-R4-review.md` and
`C03-R4-technical-evidence.json`.

**Owner direction** was approved for a final-polish *exploration only*.
**Actual R4 sprite status:** `DRAFT_AWAITING_OWNER_REVIEW`, not `APPROVED`.
Do not promote, bind Unity runtime, create additional views, start animation
or call IP rights cleared without explicit independent/owner gates.

## Owner correction R4 → staged personality pass R5 (2026-10-16)

**Owner decision:** **C03-R4 REVISE REQUIRED**, even though native RGBA/8×
checks passed. R4 became too generic: facial emotion and signature fringe
flattened, the head/torso did not share one shape language, pose looked like
a mannequin, and shoe/strap details did not rescue the whole character.
Do not promote R4 or treat its previous `APPROVE_DIRECTION` as acceptance
of the raster. Technical symmetry/ratio checks are only a supporting gate.

**Next art direction:** Restore the playful, asymmetric R2 *intent* without
copying its pixel shapes. Evaluate R5 **Silhouette → Face & Hair → Full
Character**, each at actual native 1× and integer-nearest 8×. Prioritize an
expressive small eye/brow rhythm, diagonal fringe (not a cap), one weight-
bearing leg and one relaxed leg, genuinely purposeful strap-grip hand.
Reduce irrelevant pixel noise. Draw one front view only.

R5 reproducible draft source: `Assets/StartupLife/Art/Source/Characters/golden-character-c03/r5-personality-front-review.json`.
Review/QA: `docs/evidence/production-art/C03-R5-review.md` and
`C03-R5-technical-evidence.json`.

**Current R5 gate:** `DRAFT_AWAITING_OWNER_REVIEW` — aesthetic personality
not signed off. All 21 existing semantic names are present in the study
source, but full rig-ready layers, Unity sorting/pivots, independent IP and
device BeautyCell are **NOT approved**. No other views, cosmetics, animation,
save/gameplay or approved registry changes.

## Owner R5 verdict and native R5.1 final personality pass — 2026-10-16

**Owner R5 decision: REVISE, not REJECT.** R5 is currently the strongest
personality direction, yet its monochrome silhouette is unmemorable, its
left sweeping fringe still merges into a dark block at 1×, and the right
hand appears to rest on the chest rather than grasping the strap. Its
stance also lacks clear weight distribution. R5 is **not** a front-view
lock or Golden approval.

R5.1 is a **controlled final polish of one FRONT character**. Keep the R5
facial coordinates and slightly asymmetric smile, exactly the R5 palette,
olive jacket, cream T-shirt, blue-grey trousers, brown bag, 64×80 native
grid and 2.25-head ratio. Retouch only left fringe clusters, thumb/palm/
finger-strap geometry, shoulder/pelvis and planted/relaxed leg geometry.
All R1–R5 historical sources remain immutable.

New source: `Assets/StartupLife/Art/Source/Characters/golden-character-c03/r5-1-personality-front-review.json`.
Renderer: `scripts/Render-GoldenCharacterC03R5_1.py`.
Review and QA: `docs/evidence/production-art/C03-R5.1-review.md`;
`C03-R5.1-technical-evidence.json`.

The three-stage review **Silhouette → Face & Hair → Full Character** is still
mandatory at true 1× plus exact NN 8×, and the owner must explicitly decide
`APPROVE_DIRECTION / REVISE / REJECT`. R5.1 remains
`DRAFT_AWAITING_OWNER_REVIEW`, with IP, independent animation rig, Unity
and device QA not performed. The approved registry is unchanged and no
other views, expressions or outfits may proceed before the owner verdict.

## C03 Neutral Idle anatomy gate — R5.1 hand rejected (2026-10-16)

**Owner Art Director verdict: R5.1 front hand pose REVISE — chest-high
strap grip rejected.** This supersedes any previous implication that
R5.1 grip was aesthetically approved; the remaining portrait/body
direction remains in force.

Exactly **two first-party native front-view candidates** have been created:
`C03-IdlePose-A.json` (Relaxed Idle, preference) and
`C03-IdlePose-B.json` (Casual Idle fallback), under
`Assets/StartupLife/Art/Source/Characters/golden-character-c03/`.
These preserve R5.1 head/face/hair, outfit, legs, palette and 2.25-head
proportion. Arms/wrists/palms, shoulder contour and B's hip bag are
redrawn from native shapes. The front bag strap must stay visually
continuous with no hand on chest. `hand_r` renders **before**
`accessory_front` in both draft sources; this sort order is **NOT**
verified in live Unity.

Owner handoff, actual 1× and pixel-exact 8× preview evidence:
`docs/evidence/production-art/C03-IdlePose-AB-review.md`.
Status `A_AND_B_AWAITING_OWNER_REVIEW`; select **A / B / REDESIGN**.
On rejection of both, STOP micro-polish and seek new silhouette-level
arm/bag layout direction. No R5.2, R5.3, turnaround, animation,
production prefabs or asset registry promotion. Technical tests do
not qualify as Golden art approval.

## CURRENT OWNER LOCK: exact image selected for C03 — 2026-10-16

**This is the latest owner directive and supersedes all earlier R5.1/idle A/B
review requests:** Owner has chosen their uploaded image
`image(20261016-093800).png` as the **preferred C03 character design**.
Use this visual master instead of the existing procedural 64×80 R5.1 hand-grip
sprite or neutral idle A/B drafts. Do not restart micro-polish or present
additional A/B pose-choice gates.

The authoritative selection/provenance record is
`docs/evidence/production-art/C03-OWNER-SELECTED-VISUAL-MASTER.md` with
machine-readable `C03-OWNER-SELECTION.json`; the owner's exact PNG has
SHA-256 `457478717a96f5bcfeddc5976af4c3034ddd7cf9086e988b093707e039d928aa`.
It is a 1122×1402 RGBA image **with fully opaque beige background**, not
a native 64×80 cutout or rig, and **its bytes are not yet checked into Git**.

**Decision split:** `OWNER_SELECTED_VISUAL_MASTER` for art direction;
`NOT_PRODUCTION_READY` for imported game asset. Next technical pass is to
version exact original PNG, resolve clean alpha/native grid fidelity without
changing identity, build semantic rig layers and verify Unity/motion/IP/mobile.
Do **not** use any unapproved image in the runtime or silently treat this as
Golden release approval. Approved Registry and game systems remain unchanged.

## Follow-up to owner-selected image — limited neck/ears/legs/pose polish (2026-10-16)

An owner-directed **small polish** of the selected friendly C03 illustration
was generated later in the conversation. Do **not** mistake this for approval
of a native game sprite, and do not erase the earlier exact-image selection.

The reproducible **selection/change decision** (not PNG bytes) is now recorded in
`docs/evidence/production-art/C03-LATEST-MINOR-POLISH-REFERENCE.json`
and `C03-LATEST-MINOR-POLISH-REFERENCE.md`. The new illustration is
`chibi_pixel_traveler_with_satchel.png`, **1149×1368 RGB**,
SHA-256 `64bb3520c38537d8b0ebaed1daac9a879215b81564642eee088d9e0cbf98807b`.
Its exact binary is **NOT YET COMMITTED TO GIT**. The earlier owner selection
retains its independent provenance record and remains the approved *direction*
baseline until the specific latest minor polish is visually confirmed.

The only requested changes are: shorten exposed neck, tuck ears, refine
trousers/sneaker shape and gentle foot turn, and subtly offset shoulders/hands.
Lock the friendly face, distinctive fringe, olive jacket, cream tee, blue-grey
trousers, brown sling bag and front-only scope. Do not introduce another
mascot, alternate A/B poses, or automatic native resampling.

The image has an opaque background, no 64×80 native authored source and
no 21-part rig. Do not present Git text metadata or a render as a committed
pixel sprite. Commercial originality, animation, device and Golden review gates
remain open; approved registry unchanged.

## Latest owner edit — hand must sit BEHIND the bag (2026-10-16)

After the selected-character neck/ear/legs polish, owner requested a **single
localized occlusion correction**: the hand beside the brown crossbody satchel
must remain *under and behind* the satchel body/flap, with only a small skin
sliver visible below its lower edge. This **supersedes all older depictions**
where the hand is in front of/on the bag. Do not redesign the face, hair,
outfit, bag strap, feet or stance as part of this fix.

Source-of-truth for this correction:
- `docs/evidence/production-art/C03-HAND-BAG-OCCLUSION-REVIEW.md`
- `docs/evidence/production-art/C03-LATEST-MINOR-POLISH-REFERENCE.json`
- `scripts/Verify-C03HandBagReference.py`

Latest exact **draft concept**: `pixel_art_boy_with_brown_satchel.png`,
1171×1343 RGB, SHA-256
`d2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e`.
Its binary is **not committed yet**; only its verified metadata and occlusion
contract are in the repository. Do not report full art sync, approval or
runtime integration until the PNG itself is in Git and verified.

For native semantic rigging, hand-under-bag is a **localized layering rule**:
bag `accessory_front` must be in front of affected hand in the overlap area.
Unrelated hand and prop interactions have their own sorting contracts. No
change to approved registry, saved games or gameplay. Unity, device, IP and
owner final-art gates remain open.

## OWNER APPROVAL — exact front appearance, hand behind bag (2026-10-16)

**Latest owner verdict: APPROVED VISUAL DIRECTION** for the single front-view
C03 image with the satchel fully covering the relevant upper hand pixels and
only a small natural hand segment below it. This supersedes older requests to
choose between R5.1/idle A/B, or continue face/hair micro-polish. Keep the
selected face, hair, clothing, shoes, body silhouette and bag pose fixed.

Implementation reference is the exact generated visual
`pixel_art_boy_with_brown_satchel.png`,
SHA-256 `d2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e`.
The image is an opaque 1171×1343 RGB concept, **not a native 64×80 layered sprite**;
its exact binary is still not in Git, so the asset has not been registered,
rigged or Unity/device tested.

A separate draft authoring handoff now exists:
`Assets/StartupLife/Art/Source/Characters/golden-character-c03/C03-owner-front-rig-contract.json`
and `scripts/Validate-C03OwnerRigHandoff.py`. The 21 original semantic IDs are
preserved. Within the front-view bag overlap, `accessory_front` composites
over `hand_r`; the hand stays authored in its own part for future joint motion.
Do not use the `CoveredPartIds` mechanism to delete the whole hand. The exact
anatomical left/right part assignment must be verified at Unity rig binding.

Eight engine-free synthetic regression checks passed locally; the actual
21 native layers **do not exist yet**. See
`docs/evidence/production-art/C03-OWNER-APPROVED-FRONT-RIG-HANDOFF.md`.

**Next blocking gates**: version exact concept PNG from the handoff ZIP,
independently author crisp native front pixels and layered hand+bag, check
real 1×/8× occlusion, rig pivots and actual motion in Unity, independent
rights/IP clearance, device BeautyCell, final owner Golden approval.
Approved registry, runtime bindings, save/economy and other views remain unchanged.

## Post-approval technical continuation — draft cutout and stricter rig occlusion (2026-10-16)

The owner-approved C03 front visual **has not changed**. The team created
an optional high-resolution transparency draft using
`scripts/Prepare-C03OwnerConceptCutout.py`, for foreground isolation
QA only. It preserves the original RGB appearance and supplies a separate
binary-alpha mask. This output is **not** approved pixel art or production
rigging. Details, real image bounds and evidence:
`docs/evidence/production-art/C03-CONCEPT-ISOLATION-REVIEW.md`.

The owner-approved concept source PNG is still NOT present in Git. Its
strict hash-gated handoff ZIP and staging script provide the correct upload
path without calling it uploaded. The C03 partial-occlusion validator now
rejects skin appearing above/through the bag front; this is applied to the
specific affected hand, not an unrestricted global hand sorting rule.

Do not unlock additional character variants or register the high-resolution
draft. Remaining release gates: binary source commit, manually authored native
pixel layers/pivots, actual localized bag mask and overlap, Unity joint motion,
phone BeautyCell, independent IP clearance and owner Golden art review.

## Production continuation — first native 128×160 C03 front (2026-10-16)

The high-resolution front-view C03 selected by the owner remains **the
authoritative design reference**; face/hair/outfit redesign is not authorized.
A first **manually authored native 128×160 sprite** is now available in Git
as DRAFT art source. Unlike the prior concept cutout this is not a resized
HD reference, but its visual fidelity still requires an **independent owner
native-render review** before animation and other camera views.

Sources: `C03-native-front-source.json` and SHA-pinned
`C03-native-front-pixel-runs.lzma.b64` under the character source folder;
reconstruction: `scripts/Render-C03OwnerNativeFront.py`.
The native output has all canonical 21 layers, and a separate bag mask
measuring **191 hand pixels behind the bag, 24 visible below, 0 incorrectly
exposed above/through the bag**. Its actual 147-pixel height and 66-pixel
head landmark yield **2.227 heads tall**. It uses the production 128×160
grid and 32 PPU; no generic PNG resampling or new runtime renderer.

Review, commands, provenance and QA:
`docs/evidence/production-art/C03-NATIVE-FRONT-128-REVIEW.md`.
Native-art verdict remains `DRAFT_AWAITING_OWNER_NATIVE_VISUAL_REVIEW`.
The exact earlier high-resolution owner concept PNG has not yet been
versioned in Git. No Unity pivot, independent joint motion, mobile BeautyCell,
IP clearance or registry promotion. **Do not start other views or animation
until owner reviews the authored native translation.**

## C03 approved native front — 16-frame idle and Unity authoring handoff (2026-10-17)

The native front direction already explicitly approved by the owner is
**frozen for identity**; no new hairstyle/face, outfit, additional camera
angles, or production registry promotion in this implementation slice.

New draft tooling:
`scripts/Preview-C03IdleLoop.py` renders 16 genuine hierarchical joint
rotation frames from the existing 16-joint/21-layer native C03 source.
Its contract is `C03-front-joint-plan-DRAFT.json/idle_loop_proof`,
with eight limb connections tested frame-by-frame (maximum 1 native pixel
gap), continuous root-fixed motion, preserved bag-over-hand occlusion,
native bounds and non-duplicate loop frames. A separate
`scripts/Export-C03UnityRigHandoff.py` exports Unity transform/renderer
authoring offsets with 32 PPU, Point/no-mip import requirements and explicit
sorting. It **does not edit Unity assets, .meta or scenes**.

Local 16-frame pixel-probe evidence: 128/128 contact checks PASS, changed
pixels between adjacent frames 82–466, hand/bag overlap 191 px, 24 px exposed
below and none exposed through bag. This is **engine-free** evidence only;
actual Editor SpriteEditor capability check, Unity import/pivots, runtime
animation, mobile BeautyCell, independent IP and final Golden signoff
are **NOT RUN / OPEN**. Record and reproduction:
`docs/evidence/production-art/C03-IDLE-16-FRAME-HANDOFF.md`.

The exact approved original concept PNG binary remains missing in Git. Do
not confuse a drafted Unity authoring manifest with a working prefab or the
unchanged Approved Registry with a production integration.

## Owner animation veto — C03 native Idle v1 (2026-10-17)

**Owner verdict on 16-frame C03 Idle animation: REJECT.** The front
character's *appearance direction* remains approved, but the mechanical
motion from `Preview-C03IdleLoop.py` is artistically invalid. Technical
checks from the earlier proof (16 frames, seam adjacency, sorted bag-over-hand,
point sampling, per-frame changed pixel count) **do not** overrule this
explicit Art Director veto.

The former cosine joint oscillation over full-canvas inverse-sampled
layers made the head/hair wobble and pixel contours crawl. Recomputed
native RGBA regional max changes relative to frame 0:
**head 1,317 px**, torso 130 px, foot 0 px; row-normalized motion
ratio **5.81×**. This is not a breathing Idle. The historical
renderer now fails closed unless explicitly invoked with
`--archive-rejected`; historical tests require that deliberate opt-in.

Replacement production direction is **hand-authored front pixel KEY POSES
with real timing/dwell**, not another continuously rotated cosine loop.
The motion art gate is
`Assets/StartupLife/Art/Source/Characters/golden-character-c03/C03-idle-v2-art-direction.json`;
preflight validator `scripts/Review-C03NativeMotion.py`.
The first owner review should show isolated **rest/inhale/blink native
keyposes** at 1× and integer 4×, then a correctly timed loop after
keyposes are approved. Keep the feet planted, stable face/hair silhouette,
natural upper torso movement, and the hand BEHIND the brown satchel.

**Release still BLOCKED.** No new Idle animation is yet approved or
implemented in Unity. No game runtime, save, registry or owner-selected
front appearance was changed. Detailed decision and reproducible diagnostics:
`docs/evidence/production-art/C03-IDLE-V1-REJECTION-AND-V2-REAUTHOR.md`.
