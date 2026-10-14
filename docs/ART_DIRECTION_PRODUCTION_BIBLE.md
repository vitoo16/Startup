# STARTUP LIFE — ART DIRECTION PRODUCTION BIBLE
**Status:** Active design direction; NOT Golden-asset approval or Unity/device verification. **Date:** 2026-10-15. **Owner:** GPT-6 Art Director / Technical Director.  
**Authority:** `PRODUCTION_PIXEL_STYLE_CONTRACT.md` (technical style), `PRODUCTION_ASSET_IP_POLICY.md` (rights), `GDD.md` (game rules). Style ID: `vn_everyday_pixel_v1`.

## 1. Core visual promise
**Một cuộc đời nhỏ, những giấc mơ lớn.** Original cute pixel art portraying a modern Vietnamese adult's career, study and entrepreneurship journey. Characters are short, round, expressive, mature in context; gameplay scenes are visually alive; UI is legible on narrow portrait phones. Differentiate with first-party silhouettes, original clustered pixels, restrained receipt/notebook-inspired UI, human movement and believable contemporary Vietnamese places. Do not copy known game/app designs.

## 2. Character visual contract
- **Target proportions:** 2.2–2.4 total heads; nominal **2.3** (~43.5% head height). Overrides historical 2.5–2.8/2.6 suggestions. This is an ART TARGET pending actual Unity/device proof, not an already-compliant prototype.
- Large soft square-rounded head, short torso and limbs, distinct knees/elbows and hands, slightly oversized shoes; characters represent **adults**, not infantilized children.
- Construct three independently conceived first-party silhouettes with asymmetric but practical hair/coat/bag/shoe details. No tracing, recoloring, pixelizing, or near-cloning third-party characters or online sprites.
- Views: **front, three-quarter-left, side, back** with stable identity; right-side mirrors must handle asymmetric gear correctly.
- Expressions: neutral, happy, focused, worried, tired, determined. Outfits: casual home, smart casual office, evening study, practical F&B workwear, founder.
- Shared layered original rig: hair_back/front; head; face/eyes/brows/mouth; torso/pelvis; both upper arms, forearms, hands, thighs, lower legs, feet; outfit/attachment layers; stable pixel-snapped pivots and semantic joint names.
- Animation: idle breathing/blinking, walking with real planted-foot/knee/arm movement, sitting, talking, typing, studying, carrying, serving and celebrating. Whole-body sliding is **not** walking. Pixel-snapped replacement-limb poses are allowed if skeletal deformation blurs clusters.
- The existing `Art/Source/Characters/golden-character-c01` is a **draft built to the older proportions**, only one view is documented. It requires redesign, review and physical proof; do not mark it approved based on this document.

## 3. Responsible portrayal and anti-discrimination
Give players/NPCs varied natural skin tones, hairstyles, body silhouettes, presentations, ages, outfits and roles. **Never encode competence, profession, earning power, intelligence, ethics or comic inferiority** via gender, skin tone, body size, disability, class or Vietnamese region. Outfit conveys function, not stereotype. Comedy comes from workplace situations. Evaluate sensitive depictions with an independent human cultural/accessibility reviewer. Adult career characters must remain adult-coded even at small chibi proportions.

## 4. Native-pixel contemporary Vietnam environments
- **No photo-to-pixel filter** on internet images; no online tiles, game screenshots or downloaded sprite sheets as production drawing input. Write an original locality brief, draw independent tile geometry and manually authored pixel clusters with coherent 2D mini-perspective.
- Each room: 2–4 plausible Vietnamese local anchors, purpose, socioeconomic context, perspective, visual hierarchy, non-stereotype exclusions, signage strategy and action zones.
- Do not automatically scatter lanterns, rice paddies, conical hats, flags or branded packaging. Vietnamese identity comes from everyday spatial forms, mobility, business workflows, objects, language and lifestyle.
- Any signage/text is blank in art and rendered with correct Vietnamese diacritics via native TMP or controlled licensed texture process. No generative garbled words.
- Use shared 32px tile / 32 PPU / 360x640 logical **candidates** currently in art spec. Point filtering, no mipmap, padding and URP 2D pixel-perfect camera; keep TMP native resolution.
- Consistent environment layers: background/wall → midground furniture → active actor → interactive props → foreground occlusion → atmosphere, with native uGUI separate.

### Locality and layout sheet template
```yaml
scene_id: ENV-HOME-01
purpose: evening study and side hustle
socioeconomic_context: modest contemporary urban room
environment_type: Vietnamese apartment bedroom with work corner
local_anchors: [compact desk, wall fan, helmet hook, warm task lamp]
prohibited_stereotypes: [tourism motif collage, mocked poverty, generic Asian interior]
text_strategy: blank signs; localized TMP text
perspective: consistent small-room mini-perspective
activity_zones: [walk-to-desk, sit, type, study, sleep]
```

### Environment family
| ID | Purpose | Signature cues / gameplay animation |
| --- | --- | --- |
| ENV-HOME-01 | First modest home | compact desk, helmet hook, practical fan, task lamp; sit/type/study; day/night |
| ENV-OFFICE-01 | Modern career | workstation cluster, water dispenser, meeting corner; type/talk/meeting |
| ENV-BIZ-01 | Takeaway coffee kiosk | narrow street-facing counter, blank QR stand, takeaway cups; 2-actor serve/receive |
| ENV-KITCHEN-01 | Home food preorder | cooking/prep/packing zone, delivery containers; prep/carry |
| ENV-FOUNDER-01 | Growing business studio | more desks/meeting supplies/staff only if progression actually unlocks them |

City/region variants require individually researched scene evidence; no one-size-fits-all “Vietnamese” stereotype. Scene upgrades only visualize **authoritative** progress and never mutate economy state.

## 5. UI/UX system
Five primary tabs: **Cuộc sống, Sự nghiệp, Kỹ năng, Kinh doanh, Hồ sơ**. Character always visible in life/world play; Career distinct from Skill; one clear next decision; user need not micro-manage work. Scene foreground is important, not just a spreadsheet decorated with a mascot.

- **Life:** current actor/action, day/time, cash, timeline, one next-action CTA; employed shifts fast-forward through real career vignettes, evenings offer meaningful choices.
- **Career:** profession scene, employer, salary, tenure, Career XP and promotion choice.
- **Skill:** study pose, levels, cost, hours/evenings, prerequisites.
- **Business:** active business scene, revenue/cost and paused/closed state; all money/time logic comes from simulation, never fabricated UI.
- **Profile/creator:** large character turnaround/preview and timeline; creator flows through identity → look → age/background → review, preserving state on Back.

**Responsive:** portrait-first 360×640, 390×844, 430×932 plus notched/gesture devices; canvas safe area, native uGUI/TMP Vietnamese, wrapping and 96 reference-pixel touch target contract. No non-uniform sprite stretching. Pixel UI uses first-party 9-slice notched receipt panels, distinct notebook-edge dividers, original icons; existing MobileTheme owns palette (chat moodboards are not production tokens).

**Required states:** loading, empty, locked prerequisites, insufficient funds/time, disabled, business paused/closed, error/save failure, retry, reduced motion and long Vietnamese labels. Clear reasons in words (not color only). No celebratory receipt/stamp before committed authoritative result. Character gesture and scene feedback must not imply nonexistent goods, salary or staff.

## 6. Motion and technical gates
Real articulated gestures; restrained UI page-turn/stamp feedback; bounded fan/rain/lamp ambient loops; pause/limit when offscreen; Reduced Motion alternative. Keep 60 fps as a target, not measured fact. Source layered character family and shared animation semantic controller; original tile atlas with point sampling and stable camera/PPU. UI type at native resolution.

## 7. Golden pipeline / acceptance
1. Write first-party art and locality briefs, anti-copy and representation checklists.
2. Make ≥3 distinct first-party silhouette/composition explorations; 8–12 controlled candidates per Golden category.
3. Record sources, prompt/tool terms when AI-assisted, licenses, editable layers, iteration history and provenance.
4. Reject copied/near-derivative sprites, pixel-filtered photos, brand/trade dress, anti-Vietnam stereotypes, demeaning human portrayals, fake text and blur.
5. Cleanup source, articulate pose/rig, import through Unity, demonstrate real motion and pixel consistency.
6. Capture real Unity **phone BeautyCell** including safe-area, readable Vietnamese and expression/motion, and verify colors/scale with diverse appearances.
7. Separate human independent art/IP/cultural reviewer; Golden score ≥15/16, no category zero; no universal originality guarantee from generation/search.
8. Add to `Art/Approved/registry.json` **only after evidence**. No author/model self-approval; all current briefs remain unapproved.

## 8. Execution packages and current truthful status
ART-01: art direction briefs/spec synchronization (**document/spec only**).  
ART-02: rebuild original 2.3-head four-view Golden character and true walking/working rig.  
ART-03: independently author night home, modern office and kiosk Golden Vietnam pixel scenes.  
ART-04: responsive five-tab UI/creator and all negative states.  
ART-05: shared motion and ambient action.  
ART-06: Unity+phone+human IP clearance, atlas/perf, approved registry.

**NOT VERIFIED:** No new approved Golden sprite, asset/license clearance, Unity play scene, phone capture, character redesign or performance claim is created merely by writing this Bible. Prior C01 remains draft, with source size/PPU/camera candidates subject to review.
