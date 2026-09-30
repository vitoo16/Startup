# STARTUP LIFE — FULL GAME DESIGN DOCUMENT v1
**Working title:** Startup Life  
**Genre:** Cozy life simulation + career simulation + light business management  
**Platform:** iOS + Android  
**Orientation:** Portrait-first 9:16  
**Primary engine:** Unity 6.3 LTS, Universal 2D (URP), C#  
**Art:** HD 2D chibi / cutout skeletal animation  
**Core audience:** Players who enjoy progression, life simulators and tycoon games but do not want spreadsheet-heavy micromanagement  
**Document goal:** Define the full product direction from MVP → Early Access → 1.0 while keeping the core loop simple

---

# 1. High concept

The player creates a person, not a company.

That person has:
- an age,
- a visual identity,
- a career history,
- savings,
- transferable skills,
- professional tenure,
- and eventually one or more businesses.

The game follows the player's life over calendar time.

A normal employed day mostly passes automatically:
- work hours fast-forward,
- profession-specific scenes appear,
- salary and progress accumulate.

At night, the player regains control over meaningful life decisions:
- study,
- save,
- launch a compatible side hustle,
- reinvest,
- or prepare to resign.

If unemployed or a full-time founder, the entire day becomes available.

The player is never asked to perform the real technical work of a developer, chef, marketer or salesperson. The game shows the character doing that work.

---

# 2. Product identity

## 2.1 Fantasy
"I want to see how a person with a particular background can gradually build a career, learn useful skills, save capital and turn a side project into a real business."

## 2.2 What makes the game distinct
Most tycoon games start with a business.

Startup Life starts earlier:
- before the business,
- while employed,
- while saving,
- while learning,
- while deciding whether it is safe to resign.

The player's earlier life remains mechanically relevant.

A chef does not receive a magical "restaurant class."
Instead:
- culinary career history changes relevant scene experience,
- communication/leadership/time-management skills carry across careers,
- savings determine startup timing,
- prior employment can create business affinity and opportunity.

## 2.3 Tone
- warm,
- optimistic,
- lightly humorous,
- aspirational,
- not brutally punishing.

Failure should slow the story, not erase it.

---

# 3. Design pillars

## Pillar A — Simple decisions, visible consequences
The player makes a few decisions.
The simulation performs the detailed work.

## Pillar B — Career history matters
Profession is not cosmetic.
Tenure, salary, career scenes and career-earned skills persist.

## Pillar C — Time is the main resource
The conflict is not "do I have enough energy points?"
It is:
- do I spend this evening learning,
- working on the side business,
- or saving money by staying employed longer?

## Pillar D — The character is always visible
Scenes communicate work and progress.

## Pillar E — Side hustle before founder
The game celebrates the common path:
job → savings → night project → traction → resignation → founder.

## Pillar F — No fake complexity
The game can simulate deep systems internally while exposing simple controls.

---

# 4. Anti-pillars

The game should not become:

- a coding minigame,
- a cooking minigame,
- a detailed accounting package,
- a manual staff-shift spreadsheet,
- an inventory clicker,
- a survival life-sim,
- a real-time 3D open world,
- a "watch ad to continue every 30 seconds" game.

---

# 5. Core gameplay loop

```text
LIFE
↓
Career / Job
↓
Salary + Tenure + Professional Experience
↓
Transferable Skill Growth
↓
Savings
↓
Night Study or Side Hustle
↓
Business Revenue / Traction
↓
Decision: keep job or resign
↓
Full-time Founder
↓
Hire / automate / expand
↓
Open another business or improve current company
↓
Life continues
```

A player can stay employed for years if desired.
Founding a company is a path, not an immediate mandatory tutorial step.

---

# 6. Calendar and time system

## 6.1 Time scale
Game time uses:
- year,
- month,
- day,
- day-of-week,
- hour.

The actual visual clock can be compressed and tuned for session length.

## 6.2 Day states

### Employed — weekday
```text
Morning
→ commute/transition
→ workday fast-forward
→ several profession vignettes
→ evening
→ startup/study/player decisions
→ sleep
```

### Unemployed
```text
Morning
→ available
→ afternoon
→ available
→ evening
→ available
→ sleep
```

### Full-time founder
```text
Morning business block
→ afternoon business block
→ evening optional business/study/personal block
→ sleep
```

### Weekend
Job rules vary by profession.
Traditional office careers usually have no weekday-style work block.
Hospitality careers may include shifts.

## 6.3 Time skip presentation
Work should not disappear behind a loading screen.

During fast-forward:
- office/career scene plays,
- clock accelerates,
- Career XP increments,
- salary accrual subtly updates,
- scene outcome floats briefly,
- background switches if necessary.

The goal is to make "going to work" satisfying even though it is mostly automatic.

---

# 7. Character creation

## 7.1 Inputs
- Name.
- Male / Female presentation at v1.
- Age.
- Hair.
- Face preset.
- Skin-tone palette.
- Starting outfit.
- Starting path/background.

Future versions can expand body/identity options without changing the core architecture.

## 7.2 Age
Age decides:
- character visual variant,
- how much previous career history is plausible,
- which starting career ranks are available,
- starting savings packages,
- narrative flavor.

Age should not impose arbitrary harsh debuffs.

Suggested starting groups:

### 18–22
- little or no starting savings,
- entry-level job path,
- minimal prior tenure.

### 23–29
- short previous-career history,
- small savings,
- can begin at Junior/Experienced depending on background.

### 30–40
- longer work history,
- more savings,
- more career milestone history.

### 41+ — post-MVP/EA
Possible later.
Needs dedicated art variants and balancing.

## 7.3 Character persistent attributes
Keep attributes minimal.

Recommended:
- LearningSpeed
- OpportunityRate modifier
- BaseLivingCost tier

Avoid a large RPG attribute sheet.

---

# 8. Career and profession system

Career is distinct from Skill.

## 8.1 Career contains
- profession,
- employer,
- salary,
- tenure,
- Career XP,
- seniority,
- work schedule,
- scene distribution,
- professional milestones,
- career history record.

## 8.2 Career ranks
Suggested generic rank ladder:

1. Entry / Junior
2. Experienced
3. Senior
4. Lead / Supervisor
5. Manager — only for careers where it makes sense

Individual careers may rename ranks in content data.

## 8.3 Promotion
Promotion is driven primarily by:
- tenure threshold,
- Career XP,
- required transferable skill floor,
- occasional opportunity event.

It is not based on twitch gameplay.

## 8.4 Job changes
Player can:
- stay,
- apply to another employer,
- switch careers,
- resign,
- become unemployed,
- become full-time founder.

Changing careers preserves:
- transferable skills,
- complete work history,
- savings,
- contacts/opportunity flags.

Career XP is career-specific.

---

# 9. Initial profession catalog

## MVP
- Software Developer
- Marketing Executive
- Sales Representative
- Office / Administrative Staff
- Cook / Chef
- Barista / F&B Staff

## Early Access expansion
- Graphic Designer
- Accountant
- HR / Recruiter
- Logistics / Procurement
- Customer Service
- Content Creator
- Retail Staff
- Product / Project Coordinator

## 1.0 expansion candidates
- Engineer
- Teacher / Trainer
- Consultant
- Photographer / Media
- Beauty Technician
- Real-estate Sales
- Hospitality Manager

Each added profession must justify itself with:
1. unique scene distribution,
2. career-to-business affinity,
3. at least one meaningful skill progression pattern.

---

# 10. Profession scene system

## 10.1 Purpose
Profession scenes:
- make work visually interesting,
- communicate career identity,
- reward the player,
- teach what the character has been doing,
- create a visual timeline of the character's life.

## 10.2 Not pure random
Use deterministic percentage quotas.

Each career defines a scene distribution.

### Developer example
- Coding: 40%
- Team meeting: 20%
- Bug fixing: 15%
- Client meeting: 10%
- Presentation/demo: 10%
- Documentation: 5%

### Marketing example
- Campaign planning: 25%
- Content review: 20%
- Analytics: 20%
- Team meeting: 15%
- Client presentation: 10%
- Partner/influencer communication: 10%

### Chef example
- Preparation: 25%
- Cooking: 30%
- Plating: 10%
- Team coordination: 10%
- Supplier discussion: 10%
- Rush period: 15%

## 10.3 Quota-deck algorithm
For a configurable cycle, e.g. 20 scenes:
- convert percentages into scene slots,
- build the deck,
- distribute to limit repeating scenes,
- use a deterministic seed only for ordering,
- persist deck position,
- rebuild on rank/career change.

This gives stable percentages without feeling like a slot machine.

## 10.4 Scene rewards
A scene can contribute:
- Career XP,
- transferable Skill XP,
- employer reputation,
- opportunity flags.

Scene rewards stay modest.
The career itself remains the main progression source.

---

# 11. Skill system

## 11.1 Philosophy
Skills are transferable personal capabilities.
They are not job titles.

A chef and a developer can both have:
- Communication,
- Leadership,
- Time Management.

## 11.2 Full skill list
Recommended v1 candidate list:

1. Communication
2. Negotiation
3. Time Management
4. Problem Solving
5. Networking
6. Leadership
7. Adaptability
8. Creativity
9. Financial Discipline
10. Self-Learning

MVP ships with the first six.

## 11.3 Levels
Recommended:
- Lv0 Untrained
- Lv1 Basic
- Lv2 Capable
- Lv3 Strong
- Lv4 Advanced
- Lv5 Expert

Keep the scale short and readable.

## 11.4 Acquisition channels

### A. Career-earned
Free.
Triggered by tenure/rank milestones.

### B. Scene exposure
Slow passive progress.

### C. Paid course
Fastest deliberate route but expensive.

Course requires:
- money,
- study hours,
- prerequisite skill level where needed.

### D. Mentoring / opportunity
Post-MVP:
network events may reduce study time or unlock a level.

## 11.5 Learning time
Each course has:
- BaseHours,
- Price,
- SkillId,
- TargetLevel.

Character LearningSpeed modifies hours.

The game should show:
"Estimated completion: 14 evenings"

rather than only displaying abstract XP.

---

# 12. Career-to-skill examples

| Career | Natural skill growth |
|---|---|
| Developer | Problem Solving, Time Management, Communication |
| Marketing | Communication, Creativity, Networking |
| Sales | Negotiation, Communication, Networking |
| Office/Admin | Time Management, Communication, Problem Solving |
| Chef | Time Management, Leadership, Problem Solving |
| Barista/F&B | Communication, Time Management, Networking |
| HR | Communication, Leadership, Networking |
| Accounting | Financial Discipline, Problem Solving, Time Management |

Career milestones can grant a full skill level where appropriate.

---

# 13. Career and business affinity

Career gives **affinity**, not a hard class lock.

Examples:
- Chef → Food affinity.
- Marketing → Customer Acquisition affinity.
- Developer → Digital Product affinity.
- Sales → B2B/Conversion affinity.
- Logistics → Supplier/Operations affinity.

Affinity can affect:
- startup setup speed,
- base quality,
- acquisition efficiency,
- operating-cost modifier.

A developer can still open a café.
It is simply less naturally advantaged at the beginning.

---

# 14. Side hustle eligibility

This is a central rule.

Every business has an operation requirement.

```text
SideHustleCompatible
FullTimeRequired
ManagerOperable
```

## 14.1 While employed
The character can personally operate only businesses whose required active hours fit around the current job.

Examples of side-hustle-friendly:
- online store,
- freelance service,
- home food preorder,
- content/affiliate,
- small digital product.

Examples normally not compatible:
- full-service café,
- restaurant,
- physical retail store,
- salon,
- manufacturing workshop.

## 14.2 After hiring management — later game
A physical business may eventually become `ManagerOperable`.

If the player hires a capable manager:
- the shop can remain open while the character works another job,
- the manager receives salary,
- efficiency depends on manager capability,
- owner loses some direct-control bonus.

This creates a meaningful late-game exception without destroying the initial job/side-hustle rule.

---

# 15. Business catalog

## MVP
1. Online Store / Reselling
2. Home Food Preorder
3. Freelance Service
4. Coffee Kiosk

## Early Access
5. Marketing/Creative Agency
6. Small Café
7. Fashion Store
8. Micro-SaaS / Digital Product
9. Beauty / Personal Service

## 1.0 candidates
10. Restaurant
11. Retail chain
12. Production workshop / small manufacturing
13. Training / education business
14. Logistics/service company
15. Franchise ownership

The design rule is:
new business types should create different schedules/scenes/affinities, not merely different icons.

---

# 16. Business interaction model

The player should not manage every operational detail.

For most businesses the player controls:

- startup decision,
- capital committed,
- pricing posture,
- reinvestment,
- hiring,
- manager assignment,
- expansion/relocation,
- whether to remain employed.

The simulation handles:
- orders,
- customer flow,
- production work,
- routine marketing,
- daily operations.

Optional higher-level decisions can appear as events.

---

# 17. Business progression model

Suggested stages:

1. Idea
2. Side Project
3. Operating
4. Stable
5. Growing
6. Established

Stage is based on:
- revenue history,
- reputation,
- capacity,
- sustained profitability.

Do not use a single arbitrary "Business Level" as the only metric.

---

# 18. Economy

## 18.1 Main player resources
Primary:
- Cash

Derived:
- Monthly Salary
- Monthly Business Profit
- Net Worth
- Recurring Expenses

## 18.2 Salary
Career salary is:
- profession-based,
- rank-based,
- employer-modified,
- skill/opportunity-modified.

Salary accrues through the workday and pays on a monthly date.

## 18.3 Living expense
Living cost is a monthly sink.

Keep it simple:
- Basic
- Comfortable
- Premium lifestyle tiers later.

MVP should use one automatic living-cost tier.

## 18.4 Business cash flow
Business income occurs daily.
Fixed expenses can be:
- rent,
- payroll,
- platform/software cost.

Variable expenses:
- materials,
- fulfilment,
- supplier cost.

## 18.5 Course economy
Courses need to feel costly.

The player should sometimes ask:
"Do I pay for this course now, or work six more weeks until my career unlocks the same skill?"

That tension is intentional.

## 18.6 Inflation/economic index — later
A simple yearly economic index can scale:
- salaries,
- living cost,
- rent,
- prices.

Do not model macroeconomics deeply.

---

# 19. Customer AI / demand model

Customer AI should be a simulation model, not hundreds of expensive autonomous agents.

## 19.1 Segments
Candidate segments:
- Budget-sensitive
- Convenience-focused
- Quality-focused
- Trend-aware
- Loyal/relationship-driven
- Business/B2B

## 19.2 Segment values
Each segment can have:

```text
PriceSensitivity
QualityWeight
ConvenienceWeight
TrendWeight
BrandTrustWeight
RepeatPurchaseRate
```

## 19.3 Demand
Conceptually:

```text
SegmentDemand =
BaseMarketDemand
× ProductFit
× PriceFit
× Reputation
× Reach
× Seasonality
× EventModifier
```

Total demand sums segment results.

## 19.4 Visual customer NPCs
The scene renders only a small sample:
- entering,
- browsing,
- purchasing,
- eating/drinking,
- leaving.

NPC count is presentation, not authoritative demand.

This protects mobile performance.

---

# 20. Employee AI

## 20.1 Early Access model
Employees have:
- role,
- wage,
- competence,
- reliability,
- morale,
- tenure.

## 20.2 Roles
Examples:
- Staff
- Specialist
- Supervisor
- Manager

## 20.3 Employee simulation
Employees should not require hourly drag-and-drop shift planning.

The player:
- hires,
- assigns broad role,
- adjusts compensation occasionally,
- promotes or dismisses,
- assigns manager.

The system schedules routine work.

## 20.4 Manager
Manager is strategically important.

A manager:
- enables owner absence for certain businesses,
- reduces direct owner operating requirement,
- costs significant salary,
- creates a performance dependency.

## 20.5 Employee scenes
Examples:
- training,
- helping customer,
- team briefing,
- mistake/recovery,
- manager report,
- resignation request.

---

# 21. Employer/world persistence

A career job is not deleted after resignation.

Persist:
- employer name,
- industry,
- dates worked,
- final rank,
- final salary,
- colleagues/contacts if introduced,
- workplace history.

The old employer can later appear in:
- networking events,
- client opportunities,
- references,
- partnerships,
- competitor flavor.

Environment prefabs are reused for production efficiency, but a workplace entity and a player-owned business entity remain distinct game objects/data.

---

# 22. Event system

## 22.1 Event categories
- Career
- Skill/Learning
- Personal finance
- Business
- Customer
- Employee
- Supplier
- Market
- Networking

## 22.2 Event rules
Each event defines:

```text
EventId
RequiredTags
ForbiddenTags
MinYear/Stage
Cooldown
Weight
ChoiceList
OutcomeList
Severity
```

## 22.3 Difficulty philosophy
Events should mostly create variation.

Avoid repeatedly destroying the player's run.

Recommended severity mix:
- 60% flavor/opportunity,
- 30% moderate trade-off,
- 10% significant challenge.

No sudden irreversible wipe with no warning.

## 22.4 Deterministic event model
Store simulation seed.

Use scheduled event windows plus weighted eligible selection.

This means:
- career scenes stay quota-driven,
- events vary runs,
- save/load remains reproducible.

---

# 23. Failure

No conventional "Game Over" for losing a business.

If business fails:
- business closes,
- remaining cash/debt rules apply,
- career and skills remain,
- player can find another job,
- player can start again.

Failure creates biography.

Later versions can award a small:
`Founder Experience`
modifier after a legitimate failed business, but it must not be exploitable by intentionally opening/closing businesses.

---

# 24. UI / UX

## 24.1 Mobile philosophy
One-handed portrait layout.

At any moment:
- top half shows the character/environment/activity,
- bottom area shows only the information/action relevant to the current moment.

## 24.2 Main tabs
Recommended:

- Life
- Career
- Skills
- Business
- Finance

Profile/settings accessed separately.

## 24.3 Life screen
Primary home screen.

Contains:
- date,
- clock,
- location,
- animated character,
- current activity,
- cash,
- timeline,
- next decision.

## 24.4 Career screen
Contains:
- job,
- employer,
- salary,
- tenure,
- Career XP,
- rank,
- career milestones,
- scene distribution/history,
- resign/job-change actions.

## 24.5 Skills
Contains:
- skill grid,
- level,
- progress,
- source of current progress,
- courses,
- money cost,
- estimated completion time.

## 24.6 Business
Contains:
- business cards,
- operating status,
- side-hustle/full-time badge,
- revenue,
- profit,
- reputation,
- capacity,
- pricing posture,
- staff summary,
- reinvest/expand actions.

## 24.7 Finance
Contains:
- cash,
- salary,
- business income,
- living expenses,
- payroll,
- course expenses,
- monthly trend.

Keep charts simple.

## 24.8 Calendar/history
A lightweight life timeline:
- joined company,
- promoted,
- learned skill,
- launched business,
- resigned,
- first profitable month,
- expansion,
- business closure.

This reinforces the personal-story fantasy.

---

# 25. Scene catalog philosophy

A "scene" is a small visual simulation vignette.

Scene consists of:
- environment prefab,
- actor slot(s),
- animation clip,
- props,
- camera profile,
- text label,
- duration,
- reward hooks.

A single office background should support many scenes.

Example:
same meeting-room scene can show:
- developer sprint planning,
- marketing presentation,
- sales pitch,
- manager review.

This keeps content cost under control.

---

# 26. Art direction

## 26.1 Recommended final direction
**HD 2D chibi with cutout skeletal animation.**

Why:
- cute,
- readable on phone,
- lower runtime cost than 3D,
- easier to build many careers/outfits,
- compatible with AI-assisted art,
- easy to reuse animations,
- enough visual personality to avoid looking like a spreadsheet app.

## 26.2 Do not choose Three.js
Three.js is not the right primary runtime for this project.

The game needs:
- mobile game lifecycle,
- animation,
- sprite workflows,
- asset import,
- build tooling,
- scene/prefab systems,
- testing,
- iOS/Android release.

Using Three.js would trade away too much engine infrastructure merely to use a trending web 3D library.

## 26.3 Pixel art
Pixel remains a valid alternative art direction, but it should be a deliberate brand choice rather than a technical shortcut.

For this game, HD layered chibi is preferred because:
- age variants,
- clothing,
- career outfits,
- expressions,
- workplace scenes,
- AI-generated source assets

are easier to manage in a layered cutout pipeline.

---

# 27. AI-assisted art pipeline

Never allow one-off AI images to enter production directly.

Pipeline:

```text
Art brief
↓
art-spec / style bible
↓
Golden character
↓
Turnaround/reference sheet
↓
Approved palette and proportions
↓
Generate/paint variants
↓
Manual cleanup
↓
Layered PSB
↓
Unity import
↓
Rig
↓
Animation
↓
In-game screenshot QA
↓
Approved asset registry
```

## 27.1 Character layers
Recommended:
- body,
- head,
- eyes,
- brows,
- mouth,
- front hair,
- back hair,
- top,
- bottom,
- shoes,
- accessory.

## 27.2 Age
Do not create a fully unique rig for every age.

Create compatible visual groups sharing rig proportions:
- young adult,
- adult,
- mature adult later.

Use face/hair/outfit variants to communicate age while preserving animation reuse.

---

# 28. Animation catalog

Core reusable animations:
- idle,
- walk,
- sit,
- type,
- phone call,
- talk,
- listen,
- presentation,
- point/explain,
- write/document,
- cook/prep,
- serve,
- pack order,
- use laptop at night,
- celebrate,
- worry,
- think.

Business-specific clips can be added later.

---

# 29. Audio

Audio should support the cozy simulation feel.

Needed:
- UI click,
- money tick,
- promotion,
- skill unlock,
- course complete,
- order complete,
- phone/notification,
- office ambience,
- café ambience,
- home-night ambience.

Music can be location/time based:
- daytime career,
- evening startup,
- weekend,
- success milestone.

---

# 30. Technical stack

## 30.1 Engine
**Unity 6.3 LTS**

Reason:
- current LTS,
- mobile production maturity,
- strong 2D tooling,
- iOS/Android support,
- official AI Agent Skills/CLI workflows.

## 30.2 Render
- Universal Render Pipeline.
- Universal 2D template.
- Orthographic camera.
- 2D lights only where visually useful.
- Keep post-processing restrained on mobile.

## 30.3 Language
C#.

## 30.4 UI
- uGUI,
- TextMeshPro,
- safe-area component,
- responsive anchors/layout groups.

UI Toolkit can be used for editor tooling, but runtime mobile UI should default to uGUI unless later profiling/design needs justify change.

## 30.5 Character animation
- Unity 2D Animation.
- Unity 2D PSD Importer.
- Sprite Library / Sprite Resolver.
- Animator controllers.

## 30.6 Content data
ScriptableObjects:
- CareerDefinition
- CareerSceneDefinition
- SkillDefinition
- CourseDefinition
- BusinessDefinition
- CustomerSegmentDefinition
- EmployeeRoleDefinition
- EventDefinition
- EconomyBalanceDefinition

## 30.7 Runtime data
Pure serializable C# classes/records.
Do not make simulation state depend on MonoBehaviour.

---

# 31. Architecture

## 31.1 Layering

```text
Core
  ↓
Simulation
  ↓
Application
  ↓
Presentation
  ↓
Unity Views
```

Infrastructure sits beside application for:
- saves,
- analytics,
- cloud.

## 31.2 Assemblies

```text
StartupLife.Core
StartupLife.Simulation
StartupLife.Application
StartupLife.Content
StartupLife.Presentation
StartupLife.Infrastructure
StartupLife.Editor
StartupLife.Tests.EditMode
StartupLife.Tests.PlayMode
```

## 31.3 Main services

```text
GameClockService
DayPlannerService
CareerService
CareerSceneScheduler
SkillService
CourseService
OpportunityService
BusinessService
BusinessCompatibilityService
EconomyService
CustomerDemandService
EmployeeService
EventService
HistoryService
SaveService
```

## 31.4 Rule
The game simulation must be able to:

```text
AdvanceDay()
AdvanceMonth()
SimulateBusinessDay()
ApplyCareerScene()
```

inside EditMode/unit tests without a loaded scene.

This is essential for balancing and regression safety.

---

# 32. Save architecture

## 32.1 Local-first
MVP and early development use local save.

Path:
`Application.persistentDataPath`

## 32.2 Save model

```text
SaveRoot
├─ Metadata
│  ├─ SaveVersion
│  ├─ CreatedAt
│  ├─ UpdatedAt
│  └─ SimulationSeed
├─ Character
├─ Calendar
├─ Careers
├─ Skills
├─ Courses
├─ Businesses
├─ Employees
├─ Economy
├─ SceneDecks
├─ Events
├─ History
└─ Settings
```

## 32.3 Reliability
- write temp file,
- validate,
- replace primary,
- retain backup,
- autosave on day boundary,
- autosave on app pause,
- autosave after large financial decisions.

## 32.4 Migration
Every save has a version.

Implement:
`ISaveMigration`

Do not break player saves when balancing definitions change.

---

# 33. Cloud architecture — later

MVP requires no backend.

For Early Access / 1.0:
use Unity Gaming Services only where useful:

- Authentication,
- Cloud Save,
- Remote Config,
- Analytics,
- optional Economy.

Cloud is for:
- backup,
- cross-device progression,
- balance tuning,
- analytics.

Do not make a single-player game permanently online.

The player should still be able to open the game and play offline after authentication has been established where platform policy allows.

---

# 34. Analytics — Early Access

Important events:
- character_created,
- job_started,
- job_changed,
- promotion,
- skill_unlocked,
- course_started,
- course_completed,
- business_started,
- side_hustle_started,
- resignation,
- business_profitable,
- business_closed,
- session_day_count.

Funnels:
1. Create character → first job
2. First job → first skill
3. First skill → first side hustle
4. Side hustle → resignation
5. Resignation → profitable full-time business

Do not collect unnecessary personal data.

---

# 35. Performance targets

Because the design is mostly 2D:
- target stable 60 FPS on mid-range devices,
- provide 30 FPS fallback if needed,
- minimize overdraw,
- use sprite atlases,
- pool repetitive NPC visuals,
- unload unused environment assets,
- keep background NPCs cosmetic.

Authoritative simulation should be lightweight enough to run faster than real time during workday skips.

---

# 36. Agent Skill research and chosen workflow

## 36.1 Stack comparison

### Unity
Strengths:
- official reusable Agent Skills,
- Unity CLI,
- editor-driving workflows,
- project/package/build skills,
- mobile UI and live-service skills,
- community skills for art pipeline, gameplay, economy, profiler and QA.

Result:
**Chosen.**

### Godot
Strengths:
- excellent lightweight 2D engine,
- agent-first community tooling exists,
- GDScript skills are increasingly strong.

Weakness for this project:
- agent ecosystem is more community-fragmented,
- less official end-to-end production-skill coverage.

Result:
**Viable fallback, not primary.**

### React Native / Expo
Strengths:
- excellent application UI ecosystem,
- strong AutoSkills coverage.

Weakness:
- not ideal as the main game simulation/rendering engine once animated scenes and game-like content grow.

Result:
**Not primary game runtime.**

### Three.js / React Three Fiber
Strengths:
- extensive Three.js skills in some registries,
- strong 3D web rendering.

Weakness:
- solves the wrong problem for this predominantly 2D mobile sim.

Result:
**Rejected for core runtime.**

### Cocos Creator
Strength:
- TypeScript-friendly game engine.

Weakness:
- agent-skill ecosystem currently less comprehensive than the chosen Unity workflow.

Result:
**Secondary alternative.**

---

# 37. Required Agent Skills

## Tier 1 — official Unity
Install first:
- new-unity-project
- unity-cli
- unity-package-management
- ui
- ui-ugui
- urp-postprocessing
- localization
- build-live-game
- implement-in-app-purchases when needed
- levelplay-unity-integration only if ads are eventually chosen

Official collection:
`npx skills add Unity-Technologies/skills`

## Tier 2 — project execution / studio skills
Recommended:
- unity-game-director
- unity-mcp-bridge
- unity-mcp-skill
- unity-project-setup
- unity-gameplay-systems
- unity-game-economy
- unity-art-direction
- unity-asset-pipeline
- unity-scene-composition
- unity-animation
- unity-ui-designer
- unity-debug-profiler
- unity-qa-release
- unity-localization

## Tier 3 — generative asset skills
Optional:
- unity-image-generator
- unity-audio-generator
- unity-pixel-art if the art direction later changes
- unity-3d-generator only for isolated 3D asset experiments

Generated art must still pass project asset QA.

---

# 38. AI coding rules for the repository

Create project-level `AGENTS.md` with rules such as:

1. Never bypass the simulation layer by writing economy state directly from UI code.
2. Never hard-code profession/business balance values in MonoBehaviours.
3. New content must use definitions/data assets.
4. Every new career needs a scene quota table.
5. Career and Skill remain separate domains.
6. Side-hustle compatibility must be data-driven.
7. No new core system without EditMode tests.
8. Every save schema change needs migration coverage.
9. Never replace approved art with raw generated art.
10. Verify mobile safe areas and portrait layout.
11. Run tests before merge.
12. Prefer small PRs by feature slice.

---

# 39. MVP scope

## Content
- 6 careers,
- 6 skills,
- 4 businesses,
- 4 customer segments,
- 15–20 events,
- ~25–35 work/business visual scenes,
- one city/context,
- local save.

## Systems
- time,
- job fast-forward,
- career progression,
- scene quota decks,
- skills,
- courses,
- finance,
- side hustle,
- resignation/founder transition,
- basic business demand,
- minimal helper employee,
- events.

## Objective
Prove:
"Is it fun to watch a normal work life gradually turn into a startup life with only light player interaction?"

---

# 40. Early Access scope

Target additions:
- 10–12 careers,
- 8–10 skills,
- 7–9 businesses,
- employee system,
- managers,
- job switching,
- multiple employers per profession,
- richer customer segments,
- ~60+ events,
- business upgrades/locations,
- cloud save,
- remote config,
- analytics,
- first localization pass,
- balancing based on real play data.

Key new mechanic:
**Manager enables owner-absent operation.**

This expands the side-hustle concept without invalidating it.

---

# 41. Version 1.0 scope

Target additions:
- 15+ careers,
- 12+ meaningful business models,
- mature employee/manager system,
- richer company expansion,
- supplier/partner relationships,
- broader economic variation,
- long-form life history,
- achievements,
- cross-device cloud save,
- localization,
- polished accessibility,
- mature onboarding,
- release monetization only if it does not damage the core experience.

Potential 1.0 stretch:
- business acquisition,
- franchise,
- angel investing,
- mentorship.

Do not promise these until the core game retains players.

---

# 42. Roadmap gates

## Gate A — Prototype
Pass when:
- employed day fast-forward works,
- night action works,
- career scene plays,
- salary changes,
- one skill can progress.

## Gate B — Vertical slice
Pass when:
- character art feels final,
- one career is polished,
- one side hustle is polished,
- job + startup loop feels satisfying on device.

## Gate C — MVP
Pass when the full first founder arc is playable.

## Gate D — Early Access
Pass when:
- content variety supports multiple runs,
- employee/manager system works,
- analytics confirms no major progression dead zones.

## Gate E — 1.0
Pass when:
- onboarding,
- content,
- balancing,
- saves,
- performance,
- localization,
- release QA

are all production-ready.

---

# 43. Example player story

**2026 — Age 21**
- Character starts with almost no money.
- Takes a junior developer job.
- Workday mostly fast-forwards.
- Scenes: coding, stand-up, bug fix.

**Late 2026**
- Six-month career milestone gives Problem Solving Lv1.
- Player buys a Communication course.
- Evening study takes multiple weeks.

**2027**
- Player has savings.
- Starts a freelance web service at night.
- Daytime remains developer job.
- Night scenes show client work and laptop work.
- Income now comes from salary + side business.

**Late 2027**
- Freelance revenue becomes stable.
- Player resigns.
- Entire day becomes available.

**2028**
- Freelance operation grows into a small agency.
- Player hires first employee.

This is the intended emotional arc:
ordinary person → capability → savings → side project → confidence → founder.

---

# 44. Key balance questions for playtesting

1. How long before the first skill unlock?
2. How long before the player can afford a side hustle?
3. Does the workday timeskip feel satisfying or boring?
4. Does night startup time feel valuable?
5. Is quitting too obviously optimal?
6. Are paid courses expensive enough to make career milestones meaningful?
7. Can a player recover from a failed business?
8. Are profession affinities useful without becoming hard class locks?
9. Does age meaningfully change the start without creating a clearly "best" age?
10. Can a 10–15 minute mobile session produce at least one visible progress moment?

---

# 45. Decisions locked by GDD v1

Locked:
- Unity, not React Native, as the game runtime.
- No Three.js.
- 2D chibi/cutout as primary visual direction.
- Career and Skill are separate systems.
- Employed workday fast-forwards.
- Evening is the primary startup/study window.
- Unemployed/full-time founder sees the full day.
- Side-hustle compatibility is a formal business rule.
- Work profession scenes use percentage quota scheduling rather than pure random.
- Skills can be earned free through career progression or purchased through expensive courses + study time.
- Simulation remains simple for the player.
- MVP is local-first/offline.

Open for balancing:
- exact salaries,
- exact ages/savings,
- exact scene percentages,
- exact course pricing,
- exact business margins,
- exact game-time speed.

---

# 46. Research references

Checked: 2026-09-30.

## Engine / official stack
- Unity 6 release page: https://unity.com/releases/unity-6
- Unity 6 support page: https://unity.com/releases/unity-6/support
- Unity official Agent Skills: https://github.com/Unity-Technologies/skills
- Unity official new-project Agent Skill: https://github.com/Unity-Technologies/skills/blob/main/skills/new-unity-project/SKILL.md
- Unity official UI Agent Skill: https://github.com/Unity-Technologies/skills/blob/main/skills/ui/SKILL.md
- Unity Gaming Services Agent Skill: https://github.com/Unity-Technologies/unity-agent-plugin/blob/main/skills/build-live-game/SKILL.md

## 2D art / animation
- Unity 2D Animation: https://docs.unity3d.com/current/Manual/com.unity.2d.animation.html
- Unity 2D PSD Importer: https://docs.unity3d.com/6000.0/Manual/com.unity.2d.psdimporter.html
- Unity Sprite Library API: https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/api/UnityEngine.U2D.Animation.SpriteLibrary.html
- Unity ScriptableObject: https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html

## Agent-skill ecosystem considered
- AutoSkills registry: https://www.autoskills.sh/
- Unity production skill pack research: https://github.com/tea-x-random/unity-game-skills
- Unity official skills collection: https://github.com/Unity-Technologies/skills
- Godot agent CLI/Skill: https://github.com/aigengame/godot-agent
- Godot AI game-dev skill: https://github.com/IvanMurzak/ai-game-dev-plugin
- Godot community skill library example: https://github.com/bgrenat/godot-game-dev-studio
- Game designer Agent Skill example: https://github.com/edhahn/agent-skills/blob/main/game-designer/SKILL.md

---

# 47. Recommended next repository artifacts

When implementation begins, add:

```text
/docs/GDD.md
/docs/MVP_PLAN.md
/docs/ART_BIBLE.md
/docs/BALANCING.md
/docs/SAVE_SCHEMA.md
/docs/CONTENT_SCHEMA.md
/docs/QA_CHECKLIST.md
/AGENTS.md
```

The two documents in this delivery should become the source material for those repo files.
