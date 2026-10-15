# P03-CHAR-02A — Character Look Draft & Career Wardrobe Pure Model

**Date:** 2026-10-15  
**Owner:** GPT-6 Technical Director  
**Baseline:** `60b2dd8120c459e6907548046388c5752ced8d83`, Engine-free CI SUCCESS.  
**Status:** IMPLEMENTED_UNVERIFIED until exact-HEAD CI; Unity runtime/art review not executed.

## Scope and acceptance

- Read `AGENTS.md`, the new Character Creator/Trait/Wardrobe proposal, active production roadmap, GDD and skill routing. Used: `startup-life-session-orchestrator`, `startup-life-gameplay-guardian`, `startup-life-asset-quality-gate`, `vietnam-art-direction`, `ui-ugui`, `sprite-editor`, `2d-pixel-perfect`.
- First code slice introduces pure `CharacterLookDraft`, `CharacterVisualOption`, `CharacterLookCatalog`, `CharacterWardrobeRule`, `CharacterWardrobeResolver` in the **existing** Presentation assembly — not a new engine. Nam/Nữ `base.female`/`base.male` remain exact legacy appearance IDs, with independently selected face/hair/hair color/skin/accessories/outfit/trait.
- Character trait IDs are visual-selection-only drafts **without any effect** on stamina, customers, ability, salary, economy or LearningSpeed. No gender-derived modifier. A future authoritative trait balance/migration would be a separate owner decision.
- Wardrobe maps a pure career ID to an outfit ID **only** during work context, returns chosen personal clothes for home/study/business/unknown work IDs, with explicit invalid/duplicate-ID failure. No career command, save write, Unity prefab, image reference or external asset.
- Constructor validates catalog defaults, legacy appearance IDs and unique cosmetic options. Non-mutating `Choose` returns a new draft only on change; session creator can keep it entirely in memory without reading a save on each tap.
- Added eight engine-free PresentationChecks regressions; compiler includes new .cs in existing `StartupLife.PresentationModel.csproj`. Gameplay/save/frozen V2 receipts untouched. No new unapproved sprites appear in production.

## Verification / exit

**Tests:** GitHub engine-free regression has to PASS on exact commit before claiming compile success; no local dotnet/pwsh. Eight PresentationChecks cases added.  
**Visual evidence:** NONE; does not add UI controls, actual sprites, rig or approval.  
**Save impact:** NONE; existing `AppearanceId` receipt bytes untouched. Draft and trait selections intentionally **not persisted** or shown yet.  
**Known limitations:** Cosmetic options in tests are model fixtures, not an approved runtime catalog or final outfit art; Unity visual assembly, responsive creator UI, outfit sprite mapping and V4 cosmetic persistence remain in subsequent slices. Runtime engine-free tests cannot measure 60fps, draw calls or UI responsiveness.  
**Files changed:** `Assets/StartupLife/Scripts/Presentation/CharacterCustomizationModel.cs` plus .meta, `tools/PresentationChecks/StartupLife.PresentationModel.csproj`, `tools/PresentationChecks/Program.cs`, this session record.  
**Commit:** see Git SHA.
