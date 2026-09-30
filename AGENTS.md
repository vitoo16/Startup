# AGENTS.md — STARTUP LIFE

This repository follows `docs/AI_PRODUCTION_WORKFLOW.md`.

## Mandatory first action
Before modifying code, scenes, prefabs, ScriptableObjects or production assets:

1. identify the task domain;
2. load `startup-life-session-orchestrator`;
3. load every required domain skill;
4. read the active milestone and acceptance criteria.

**No skill = no implementation.**

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
