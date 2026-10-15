# P03-CHAR-04A — Creator to shared approved-only visual rig bridge

**Date:** 2026-10-15; owner GPT-6 Technical Director.  
**Baseline:** `cfead04165f3c5ae87d57dbef2272dd0e255ef2b`, engine-free CI SUCCESS.

## Implementation
Added `CharacterCreatorVisualPreviewBridge`, a Unity-facing adapter between the existing `CharacterCreatorDraftSession`, `CharacterVisualComposer` and reusable preauthored `CharacterVisualAssembler`. Once **externally configured** with an approved layered composer/rig and the actual baked skin/hair palette pair, any selection change or Nam/Nữ switch in `CharacterCreationViewController` updates the same SpriteRenderers; hiding the creator clears the preview. Context changes (Home vs Work/Study/Business) drive the existing wardrobe resolver without modifying the personal outfit. Four-view requests are handled by the existing independent view contract; missing Side/Back assets hide the preview rather than reflecting front art. Palette choices the adapter cannot truly render are hidden, **not** presented falsely with unchanged colors.

No implicit scene scan, resource loading, GameObject Instantiate, hard-coded gender effect, save write or economy command was added. **The current production scene has NO approved preview bound**: `Art/Approved/registry.json` remains empty. This feature provides the runtime integration path once first-party chibi layered assets complete human rights/BeautyCell approval; it does not claim any asset is production-ready. All runtime sprite references continue through externally configured `IApprovedCharacterSpriteResolver` and explicit allowlisted `CharacterVisualComposer`.

## Checks
- **Skills used:** startup-life-session-orchestrator, ui-ugui, unity-cli, asset-quality-gate, vietnam-art-direction, optimize-text-mesh-pro.
- **Acceptance:** immediate draft-to-render update for face, hair, outfit and Nam/Nữ when approved; reused rig and contextual work uniform; no stale sprite if palette/unapproved view missing; no raw sprite/faked approval; null unconfigured preview safe.
- **Tests:** added actual C# stub-harness behavioral tests for Nam/Nữ same rig, personal/work attire, failure of unapproved palette, missing view and hiding stale renders; static + negative tests for quarantine. Exact commit engine-free CI verdict required.
- **Visual evidence:** NONE; no registered sprite available and no verified Unity runner visual pass.
- **Save impact:** NONE; cosmetics still intentionally session-only pending CAS/migration contract.
- **Known limitations:** no approved sprite resolver or material tinting implementation, so no production visual preview yet; Unity Editor/phone/animation evidence remains blocked.
- **Files changed:** new Unity runtime bridge/.meta, creator controller integration, stub .NET assembly and behavioral cases, UI wiring validator, quarantined preview validator/mutation tests, CI gate, session log.
