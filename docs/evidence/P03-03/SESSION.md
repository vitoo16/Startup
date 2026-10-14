# P03-03 — Pixel UI semantic theme and contrast pass

**Date:** 2026-10-14. **Owner model:** GPT-6 Technical Director.  
**Baseline:** `ad681c6f8551ecb8308436fde6d71c86d1b078a2`. **Status:** TECHNICAL_IMPLEMENTATION; no visual/device acceptance.

## Problem
LifeDashboard retained a hard-coded cream/green HD palette. BusinessManagement and EventChoice were dark blue/teal with white text despite the pixel-v2 MobileTheme asset. Real UI style was therefore hybrid even after P02 migration; a pretty character would not fix the mismatched product UI.

## Change
- One serialized `MobileTheme uiTheme` reference on `LifeScreenViewController`; scene generation and scene-repair tooling bind the same `Assets/StartupLife/Data/MobileTheme.asset`. Old scenes with no field receive one temporary, logged fallback `MobileTheme` generated from C# defaults kept in palette parity by the Golden art contract; Unity repair remains required.
- All three dynamic overlays now receive `view.UiTheme` and use `theme.panel/background/ink/accent`. High-contrast white reserved for action buttons and dark selected business cards; body/story labels use dark ink against light panel. Scrim is a transparent ink overlay. No new asset images or hardcoded RGB palettes in overlays.
- Static validation measures ink on panel/background and white on accent/ink; text ratio target >=7 for ink, >=4.5 for actions. Checks theme authority in scene builder and overlays; includes eight negative/positive engine-free cases.
- Preserved touch target sizes, uGUI/TMP fonts, event modal CAS handling and all gameplay/finance readouts. No save schema/balance changes. No generated or unapproved sprites reference production scenes.

## Visual/QA gate
CI only evaluates source and numeric contrast. It does **not** validate native phone screenshots, font 200% reflow, clipped modal choices, focus order, pixel frames, or beauty of transitions. Those are blocking W05/P03 QA items; do not claim Golden UI approval.

**Skills used:** startup-life-session-orchestrator, startup-life-asset-quality-gate, vietnam-art-direction, 2d-pixel-perfect, sprite-editor (no sprite metadata touched), ui-ugui, accessibility (contrast reference only).  
**Tests:** Engine-free CI steps and eight fixture cases added; exact HEAD verdict must be checked. Unity EditMode/PlayMode not run.  
**Visual evidence:** NONE.  
**Save impact:** NONE.  
**Known limitations:** Existing scene serialization must be repaired inside Unity; 200% font and real tap-size testing pending; runtime art/UI still lacks approved pixel textures.  
**Files changed:** the three overlays, LifeScreenViewController, FirstPlayableShellBuilder, new preflight/test fixture, CI, this session report.  
**Commit:** See enclosing Git commit.
