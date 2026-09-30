---
name: startup-life-asset-quality-gate
description: Use for every AI-assisted Startup Life visual asset before it is allowed into a production scene. Enforces Golden Asset consistency, Vietnamese context where applicable, technical readiness, best-of-N selection and approved-asset registration.
---

# Startup Life Asset Quality Gate

## Rule
AI-generated files are source material.

A production scene may reference only:
- approved prefabs/sprites;
- assets recorded in the project's approved registry.

Never reference a raw generation output directly.

## Required pipeline

1. Identify asset contract.
2. Load art spec.
3. Load `vietnam-art-direction` if relevant.
4. Generate/source N candidates.
5. Reject obvious failures.
6. Score remaining candidates.
7. Select candidate.
8. Cleanup/reconstruct layers.
9. Import with correct Unity settings.
10. Rig/animate if required.
11. Capture BeautyCell screenshot.
12. Score final in context.
13. Register as approved.

## Minimum best-of-N
- prop: 3
- environment: 4
- character/outfit: 6
- Golden Asset: 8–12 across controlled rounds

## Scorecard — 0 to 2 each
- Golden Asset/style match
- silhouette/readability
- palette
- anatomy/perspective
- Vietnam context when required
- technical cleanliness
- mobile readability
- reuse/system compatibility

Production threshold:
- >=13/16
- no zero criterion

Golden threshold:
- >=15/16
- no zero criterion

## Immediate rejection
Reject for:
- malformed anatomy;
- perspective breaks that remain obvious at target size;
- unexplained style shift;
- generic AI gloss inconsistent with art bible;
- fake/garbled Vietnamese text;
- watermark;
- unintended third-party logo/trademark;
- inconsistent character identity;
- unusable alpha edges;
- low-resolution artifacting;
- wrong sprite proportions for the shared rig.

## Character-specific
Confirm:
- head/body ratio;
- facial identity;
- hair silhouette;
- joint/layer separability;
- no baked shadows that break cutout animation;
- outfit layers can reuse shared rig.

## Environment-specific
Confirm:
- correct camera/perspective;
- character scale;
- doors/chairs/desks/props scale;
- focal path;
- adequate negative space for characters and UI;
- no excessive visual noise.

## Registry record
Each approved asset should record:

```yaml
asset_id:
source_type: generated|human|hybrid
prompt_version:
reference_set:
selected_candidate:
cleanup_notes:
unity_path:
atlas:
approved_against_golden:
vietnam_context_checked:
approval_date:
```
