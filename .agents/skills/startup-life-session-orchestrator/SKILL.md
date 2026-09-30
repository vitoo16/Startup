---
name: startup-life-session-orchestrator
description: Mandatory at the beginning and end of every Startup Life implementation session. Routes the task to the correct project, Unity, gameplay, art, UI, QA, localization and asset skills and prevents work from starting without the required skills.
---

# Startup Life Session Orchestrator

## Mandatory trigger
Use for every session that will change:
- code;
- Unity scene/prefab;
- ScriptableObject/content data;
- art/audio;
- UI;
- build/release configuration;
- project documentation that changes implementation rules.

## Before editing

1. Read:
   - `AGENTS.md`
   - `docs/GDD.md`
   - `docs/MVP_PLAN.md`
   - `docs/AI_PRODUCTION_WORKFLOW.md`
   - `docs/SKILLS_MANIFEST.md`
   - current milestone/task state.

2. Classify task:
   - setup;
   - gameplay;
   - economy;
   - save;
   - UI;
   - art;
   - animation;
   - localization;
   - debug/performance;
   - release.

3. Produce a required-skill list.

4. Load those skills before changing files.

5. Define:
   - one owner model;
   - acceptance criteria;
   - out-of-scope items;
   - save impact;
   - visual QA need.

If a matching required skill is unavailable:
- do not silently proceed as if it exists;
- use the official/general alternative only when safe;
- otherwise install/research the missing skill first.

## During implementation
- inspect state before editing;
- work as one coherent slice;
- compile after risky structural changes;
- do not let secondary models edit the owner's files concurrently.

## Before completion
Require:
- compilation;
- relevant tests;
- screenshot/visual evidence for visible work;
- save migration evidence if needed;
- project-invariant check;
- docs/checklist update.

## Session closeout
Record:

```text
Owner model:
Skills used:
Acceptance criteria:
Tests:
Visual evidence:
Save impact:
Known limitations:
Files changed:
Commit:
```

If `Skills used` is empty, the session is invalid.
