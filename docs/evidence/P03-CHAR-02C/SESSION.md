# P03-CHAR-02C — Shared Unity Character Visual Assembler

**Date:** 2026-10-15  
**Owner:** GPT-6 Technical Director  
**Baseline HEAD:** `bd8dbeb3898fc55ab09b70ab4cb31590c012cbe3` with exact-HEAD Engine-free CI SUCCESS.

## Scope and implemented code
Added `CharacterVisualAssembler` to the **existing** Unity Presentation asmdef. It receives a preauthored child-rig binding table and a strictly **approved** sprite resolver interface, with a required PPU passed by the source catalog. Its `Configure` checks duplicate/missing/external renderers and fails *before* any side effects. Its `Apply` has two phases: preflight all rig parts, approved sprite lookups, PPU/Point/no-mips and renderer existence; only if all pass, mutate existing SpriteRenderer.sprite, .sortingOrder and .enabled. No GameObject creation, hierarchy changes, transform movement, AssetDatabase, hidden disk/save reads, career/balance mutations or animation controller cloning. Switching outfits reuses the original joint transforms and performs no renderer property writes for identical plans.

`Clear` disables only controlled sprite slots; the articulated Animator rig/transform pivots stay intact. The class intentionally has **no real approved sprite resolver implementation** and has **not been attached to any production scene**: the approved registry remains empty. Until separate Art Director/IP/device BeautyCell approval, the only use is engine-internal development and test fixtures. The interface's approval promise must be backed by an independently validated registry at integration time, not by a self-set boolean.

Added Unity EditMode cases for no cloned renderers, scene-context outfit switching, unchanged plan no-op, failed sprite preflight atomicity, duplicate/external-binding rejection and clearing without joint movement. Added engine-free source/quarantine validation plus 5 mutation tests; the CI static gate is *not* a substitute for running Editor tests.

## Quality gates
- **Skills used:** startup-life-session-orchestrator, asset-quality-gate, vietnam-art-direction, sprite-editor, unity-cli, 2d-pixel-perfect, ui-ugui.
- **Acceptance criteria:** preauthored rig reuse; rollback-like atomic preflight; fail-closed for unapproved/invalid sprites; point-filtered 32 PPU prototype (actual passed PPU comes from catalog); no accidental production asset inclusion; tests never weaken Golden thresholds.
- **Tests:** exact commit engine-free CI to confirm Python source checks; Unity EditMode tests **AUTHORED, NOT RUN** due to unavailable Editor/runtime runners; no FPS/GC metrics reported.
- **Visual evidence:** NONE. No licensed character, four-view Unity capture, animator playback or on-device proof.
- **Save impact:** NONE. Cosmetic draft remains unpersisted and existing legacy `AppearanceId` unchanged.
- **Known limitations:** No approved sprite ID → Unity assembler cannot yet be used in shipping scenes. A separately audited approved-sprite resolver, visual catalog bindings and creator UI/actual SpriteAtlas will come after Art Director approval. The rig C01 remains unapproved and not 2.3-head compliant.
- **Files changed:** new runtime C# + .meta, new EditMode tests + .meta, engine-free source validator and mutation suite, engine-free CI workflow, this session.

## Follow-up — actual C# compile/behaviour preflight

Static source matching alone cannot catch C# syntax errors in Unity-only scripts. Added an engine-free `.NET 8` harness compiling the **exact runtime assembler .cs file** against minimal named Unity API stubs plus the **actual compiled** `CharacterCustomizationModel` / `CharacterVisualComposition` project. The harness executes five cases for renderer reuse, work/home state, atomic failed sprite lookups, invalid pixel import and rig binding rejection. This does not claim Unity Engine semantic parity or render/audio/device test success. Also replaced interface enumeration in `Apply` with indexed `IReadOnlyList` access to avoid per-preview enumerator allocations, and made the Unity EditMode fake resolver out parameter compile conservatively without `out sprite!` syntax.
