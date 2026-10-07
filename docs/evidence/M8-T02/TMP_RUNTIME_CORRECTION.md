# M8-T02 TMP Runtime Correction

Date: 2026-10-07  
Branch: `fix/m8-t02-tmp-runtime-glyphs`  
Failed runtime-tested SHA: `f352cac741ab4cc8699799344a5e668875c568eb`

## Root cause

1. **Actual unsupported presentation glyph dependency** — `CharacterCreationViewController.RenderSelection()` emitted raw `♀` / `♂`. These symbols are not localization requirements and were outside the required Vietnamese UI character contract.
2. **Incorrect Dynamic TMP coverage assertion** — the mobile baseline treated the initially-cleared character table of the Dynamic `NotoSansVietnamese` TMP asset as if it were a pre-baked Static table. The test now asks TMP to search fallbacks and permit dynamic population.

The failed acceptance remains failure provenance and is not rewritten as a pass.

## Changes

- `Assets/StartupLife/Scripts/Presentation/CharacterCreationViewController.cs`
  - removes raw gender-symbol presentation;
  - drives the appearance value through `LocalizedKeyLabel`;
  - maps `base.female` → `character.appearance.female`;
  - maps `base.male` → `character.appearance.male`.
- `Assets/StartupLife/Scripts/Presentation/LocalizedKeyLabel.cs`
  - adds a small compatibility helper that attaches the existing `LocalizeStringEvent` + `LocalizedKeyLabel` pattern to an already-serialized TMP value at runtime.
- `Assets/StartupLife/Scripts/Editor/FirstPlayableShellBuilder.cs`
  - creates `AppearanceValue` with the existing `LocalizedValue(...)` builder path, initially keyed to `character.appearance.female`;
  - preserves the controller's existing serialized TMP reference, so the tracked scene YAML does not need a hand edit.
- `Assets/StartupLife/Tests/PlayMode/MobileBaselineTests.cs`
  - retains Vietnamese coverage and changes glyph verification to `HasCharacter(character, true, true)`.
- `Assets/StartupLife/Tests/PlayMode/FirstPlayableFlowTests.cs`
  - adds focused coverage for default `Nữ`, toggled `Nam`, return to `Nữ`, no raw gender symbols in rendered presentation, and the existing unexpected-log gate.

No business eligibility source, save schema, migration, font asset, TMP settings, or fallback asset was modified.

## Appearance localization

- female → `character.appearance.female` → `Nữ`
- male → `character.appearance.male` → `Nam`
- no direct runtime presentation dependency on `♀` / `♂`

The controller retains the existing serialized `TMP_Text appearanceValue` reference to avoid requiring a scene YAML mutation. It resolves/creates the same `LocalizedKeyLabel` + `LocalizeStringEvent` adapter used elsewhere. A future Unity builder regeneration produces the adapter directly through `LocalizedValue(...)`.

## Vietnamese glyph coverage

The mandatory Vietnamese sample remains unchanged, including `ộ` (U+1ED9). The corrected assertion permits TMP Dynamic source/fallback lookup and dynamic population instead of only inspecting the initially-cleared persisted table.

## Font assets

Git blob hashes at failed source SHA and correction branch:

- `Assets/StartupLife/UI/Fonts/NotoSansVietnamese.asset`
  - before: `07011f62e643621ef9625d2a659879acaf27df9e`
  - after:  `07011f62e643621ef9625d2a659879acaf27df9e`
  - mutation: **none**
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`
  - before: `e71ce839e4d108ec87bce34f6f75789aacd9aa07`
  - after:  `e71ce839e4d108ec87bce34f6f75789aacd9aa07`
  - mutation: **none**

## Focused PlayMode

Required focused cases:

- `MobileBaselineLoadsVietnameseThroughLocalizationWithGlyphCoverage`
- `AppearanceSelectionUsesLocalizedLabelsWithoutRawGenderGlyphs`

Status: **NOT RUN in this connector session**. The repository's Unity runtime workflow is manual-dispatch on a provisioned self-hosted Unity runner; no workflow-dispatch action is exposed by the connected GitHub tool. Do not treat this as acceptance evidence.

## Full Unity

Status: **NOT RUN** in this connector session.

Required acceptance remains:

- fresh import PASS
- C# errors 0
- C# warnings 0
- EditMode PASS
- PlayMode PASS
- runner exit 0
- parsed XML reports

## Engine-free

Status: **PENDING PR CI**. Record actual counts after the pull-request workflow completes.

Expected historical baseline only (not a claimed new-head result):

- Simulation 49/49
- Business ownership + eligibility 68/68
- Foundation H1/H2/R2 20/20
- M2 restore/provenance 35/35
- Presentation 11/11
- Content 13/13

Expected counters must not be edited merely to force green.

## Save

- `SaveSchema.CurrentVersion = 2`
- no new persisted fields
- no migration

## Findings

Current implementation review:

- BLOCKER: 0 source blockers identified
- HIGH: 0 source findings identified
- MEDIUM: 0 source findings identified
- LOW: 0 source findings identified

Runtime acceptance is still outstanding and is not represented by the source finding counts.

## M8-T02

`M8-T02 CORRECTION IMPLEMENTED — ASTRA SOURCE RE-AUDIT REQUIRED`

Do **not** mark closed before Astra returns:

- BLOCKER 0
- HIGH 0
- MEDIUM 0

and a new final Unity acceptance is executed on that approved correction head.

## M9

`M9-T01 NOT READY — blocked by M8-T02 correction review/runtime acceptance`

## PR

PR number and exact head are recorded after PR creation. The PR must not be merged automatically.
