# P03-CHAR-03B — Idempotent Unity scene migration and creator UI efficiency

**Date:** 2026-10-15. **Owner:** GPT-6 Technical Director.
**Baseline:** `236a7769c0dc410295e7010a08ceafe5375c117b`, Engine-free CI SUCCESS.

## Confirmed blocker & change

Existing `LifePanel` is deliberately inactive in the saved first-playable scene, but `RepairGeneratedAssetReferences` used the default `FindAnyObjectByType<LifeScreenViewController>()` which skips inactive GameObjects. Migration could abort despite a valid serialized component. Fixed it with `FindObjectsInactive.Include` without recreating the scene.

`FirstPlayableRuntimeGate.BuildAndVerify()` now calls the idempotent Editor `RepairGeneratedAssetReferences` rather than merely verifying the old scene. `VerifyScene()` now checks newly serialized catalog/page/label refs, a real vertical scroll, the original Create button & paging outside its viewport, the initial identity page and all **16** persistent creator listeners. No game economy or save actions were changed.

The creator controller avoids rewriting unchanged TMP age labels, repeated localization refreshes for unchanged options/appearance and redundant SetActive page transitions. Only chosen option/page changes cause presentation updates. No per-frame polling, extra saves or GameObject creation.

## Evidence and boundaries

**Skills used:** startup-life-session-orchestrator, ui-ugui, scrollview-setup, unity-cli, optimize-text-mesh-pro, asset-quality-gate, vietnam-art-direction.
**Acceptance:** old inactive scene repairs, all new bindings verified, unchanged selections cause no redundant TMP/Canvas rebuilding, historic CreateCharacter retains its immutable command, no unapproved asset reference.
**Tests:** source validator and mutation tests, engine-free .NET compiler/functional tests; exact commit CI verdict must be checked. Unity Editor EditMode/PlayMode **NOT RUN**.
**Visual evidence:** NONE, no Unity/phone screenshot or BeautyCell.
**Save impact:** NONE; cosmetic selection still in-memory only.
**Known limitations:** current `.unity` still has old hierarchy pending Unity Editor migration; physical DPI, 200% fonts, keyboard/IME and device performance require Unity runner/device.
**Files changed:** FirstPlayableShellBuilder.cs, FirstPlayableRuntimeGate.cs, CharacterCreationViewController.cs, Validate-CharacterCreatorUi.py, CharacterCreatorUiChecks/test_ui.py, this session evidence.
**Commit:** See Git SHA.

## Responsive touch height support (P03-RESP bridge)

Added a pure `CharacterCreatorTouchPolicy` with 48 Android dp-class minimum target estimation, CanvasScaler conversion, conservative pixel-width fallback when DPI is missing and validation for invalid size/scale. The runtime `CharacterCreatorTouchSizer` caches authored button/age/Nam-Nữ/selector/navigation/input row `LayoutElement` references on enable, adjusts min/preferred heights on canvas resize (not per-frame), and never creates controls or writes save. Both Create and Repair attach the same component once, serializing the authoritative `MobileTheme`. The reopened-scene gate checks that theme binding. Exact implementation is compiled via the existing Unity-API-stub harness; pure calculations are covered by engine-free PresentationChecks and Unity EditMode test definitions.

**Physical-device caveat:** `Screen.dpi` can be inaccurate, and Android dp/iOS pt differ. This is a conservative baseline, **not proof of >=48dp/44pt measured on every device**. Text 200% reflow, portrait safe-area/IME overlap, Unity screen capture, frame time and device acceptance remain BLOCKED until real Unity tests.

## 100/150/200% typography + touch sizing follow-up

Added `CharacterCreatorTextScalePolicy` (pure 3-step scale, 2-line responsive control height) and `CharacterCreatorTextScaler` (one-time TMP font-size cache, intrinsic preferred heights on ordinary labels, no Update polling, changes only when user taps). The in-creator text-size row uses original localization/keyed controls. `CharacterCreatorTouchSizer` now increases minimum rows for 2-line copy as accessibility scaling grows.

**Important bug fixed:** touch-sizer used to be added to an active scene panel *before* creating later avatar selectors, so its `OnEnable()` cached incomplete controls. Migration now calls `UpgradeCreatorTextScaling` at the END of first-time creation, or as an idempotent follow-up for previously upgraded scenes; it attaches the touch sizer only after the creator subtree is complete.

Added exact source validator + mutation check, pure PresentationChecks, engine-free compilation of the **real Unity text-scaler file** and strict Editor scene gate for the two serialized text-size buttons. Actual Unity Editor/device, keyboard overlap and 200% screenshot **not run**, remains release BLOCKED.

## Keyboard safe-area avoidance (following commit)

Added pure `CharacterCreatorKeyboardPolicy` with regression cases for keyboard intersection, safe-area replacement and viewport floor. Added `CharacterCreatorKeyboardAvoidance` to the existing creator panel; it only changes its bottom RectTransform offset while the name field is focused and the soft keyboard is visible, restores on dismissal, and preserves horizontal position. For devices with missing keyboard-area metrics it uses a bounded 38%-height **estimate**, not a device-certified fit. Editor repair wires a serialized name input and Canvas reference; the runtime gate enforces both. No per-frame allocations, save writes or game commands.

**Unverified:** IME animation, Samsung floating keyboard, iOS keyboard accessory bar, OS dynamic font scaling and real screenshots. This remains an explicit on-device QA gate rather than claiming universal phone compatibility.
