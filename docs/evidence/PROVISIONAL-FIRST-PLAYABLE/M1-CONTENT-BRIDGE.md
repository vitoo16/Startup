# M1 Content bridge — provisional closure candidate

Date: 2026-10-03  
Owner model: GPT-5.6 Sol  
PR: #15 — `feat: add runtime content catalog bridge`

## Why this slice exists

The post-freeze repository audit correctly identified M7-T01 as the next major first-playable target, but source inspection found a prerequisite gap that the audit had classified too optimistically: the architecture requires a `StartupLife.Content` assembly, while the repository contained only Core/Simulation/Application/Infrastructure/Presentation/Editor source assemblies.

Building the M7 composition root without this bridge would force balance/content construction into Presentation. This slice closes that architecture gap before UI integration.

## Skills used

- `startup-life-session-orchestrator`
- `startup-life-gameplay-guardian`
- pinned `unity-game-director`
- pinned `unity-gameplay-systems`
- pinned `unity-mcp-bridge`
- pinned `unity-qa-release`

The live Unity MCP/Editor is unavailable in this session. Following the documented fallback, only plain C#, asmdef text, tests, and an Editor builder source were authored. No scene/prefab/asset YAML was hand-edited.

## Implementation

### StartupLife.Content

A new `StartupLife.Content` asmdef depends only on `StartupLife.Core`.

`ContentCatalogSource` and its serializable child source types represent authoring data for:

- six skills;
- careers, scenes, ranks, schedules and quotas;
- courses;
- character starting packages/appearances;
- economy balance;
- wake/sleep schedule.

`ContentCatalogSource.Build()` converts this mutable authoring data through the existing Core constructors into an immutable `ContentCatalog`. Existing Core validation therefore remains authoritative for stable IDs, duplicate IDs, quota totals, schedules, skill references, courses, and starts.

### ScriptableObject boundary

`StartupLifeContentCatalogAsset` is a thin ScriptableObject authoring container. Runtime simulation receives only the detached immutable `ContentCatalog` produced by `BuildCatalog()`.

### First-playable seed

`FirstPlayableContentTemplate` describes the currently verified Developer first-playable seed:

- six transferable skills;
- Developer 09:00–17:00 weekday schedule;
- four scene boundaries per shift;
- 20-slot 40/20/15/10/10/5 scene distribution;
- junior and mid ranks;
- communication and problem-solving courses;
- fresh start at 3,000,000 VND / LearningSpeed 10,000;
- wake 08:00 / sleep 22:00;
- payday/expense day 1 and 1,000,000 VND living cost.

This template is used to seed an editable Unity asset; Presentation must consume the asset/catalog rather than reproduce these values.

### Editor builder

`FirstPlayableContentBuilder.CreateOrUpdate()` creates or updates:

`Assets/StartupLife/Data/FirstPlayableContent.asset`

through Unity's AssetDatabase. It converts the source once before saving so invalid content fails at the authoring boundary.

No .asset YAML is committed by this engine-free session.

### Dependency graph

- Content → Core
- Presentation → Core, Application, Content, Infrastructure
- Editor → Core, Content, Presentation
- EditMode tests → Core, Simulation, Application, Content, Infrastructure, Presentation

No Core/Simulation/Application dependency on Unity Content was introduced.

## Verification

Engine-free CI run `37135863561` (#51) at head
`ac70632df5d746b9847ce6df58b669018ef29f42` completed successfully.

Results:

- documentation validation: PASS;
- static Unity foundation: **31/31**;
- frozen M1 public API: **31/31**;
- Content bridge static contract: **8/8**;
- Unity runner contract static gate: **30/30**;
- repository security/release hygiene: **28/28**;
- SimulationChecks: **45/45**;
- H1/H2/R2: **19/19**;
- M2 restore/provenance: **35/35**;
- ContentCatalogChecks: **8/8**;
- all four .NET harness builds: **0 warnings / 0 errors**;
- clean-worktree verification: PASS.

Artifact:

- `engine-free-foundation-reports`
- ID `11278945645`
- SHA-256 `5eddb5ea9ca7a7a9a9280bd22819cc9ec1e189a4ed9e31294d1da06151fff0e2`

The content harness proves:

1. the first-playable source converts to the expected immutable catalog;
2. duplicate IDs reject;
3. missing career skill references reject;
4. missing course skill references reject;
5. invalid appearance IDs reject;
6. careers outside the wake/sleep schedule reject;
7. the built catalog is detached from later source-array mutation;
8. null authoring entries reject explicitly.

## Unity tests prepared but not executed

`ContentCatalogTests.cs` is added for EditMode execution:

- first-playable source → Core catalog;
- ScriptableObject wrapper → detached catalog;
- invalid authoring data → exception before publication.

These tests have source/static evidence only in this session. They are not claimed as Unity EditMode passes.

## Save impact

None.

- SaveVersion remains 1.
- No persisted GameState field changes.
- No migration.
- Content version for the new authoring seed is `first-playable.v1`; this is content identity, not save schema identity.

## Visual evidence

None. No scene, prefab, UI hierarchy, screenshot, or PlayMode action was produced.

## Known limitations

- Unity Editor import/compile is not evidenced.
- The ScriptableObject asset has not yet been generated by the Editor builder.
- EditMode tests have not run in Unity.
- M7 composition root/view wiring has not started in this PR.
- Android/iOS/device evidence remains pending.

## Ledger boundary

No checkbox is changed. This closes a missing implementation prerequisite in source/engine-free evidence only; complete M1/M7 acceptance still requires the ledger's Unity/runtime gates.
