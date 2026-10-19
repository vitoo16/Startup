# Unity Console triage — Startup Life

**Scope:** Engine-free classification aid for Unity Editor/Console logs. This is **not** a build, test, shader, device, visual QA, or release verdict.

## Usage

Save the relevant Console/Editor output to a UTF-8 text file and run from repository root:

```bash
python3 scripts/Triage-UnityConsole.py path/to/Editor.log
python3 scripts/Triage-UnityConsole.py path/to/Editor.log --format json
python3 scripts/Triage-UnityConsole.py path/to/Editor.log --fail-on-unclassified-error
```

Optional strict mode exits 3 for *unclassified error lines*, 2 for usage/read errors, and 0 otherwise. A zero exit is **never** proof Unity compiled or tests passed.

## Currently recognized diagnostics

| Category | Evidence requirement |
| --- | --- |
| `external_urp_precision_warning` | Only exact `TraceVirtualOffset` `[-Wconversion]` warning lines from Unity Core RP `ShaderLibrary/Sampling/{Fibonacci,Hammersley,Sampling}.hlsl` in `Library/PackageCache`. These warnings are retained in output and require shader/material visual review on relevant target devices; never rewrite PackageCache. |
| `external_unity_ai_subscription_error` | Only `NoSubscription` errors naming `generators.ai.unity.com`. Check the local Unity AI installation/account separately; do not mistake this for game C# compilation success or failure. |
| `localization_table_modified` | `DashboardLocalizationEditorSync` reports a nonzero count of modified Vietnamese entries; inspect Git diff of localization tables/SharedTableData, validate glyphs, and do not automatically claim no asset changes. |
| `unclassified_error` | Needs immediate investigation; includes unrecognized exceptions, shader errors, C# compile errors and other recognized error prefixes. |
| `unclassified_warning` | Needs inspection; includes non-allowlisted shader warnings and other recognized warning prefixes. |

`REVIEW_REQUIRED` **does not mean PASS**: it includes known external warnings/errors and localization asset changes. `NO_RECOGNIZED_DIAGNOSTICS` means only that this conservative, non-exhaustive parser saw no matching messages. For complete QA follow `docs/PRODUCTION_EXECUTION_ROADMAP.md` and `docs/ci/UNITY_RUNNER_CONTRACT.md`.

Do not add broad warning suppressions, change Unity package versions, edit `Library/PackageCache`, or remove Unity AI packages without verifying the local dependency graph through Unity Package Manager. The project's tracked `Packages/manifest.json` does not list Unity AI Assistant as a direct dependency at the time of this change; the local user's Editor may still have it installed.
