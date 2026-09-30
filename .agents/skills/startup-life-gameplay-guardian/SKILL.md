---
name: startup-life-gameplay-guardian
description: Use for any Startup Life gameplay, economy, time, career, skill, business, employee or event change. Enforces the locked low-micromanagement design rules and prevents generic tycoon/RPG mechanics from drifting into the project.
---

# Startup Life Gameplay Guardian

## Locked principles

### Career is not Skill
Career contains:
- profession;
- salary;
- tenure;
- seniority;
- Career XP;
- employer/history;
- profession scene distribution.

Skill contains transferable capability such as:
- Communication;
- Negotiation;
- Time Management;
- Problem Solving;
- Networking;
- Leadership.

Never merge them into one level.

### Time
Employed weekday:
- work period fast-forwards;
- profession scenes interrupt/visualize progress;
- evening is startup/study time.

Unemployed/full-time founder:
- full day is available.

Time is the main constraint.
Do not add hunger/stamina/hygiene micromanagement unless the GDD is explicitly revised.

### Career scenes
Career scenes must use deterministic quota/percentage scheduling.
Do not implement unconstrained random daily scene selection.

### Side hustle
Business definition owns compatibility:
- SideHustleCompatible;
- FullTimeRequired;
- ManagerOperable later.

A full-time employed player cannot personally operate an incompatible daytime business.

### Low interaction
The player decides:
- what job;
- what course;
- whether to save/invest;
- what business;
- simple pricing/reinvestment;
- whether to resign.

The simulation performs professional work.

Reject feature designs that turn the game into:
- coding minigames;
- cooking minigames;
- manual accounting;
- manual inventory clicking;
- hour-by-hour staff scheduling.

### Difficulty
Failure should create recovery/story, not delete the run.

## Implementation checks
For every gameplay change:
1. identify authoritative service;
2. make data-driven values;
3. keep simulation independent from view;
4. add deterministic tests;
5. check save impact;
6. test employed and founder schedules where relevant.
