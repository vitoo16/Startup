# STARTUP LIFE — AI PRODUCTION WORKFLOW v1
**Status:** Mandatory project operating model  
**Research snapshot:** 2026-09-30  
**Project stack:** Unity 6.3 LTS + Universal 2D/URP + C# + uGUI/TMP + Unity 2D Animation  
**Art direction:** HD 2D chibi, cutout/skeletal animation, AI-assisted but art-directed  
**Platforms:** iOS + Android  

---

## 0. Non-negotiable rule

> **NO SKILL = NO IMPLEMENTATION.**

Every work session must:

1. read project state;
2. classify the task;
3. load the required Agent Skills;
4. state the session owner model;
5. define acceptance criteria;
6. implement one coherent slice;
7. compile/test;
8. perform visual QA when anything visible changed;
9. update project state/checklists;
10. record `Skills used`;
11. commit only verified work.

An agent that edits project code/assets before completing steps 1–4 has violated the project workflow.

---

# 1. AI team model

The project uses different models as different "team roles." Do not use every model for every task.

## 1.1 Astra — Lead Architect / Principal Reviewer

Use Astra for the hardest, highest-leverage decisions:

- system architecture;
- large cross-cutting refactors;
- save-version/migration design;
- economy architecture;
- simulation boundaries;
- release-blocking root-cause analysis;
- milestone design review;
- difficult technical trade-offs;
- final audit of a major vertical slice;
- art-direction/spec review when a decision changes the entire asset family.

Do **not** waste Astra on:
- repetitive ScriptableObject content;
- renaming;
- bulk descriptions;
- routine UI wiring;
- straightforward feature implementation already covered by a spec.

### Astra escalation triggers
Escalate a task to Astra when one of these is true:

- change touches 3+ architecture modules;
- save compatibility may break;
- two normal debugging attempts failed;
- performance regression has no obvious cause;
- a feature requires changing a locked GDD rule;
- a generated asset family is visibly drifting and the current art spec is insufficient;
- milestone is about to be declared complete.

---

## 1.2 GPT-5.6 Sol — Primary Senior Unity Engineer

Sol is the **default implementation owner**.

Use Sol for:

- Unity/C# gameplay implementation;
- simulation services;
- ScriptableObjects and content schemas;
- uGUI/TMP runtime screens;
- save/load;
- Unity Test Framework tests;
- career/skills/business/economy systems;
- MCP/Unity Editor operations;
- debugging;
- animation integration;
- sprite/import configuration;
- code review;
- PR preparation;
- documentation updates tied to implementation.

Sol should usually own one complete vertical slice from code → tests → Unity verification.

### Default reasoning policy
- Routine feature: normal/medium.
- Architecture-sensitive feature: high.
- Debugging after first failure: high.
- Final milestone review: Astra, not another blind Sol pass.

---

## 1.3 Gemini 3.8 Flash High — Fast Parallel Content & Analysis Worker

Use Gemini Flash High for **high-volume, low-risk work**:

- ingesting large documentation/context;
- generating first-pass content tables;
- career scene copy;
- event variants;
- course descriptions;
- NPC/customer flavor text;
- localization drafts;
- economy test matrices;
- generating many prompt variants for image generation;
- visual first-pass critique of many screenshots/assets;
- checking content consistency against schemas;
- producing test-case inventories.

Use it as a **worker**, not architecture authority.

Gemini must not independently:
- change core architecture;
- change save schema;
- change locked GDD rules;
- approve a Golden Asset by itself;
- merge code that it did not compile/test.

### Handoff rule
Gemini output enters the repo only after:
- schema validation, and
- Sol integration/review,
or after a deterministic import script validates it.

---

# 2. Model routing table

| Task | Owner | Optional reviewer/helper |
|---|---|---|
| GDD/core mechanic decision | Astra | Sol |
| Project architecture | Astra | Sol |
| Normal Unity feature | Sol | Gemini for content |
| Cross-system refactor | Astra plans → Sol executes | Astra final review |
| Career system | Sol | Astra only if architecture changes |
| Skill system | Sol | Gemini batch content |
| Economy/business simulation | Sol | Astra for formula/system boundary |
| Save/version migration | Astra | Sol implementation |
| UI screen | Sol | Gemini visual/content critique |
| Bulk event writing | Gemini | Sol schema/logic review |
| Localization draft | Gemini | Sol integration + human spot check |
| Bug fixing | Sol | Astra after escalation trigger |
| Performance/profiling | Sol | Astra if systemic |
| Art bible | Astra + human | Gemini reference analysis |
| Asset prompt batches | Gemini | Astra for Golden Asset family |
| Image rendering | Dedicated image model | Gemini/Sol visual QA |
| Unity asset import/rig | Sol | Unity skills |
| Release QA | Sol | Astra milestone audit |

---

# 3. Parallel-agent rules

Parallelism is allowed only when tasks do not edit the same state.

## Safe parallel examples
- Sol implements CareerService.
- Gemini prepares career-scene data JSON.
- Image generator produces background candidates.

## Unsafe parallel examples
- Astra and Sol both refactor SaveService.
- Gemini and Sol both edit the same ScriptableObject definitions.
- Two agents edit the same Unity scene/prefab at once.

## Git rule
If parallel coding is needed:
- use separate branch/worktree;
- assign file/domain ownership;
- merge only after each branch is independently verified.

One task always has exactly one **owner model**.

---

# 4. Required skill layers

The project has four skill layers.

## Layer A — Official Unity skills: mandatory foundation

Install:

```bash
npx skills add Unity-Technologies/skills
```

Use official skills whenever their domain matches.

Core skills to keep installed:
- `new-unity-project`
- `unity-cli`
- `unity-package-management`
- `ui`
- `ui-ugui`
- `localization`
- `sprite-editor`
- relevant Unity build/test/package skills exposed by the installed collection

Official Unity skills are the preferred source when they overlap a community skill.

---

## Layer B — Unity game-production skills

Recommended community pack:
`tea-x-random/unity-game-skills`

Important:
- community-maintained, not Unity-official;
- review before granting broad permissions;
- pin a known-good commit/version in production;
- do not silently auto-update during a milestone.

Core project set:
- `unity-game-director`
- `unity-mcp-bridge`
- `unity-mcp-skill`
- `unity-project-setup`
- `unity-gameplay-systems`
- `unity-game-economy`
- `unity-art-direction`
- `unity-asset-designer`
- `unity-asset-pipeline`
- `unity-scene-composition`
- `unity-animation`
- `unity-ui-designer`
- `unity-debug-profiler`
- `unity-qa-release`
- `unity-localization`

Optional later:
- `unity-image-generator`
- `unity-audio-generator`
- `unity-analytics-liveops`
- `unity-monetization`
- `unity-aso-growth`

The community pack is useful because it defines an orchestrated production flow and can drive a live Unity Editor via MCP.

---

## Layer C — AutoSkills

Run:

```bash
npx autoskills --dry-run
```

then review before installation.

**Do not depend on AutoSkills for Unity.**
As of the research snapshot, AutoSkills does not list Unity as a supported technology entry. It does list C# and many general technologies.

Use AutoSkills only as a supplementary detector for:
- C# helper skills,
- backend technology later,
- test/tooling dependencies later.

Never let AutoSkills replace the project's pinned Unity skill manifest.

---

## Layer D — Startup Life custom skills

These live in this bundle:

- `startup-life-session-orchestrator`
- `startup-life-gameplay-guardian`
- `vietnam-art-direction`
- `startup-life-asset-quality-gate`

They override generic defaults when project-specific intent is at stake.

---

# 5. Mandatory skill routing by task

## Project setup / packages
Required:
- `startup-life-session-orchestrator`
- `new-unity-project`
- `unity-cli`
- `unity-package-management`
- `unity-project-setup`
- `unity-mcp-bridge`

## Career / Skill / Time gameplay
Required:
- `startup-life-session-orchestrator`
- `startup-life-gameplay-guardian`
- `unity-game-director`
- `unity-gameplay-systems`
- `unity-mcp-bridge`

Add:
- `unity-game-economy` when money/rewards are affected.

## Economy / Business
Required:
- `startup-life-session-orchestrator`
- `startup-life-gameplay-guardian`
- `unity-game-economy`
- `unity-gameplay-systems`
- `unity-mcp-bridge`
- `unity-qa-release`

## Runtime UI
Required:
- `startup-life-session-orchestrator`
- official `ui`
- official `ui-ugui`
- `unity-ui-designer`
- `unity-mcp-bridge`

Add:
- `vietnam-art-direction` if visual style/text/cultural presentation changes.

## Character/environment art
Required:
- `startup-life-session-orchestrator`
- `vietnam-art-direction`
- `startup-life-asset-quality-gate`
- `unity-art-direction`
- `unity-asset-designer`
- `unity-image-generator` when generating imagery
- `unity-asset-pipeline`
- `unity-scene-composition`

## Sprite/cutout integration
Required:
- all art skills above
- official `sprite-editor`
- `unity-animation`
- `unity-mcp-bridge`

## Localization
Required:
- `startup-life-session-orchestrator`
- `vietnam-art-direction`
- official `localization`
- `unity-localization`

## Debug/performance
Required:
- `startup-life-session-orchestrator`
- `unity-debug-profiler`
- `unity-mcp-bridge`
- `unity-qa-release`

## Release build
Required:
- `startup-life-session-orchestrator`
- `unity-debug-profiler`
- `unity-qa-release`
- official Unity CLI/build skills
- `unity-localization`

---

# 6. Session lifecycle

Every AI session follows this exact lifecycle.

## SESSION_START

Read in order:

1. `AGENTS.md`
2. `docs/GDD.md`
3. `docs/MVP_PLAN.md`
4. `docs/AI_PRODUCTION_WORKFLOW.md`
5. `docs/SKILLS_MANIFEST.md`
6. current milestone/task file
7. relevant code/assets
8. latest test/CI state

Then output internally or in the work log:

```text
SESSION
Owner model:
Task:
Milestone:
Files/domains expected:
Required skills:
Acceptance criteria:
Risks:
Out of scope:
```

The session may not edit files until `Required skills` is populated.

---

## PLAN

Plan only the smallest coherent vertical slice.

Good:
> Employed workday timeskip + Developer quota scenes + Career XP + day-summary evidence.

Bad:
> Finish career system.

Define:
- behavior;
- data changes;
- tests;
- visual proof;
- save impact.

---

## IMPLEMENT

Rules:
- follow existing architecture;
- simulation remains independent from MonoBehaviour;
- no balance constants hidden in view code;
- one responsibility per service;
- inspect before editing;
- small compile-safe changes;
- never rebuild a scene/prefab just to fix one property.

---

## VERIFY_CODE

Minimum:
- Unity compilation clean;
- relevant EditMode tests;
- relevant PlayMode tests;
- no new warnings in touched code;
- deterministic simulation tests when applicable.

For save changes:
- migration test mandatory.

For economy changes:
- boundary and regression test mandatory.

---

## VERIFY_VISUAL

Required whenever the user can see the change.

Capture:
- target phone aspect;
- at least one normal state;
- edge state if relevant;
- safe-area state for UI.

Check:
- hierarchy;
- readability;
- clipping;
- touch target;
- Vietnamese diacritics;
- art-style consistency;
- no placeholder/generated junk accidentally shipped.

Visual change without screenshot/evidence is incomplete.

---

## UPDATE_STATE

Update:
- milestone checklist;
- plan tick marks;
- any GDD decision changed through approved process;
- balance notes if applicable;
- session log.

Record:

```text
Skills used:
- ...
- ...

Tests:
- ...

Visual evidence:
- ...

Known limitations:
- ...
```

---

## COMMIT

Commit only if verified.

Commit should represent one coherent slice.

Examples:
- `feat: add deterministic career scene scheduler`
- `feat: add evening course progression`
- `fix: preserve scene deck state across saves`
- `test: cover business side-hustle compatibility`

---

# 7. AI art policy

## 7.1 Can the game use AI-generated assets?

**Yes, but AI is an art-production tool, not the art director.**

AI may produce:
- concept art;
- exploration;
- backgrounds;
- props;
- icon candidates;
- character turnaround candidates;
- clothing ideas;
- static illustrations;
- source images for cutout reconstruction;
- texture/reference material.

AI output must not automatically become a production asset.

---

## 7.2 Dedicated image models

`Gemini 3.8 Flash High` is used primarily for text/multimodal analysis, batch prompting and critique.

For actual image rendering use a dedicated image-generation model/service.

Preferred workflow:
- **Gemini image model** through `unity-image-generator` for high-volume non-pixel 2D exploration and source art.
- A high-fidelity image-editing model for difficult consistency/edit passes when available.
- Never force a text model to act as an image renderer if the host does not expose image output.

The community Unity image skill is explicitly designed around a `GEMINI_API_KEY` for non-pixel 2D image generation.

---

# 8. Anti-"AI slop" asset workflow

The project uses a production funnel.

```text
REFERENCE RESEARCH
      ↓
ART SPEC
      ↓
GOLDEN SCREEN / GOLDEN ASSET
      ↓
BEST-OF-N GENERATION
      ↓
CRITIQUE + REJECT
      ↓
CLEANUP / LAYER RECONSTRUCTION
      ↓
ASSET CONTRACT
      ↓
UNITY IMPORT
      ↓
RIG / ANIMATION
      ↓
BEAUTY-CELL SCREENSHOT
      ↓
QUALITY GATE
      ↓
APPROVED ASSET REGISTRY
```

Raw AI files are never referenced directly by game scenes.

---

# 9. Art Bible lock

Before generating an asset family, lock:

## Shape language
- cute chibi;
- large head / compact body;
- clean silhouette;
- simple readable hands;
- limited micro-detail;
- mobile readability first.

## Rendering
- soft hand-painted/vector-like finish;
- restrained highlights;
- soft shadows;
- no hyper-glossy "AI 3D cartoon" look;
- no random cinematic depth of field in runtime sprites.

## Palette
Use a controlled project palette.
Each location may have a sub-palette, but it must inherit the global palette.

## Line behavior
Decide one:
- clean colored outline, or
- mostly lineless.

Do not mix styles asset-by-asset.

## Proportions
Create numeric references:
- head/body ratio;
- eye placement;
- hand/foot size;
- prop scale;
- environment door/table/chair scale.

AI prompts must reference these constraints.

---

# 10. Golden Asset strategy

Never generate 100 production assets before approving one.

Create:

1. one Golden Character;
2. one Golden Home/Night Startup scene;
3. one Golden Office scene;
4. one Golden Business scene;
5. one Golden UI screen.

These five targets define the product's visual DNA.

A new asset family must visually fit these targets.

If it cannot, fix the spec before scaling generation.

---

# 11. Vietnam localization / visual identity

The game should feel **made in/for contemporary Vietnam**, not "generic cute Asian city."

Use `vietnam-art-direction` on every:
- environment;
- prop family;
- Vietnamese UI/copy;
- business scene;
- clothing set;
- food/drink asset;
- signage concept.

## Cultural-detail strategy

Use details in controlled layers.

### Layer 1 — structural
Examples:
- Vietnamese urban storefront proportions;
- mixed-use shop/home layouts;
- narrow frontage where appropriate;
- motorbike-oriented streets/parking;
- office/café spatial cues familiar in Vietnam.

### Layer 2 — everyday props
Examples:
- helmets;
- scooters/motorbikes;
- plastic/metal café furniture when contextually appropriate;
- delivery bags;
- QR-payment stand;
- desk fans/air conditioners;
- thermos/flasks;
- familiar takeaway cup/bag proportions.

### Layer 3 — food & commerce
Examples:
- phin coffee;
- milk coffee;
- iced tea;
- bánh mì / rice/noodle food cues where context fits;
- Vietnamese-style online packing and delivery context;
- local café/kiosk display language.

### Layer 4 — written language
Use real Vietnamese text through TMP/UI overlays.

**Do not rely on an image model to bake Vietnamese signs into the art.**
AI-generated letters are too inconsistent and make art look synthetic.

### Layer 5 — behavior
Examples:
- after-work night side hustle;
- motorbike commute presentation;
- delivery pickup;
- small online-store packing;
- office lunch/coffee rhythms.

Vietnamization must affect gameplay presentation, not only decorative flags.

---

# 12. Avoiding stereotype soup

A Vietnamese scene does not need:
- conical hats everywhere;
- flags everywhere;
- lanterns everywhere;
- all iconic foods in one frame.

Rule:
> **Use 2–4 contextually correct local anchors per scene, not every cultural symbol at once.**

A modern developer office in Vietnam should still look like a modern developer office.
Vietnamese identity comes from subtle environment, language, objects and behavior.

---

# 13. Text and signage rule

For production scenes:
- image generators create signboards **without final text**;
- Unity overlays or texture-authoring step adds correct Vietnamese text;
- use a licensed font with full Vietnamese diacritic coverage;
- test: `ă â đ ê ô ơ ư á à ả ã ạ`.

Never ship:
- malformed accents;
- fake Vietnamese;
- generated gibberish labels.

---

# 14. Character asset pipeline

## Stage A — reference
Create:
- front;
- 3/4;
- side when needed;
- expression sheet;
- outfit callout;
- palette.

## Stage B — consistency pass
Use image editing/reference conditioning to keep:
- face shape;
- hairstyle;
- eye style;
- body ratio;
- palette.

Reject if any defining feature drifts.

## Stage C — layer reconstruction
Final runtime character should be reconstructed into controlled layers:

```text
Head
Face
Eyes
Brows
Mouth
HairBack
Body
Top
Bottom
Shoes
HairFront
Accessory
```

The layered PSB/PSB-like source is a controlled production artifact.

Do not expect one generated PNG to magically become a robust animation rig.

## Stage D — Unity
- PSD/PSB Importer;
- Sprite Library;
- 2D Animation bones;
- reusable animation clips;
- role-specific props.

---

# 15. Asset quality gate

Every asset receives scores 0–2:

| Criterion | 0 | 1 | 2 |
|---|---|---|---|
| Style match | wrong | near | matches Golden Asset |
| Silhouette | poor | usable | clear |
| Palette | drift | minor drift | compliant |
| Vietnam context | generic/wrong | partial | natural/local |
| Anatomy/perspective | broken | acceptable | clean |
| Production readiness | raw | cleanup needed | clean |
| Mobile readability | poor | okay | strong |
| Reusability | one-off | partial | system-friendly |

Minimum:
- production asset ≥ 13/16;
- Golden Asset ≥ 15/16;
- no criterion may be `0`.

If `Vietnam context = 0`, reject regardless of total score for assets that are supposed to carry local identity.

---

# 16. Best-of-N rule

Production generations should create multiple candidates.

Default:
- simple prop: N=3;
- environment: N=4;
- character/outfit: N=6;
- Golden Asset: N=8–12 over multiple controlled rounds.

Do not keep the first "pretty enough" generation.

Record:
- prompt version;
- seed/reference where supported;
- selected candidate;
- rejection reasons.

This allows art direction to improve instead of prompting randomly.

---

# 17. What AI should NOT generate as unattended final output

Do not auto-ship:
- logos/trademarks;
- legal text;
- app-store screenshots;
- production Vietnamese signage text;
- currency notes;
- complex repeating character rigs;
- final animation timing;
- culturally sensitive references;
- any asset with obvious anatomy/perspective errors.

AI can prepare drafts; production gate still applies.

---

# 18. Scene construction workflow

1. Lock gameplay purpose.
2. Load `vietnam-art-direction`.
3. Build graybox/composition.
4. Capture composition screenshot.
5. Approve focal hierarchy.
6. Generate/source assets by surface.
7. Run asset quality gate.
8. Import approved assets.
9. Compose using only approved registry.
10. Add animation.
11. Capture BeautyCell screenshot.
12. Compare to Golden Screens.
13. Fix before declaring done.

Do not generate assets first and hope they compose later.

---

# 19. Gameplay-content generation workflow

For careers, events, courses and business definitions:

## Step 1
Sol defines schema and invariant rules.

## Step 2
Gemini generates batches **inside the schema**.

## Step 3
Automated validator checks:
- required fields;
- enum values;
- percentages;
- duplicate IDs;
- localization keys;
- value bounds.

## Step 4
Sol checks gameplay meaning.

## Step 5
PlayMode/EditMode simulation verifies that content does not create:
- impossible schedules;
- negative runaway economy;
- duplicate rewards;
- broken save serialization.

This is how the project gains content volume without turning into AI-written sludge.

---

# 20. Example model workflows

## Example A — Add Marketing career

**Owner:** Sol  
**Skills:**
- session orchestrator;
- gameplay guardian;
- unity-gameplay-systems;
- unity-mcp-bridge;
- qa-release.

Flow:
1. Sol defines Marketing career data and tests.
2. Gemini drafts 20 scene descriptions in the fixed quota schema.
3. Sol selects/edits and imports.
4. Unity simulation runs a 20-scene quota test.
5. Scene UI/animation is verified.
6. Commit.

---

## Example B — Create Vietnamese café environment

**Owner:** Sol for integration; Astra approves art direction if new family  
**Skills:**
- session orchestrator;
- vietnam-art-direction;
- art-direction;
- asset-designer;
- image-generator;
- asset-pipeline;
- scene-composition;
- asset-quality-gate;
- animation if applicable.

Flow:
1. Graybox the café.
2. Write asset contracts.
3. Gemini creates prompt matrix.
4. Dedicated image model renders candidates.
5. Gemini does cheap first-pass rejection.
6. Astra/human approves Golden candidate family.
7. Cleanup/layering.
8. Sol imports and configures sprites.
9. BeautyCell screenshot.
10. Quality gate.
11. Approved registry.

---

## Example C — Fix save corruption

**Owner:** Sol first; Astra escalated if architecture issue  
**Skills:**
- session orchestrator;
- gameplay guardian;
- debug-profiler;
- qa-release.

Flow:
1. Reproduce from save fixture.
2. Write failing migration/load test.
3. Fix.
4. Test old/current save versions.
5. If root cause crosses schema boundaries → Astra review.
6. Commit only with migration evidence.

---

# 21. Definition of Done for an AI-generated feature

A feature is not done because code exists.

It is done when:

- required skills were used;
- acceptance criteria pass;
- Unity compiles;
- tests pass;
- visible change has screenshot evidence;
- save compatibility checked if applicable;
- no architecture rule was violated;
- no raw generated asset leaks into production;
- Vietnamization checked if relevant;
- docs/checklist updated;
- `Skills used` logged;
- coherent commit created.

---

# 22. Research basis

Verified on 2026-09-30:

- Unity 6.3 LTS is supported through December 2027.
- Unity publishes an official reusable Agent Skills collection and recommends using it with Unity CLI for Editor/build/test workflows.
- Official Unity skills include UI/localization/sprite-oriented workflows.
- AutoSkills currently does not list Unity in its supported technology registry, so it is supplementary rather than the Unity skill source.
- The community `unity-game-skills` pack includes gameplay, economy, art direction, generated image, asset pipeline, animation, mobile UI, profiler, localization and release/QA skills; its generative image skill uses Gemini and explicitly treats generated art as source that must be promoted through an asset pipeline.
- Unity's PSD Importer is designed for multi-sprite character animation, and Unity 2D Animation provides sprite skeletal-animation tooling.
- Dedicated Gemini image models support image generation/editing; a text-oriented Gemini Flash model should not be assumed to be the renderer unless the chosen host exposes an image-output model.

Sources:
- https://unity.com/releases/unity-6/support
- https://github.com/Unity-Technologies/skills
- https://www.autoskills.sh/
- https://github.com/tea-x-random/unity-game-skills
- https://docs.unity3d.com/current/Manual/com.unity.2d.animation.html
- https://docs.unity3d.com/6000.0/Manual/com.unity.2d.psdimporter.html
- https://ai.google.dev/gemini-api/docs/image-generation
