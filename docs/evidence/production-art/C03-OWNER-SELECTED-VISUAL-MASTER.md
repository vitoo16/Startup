# C03 — Owner-selected visual master (supersedes idle-pose A/B)

**Date:** 2026-10-16  
**Project:** Startup Life  
**Character:** C03 / Sáng Ngõ Nhỏ  
**Owner instruction:** "thôi xài lại bản này đi" — select the attached image as the character's visual identity.  
**Direction decision:** `OWNER_SELECTED_VISUAL_MASTER`  
**Runtime asset status:** `NOT_PRODUCTION_READY`  
**Previous idle variants:** `SUPERSEDED — DO_NOT_SELECT_A_OR_B`

## Decision

The **exact image attached by the owner** now supersedes the C03 R5.1 hand-grip design and both C03 Neutral Idle A/B experiments as the *visual-design reference*. Do not continue to offer A/B selection, do not propose R5.2/R5.3, and do not modify hairstyle, eyes, cheeks, smile, jacket, bag, shoes, face proportions or pose without a new owner-directed change.

The owner prefers this softer, expressive, friendly short-haired commuter wearing a muted green jacket, light shirt, blue-grey pants, broad sneakers and a brown cross-body bag, with both hands relaxed by the body. Preserve the exact illustrated silhouette and personality, not the previously reviewed native C03 R5-series sprite look.

**Asset master evidence** (owner-supplied image, 2026-10-16):
- Original upload name: `image(20261016-093800).png`
- SHA-256, original uploaded bytes: `457478717a96f5bcfeddc5976af4c3034ddd7cf9086e988b093707e039d928aa`
- Pixel dimensions: **1122×1402**.
- Format: PNG **RGBA** but alpha is **255 for every pixel**; the beige background is baked into the bitmap, and is not usable as a transparent isolated character sprite.
- Original conversation attachment is the source of truth. Do not substitute the 64×80 previous drafts, the earlier R4 generated export, or a lossy recreation.
- **Binary availability:** The attachment is available in this conversation's artifact handoff, but its **exact PNG binary has not been committed into the Git repository**. This Markdown file is a verifiable selection record, **not** a false assertion that GitHub already contains the image. The original SHA allows future byte-for-byte matching when stored in a versioned source-assets path.

## Integration rules and next technical task

1. **Stop A/B and old native-polish work:** C03-IdlePose-A/B and R5/R5.1 remain archived draft evidence only; no automated code may select them as the default character. Do not change runtime bindings based solely on an image approval.
2. **Version the original exact master PNG:** place it in a separate art-reference/source location (never in `Art/Approved`), then independently verify SHA-256 exactly matches the owner attachment hash above. Until this happens, it is not a reproducible repo asset.
3. **Preserve appearance:** prepare transparent character isolation or native-pixel source reconstruction without changing the visible design. Because the chosen image is 1122×1402 with opaque background, blindly using it as an RGBA 64×80 sprite would distort/lose identity. Do not downsample it or call a pixel filter "native art."
4. **Resolve production style-grid compatibility explicitly:** compare intended in-game sprite size to `PRODUCTION_PIXEL_STYLE_CONTRACT.md`; if the high-resolution chosen design needs an alternate authored native grid or a higher-resolution cutout approach, obtain a documented art/technical contract decision **before** altering the approved pipeline.
5. **Animation readiness requires re-authoring separated pieces** and validating real pivots, hierarchy, arm/hand motion, palette and sorting in Unity. A flattened illustration is not a 21-layer working cutout rig.
6. **Before production promotion:** independent originality/IP review, PNG provenance, transparent-edge and device BeautyCell checks, real Unity joint animations and separate owner Golden approval. Do not modify `Assets/StartupLife/Art/Approved/registry.json` at this stage.

**One sentence of authority:** *The character design is selected; the shipping asset is not yet approved.*

## Session closeout

**Owner model:** GPT-6  
**Skills used:** startup-life-session-orchestrator, vietnam-art-direction, startup-life-asset-quality-gate, 2d-pixel-perfect (technical compatibility guidance).  
**Tests:** Verified original PNG dimensions, RGBA mode, alpha extrema=(255,255), SHA-256. Verified GitHub branch head and approved-registry state. No Unity/device/animation/IP tests.  
**Save impact:** None.  
**Scope:** Decision/handoff only; no new sprite, no retouch, no new camera views, no gameplay modification.  
**Known blocker:** Exact binary was supplied to ChatGPT, not yet uploaded as a binary file to GitHub.
