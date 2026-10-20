# PLAYER-FACING VISUAL/UX BLOCKER — 2026-10-20

Status: **FAIL — production appearance not accepted**. This is an owner-reported in-Editor Game View review of `FirstPlayable.unity`, not a CI/compiler failure or an approved new art brief. The captured Life panel shows a plain text-and-button form, no displayed character, no environmental composition, little visual hierarchy, no evidenced in-scene character animation, and player-facing text that reads like a development dashboard.

The existing save/simulation, multi-page Character Creator, uGUI and editor tooling are engineering substrates; they do **not** constitute a production-quality game. **No release, screenshot, promotional trailer or milestone visual approval is justified by this state.**

## Root causes observed in source

- The five-tab dashboard and baseline Life controls are predominantly uGUI `Image` / `Button` / TMP content. Their layouts are functional and data-bound, not artwork-driven.
- The approved asset registry remains empty. C03 character raster has been explicitly rejected by the owner; no one may substitute a quarantined draft or false "placeholder approval".
- No accepted rig + animations + phone BeautyCell evidence is currently bound to the character preview. Existing creator cosmetic choices are DRAFT and some are intentionally not persisted.
- Dynamic on-screen explanation text is assembled inside `LifeDashboardOverlay`. The text is readable in Vietnamese, but technical terminology and raw financial-status formatting still make the game feel like a control panel rather than lived experience.
- Theme color parity/contrast and static CI validators do not prove silhouette, composition, scene storytelling, sprite import/rig playback, touch feedback, or aesthetic originality.

## Correct production vertical slice: a day in a Vietnamese neighborhood

**Art/scene:** one authored, original small contemporary city room/work desk with 2–4 local anchors (e.g. helmet hung near door, plain takeaway cup, compact fan, familiar laptop/desk). Do not force tourist symbols or copy real establishments. Render a visibly animated character *inside* a location, not a floating avatar or a text-only form.

**Character:** one acceptable original C03-style adult chibi with clear 1× native pixel clusters; expressive idle (breathing/eyebrow/hand), a working activity with independently moving joints, a studying action and stable footplant. Only genuine approved rig and assets may be integrated; avoid whole-sprite vertical bob pretending to walk/work. Owner reviews native-scale preview first; independent rights review and Android BeautyCell are later gates.

**Life layout:** make current activity the hero, not a long table. Present the character and room as the visual focal point; concise date/time greeting above, three meaningful status summaries (money, work, learning), 1–3 context-relevant actions with obvious primary/secondary distinction, and a navigable notebook/receipt-inspired progress panel. Keep essential decisions reachable at 360×640 and at enlarged Vietnamese text, with safe-area and scroll retention. No fake visual asset or third-party image substituted as final art.

**Motion language:** short restrained UI fades/selection feedback, contextual activity transitions with genuine limb/environment movement, responsive actions and a quiet outcome cue; no glitter spam, generic particle storms, fake pixel overlay, fractional sprite sliding or continuous UI wobble. Reduced-motion setting must be reachable by players before signoff.

**Copy:** player-facing strings are concise natural Vietnamese and localized via tables/ICU formatting. Do not show implementation terms like revision, CAS, committed finance, serialization, mechanical "quota" labels, raw gameplay keys or technical simulation explanations. Display finance facts accurately but prioritize decisions over bookkeeping. This is not permission to change simulation semantics, balances or receipt invariants.

## Incremental slice landed in code, not visual acceptance

- Shared `UiPanelReveal` adds a small unscaled-time opacity transition that does not shift pixel geometry, reuses lifecycle guards, and honors `startup-life.ui.reduce-motion.v1`.
- Character Creator, Life panel and Dashboard reuse it; Dashboard buttons get short pressed/highlighted feedback.
- Reworded some Dashboard copy and synchronized changed static headings with `DashboardLocalizationCatalog`; actual serialized string tables synchronize in Unity Editor. Remaining dynamic copy must be moved to full localization before shipping.
- New PlayMode motion tests plus engine-free source checks. They remain `NOT RUN` as Unity/visual evidence until exercised on the owner's Editor.

## Exit gates (do not mark PASS without evidence)

1. **Scene:** portrait 360×640, 390×844, 430×932 screenshots at 100/150/200% text; no clipped buttons/labels, no overlap, clear active choice, legible Vietnamese diacritics.
2. **Motion:** PlayMode verifies tab/panel animations and zero-navigation regression, plus on-device observation of working/studying joints and genuine idle loops.
3. **Identity:** accepted native-grid original chibi/scene/art composition. Current C03 rejected assets stay quarantined and the production asset registry stays empty until approval.
4. **Copy:** qualified Vietnamese review of all visible states, especially money, study, job, and save recovery; no technical terms unless intentionally in an advanced diagnostic mode.
5. **Save/gameplay:** replay/simulation stays byte-semantics compatible, new UI submits commands only through `FirstPlayableFlow`, no extra shadow state or UI-driven economic calculations.
6. **Approval:** owner visual verdict on actual Unity screenshot/video; independent rights/provenance clearance; Android device/render/performance test. Absent proof remains BLOCKED, not PASS.

## Technical session receipt

Owner model: GPT-6 Technical Director.  
Skills used: startup-life-session-orchestrator, ui, ui-ugui, unity-cli, localization, vietnam-art-direction; production pixel/IP contracts.  
Code changes: `UiPanelReveal.cs`, Character Creator and Life controllers, `LifeDashboardOverlay.cs`, `DashboardLocalizationCatalog.cs`, motion PlayMode tests and source regression test + CI.  
Save impact: None.  
Art registry impact: None; rejected artwork is not promoted.  
Unity compilation: **NOT RUN in this coding session**.  
Visual/device evidence: owner screenshot shows **FAIL** for current base Life design; new motion not yet reviewed in Unity.  
Known limitation: main Life scene remains legacy prototype geometry, no approved character/environment/rig, only incremental motion/copy improvements landed.  
