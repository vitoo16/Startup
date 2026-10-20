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


## Incremental Life journal layout — 2026-10-20 follow-up

Code-only candidate, **not an owner-approved visual**:

- `LifeHomeComposition` reuses all existing source-wired Life row GameObjects and six existing UnityEvent-bearing buttons; it re-parents them into a `MobileScrollViewFactory` viewport and groups the header, progress and daily decisions in separate readable journal cards.
- A flat geometric 2D desk/window/notebook composition is an original **layout staging sketch**, not final native-pixel environment artwork or a character. It cannot satisfy C03/rig or the mandatory device BeautyCell gate.
- Top scroll padding reserves clearance for the existing Dashboard/Business anchored navigation. The primary visible action gets the theme accent, while the other actions remain discoverable and keep existing `interactable` authority from `LifeScreenViewController`.
- A contextual daily sentence uses a shared localized catalog entry and updates only when the state changes; the game still needs complete review of historical hardcoded dynamic copy.
- No changes to `GameState`, `FirstPlayableFlow`, event journal, CAS gateway, save path, command handlers, approval registry, art source or player preferences.

**Unity QA to perform:** Pull exact head, open FirstPlayable on a test save and on a new run, verify the old Life controls are now children of `LifeHomeScroll/Viewport/Content`; scroll from journal to actions, click each available action, open/close dashboard and business, confirm save restore/cold restart and readable Vietnamese. Check 360×640/390×844/430×932 and enlarged text. Character and world animation remain BLOCKED; user visual approval is still missing. Source regression + PlayMode test are additional checks but not a substitute for Unity runtime screenshots.

## 2026-10-20 continuation: action-first Life and motion accessibility

**Owner:** GPT-6 Technical Director. **Lane:** `main`. **Goal:** improve real player touch ergonomics/readability before proposing any art approval.

**Code slice:** `LifeHomeComposition` now presents the existing action card before the long statistics card, moves the highest-priority currently enabled action to the top without replacing any GameObject/UnityEvent, and derives baseline Life/title/action typography from serialized `MobileTheme`. The `LifeDashboardOverlay` Profile page now exposes a localized, persistent toggle for `UiPanelReveal.ReducedMotionPreference`; it changes only UI animations, not gameplay time or state.

**Skills used:** `startup-life-session-orchestrator`, `ui`, `ui-ugui` (including `scrollview-setup.md`), `unity-cli`, `localization`, `vietnam-art-direction`; GDD and production roadmap checked.

**Tests:** Added source contracts plus PlayMode assertions for action-card precedence, readable typography and user-accessible reduced motion. Engine-free GitHub CI must be read at the final HEAD. **Unity Editor compilation, 360×640/390×844/430×932 screenshots, 200%-text, touch and device tests: NOT RUN in this session.** No claim of visual approval.

**Visual evidence:** Existing owner screenshot remains FAIL for legacy UI; no new accepted Game View footage yet. Existing geometry-only environment remains staging, not approved original scene art. C03 raster remains rejected and the approved-art registry is not changed.

**Save impact:** No schema, serializer, receipts or economy changes; reduced-motion option uses `PlayerPrefs` only.

**Known limitations:** Not yet a character-driven animated Life scene. Gameplay buttons remain inherited prototype controls and may require responsive typography/layout adjustment after device evidence. Full Vietnamese dynamic copy review remains open.

**Files changed:** `LifeHomeComposition.cs`, `LifeDashboardOverlay.cs`, `DashboardLocalizationCatalog.cs`, `LifeHomeCompositionTests.cs`, `test_life_journal_source.py`, `test_presentation_motion_source.py`, and this evidence note.

**Manual Unity review request:** Stop Play → update to HEAD → FirstPlayable → Play, check that **Đi làm/Nhận việc/Học** (when enabled) is first and visibly emphasized, all six old actions remain accessible by scrolling, Dashboard → Hồ sơ can toggle reduced motion, and Vietnamese accents/heading sizes remain legible. Send a 390×844 portrait screenshot of the top and bottom of the scroll. Treat errors/overlap or unreachable controls as FAIL.

## 2026-10-20 Player home-menu and safe new-run entry

**Status: CODE CANDIDATE, not production visual approval.** Older saved games now enter via a localized choice panel (**Tiếp tục hành trình / Trò chơi mới / Cài đặt**), while a fresh uncommitted run still enters Character Creator directly. A persistent Menu entry permits revisiting this panel without file manipulation. Settings currently provides reduced motion only; full audio/language controls remain future work.

**New-run safety:** a named run must explicitly confirm archival before any scene reload. `LocalCheckpointArchiver` is shared by the runtime and Unity Editor Debug menu; it takes the same `.lock` as `AtomicFileSaveStore`, moves only primary + recovery backup into a timestamped `RunArchives`, attempts rollback on failure, and does not deserialize/rewrite gameplay state. Refusal or archive/reload failure leaves the gameplay UI inaccessible rather than risking a write from an orphaned in-memory session. Multiple concurrently open players can still create a new save after the lock is released, so use one running Editor/player instance per test.

**Skills used:** `startup-life-session-orchestrator`, `ui`, `ui-ugui`, `ui-ugui/references/scrollview-setup.md`, `unity-cli`, `localization`, `vietnam-art-direction`, GDD and production roadmap.

**Tests:** `tools/SaveArchiveChecks` runs .NET 8 tests against the actual archive source (pair/backup-only/missing/locked/sequential); `tools/tests/test_main_menu_source.py` guards confirmation/localization/entry; existing Editor diagnostic source tests updated; `FirstPlayableMainMenuTests.cs` covers menu cancellation, settings and unchanged revision. Final CI result must be read at exact HEAD. **Unity compilation, PlayMode and device screenshots: NOT RUN in this coding session.**

**Save impact:** no schema migration, receipt change or economic logic change. Primary + backup move only after explicit player confirmation. Archive is a recovery path, not a complete multi-slot save manager.

**Visual evidence:** the previously rejected flat Life UI remains unapproved; this menu uses the current MobileTheme/TMP substrate and is likewise awaiting owner screenshot review. Pixel character and real scene animation are still BLOCKED pending independent art approval.

**Known limitations:** full main-menu art, true scene/character motion, scroll/portrait accessibility, other settings, and UI-based restore of archived runs are not complete.

**Files changed:** `LocalCheckpointArchiver.cs` and .meta, `FirstPlayableMainMenu.cs` and .meta, `StartupLifeBootstrapper.cs`, `DashboardLocalizationCatalog.cs`, `StartupLifeLocalSaveDiagnostics.cs`, `SaveArchiveChecks`, `test_main_menu_source.py`, `test_editor_save_diagnostics.py`, `FirstPlayableMainMenuTests.cs` and .meta, `engine-free-ci.yml`, `LOCAL_SAVE_EDITOR_DIAGNOSTICS.md`, and this note.
