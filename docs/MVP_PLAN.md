# STARTUP LIFE — MVP PLAN v1
**Working title:** Startup Life  
**Document status:** Implementation-ready MVP scope  
**Target:** iOS + Android  
**Primary orientation:** Portrait 9:16  
**Engine decision:** Unity 6.3 LTS + Universal 2D (URP) + C#  
**Art direction:** HD 2D chibi / cutout sprites, AI-assisted source art, skeletal animation  
**Core principle:** The player watches a life/career/business simulation unfold and makes a small number of meaningful decisions. The player does **not** micromanage professional work.

---

## 1. Product thesis

Startup Life is a light life/business simulation where the player creates a character, works a normal career, builds soft skills and savings, then starts a side business or eventually leaves employment to run a startup full time.

The differentiator is that the character's **career history actually matters**:

- Career is a separate progression track from soft skills.
- Working builds tenure, salary, career milestones and scene history.
- Career milestones can unlock soft skills for free.
- Courses can buy the same skills, but cost significant money and study time.
- A business can be started while employed only if that business is compatible with after-work operation.
- Quitting a job opens daytime business operation.
- The same world, workplace, shops and career history persist across years rather than resetting when the player changes paths.

The game should feel closer to a cozy simulator than a spreadsheet.

---

## 2. MVP design pillars

### 2.1 Low interaction burden
The player should rarely need more than 2–4 meaningful decisions in one in-game day.

No:
- typing code,
- cooking recipes manually,
- moving inventory item by item,
- manually serving customers,
- complex accounting,
- dozens of stat sliders.

Yes:
- choose job,
- choose course,
- choose whether to save or invest,
- choose side business,
- choose pricing posture,
- choose when to quit,
- choose whether to reinvest.

### 2.2 Time should feel alive
The clock runs continuously through a day.

**Employed**
- Morning before work: short transition.
- Work hours: fast time-skip with career scenes.
- Evening/night: normal-speed startup/study time.
- Weekend: longer playable period.

**Unemployed / full-time founder**
- Morning, afternoon and evening are visible.
- The character performs business/study/search activities across the full day.

### 2.3 Watch the character live the decisions
The simulation is visual.

Examples:
- Developer: coding, daily meeting, presentation, client call.
- Marketing: campaign planning, content review, presentation, analytics review.
- Chef: prep, cooking, plating, supplier discussion.
- Sales: calls, pitch, meeting, follow-up.
- Office/Admin: documentation, meeting, reporting, coordination.
- Barista/F&B staff: serving, preparing drinks, cleaning, customer interaction.

The player sees scenes, money changes and progress rather than directly performing the professional task.

### 2.4 Career and skill are separate
**Career track** = profession, salary, tenure, seniority, work scene pool.  
**Skill track** = transferable soft/business skills.

They must never be merged into one level system.

---

# 3. MVP player loop

```text
Create Character
      ↓
Choose starting age + starting path
      ↓
Get / start a job
      ↓
Workdays auto-progress
      ↓
Salary + tenure + career milestones
      ↓
Study / save money at night
      ↓
Unlock or purchase transferable skills
      ↓
Start an eligible side hustle
      ↓
Job by day + startup by night
      ↓
Grow cash flow
      ↓
Decide whether to remain employed or resign
      ↓
Full-time founder mode
      ↓
Grow first sustainable business
```

The MVP success condition is not "become a billionaire."

The first playable arc is complete when the player can experience:

1. employment,
2. career progression,
3. skill progression,
4. saving,
5. starting a side business,
6. running job + business together,
7. resigning,
8. operating the business full-time.

---

# 4. Character creation — MVP

## 4.1 Player inputs
- Gender presentation: Male / Female.
- Age: 18–40.
- Name.
- Hair preset.
- Outfit preset.
- Starting path.

## 4.2 Age rules
Age primarily changes:
- visual age variant,
- available prior-work-history options,
- starting savings package,
- believable career seniority options.

Age should **not** directly make the character "bad."

Suggested MVP bands:

| Age | Starting condition |
|---|---|
| 18–22 | Starts with almost no savings; must begin with entry-level work |
| 23–29 | May choose a short prior career history and modest savings |
| 30–40 | May choose a longer prior career history and higher savings |

The exact numbers must be balancing data, not hard-coded.

## 4.3 Character learning stat
Character has one simple modifier:

`LearningSpeed`

It controls the amount of in-game study time needed to gain a skill level.

Sources:
- starting trait/background,
- selected age/path package,
- later permanent bonuses.

Do not add IQ, intelligence, hunger, thirst or other life-sim stats in MVP.

---

# 5. Career system — MVP

## 5.1 MVP professions
Start with six careers:

1. Software Developer
2. Marketing Executive
3. Sales Representative
4. Office / Administrative Staff
5. Cook / Chef
6. Barista / F&B Staff

Plus:
- Unemployed
- Full-time Founder

## 5.2 Career data
Each career definition contains:

```text
CareerId
DisplayName
WorkStart
WorkEnd
BaseSalary
SalaryGrowth
CareerXpCurve
PromotionThresholds
SceneDistribution
FreeSkillMilestones
BusinessAffinityTags
```

## 5.3 Career progression
Career has its own:
- Career XP
- Tenure in months
- Seniority
- Salary
- Employer history

Suggested ranks:
- Junior
- Experienced
- Senior
- Lead

Promotion should be mostly automatic when tenure + Career XP thresholds are met.

The player should not need to solve interview minigames in MVP.

---

# 6. Profession scene scheduler

The user specifically should **not** feel that a random number generator is arbitrarily deciding the workday.

Use a **quota-deck scheduler**.

Example: Developer scene distribution across a 20-scene cycle:

| Scene | Target share | Slots / 20 |
|---|---:|---:|
| Coding | 40% | 8 |
| Team meeting | 20% | 4 |
| Bug fixing | 15% | 3 |
| Client discussion | 10% | 2 |
| Presentation / demo | 10% | 2 |
| Documentation | 5% | 1 |

The scheduler:
1. builds the quota deck,
2. distributes scenes to avoid repetitive streaks,
3. stores the deck index in the save,
4. refreshes the deck after completion,
5. can modify distribution when rank changes.

This gives percentage-based distribution without pure daily RNG.

Each scene can award:
- Career XP,
- small Skill XP,
- salary/performance modifier,
- visual feedback.

Scene duration on screen: roughly 3–8 seconds before the next accelerated work segment.

---

# 7. Time system — MVP

## 7.1 Day phases

Suggested simulation phases:

```text
06:00–08:00   Morning
08:00–17:30   Workday
18:00–23:30   Evening / Startup / Study
23:30–06:00   Sleep / day transition
```

Exact times are presentation data.

## 7.2 Employed weekday
- Character automatically goes to work.
- Work block runs at accelerated speed.
- Career scenes interrupt the timeskip briefly.
- Salary is accrued.
- At evening, the game returns to normal visual pace.
- Player can perform startup/study actions.

## 7.3 Unemployed weekday
- No mandatory timeskip.
- Morning, afternoon and evening can be used for:
  - job search,
  - courses,
  - starting/operating business,
  - business growth.

## 7.4 Weekend
- No normal office work.
- Extended startup/study period.
- Some F&B professions may have weekend shifts depending on career data.

## 7.5 No energy micromanagement in MVP
Do not add:
- hunger,
- stamina bar,
- sleep meter,
- hygiene.

Time itself is the main constraint.

---

# 8. Skill system — MVP

## 8.1 MVP skills
Use six transferable skills:

1. Communication
2. Negotiation
3. Time Management
4. Problem Solving
5. Networking
6. Leadership

Each skill: Level 0–5.

## 8.2 Skill acquisition methods

### Career milestone
Free.

Example:
- 6 months in Sales → Negotiation Lv1.
- 12 months in Marketing → Communication Lv1.
- Lead promotion → Leadership Lv1.

### Work-scene exposure
Small passive progress.

Examples:
- presentation scenes → Communication XP,
- client meetings → Negotiation XP,
- team coordination → Leadership XP.

### Paid course
Costs:
- money,
- study hours.

Course learning duration:

```text
EffectiveStudyTime =
BaseCourseHours / LearningSpeed
```

Paid learning must be meaningfully expensive enough that career-earned skills feel valuable.

## 8.3 Skill effect philosophy
Avoid giant skill trees in MVP.

Each skill should change simple simulation coefficients.

Examples:
- Negotiation → lower supplier cost / better salary offers.
- Communication → higher customer conversion / stronger presentation outcomes.
- Time Management → more efficient night startup progress.
- Problem Solving → lower operational loss.
- Networking → more opportunities and customer referrals.
- Leadership → improves employee/manager performance later.

---

# 9. Business system — MVP

## 9.1 Side-hustle compatibility is mandatory
Every business type has:

```text
OperationMode:
- SideHustleCompatible
- FullTimeRequired
- ManagerOperable
```

While the player holds a normal full-time job:
- only `SideHustleCompatible` businesses can be personally operated,
- full-time businesses remain locked.

After resignation:
- full-time business types can operate.

The manager exception is a post-MVP system.

## 9.2 MVP businesses

### A. Online Reselling / Small Online Store
- side-hustle compatible,
- evening operation,
- marketing + communication affinity,
- low entry capital.

### B. Home Food Preorder
- side-hustle compatible,
- chef/F&B career affinity,
- evening prep + scheduled order fulfilment,
- medium entry capital.

### C. Freelance Service
- side-hustle compatible,
- developer/marketing/office affinity,
- almost no inventory,
- low entry capital.

### D. Small Coffee Kiosk
- full-time required,
- demonstrates the "quit job → founder" transition,
- simple staff/customer visuals,
- available after minimum capital threshold.

## 9.3 Player controls
The player should control only:
- start / close business,
- initial investment amount,
- simple pricing stance: Budget / Standard / Premium,
- reinvestment toggle or amount,
- whether to keep job or resign.

No detailed recipe, SKU or ad-campaign editor in MVP.

---

# 10. Business simulation — MVP

Each business runs from a small set of values:

```text
BaseDemand
Quality
Reputation
PricePosition
Capacity
MarketingReach
OperatingCost
SkillModifiers
CareerAffinity
```

Example simplified demand:

```text
DemandScore =
BaseDemand
× CustomerFit
× ReputationModifier
× MarketingModifier
× PriceModifier
× EventModifier
```

Revenue:

```text
Orders = min(DemandScore, Capacity)
Revenue = Orders × AverageOrderValue
Profit = Revenue - VariableCost - FixedCost
```

The player sees:
- customers,
- orders/transactions,
- revenue popups,
- daily summary,
- business growth.

The math stays under the hood.

---

# 11. Customer simulation — MVP

Do not simulate hundreds of autonomous customer agents.

Use **aggregated customer segments**, then render a few representative NPCs.

MVP segments:
- Budget-sensitive
- Convenience-focused
- Quality-focused
- Trend-aware

Each segment has:
- price preference,
- quality preference,
- channel preference,
- repeat-purchase tendency.

Rendered customer sprites are visual representatives of the aggregate demand result.

This keeps the game performant and predictable on mobile.

---

# 12. Employee system — MVP scope

Full employee AI is **not part of MVP**.

MVP Coffee Kiosk may use one simplified helper NPC with:
- fixed wage,
- fixed efficiency,
- visual work animation.

No:
- recruitment market,
- morale,
- promotion,
- manager automation,
- complex shift scheduling.

Those belong to Early Access.

---

# 13. Event system — MVP

Events should add flavor, not punish the player heavily.

MVP event categories:
- salary raise opportunity,
- small unexpected expense,
- customer compliment,
- negative review,
- supplier discount,
- viral social post,
- overtime week,
- course discount.

Rules:
- maximum event frequency,
- cooldown by category,
- no catastrophic bankruptcy event,
- seeded selection so saves are reproducible,
- events may be conditioned by profession/business/skill.

Career scenes remain quota-driven; events are a separate system.

---

# 14. Economy — MVP

Currencies:
- Cash only.

Income:
- salary,
- business revenue.

Expenses:
- living cost,
- course fees,
- business startup capital,
- business operating costs.

MVP should avoid:
- stock market,
- crypto,
- complex taxes,
- mortgage,
- bank loans,
- insurance,
- multiple currencies.

Recommended salary flow:
- salary accrues daily,
- displayed as monthly employment income,
- paid on a fixed monthly payday.

Business revenue:
- summarized daily.

Living expenses:
- deducted monthly.

This creates an understandable personal cash-flow rhythm.

---

# 15. UI screens — MVP

## Core navigation
Bottom navigation:

1. Life
2. Career
3. Skills
4. Business
5. Finance

## Screens

### Life / Today
- current date/time,
- current character scene,
- current activity,
- cash,
- quick timeline,
- next meaningful event/action.

### Career
- current job,
- employer,
- salary,
- tenure,
- seniority,
- Career XP,
- scene history,
- career milestone rewards,
- quit button.

### Skills
- six skills,
- level,
- progress,
- sources of progress,
- available courses,
- course price and study duration.

### Business
- current businesses,
- side-hustle compatibility,
- daily revenue/profit,
- simple pricing stance,
- reinvest control,
- start/close/resign prompts.

### Finance
- cash,
- salary,
- business income,
- expenses,
- month summary.

### Character
- name,
- age,
- appearance,
- career history,
- business history.

### Day Summary
At sleep/day transition:
- salary accrued,
- business revenue,
- expenses,
- Career XP,
- Skill XP,
- milestone unlocks.

---

# 16. Art direction — MVP

## 16.1 Style
Use:
- cute HD chibi,
- clean shapes,
- soft shading,
- strong silhouette,
- layered clothing/hair,
- readable expressions,
- cozy mobile UI.

Avoid full 3D for MVP.

Avoid pixel art until the team deliberately chooses it as the final brand direction.

## 16.2 Character pipeline
Recommended pipeline:

```text
AI concept / reference
      ↓
Approved character turnaround
      ↓
Layered PSB:
body / face / hair / top / bottom / shoes / accessories
      ↓
Unity PSD Importer
      ↓
Unity 2D Animation rig
      ↓
Reusable animation clips
      ↓
Sprite Library variants
```

MVP animations:
- idle,
- walking transition,
- typing,
- meeting/talking,
- presentation,
- phone call,
- cooking/prep,
- serving,
- packing orders,
- laptop night work,
- celebration.

## 16.3 Environment scenes
MVP environment set:
- bedroom/home desk,
- software office,
- generic meeting room,
- marketing/office workspace,
- sales/client room,
- kitchen,
- café counter,
- home business packing table,
- coffee kiosk.

Reuse backgrounds aggressively.

---

# 17. Technical stack — MVP

## Engine
- Unity 6.3 LTS.
- Universal 2D / URP.
- C#.
- IL2CPP for release builds.

## Unity packages
- Universal Render Pipeline.
- 2D Animation.
- 2D PSD Importer.
- TextMeshPro.
- Input System.
- Sprite Atlas.
- Unity Test Framework.

## UI
- uGUI + TextMeshPro.

Reason:
- mature runtime UI,
- straightforward mobile safe-area handling,
- good fit for scene + panel hybrid layout,
- strong Agent Skill coverage.

## Data
Use ScriptableObjects for immutable game definitions:
- careers,
- profession scenes,
- skills,
- courses,
- business types,
- customer segments,
- events.

Runtime player state should be plain serializable C# data models.

## Backend
None required for MVP.

MVP is offline/local-first.

This reduces failure modes and allows the game loop to be validated before paying the complexity cost of cloud services.

---

# 18. Save architecture — MVP

Save at:
- end of each in-game day,
- app pause,
- app quit,
- major transaction.

Save structure:

```text
SaveRoot
├─ SaveVersion
├─ CharacterState
├─ CalendarState
├─ CareerState
├─ SkillState
├─ FinanceState
├─ BusinessState
├─ SceneSchedulerState
├─ EventState
└─ SettingsState
```

Requirements:
- versioned schema,
- migration interface,
- atomic write via temporary file,
- previous-save backup,
- deterministic scene/event seeds stored,
- never serialize Unity scene objects directly.

Recommended:
- JSON during development for inspectability.
- Compression optional later.

---

# 19. Code architecture

Keep simulation logic independent from rendering.

Suggested assemblies/modules:

```text
StartupLife.Core
StartupLife.Simulation
StartupLife.Content
StartupLife.Presentation
StartupLife.Infrastructure
StartupLife.Tests
StartupLife.Editor
```

Core services:

```text
GameClockService
CareerService
CareerSceneScheduler
SkillService
CourseService
BusinessService
EconomyService
CustomerDemandService
EventService
SaveService
```

Important rule:

`Simulation must be able to advance one day in EditMode tests without loading a Unity scene.`

This will make balancing, testing and AI-agent implementation much safer.

---

# 20. Agent Skill stack

## Mandatory foundation — official Unity skills
Install the official Unity skill collection first.

Use for:
- project creation,
- Unity CLI,
- package management,
- UI,
- 2D setup,
- builds/tests,
- later cloud/live-service integration.

Key skills:
- `new-unity-project`
- `unity-cli`
- `unity-package-management`
- `ui`
- `ui-ugui`
- `urp-postprocessing`
- `build-live-game` later
- `localization` later
- `implement-in-app-purchases` later

## Recommended game-production skills
Use a Unity game skill pack for:
- orchestration,
- gameplay architecture,
- art direction,
- generated asset validation,
- animation,
- UI,
- profiling,
- economy,
- QA/release.

Recommended skills:
- `unity-game-director`
- `unity-mcp-bridge`
- `unity-project-setup`
- `unity-gameplay-systems`
- `unity-game-economy`
- `unity-art-direction`
- `unity-asset-pipeline`
- `unity-scene-composition`
- `unity-animation`
- `unity-ui-designer`
- `unity-debug-profiler`
- `unity-qa-release`
- `unity-image-generator` optional
- `unity-audio-generator` optional

## Why Unity won the stack decision
Godot currently has strong community agent tooling, including agent-first CLI/MCP projects, but Unity has:
1. official reusable Agent Skills,
2. official CLI-oriented project workflows,
3. mature mobile export,
4. strong 2D skeletal/PSD pipeline,
5. deeper community skill coverage for game art, QA and shipping.

For this project, AI-agent maintainability is a first-class requirement, so the combination matters more than raw engine size.

---

# 21. MVP implementation roadmap

## M0 — Project foundation
- [ ] Install Unity 6.3 LTS with Android + iOS modules.
- [ ] Create Universal 2D project.
- [ ] Configure Git + Unity `.gitignore`.
- [ ] Create asmdef/module structure.
- [ ] Add test assemblies.
- [ ] Add agent instructions and approved skill set.
- [ ] Lock portrait reference layout.
- [ ] Create placeholder visual baseline.

## M1 — Pure simulation core
- [ ] Calendar/day model.
- [ ] Employed vs unemployed schedule.
- [ ] Money ledger.
- [ ] Career definitions.
- [ ] Career XP/tenure/promotion.
- [ ] Skill progression.
- [ ] Course time/cost calculation.
- [ ] Unit/EditMode tests.

## M2 — Career scene system
- [ ] Quota-deck scheduler.
- [ ] Developer scenes.
- [ ] Marketing scenes.
- [ ] Sales scenes.
- [ ] Office scenes.
- [ ] Chef scenes.
- [ ] Barista scenes.
- [ ] Workday fast-forward presentation.
- [ ] Scene reward hooks.

## M3 — Mobile shell
- [ ] Life screen.
- [ ] Career screen.
- [ ] Skills screen.
- [ ] Finance screen.
- [ ] Bottom navigation.
- [ ] Safe area.
- [ ] Day summary.
- [ ] Save/load.

## M4 — Side business simulation
- [ ] Business definitions.
- [ ] Compatibility rules.
- [ ] Online Store.
- [ ] Home Food Preorder.
- [ ] Freelance Service.
- [ ] Demand calculation.
- [ ] Daily revenue/profit.
- [ ] Night startup loop.
- [ ] Job + side hustle combined flow.

## M5 — Founder transition
- [ ] Resignation.
- [ ] Full-day founder schedule.
- [ ] Coffee Kiosk.
- [ ] Simple helper NPC.
- [ ] Full-time business scenes.
- [ ] Career history persistence.

## M6 — Art vertical slice
- [ ] Final art spec.
- [ ] One golden character.
- [ ] Male/female variants.
- [ ] Age variants.
- [ ] Layered PSB pipeline.
- [ ] Core animation clips.
- [ ] Core environments.
- [ ] Sprite atlas/import rules.
- [ ] Visual QA screenshots.

## M7 — Events + balancing
- [ ] Event engine.
- [ ] 15–20 MVP events.
- [ ] Salary tuning.
- [ ] Course-cost tuning.
- [ ] Business margin tuning.
- [ ] First-year pacing.
- [ ] Prevent dead-end saves.

## M8 — Device QA
- [ ] Android physical-device test.
- [ ] iOS physical-device test.
- [ ] Safe-area tests.
- [ ] Save interruption tests.
- [ ] Low-memory resume tests.
- [ ] Performance profiling.
- [ ] Release candidate build.

---

# 22. MVP acceptance criteria

MVP is complete only when a fresh player can:

- create a character,
- choose age/start path,
- hold a career,
- watch work scenes and workday timeskip,
- earn salary,
- gain tenure and promotion,
- gain at least one free career-earned skill,
- buy at least one course,
- see course duration change with LearningSpeed,
- start a valid side hustle,
- run job by day and startup by night,
- be blocked from starting an incompatible full-time business,
- quit the job,
- switch into full-day founder schedule,
- run the Coffee Kiosk,
- save and resume without losing the deterministic schedule,
- complete this arc without complex micromanagement.

---

# 23. Explicitly NOT in MVP

- multiplayer,
- stock market,
- loans,
- investors,
- equity cap table,
- dating/family simulation,
- real estate,
- advanced employee AI,
- advanced inventory,
- detailed manufacturing,
- multiple cities,
- PvP rankings,
- full cloud backend,
- complex taxes,
- user-generated businesses,
- 3D world exploration.

These may be reconsidered after the core loop proves fun.

---

# 24. Research references used for the stack decision

Checked: 2026-09-30.

- Unity 6 release/support: https://unity.com/releases/unity-6
- Unity 6 support policy: https://unity.com/releases/unity-6/support
- Unity official Agent Skills: https://github.com/Unity-Technologies/skills
- Unity 2D Animation package: https://docs.unity3d.com/current/Manual/com.unity.2d.animation.html
- Unity PSD Importer: https://docs.unity3d.com/6000.0/Manual/com.unity.2d.psdimporter.html
- Unity ScriptableObject: https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html
- Community Unity game skills research: https://github.com/tea-x-random/unity-game-skills
- Godot agent CLI/skill reference considered: https://github.com/aigengame/godot-agent
- Godot game-dev agent skill considered: https://github.com/IvanMurzak/ai-game-dev-plugin
