# P02-01 — Pixel art technical contract migration

Date: 2026-10-14  
Owner: GPT-6 Technical Director  
Previous main HEAD: `9d1a3d3bde27d045e9757be7f41de6ba993f77f7` (engine-free CI SUCCESS).  
Status: IMPLEMENTED_UNVERIFIED: no physical Unity or human art approval.

## Work and acceptance
Synchronized the active art-spec, composition, five Golden briefs, approved registry style, MobileTheme.asset and C# builder Color32 fallbacks to a single `vn_everyday_pixel_v1` contract. Technical prototype values: 32 PPU / 32×32 tile / 128×160 avatar / 360×640 world reference with URP PixelPerfectCamera contract, screen-space native uGUI/TMP, 96-unit touch target. One provisional independently authored palette in MobileTheme.asset (fallback parity statically checked); no import or approval claimed. Archived original M13 HD source by identifying previous exact Git SHA and updating the historical art bible index. The five Golden targets remain unapproved, no asset is added to the registry.

Replaced HD-only Golden validator with a pixel-specific strict gate (color cross-check, pipeline, sampling, sizes, joint motion, screen resolution, locality and unchanged >=15/16 QA threshold). Included 13 engine-free negative/positive fixtures in CI. Never change Git history, historical save, receipts or GDD product rules.

**Skills used:** startup-life-session-orchestrator; startup-life-asset-quality-gate; vietnam-art-direction; 2d-pixel-perfect; sprite-editor (no direct importer .meta edits).  
**Tests:** new CI tests authored; verdict requires exact-head GitHub Actions. Unity EditMode/PlayMode and device NOT RUN.  
**Visual evidence:** NONE; no real pixel BeautyCell, source rig, importer or artist approval.  
**Save impact:** NONE.  
**Known limitations:** active technical spec is pixel but live Unity scenes need true sprite/rig/import/camera authoring. Palette and scale remain candidates for independent visual/IP review; static checks do not prove original artwork.  
**Files changed:** canonical YAML-JSON specs, registry, MobileTheme.asset/.cs, validator, tests, CI, pixel contract doc, M13 historic index, session record.
