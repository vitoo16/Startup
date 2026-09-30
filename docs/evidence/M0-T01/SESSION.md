# M0-T01 — Canonical documentation and task ledger

**Status:** Complete  
**Date:** 2026-09-30  
**Owner model:** GPT-5.6 Sol

## Skills used

- `startup-life-session-orchestrator`

## Acceptance criteria

- [x] The four authoritative root documents exist under their canonical `docs/` paths.
- [x] Original filenames and SHA-256 values are recorded; canonical files match the source bytes.
- [x] Former root copies are absent, leaving one authoritative path per source.
- [x] The complete approved implementation plan is saved and its historical audit is clearly labeled.
- [x] All 49 stable task IDs and valid dependencies are present.
- [x] Every task has an exact expanded skill list with no bundle alias.
- [x] Legacy M0–M8 milestones map to executable task IDs.
- [x] Active status and evidence conventions are documented.
- [x] README links and local Markdown links resolve.

## Tests

- `pwsh -NoProfile -File scripts/validate-documentation.ps1`
  - Result: Passed.
  - Source parity: 4/4 canonical documents.
  - Task ledger: 49/49 expected unique IDs; only M0-T01 checked.
  - Exact approved skill expansions and dependencies: Passed.
  - Local Markdown links: Passed.
- Approved-plan preservation audit (task blocks normalized only for checkbox and expanded skill field)
  - Result: Passed.
  - Task bodies preserved: 49/49.
  - Original headings preserved: 53/53.

## Visual evidence

None. M0-T01 changes documentation only.

## Save impact

None. No Unity project, runtime state, or save schema was changed.

## Known limitations

- Unity Editor/modules, community production skills, Unity automation, CI runners, Mac/iOS signing, and physical devices remain unverified M0 dependencies.
- Git does not yet exist in this workspace, so no commit identity is available for this record.

## Files changed

- `README.md`
- `docs/GDD.md` (moved from `startup_life_FULL_GDD_v1.md`)
- `docs/MVP_PLAN.md` (moved from `startup_life_MVP_PLAN_v1.md`)
- `docs/AI_PRODUCTION_WORKFLOW.md` (moved from `AI_PRODUCTION_WORKFLOW.md`)
- `docs/SKILLS_MANIFEST.md` (moved from `SKILLS_MANIFEST.md`)
- `docs/IMPLEMENTATION_PLAN_MVP.md`
- `docs/evidence/M0-T01/SESSION.md`
- `scripts/validate-documentation.ps1`

## Commit

Not created. Git initialization is owned by the later foundation task.
