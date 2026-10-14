# AGENTS.md — STARTUP LIFE

This repository follows `docs/AI_PRODUCTION_WORKFLOW.md`.

## Mandatory first action
Before modifying code, scenes, prefabs, ScriptableObjects or production assets:

1. identify the task domain;
2. load `startup-life-session-orchestrator`;
3. load every required domain skill;
4. read the active milestone and acceptance criteria.

**No skill = no implementation.**

## Current Technical Director directive (updated 2026-10-14)

- Technical Director/implementation and asset-production owner: **GPT-6** in this project. Gemini is **not** an asset author or default implementation dependency.
- **Production-first decision (owner, 2026-10-14):** The active execution roadmap is `docs/PRODUCTION_EXECUTION_ROADMAP.md`. Build toward a quality-gated commercial release, **not "finish MVP first"**. `MVP_PLAN.md` and `IMPLEMENTATION_PLAN_MVP.md` remain historical engineering references. Unity runtime, visual and device QA are now integrated progressive production gates (rather than one final batch). Keep engine-free CI on code revisions. **Never label missing Unity/device evidence PASS or close a milestone without proof.**
- Design for clean assembly boundaries and reusable components. No new gameplay-engine copy per save schema, no hidden gameplay balance values in UI, no unnecessary duplicate code, no magic IDs or undeclared coupling. Immutable historical replay implementations are a justified exception.
- **New visual target: original cute pixel art** with contemporary Vietnamese identity per `docs/PRODUCTION_PIXEL_STYLE_CONTRACT.md`. Existing M13 non-pixel Unity style spec remains the active technical contract only until the explicit W03 style/validator migration succeeds. Characters require reusable articulated pixel cutout/skeletal rigs or verified joint-moving replacement-pose sprites and genuine limb motion; rigid whole-body translation is not walk animation.
- Golden asset workflow: original brief -> provenance/right-to-use evidence -> distinct candidates -> independent originality review -> layered/rig-ready source -> Unity import -> real motion -> device BeautyCell -> art approval -> registry. Follow `docs/PRODUCTION_ASSET_IP_POLICY.md`. No generator may self-approve; no claim of absolute worldwide uniqueness is possible or accepted as proof.
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
- Shipping visual/audio/fonts/UI must pass independent originality, commercial licensing and provenance clearance; documentation or static CI alone never approves an asset.
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
- GPT-6: current Technical Director, architecture + implementation owner on `main` unless an owner instruction changes this.
- Other assistants/models may review or draft under delegated tasks, but are not required authorities and may not merge or self-approve artwork.
- Independent human artistic/IP reviewers approve shipping assets; a model used to generate imagery never approves its own output.

## Parallel editing
Never allow two agents to modify the same Unity scene/prefab/system concurrently.
Use non-overlapping file ownership and serializable SHA-checked writes on owner-authorized `main`; separate worktrees only when explicitly coordinated.

## Art
Production art pipeline:
art spec → Golden Asset → best-of-N → QA → cleanup/layers → asset contract → Unity import → BeautyCell screenshot → approved registry.

## Vietnamization
Do not use "generic Asian" art as a substitute for Vietnamese context.
Do not bake AI-generated Vietnamese text into final art.
Use real localized text in Unity/TMP or controlled texture authoring.

## Mandatory audio direction and owner listening approval (2026-10-15)

- Audio authority: `docs/SOUND_DIRECTION_PRODUCTION_PLAN.md`. This covers all music, sound effects, vocalizations, ambience, audio mix changes and audio used in videos/store media; IP policy remains binding.
- **Silence is NOT consent.** No owner reply means `AWAITING_OWNER_REVIEW` or `NOT_STARTED`, NEVER `APPROVED`. The generating/implementing AI may not self-approve; a green CI check or written plan is not listening proof.
- At each Golden cue, representative audio family, significant audible revision, integrated gameplay mix and release candidate, **notify owner explicitly in user-facing conversation** with verified playable audio and in-game context, version/ID, rights information and a clear `APPROVE / REVISE / REJECT` decision request. A repository TODO/issue alone is not sufficient proof of notification; never claim a notification was delivered without evidence.
- If audio cannot actually be rendered/auditioned, report `BLOCKED_NO_PREVIEW` or `NOT_TESTED` with reason. Do not fake a sample or approval. Owner feedback on one asset/version does not approve other assets or changed versions.
- Never ship audio without owner sign-off **and** independent rights/provenance clearance and device/audio QA. Keep pending asset/mix work visibly open; implement unrelated tasks if feasible.
- Extend audio session closeout with: `Audio assets produced:`, `Preview evidence:`, `Owner notified:`, `Owner review state:`, `Audio QA / device evidence:`, `Audio rights clearance:`.
