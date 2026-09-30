# Startup Life

Startup Life is a portrait-first, offline Unity 6.3 LTS mobile game about moving from employment through learning and side businesses into full-time company ownership. Product rules, implementation gates, and task status are authoritative in the documents below.

## Canonical documentation

| Document | Purpose | Source provenance | SHA-256 of preserved source |
|---|---|---|---|
| [GDD](docs/GDD.md) | Full game design | Moved byte-for-byte from `startup_life_FULL_GDD_v1.md` | `aa2635429901b64b7f9fb5226814415072e8b28fb7b17294dcc2095367fa60ca` |
| [MVP plan](docs/MVP_PLAN.md) | Original MVP scope and M0–M8 checklist | Moved byte-for-byte from `startup_life_MVP_PLAN_v1.md` | `33973cc9dda02ecc902b3644064b65e1f27b79df89f0a0ea8448943bc2bc8e5a` |
| [AI production workflow](docs/AI_PRODUCTION_WORKFLOW.md) | Mandatory operating model | Moved byte-for-byte from `AI_PRODUCTION_WORKFLOW.md` | `6a5918f0725febf030ea1cf82650551b8bce27e4077e7ecbb6305e23ca2f3fb5` |
| [Skills manifest](docs/SKILLS_MANIFEST.md) | Required skill inventory and routing | Moved byte-for-byte from `SKILLS_MANIFEST.md` | `8b6d59a7484757b679425a8078ea67ca83aac4044d17379d524f27ee2290e0b8` |
| [MVP implementation ledger](docs/IMPLEMENTATION_PLAN_MVP.md) | Active M0–M19 task graph, status, acceptance, and evidence rules | Saved from the user-approved plan on September 30, 2026 | Maintained document |

The four design/workflow sources keep their original bytes under canonical names. This table records their former root filenames and source hashes; the former root copies were removed so each has one authoritative path.

## Current work

M0-T01 is complete. M0 tooling/source-control preparation and a provisional Developer/work/study/save backend are in progress. The four engine-free libraries compile under .NET Standard 2.1 and 35 .NET behavior checks pass. Unity project import, UI, EditMode/PlayMode, IL2CPP, and device verification remain pending. No Mac host is available for the required iOS route.

The implementation ledger is the sole source for task status; the older M0–M8 checklist remains provenance and is mapped to the executable task IDs in that ledger. [Provisional evidence](docs/evidence/PROVISIONAL-FIRST-PLAYABLE/SESSION.md) names implemented behavior and remaining gaps.

Every implementation session starts with [AGENTS.md](AGENTS.md), loads `startup-life-session-orchestrator`, reads the canonical documents and active task acceptance criteria, and writes its record under `docs/evidence/<TASK-ID>/SESSION.md`.

Run the documentation gate with:

```powershell
pwsh -NoProfile -File scripts/validate-documentation.ps1
```

Run the provisional backend checks with .NET SDK 8.0.425:

```powershell
pwsh -NoProfile -File scripts/Test-Simulation.ps1
pwsh -NoProfile -File scripts/Test-Tooling.ps1
pwsh -NoProfile -File scripts/Test-Tooling.ps1 -RequireEditor
```

`Test-Tooling.ps1 -RequireEditor` fails until the pinned **6000.3.25f1** Editor and an active license are present. These commands do not substitute for Unity/mobile gates. The Editor installation recipe defaults to a dry run; `scripts/Install-UnityEditor.ps1 -Install` performs installation through the official CLI.

