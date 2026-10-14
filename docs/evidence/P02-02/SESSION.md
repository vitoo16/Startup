# P02-02 — Shared Unity Editor pixel camera setup

**Date:** 2026-10-14  
**Owner:** GPT-6 Technical Director  
**Starting main HEAD:** `5536bbc224377c3ed532de1dead14c12f6cc86d6`.

- **Skills used:** startup-life-session-orchestrator, 2d-pixel-perfect (URP camera reference), sprite-editor (no sprite importer/meta modification), startup-life-asset-quality-gate, vietnam-art-direction.
- **Acceptance criteria:** new scene creation must reuse one URP PixelPerfectCamera builder; no duplicate PPU/camera setups, no accidental Built-in pixel camera; active art spec and Editor C# numeric fields must match under engine-free validator.
- **Implementation:** shared `PixelWorldCameraSetup` inside existing Editor assembly; both baseline and FirstPlayable scene builders invoke it, retaining their native screen-space TMP uGUI canvas. The helper rejects non-URP before scene setup, uses URP enum snapping/cropping, and disables MSAA, HDR, dynamic resolution and camera postprocessing. No Unity Sprite import metadata hand-edits. A stable new C# .meta GUID was authored; Editor import must confirm it.
- **Tests:** two negative static regression fixtures added (camera PPU drift and reintroduced HD scene camera), plus existing 13 Golden cases. Exact-head CI success required, Unity compilation/PlayMode NOT RUN in this environment.
- **Visual evidence:** NONE; committed existing Unity scenes have not been altered or rendered; no physical-device BeautyCell.
- **Save impact:** NONE.
- **Known limitations:** scene builder changes are not scene migrations; asset sprites/rig/atlases need Editor import and device testing. Authoritative style numeric values are still prototypes pending visual/IP clearance.
- **Files changed:** two existing Editor builders, new shared PixelWorldCameraSetup.cs + .meta, Golden validator/fixtures, production style contract and this session note.
