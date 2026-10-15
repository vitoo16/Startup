# P03-RESP-05 — Unity graphical runner and 36 native-size captures

**Date:** 2026-10-15 | **Owner:** GPT-6 Technical Director  
**Baseline:** `0096b295a4b3021a000c0adeaddce757fdd1ddb4` engine-free CI SUCCESS.

## Concrete improvement
The existing manual `Unity Runtime Smoke` workflow passed `-nographics` to **both** EditMode and PlayMode; `CharacterCreatorResponsiveMatrixTests` now needs native-screen `Screen.SetResolution`, CanvasScaler and real screen captures. Added opt-in `-EnableGraphics` to `scripts/Test-UnityHeadless.ps1`, so EditMode remains headless while graphical PlayMode stops forcing `-nographics`. Both Windows/Android and macOS/iOS self-hosted smoke jobs request graphical PlayMode and configure a fresh evidence directory.

Added `scripts/Verify-CreatorResponsiveEvidence.ps1` which requires **36 distinct PNGs** (3 native sizes × 3 typography scales × 4 creator pages), validates PNG signatures and IHDR actual width/height, records per-file SHA-256 and rejects stale/extra/missing captures. Both smoke jobs only reach their build steps after full Unity EditMode+PlayMode verdict **and** all 36 captures are verified. CI screenshots are uploaded as artifacts for manual visual review; PNG integrity alone does not certify content aesthetics/accessibility. The workflow has manual dispatch and **self-hosted graphical runner prerequisites**, no claim that any such runner is online. Added 4 mutation/source regressions in engine-free CI; these do not constitute Unity runtime evidence.

## Acceptance and restrictions
- **Skills used:** startup-life-session-orchestrator, ui-ugui, scrollview-setup, unity-cli, optimize-text-mesh-pro, asset-quality-gate, vietnam-art-direction.
- **Acceptance:** graphical PlayMode not forced to `-nographics`; exact 36 native captures with hash reporting; clean evidence directory; no fake pass if GUI/Unity/PNG unavailable; actual tests still gate simulation.
- **Tests:** exact-head engine-free static/mutation CI (check run). Actual graphical Unity and mobile not run.
- **Visual evidence:** NONE until the dispatch job uploads its 36 captures. Explicit owner review required before UX signoff.
- **Save impact:** NONE to shipping save, no schema, commands or simulation changes.
- **Known limitations:** physical Android/iOS not run; macOS self-hosted runner must have an interactive/graphical session. Unity CLI may reject unsupported GUI flags or native resolutions; failures must remain failures until verified.
- **Files changed:** Unity CLI runner, manual Unity smoke workflow, evidence verifier, static gate, mutation tests, engine-free workflow, session record.
