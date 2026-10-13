# AGENTS.md — STARTUP LIFE

This repository follows `docs/AI_PRODUCTION_WORKFLOW.md`.

## Mandatory first action
Before modifying code, scenes, prefabs, ScriptableObjects or production assets:

1. identify the task domain;
2. load `startup-life-session-orchestrator`;
3. load every required domain skill;
4. read the active milestone and acceptance criteria.

**No skill = no implementation.**

## Current Technical Director directive (updated 2026-10-13)

- Technical Director/implementation and asset-production owner: **GPT-6** in this project. Gemini is **not** an asset author or default implementation dependency.
- Finish MVP implementation before the consolidated Unity runtime/visual/device QA pass. Continue using engine-free CI, deterministic economy/save tests, and static Unity contract checks on each source revision. **Never label deferred Unity execution as PASS or mark a milestone CLOSED without its required proof.**
- Design for clean assembly boundaries and reusable components. No new gameplay-engine copy per save schema, no hidden gameplay balance values in UI, no unnecessary duplicate code, no magic IDs or undeclared coupling. Immutable historical replay implementations are a justified exception.
- Production character motion must use **articulated cutout/skeletal rigs and genuine pose/limb animation**, reusable clip/state transitions and fitting interactions. Sliding a rigid whole-body sprite is not acceptable character-walk animation.
- Golden asset workflow remains spec -> generated candidate -> art direction/cleanup -> layered/rig-ready source -> Unity import -> animation -> final visual/device QA. Do not treat a flattened concept render as a production rig or an imported sprite as a finished animated character.
- Owner explicitly authorized merging M15 continuation via PR #54 on 2026-10-13 and requested subsequent implementation **directly on `main`**. Do not move work back to a feature branch without a newer owner instruction. Inspect the remote `main` HEAD before every write; use a current blob SHA/optimistic branch lease to avoid overwriting others. A merge is not a milestone closure; retain failed/deferred evidence and keep test/visual gates truthful.

## Project invariants
- Unity 6.3 LTS.
- Universal 2D / URP.
- C#.
- Portrait-first mobile.
- Runtime game UI defaults to uGUI + TMP.
- Career and Skill are separate domains.
- Employed workdays fast-forward; evening is startup/study time.
- Unemployed/full-time founder can use the full day.
- Career scenes use deterministic percentage/quota scheduling, not unbounded daily RNG.
- Business side-hustle compatibility is data-driven.
- Player interaction remains intentionally light.
- Simulation code must be testable without a loaded Unity scene.
- Balance values belong in data/config, never presentation code.
- Save schema changes require migration coverage.
- Raw generated art must never be referenced by production scenes.
- Vietnamese-facing visual content must load `vietnam-art-direction`.

## Required session closeout
Every implementation session must record:

```text
Skills used:
Tests:
Visual evidence:
Save impact:
Known limitations:
Files changed:
```

Do not mark a milestone item complete without verification evidence.

## Model ownership
- Astra: architecture, cross-system design, escalation, milestone audit.
- GPT-5.6 Sol: default Unity implementation owner.
- Gemini 3.8 Flash High: high-volume content/analysis worker; no architecture authority.
- Dedicated image-generation model: renders source imagery; does not approve production assets.

## Parallel editing
Never allow two agents to modify the same Unity scene/prefab/system concurrently.
Use separate branches/worktrees and domain ownership.

## Art
Production art pipeline:
art spec → Golden Asset → best-of-N → QA → cleanup/layers → asset contract → Unity import → BeautyCell screenshot → approved registry.

## Vietnamization
Do not use "generic Asian" art as a substitute for Vietnamese context.
Do not bake AI-generated Vietnamese text into final art.
Use real localized text in Unity/TMP or controlled texture authoring.
