# Startup Life — Foundation model handoffs

Date: 2026-09-30  
Maintainer: GPT-5.6 Sol  
Purpose: keep model ownership explicit while Sol builds the technical foundation. These are handoff prompts, not proof that Astra or Gemini executed the work.

## Current routing

- **Sol owns now:** repository/Unity foundation, deterministic verification, CI, build/test entrypoints, integration, fixes, and evidence.
- **Astra:** hold for one architecture/foundation audit after the current foundation stack is stable. Astra should review; it should not race Sol by editing the same files.
- **Gemini 3.8 Flash High:** use only for isolated read-only/batch analysis until content schemas are frozen. Gemini output is advisory until Sol validates/integrates it.

## Handoff A — Astra foundation + architecture audit

**Status:** READY LATER — run after Sol finishes the foundation stack and before declaring the foundation/core architecture milestone complete.

**Why Astra:** this crosses repository foundation, assembly boundaries, deterministic simulation, save compatibility, CI/build seams, and later feature dependencies. The project workflow assigns architecture and milestone audits to Astra.

### Prompt for Astra

```text
You are the Principal Architect / milestone reviewer for Startup Life.

Repository:
https://github.com/vitoo16/Startup.git

Your task is REVIEW ONLY unless I explicitly ask you to edit code. Do not rewrite the implementation just because you prefer another style.

Read in this order:
1. AGENTS.md
2. docs/GDD.md
3. docs/MVP_PLAN.md
4. docs/AI_PRODUCTION_WORKFLOW.md
5. docs/SKILLS_MANIFEST.md
6. docs/IMPLEMENTATION_PLAN_MVP.md
7. docs/adr/*
8. docs/architecture/*
9. docs/evidence/M0-T02/*, M0-T03/*, M0-T04/*
10. the current Unity project settings, Packages manifest/lock, asmdefs, Core/Simulation/Application/Infrastructure code, tests, CI workflows, and scripts.

Mandatory rules:
- respect the locked Unity 6.3 LTS + Universal 2D + C# architecture;
- do not merge Career and Skill;
- simulation must remain scene/MonoBehaviour independent;
- deterministic scheduler/event RNG streams and transactional commands are intentional;
- local-first/offline MVP and nonnegative cash + arrears are locked;
- do not treat engine-free .NET checks as Unity EditMode/PlayMode proof;
- do not mark a roadmap task complete without matching evidence.

Review these areas:
A. assembly/dependency boundaries and cycles;
B. command/result/outcome transactional semantics and retry/idempotency;
C. deterministic time, quota-deck scheduling, independent RNG streams;
D. save/checkpoint schema, atomic storage, migration and compatibility boundaries;
E. economy/arreas/career/study interaction invariants;
F. Unity presentation/content adapters leaking into simulation;
G. package/project setup reproducibility and CI/build seams;
H. whether provisional M1-M6 backend code has drifted from ADRs/GDD;
I. missing architecture decisions that must be resolved BEFORE Sol expands gameplay;
J. milestone risks that would be expensive to fix after UI/content/art expansion.

Return:
1. Executive assessment (no score).
2. Findings grouped as BLOCKER / HIGH / MEDIUM / LOW.
3. For every finding: exact file/symbol, concrete failure scenario, why it violates a locked rule or creates future risk, and the smallest recommended correction.
4. "Keep as-is" decisions where the current design is sound, so Sol does not churn good code.
5. A dependency-ordered fix queue for Sol.
6. A final checklist titled "Safe to continue gameplay implementation when...".

Do not invent test results. Distinguish static review from verified runtime evidence.
```

## Handoff B — Gemini foundation consistency/test-matrix pass

**Status:** OPTIONAL NOW / useful before Astra, but read-only. Do not let Gemini edit core architecture.

**Why Gemini:** this is high-volume, low-risk cross-checking: enumerate contracts, test cases, localization strings, and coverage gaps. Sol remains responsible for deciding what enters the repo.

### Prompt for Gemini 3.8 Flash High

```text
Act as a read-only QA/content-analysis worker for Startup Life.

Repository:
https://github.com/vitoo16/Startup.git

Do NOT modify code, architecture, save schema, GDD rules, or task checkboxes.
Read AGENTS.md, docs/GDD.md, docs/MVP_PLAN.md, docs/AI_PRODUCTION_WORKFLOW.md, docs/IMPLEMENTATION_PLAN_MVP.md, ADRs, current tests, and evidence.

Produce a coverage inventory for the foundation + first playable:
- list every locked invariant that should have a deterministic automated test;
- map each invariant to existing .NET/EditMode/PlayMode/static checks when present;
- identify missing tests without claiming they fail;
- build boundary-case matrices for age 18/40, month/leap boundaries, work/weekend schedules, course/study time, salary/payday/arrears, retry/idempotency, save corruption/migration, scheduler quotas, independent RNG streams, suspension/resume, Vietnamese strings/glyphs, safe-area/aspect ratios;
- identify user-facing strings that should be localization keys rather than literals;
- flag duplicated or contradictory content requirements between GDD/MVP/implementation ledger, but do not resolve them yourself.

Return tables that Sol can consume:
1. Invariant -> existing evidence -> missing coverage -> recommended test type.
2. Edge-case matrix with setup/action/expected invariant.
3. Localization/key audit.
4. Questions requiring Astra judgement (architecture only).
5. Items safe for Gemini batch work later after schemas freeze.

Do not fabricate Unity runtime results. A source file existing is not proof that a Unity test passed.
```

## Gemini work deliberately deferred until schemas freeze

Gemini is a good fit later for:
- six-career scene/content tables;
- event variants up to the MVP target;
- Vietnamese localization first drafts;
- content-schema consistency sweeps;
- economy test matrices;
- asset prompt batches and screenshot critique.

Gemini is **not** the owner for architecture, save migrations, core economy math, Unity integration, or merge approval.

## Handoff log

| Date | Model | Task | State | Result |
|---|---|---|---|---|
| 2026-09-30 | Astra | Foundation + architecture audit | queued after Sol foundation | not run |
| 2026-09-30 | Gemini 3.8 Flash High | Foundation coverage inventory | optional/read-only | not run |
