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
