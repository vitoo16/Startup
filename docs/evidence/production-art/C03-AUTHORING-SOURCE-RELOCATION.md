# C03 authoring source relocation — Unity static-foundation CI

**Date:** 2026-10-18  
**Incident:** `Test-UnityFoundationStatic.ps1` reported 11 tracked C03
`.json`/`.b64` authoring source files under `Assets/` without
Unity `.meta` sidecars, failing the **tracked Unity assets have .meta sidecars**
check.

## Correct fix: relocate non-imported evidence, not bypass the check

The eleven files are raw pixel-authoring contracts, lossless row data,
draft Idle direction and historically rejected R5/A-B explorations.
They are **not** Unity texture import settings, scenes or production
`TextAsset` references. They have been moved byte-for-byte via Git blobs
from:

`Assets/StartupLife/Art/Source/Characters/golden-character-c03/<filename>`

to:

`docs/evidence/production-art/source/c03-authoring/<filename>`

Files moved:

- `C03-IdlePose-A.json`
- `C03-IdlePose-B.json`
- `C03-front-joint-plan-DRAFT.json`
- `C03-idle-v2-art-direction.json`
- `C03-idle-v2-keyposes-DRAFT.json`
- `C03-idle-v2.1-inhale-contour-DRAFT.json`
- `C03-native-front-pixel-runs.lzma.b64`
- `C03-native-front-source.json`
- `C03-owner-front-rig-contract.json`
- `r5-1-personality-front-review.json`
- `r5-personality-front-review.json`

**Content unchanged:** The Git blob SHA of each moved path is identical
to its pre-migration blob at HEAD
`9c5299f12392d9f3d0c5b2ee79b664915fc7d1de`.
`tools/tests/test_c03_authoring_source_migration.py` independently
checks the hash equality, moved paths and untouched R1–R4/native draft
Unity metadata pairs.

Renderer, validator, test and quarantined Unity handoff input readers
have been repointed to the new source location. R1–R4/native draft
`TextAsset` sources, which already have corresponding Unity-generated
`.meta`, remain in `Assets/`. R5 historical renderer retains access
to R2/R4 drafts at those old Unity paths while fetching its R5 authoring
source from the new evidence path.

## Hard safeguards preserved

- `scripts/Test-UnityFoundationStatic.ps1` **unchanged**; no whitelists,
  exception patterns, skip flags or lowered thresholds.
- No manually authored or fake `.meta` files / GUIDs.
- No changes to `Art/Approved/registry.json`, gameplay, simulation,
  character visuals, Unity scenes or save schemas.
- Owner-approved C03 Idle V3 Open/Half/Closed source, original
  native compressed pixel row packet, Rest/Closed hashes and 21-layer
  compositor behavior unchanged.
- Quarantined V3 Unity builder must still be run in a *live Unity Editor*
  before importer, prefab, PlayMode or mobile QA can be marked complete.

## Regression checklist

```bash
python -m unittest discover -s tools/tests -p 'test_c03_authoring_source_migration.py'
python -m unittest discover -s tools/tests -p 'test_c03_idle_v3.py'
python -m unittest discover -s tools/tests -p 'test_c03_idle_v3_unity_staging.py'
pwsh -File scripts/Test-UnityFoundationStatic.ps1
```

The current GitHub Actions **Engine-free CI** result is authoritative
for the static foundation gate; do not infer PASS solely from file
relocation. Earlier failures included these same 11 missing-metadata paths.
A Unity Editor is not available to this session, and any remaining
Editor/device quality gates remain `NOT_RUN`.

**Owner model:** GPT-6. **Skills:** session-orchestrator, unity-cli,
sprite-editor, 2d-pixel-perfect, asset-quality-gate. **Save impact:** none.
