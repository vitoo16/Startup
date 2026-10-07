# M8-T02 — Operating eligibility implementation/source-review handoff

Date: 2026-10-07 (+07:00)
Owner model: Sol 6.1
Starting main: `2afa10fc53b364a00ac84ac9a7b52b15fb2b11a0`
Branch: `feat/m8-t02-operating-eligibility`
Final source head: `740a717d4e62ef23b9c75a6aff9a58457b8ebd83`
Architecture gate: `M8-T02 ARCHITECTURE APPROVED — SOL 6.1 MAY IMPLEMENT`
Source-review status: implementation ready; exact PR head must receive Astra re-audit after engine-free CI passes. Final runtime acceptance is not performed in this implementation/source-review pass.

## Starting state and scope

The primary checkout contained unrelated work and was preserved. The attached managed worktree began at the exact starting main above; `git rev-parse HEAD` matched and `git status --short` was empty before creating the feature branch.

The slice implements individual operating compatibility only. M9 allocation, sharing, study reservations, demand, capacity, costs/revenue and settlement are untouched. M10 founder blocks, helper and kiosk operation are untouched. M16 Chef/Barista production careers remain deferred. Frozen M1 APIs and existing save DTOs are unchanged. No scene, prefab, runtime UI, font, package or ProjectSettings edit is included.

## Implemented contracts

- Immutable `BusinessOperatingWindow`: defined weekday, `0 <= start < end <= 1440`, half-open intervals, no overnight representation.
- Immutable `BusinessOperatingRequirements`: defensive copy, canonical weekday/start/end order, nonempty nonnull windows, no overlap/duplicate, adjacency allowed, positive owner minutes reachable on every represented weekday.
- Catalog validates windows against its global waking day. Invalid authoring is rejected rather than silently clipped into validity.
- The old `BusinessDefinition` constructor preserves explicit null requirements and frozen M8-T01 mode-only semantics. The new scheduled constructor and new authoring sources require valid requirements. No ID/revision heuristic or zero-minute sentinel is used.
- `BusinessEligibilityResult` derives eligibility and stable reason keys from status. Null metrics indicate no calculation; measured zero is preserved.
- A single read-only Simulation evaluator is consumed by command, query and historical validation. It resolves business/revision, Manager, FullTime/employment and legacy policy before resolving exact active career for schedule math.
- `IBusinessEligibilityReadModel` returns a detached immutable snapshot containing requested ID/revision, current date and state revision. Evaluation occurs inside the session lock; repeated queries do not save, allocate IDs, add receipts or mutate state.
- Runtime launch keeps all existing payload/content/revision/count/type/arrears/investment/cash gates before operating compatibility. Nonpositive investment remains at the investment-bounds gate. Rejections preserve checkpoint bytes.

For each business window on the simulation weekday, availability is its intersection with the waking day minus overlap with mandatory career time. A career interval is mandatory only on configured workdays at/after first shift. Multiple windows may contribute. Clock position, courses and other businesses do not consume this compatibility envelope. Required owner minutes are a threshold, never allocation.

Manager remains representable but unsupported. FullTime plus active employment rejects even on off-days or before first shift. Legacy definitions retain mode-only behavior and null minute metrics.

## Provisional functional content

The new production IDs are `freelance-service` and `coffee-kiosk`, revision `v1`. Existing content semantics and aggregate `first-playable.v1` remain unchanged.

| Definition | Mode | Every-day window | Required minutes |
|---|---|---|---:|
| Freelance | SideHustleCompatible | 18:00–22:00 | 120 |
| Coffee Kiosk | FullTimeRequired | 09:00–17:00 | 480 |

Both use explicitly provisional functional financial values: startup range 100–1000 VND, reinvestment range 1–1000 VND, Budget/Standard/Premium pricing with Standard default. These are constructor-valid acceptance data, not final economy balancing. M8-T01 financial rules and fixtures are preserved.

Production-template evaluation proves Developer weekday Freelance has 240 available / 120 required and is Eligible. Employed kiosk is EmploymentIncompatible. Unemployed kiosk has 480 available / 480 required and is Eligible.

Test-only F&B fixtures prove early 08:00–16:00 leaves 240; late 14:00–21:00 leaves 60 and rejects; threshold 14:00–20:00 leaves 120 and passes; one minute over the threshold leaves 119 and rejects. Late-shift off-day/pre-first-shift leaves 240. A configured Saturday shift is subtracted normally.

## Save and revision safety

SaveVersion 2; no new persisted fields; no migration.

Eligibility, window intersections, owner minutes and plans are not persisted. Existing IDs require a definition revision bump for any behavior-driving change. Exact unavailable business or career revisions classify UnsupportedContent; current revisions never substitute. The one-revision-per-ID catalog contract remains unchanged. Existing M8-T01 legacy definitions and v1/v2, H1/H2/R2 checks remain intact.

## Historical restore

`ValidateKnownBusinessEligibility` runs before known scheduler validation and before `FullContentAvailable`. It scans structurally validated receipts from origin/minute zero, advances only committed durations, binds ordered accepted employment records, and tracks resignation. First shift is derived from the acceptance receipt through the same pure helper used by runtime acceptance and scheduler provenance.

Known launches are evaluated with receipt date and historical employment, using the same evaluator. Missing dependent exact content skips only the unavailable schedule assertion and scanning continues; existing compatibility validation returns UnsupportedContent. A known illegal committed launch raises corruption even if unrelated content is missing. No second simulation engine or historical schedule snapshot is introduced.

| Historical case | Verified result |
|---|---|
| Legal launch, then incompatible employment | Valid |
| Illegal employed scheduled launch, then resign | Corrupt |
| Resign, then same-day launch | Valid |
| SideHustle launch before first shift | Valid; no future shift subtraction |
| Known illegal launch plus unrelated unavailable start | Corrupt |
| Required career ID or revision unavailable | UnsupportedContent |
| Exact saved business revision absent, newer balance present | UnsupportedContent |
| Earlier unavailable career, later unemployed/known-career illegal launch | Corrupt; scanning continues |
| Earlier unavailable career, later Manager launch | Corrupt |
| Legal Wednesday launch, current day has no business window | Valid; receipt weekday governs history |
| Forged persisted first shift | Corrupt when exact provenance is known |

Permissive producer definitions in hostile-byte tests deliberately construct an invalid history for validation. They are adversarial fixtures, not a policy permitting production semantics to change under the same revision.

## Engine-free evidence

Reports are retained in [engine-free](engine-free/). Counts are test groups, not individual assertions.

| Suite | Passed / total |
|---|---:|
| Simulation | 49 / 49 |
| Business ownership + operating eligibility | 68 / 68 |
| Astra foundation H1/H2/R2 | 20 / 20 |
| M2 restore/provenance | 35 / 35 |
| Presentation | 11 / 11 |
| Content catalog | 13 / 13 |
| Static Unity foundation | 31 / 31 |
| Frozen M1 public API | 31 / 31 |
| Content bridge static | 8 / 8 |
| M7 static | 43 / 43 |
| Unity runner contract | 30 / 30 |
| Security/release hygiene | 28 / 28 |

The original 31 M8-T01 business/save groups are retained; 37 focused M8-T02 groups were added. The original 8 content groups remain; 5 authoring/production eligibility groups were added. .NET builds report 0 warnings and 0 errors with warnings treated as errors.

Coverage includes malformed/immutable definitions, authored conversion, unavailable exact content, date/weekday/first-shift boundaries, clock independence, read-model purity, query/command parity, reason precedence, rejection atomicity, save roundtrip, cold retry, corruption precedence and partial historical provenance. Relevant Unity EditMode coverage was authored but not run.

## Unity authoring only

See [authoring evidence](unity-authoring.json). No live Unity MCP tools were exposed, so the supported Unity CLI/headless builder fallback was used. Installed CLI was 1.0.0-beta.12; the update check was unavailable due network DNS and no tool update was made.

Exact Editor: `6000.3.25f1 (e1dba0a9aba4)`. The existing content builder regenerated `Assets/StartupLife/Data/FirstPlayableContent.asset`; loaded-asset verification confirmed both complete schedules. The second builder run produced byte-identical content. Both runs exited 0 with 0 C# errors/warnings. Normalized TMP fonts were unchanged. Serialized asset YAML was not hand-edited.

This is authoring/regeneration and compile evidence only. EditMode: NOT RUN. PlayMode: NOT RUN. Final exact-head fresh-import acceptance: NOT RUN. Visual: N/A — M8-T02 has no visual acceptance scope. Android/iOS devices are outside this task's gate.

## Required next gate

After engine-free PR CI is green, send the exact PR head to Astra for legacy/revision safety, first-shift parity, evaluator/query purity, launch precedence, historical provenance, Corrupt-versus-UnsupportedContent behavior and M9/M10 boundary audit. Required source findings before final Unity: BLOCKER 0, HIGH 0, MEDIUM 0.

Final Unity acceptance requires separate owner instruction after Astra approval. M8-T02 remains unchecked; M9-T01 is not ready. Do not merge automatically.

## Session closeout

Skills used: startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge; unity-qa-release; unity-cli. Community skills came from pinned `tea-x-random/unity-game-skills@dafb97ef00f94e64e42e6260bc6b3af74cc83dad`.
Tests: engine-free and static groups above; documentation validation and clean-worktree checks accompany PR handoff. Final Unity test suites remain pending.
Visual evidence: N/A.
Save impact: SaveVersion 2; no new persisted fields; no migration.
Known limitations: Astra source re-audit and final Unity acceptance pending; provisional financial content is not balanced.
Files changed: Core Business/Definitions; Simulation evaluator and launch/restore; Application GameSession; Content source/template; Editor content builder; serialized first-playable content; EditMode content tests; ownership/content harness tests; task evidence and ACTIVE_STATUS wording. No M9/M10 source.
Commit: final source head above; evidence-only follow-up commit is identified by the PR head.

`M8-T02 IMPLEMENTED — ASTRA SOURCE RE-AUDIT REQUIRED`

`M9-T01 NOT READY — blocked by M8-T02 source and runtime acceptance`
