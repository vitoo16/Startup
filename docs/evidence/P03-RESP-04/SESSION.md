# P03-RESP-04 — Real-CanvasScaler responsive PlayMode matrix (authored)

**Date:** 2026-10-15 | **Owner:** GPT-6 (Technical Director).  
**Baseline:** `8551e17740735f84d15e0762343941f05bdbb7b1` on `main`, exact-HEAD engine-free CI **SUCCESS**.

## Code changed
- Fixed `FirstPlayableRuntimeGate.BuildAndVerify()`: it previously logged 'verified' after calling `RepairGeneratedAssetReferences` **without** running `VerifyScene`. This now reopens/validates the saved serialized scene and refuses missing creator references, listeners and invalid authored scroll layout.
- Added `CharacterCreatorResponsiveMatrixTests` to existing Unity PlayMode tests. Matrix actively requests **360×640, 390×844, 430×932** on the rendering system and asserts actual `Screen.width/height`. It then exercises all four creator pages at **100%, 150%, 200%** typography, with the real `ScaleWithScreenSize` CanvasScaler **enabled**.
- Each state asserts the active page, Vietnamese text overflow/translation, safe bounds and clear separation between the scroll viewport and pinned Back/Next/Submit actions. Calls to change typography remain presentation-only.
- When `STARTUP_LIFE_CREATOR_RESPONSIVE_EVIDENCE` is set to an output directory on a **graphical Unity test runner**, each config yields a native-size PNG from `ScreenCapture.CaptureScreenshotAsTexture()`. Without that environment variable, geometric assertions still run but **no images are produced**.
- A source validator and four negative mutation cases ensure a future refactor cannot claim responsiveness from a fixed 1080p capture or turn off the CanvasScaler. These are authored-code checks **only**.

## Execution & evidence
**Skills used:** startup-life-session-orchestrator, ui-ugui, scrollview-setup, unity-cli, optimize-text-mesh-pro, asset-quality-gate, vietnam-art-direction.  
**Acceptance criteria:** keep real CanvasScaler enabled, verify three widths/tallness and 100/150/200% type, no obscured primary actions, capture real-size visual evidence on a graphics-capable runner, strict reopened scene verification.  
**Tests:** exact-commit engine-free CI required (source+mutation tests). **Unity PlayMode not executed by this GitHub-only session; runner may not support Screen.SetResolution under `-nographics`**. If unsupported, test deliberately fails rather than fabricating a PASS.  
**Visual evidence:** NONE at time of authoring. Captures generated only by a graphical Unity runner after exercising the newly authored test.  
**Save impact:** NONE; V2/V3/V4 untouched.  
**Known limitations:** Unity compilation, 실제 screen rendering, notch/IME on real OS, physical dp/pt and FPS remain unverified. Native device acceptance and independent art clearance still blocked.  
**Files changed:** PlayMode responsive matrix C#/.meta, first playable Editor runtime gate, source validator, mutation tests, engine-free CI workflow and this evidence document.  
**Commit:** see Git history.  

## Standalone PlayMode save isolation (follow-up)

The matrix now safeguards `startup-life.json*` bytes in `Application.persistentDataPath` before any scene load and starts with a cold empty save. On success OR failure it destroys the active bootstrap, deletes newly written test saves and restores every original file byte-for-byte, then restores the original screen resolution. Added a negative source mutation asserting save restoration cannot silently disappear. **No verified Unity execution yet.**
