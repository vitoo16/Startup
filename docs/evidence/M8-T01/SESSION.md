# M8-T01 — Business ownership and Save v2 implementation evidence

Date: 2026-10-05  
Owner model: GPT-5.6 Sol  
Starting main: `cbe6db4edfe2dc17504d920a5a3307e76729ebc7`  
Branch: `feat/m8-t01-business-ownership-save-v2`  
PR: #24  
Unity pin: `6000.3.25f1`  
Save impact: **SaveVersion 2 — real v1→v2 migration**

## Skills loaded

- `startup-life-session-orchestrator`
- `startup-life-gameplay-guardian`
- `unity-game-director`
- `unity-gameplay-systems`
- `unity-mcp-bridge`
- `unity-qa-release`
- `unity-game-economy`

The five community Unity skills were read from the repository-pinned
`tea-x-random/unity-game-skills` revision
`dafb97ef00f94e64e42e6260bc6b3af74cc83dad`.

## Implemented scope

M8-T01 implements:

- stable business enums and immutable `BusinessDefinition`;
- persisted active/closed `BusinessState` instances using the global entity sequence;
- command ordinals 7–10 for launch, reinvest, pricing, and closure while preserving 0–6;
- shared-wallet launch/reinvestment debits and business ledger attribution;
- closure without refund/liquidation and pricing without cash effect;
- immutable `IBusinessReadModel` / portfolio snapshots without changing the frozen M1 API;
- SaveVersion 2 with `SaveSchema.CurrentVersion`;
- frozen historical v1 wire codec;
- synthetic v0→historical v1→current v2 and historical v1→current v2 migration;
- schema-preserving H1 receipt identity normalization for physical v1 checkpoints;
- restore/provenance validation and replay for business operations.

M8-T02, M9, and M10 behavior is not implemented.

## Business acceptance evidence

Automatic Engine-free CI run **#97** on implementation/test head
`0bc8747174610dd2d29ea02638880d91234e216a` passed.

`BusinessOwnershipChecks`: **12/12 PASS**, covering:

- definitions/enums/defensive copies/catalog compatibility;
- exact command ordinals and frozen historical canonical payload vectors;
- launch acceptance matrix;
- reinvest acceptance matrix;
- pricing acceptance matrix;
- closure acceptance matrix;
- retry/stale/identity-conflict/rejected-byte invariants;
- conservation, ledger attribution, entity order, and read model;
- v0/v1 migration and v2 roundtrip matrix;
- H1 v1 schema-preserving normalization and first v2 gameplay commit;
- intrinsic business corruption precedence;
- unavailable business revision classification.

## H1 / H2 / R2 evidence

`AstraFoundationChecks`: **19/19 PASS**.

The M8 reconciliation keeps physical v1 compatibility writes at v1 and same revision while
ordinary reads expose the migrated v2 projection. Regression tests distinguish raw checkpoint
schema bytes from the current in-memory projection rather than incorrectly treating
`GameState.SaveVersion` after migration as the raw envelope schema.

H1 includes completed/partial day-month replay, unknown ownership fail-closed behavior,
durable normalization, interrupted compatibility writes, competing owner resolutions,
and malformed ownership groups.

## Restore / M2 provenance

`RestoreInvariantChecks`: **35/35 PASS**.

Business restore validation is additionally covered by `BusinessOwnershipChecks`, including
corruption-before-content classification and exact saved business revision compatibility.

## Regression evidence

Engine-free CI run #97:

- documentation validation: PASS;
- Simulation: **49/49**;
- Business ownership: **12/12**;
- H1/H2/R2: **19/19**;
- M2 restore/provenance: **35/35**;
- Presentation adapter: **11/11**;
- Content catalog: **8/8**;
- Static Unity foundation: **31/31**;
- frozen M1 public API: **31/31**;
- Content bridge: **8/8**;
- M7 static: **43/43**;
- Unity runner contract static: **30/30**;
- repository security/release hygiene: **28/28**;
- .NET builds: **0 warnings / 0 errors**;
- clean-worktree gate: PASS.

## Unity runtime evidence

The repository is pinned to Unity **6000.3.25f1**.

This GitHub connector session has no callable licensed live Unity Editor / Unity MCP execution
surface, so changed-head runtime verification is **NOT RUN**:

- clean Unity import/compile: NOT RUN;
- Startup Life Unity C# warnings/errors: NOT RUN;
- relevant EditMode: NOT RUN;
- relevant PlayMode save/load smoke: NOT RUN.

No runtime evidence is fabricated. Engine-free/static PASS does not close this runtime gate.

## Acceptance status

M8-T01 remains unchecked.

Current classification:

`M8-T01 IMPLEMENTED — ACCEPTANCE BLOCKER: Unity 6000.3.25f1 changed-head compile/EditMode/PlayMode verification not run`

Downstream:

`M8-T02 NOT READY — blocked by M8-T01`

PR #24 remains open and must not be merged automatically.


---

## Astra implementation audit corrections — F1–F8

Audit correction starting head: `a2380479b7d59db2204ad936e2e07e1213121002`  
Correction implementation/test head: `de981f4153d532dc3bc71e83a7fcf3cdb17b08b3`  
Engine-free CI: **#108 — PASS**  
PR: **#24 remains open; not merged**  
M8-T01: **OPEN — Astra implementation re-audit required**

The audit required source repair before final Unity acceptance. No final changed-head Unity
compile/EditMode/PlayMode acceptance was run during this correction pass.

### F1 — frozen-v1 `Businesses` bypass

**Root cause:** `HistoricalV1Codec.RejectImpossibleV1WireMembers` used a raw substring
match for `"Businesses":`, so legal whitespace and escaped property spellings could bypass
the frozen-v1 prohibition.

**Fix:** inspect root JSON members through `JsonReaderWriterFactory`, the JSON→XML infoset
reader compatible with the project's .NET Standard target. Direct XML-safe names are read
from `LocalName`; WCF-mapped escaped names are decoded from the mapped `item` attribute.
Only direct root members are inspected for the prohibited v2-only `Businesses` member.

**Regression proof:**
- checksum-valid v1 with `"Businesses" : [...]` → **Corrupt**;
- checksum-valid v1 with `"\\u0042usinesses":[...]` → **Corrupt**;
- the same malformed historical inputs through the H1 restore path → **Corrupt**;
- primary and backup bytes remain unchanged; no projection is persisted.

### F2 — unavailable business content no longer masks known corruption

**Root cause:** business content validation threw `UnsupportedContent` immediately for the
first unavailable/mismatched definition, preventing later businesses with available definitions
from being checked for provable corruption.

**Fix:** defer the first business content-compatibility exception, continue validating all
business instances/commands whose required definitions are available, throw corruption as soon
as it is independently proven, and only report `UnsupportedContent` after those checks finish.

**Regression proof:**
- missing A + corrupt known B → **Corrupt**;
- incompatible A revision + corrupt known B → **Corrupt**;
- missing A + valid B → **UnsupportedContent**;
- closed missing A + corrupt known B → **Corrupt**.

### F3 — intrinsic business-history provenance

**Root cause:** some business history facts were only caught by full simulation replay, so
unrelated unavailable non-business content could prevent those facts from being proven corrupt.

**Fix:** `BusinessStateValidation.ValidateStructure` now reconstructs the committed receipt
timeline independently of content replay and binds each business receipt to its persisted
history/state/ledger facts. It verifies ordered receipt revisions, zero business-command
duration, history timestamp at the receipt instant, launch definition/revision/investment and
launch ledger, reinvest amount/pricing and ledger, pricing payload/posture/no-op/order, and
closure timestamp/pricing/uniqueness/no-later-mutation.

**Regression proof:** reinvest timestamp forgery, pricing no-op/posture forgery, and closure
timestamp forgery are **Corrupt** both with complete content and with unrelated character-start
content unavailable.

### F4 — ordinary commits require physical current schema

**Root cause:** ordinary `AtomicFileSaveStore.Commit` validated a candidate only after serializer
migration, so raw historical v0/v1 bytes could become a valid in-memory v2 state and be written.

**Fix:** ordinary commits additionally require
`candidate.SourceSchemaVersion == SaveSchema.CurrentVersion`. The dedicated H1
`CommitReceiptCompatibility` path remains schema-preserving and separately guarded.

**Regression proof:**
- ordinary raw v0 → **Failed**, no file/change;
- ordinary raw v1 → **Failed**, no file/change;
- ordinary raw v2 → **Committed**;
- same-schema v1 H1 compatibility normalization → **Committed as physical v1**;
- same-schema v2 compatibility normalization → **Committed as physical v2**;
- mixed schemas remain ineligible for H1 normalization.

### F5 — source and every migration stage are validated

**Root cause:** migration validation was concentrated after migration, allowing an invalid source
or intermediate stage to be theoretically repaired by a later migration.

**Fix:** `JsonSaveSerializer` validates the raw source stage as its declared schema before the
first migration and validates every produced stage before invoking the next migration. Each stage
checks the historical/current wire contract, `SaveVersion`, envelope generation versus state
revision, structural/content/provenance validation applicable to that stage. Frozen v1 continues
to use the frozen-v1 decoder/validator. `ContentCompatibilityException` is preserved as
`UnsupportedContent`, not collapsed into corruption.

**Regression proof:**
- invalid v1 source generation/revision + repairing migration → **Corrupt; repair migration calls = 0**;
- invalid v1 intermediate + later repairing migration → **Corrupt; second migration calls = 0**;
- built-in synthetic v0 → frozen v1 → v2 → **Valid**.

### F6 — current-state corruption tests repaired

**Root cause:** affected current-v2 negative tests wrapped mutated v2 state in schema 1, so the
test could reject for schema mismatch even if the intended mutation were removed.

**Fix:** affected `RestoreInvariantChecks` and `SimulationChecks` fixtures now assert the
otherwise-identical authoritative checkpoint is **Valid**, wrap the one-property mutation with
`SaveSchema.CurrentVersion`, then assert the intended corruption rejection.

**Result:** Simulation regression restored to **49/49 PASS** and M2 restore/provenance remains
**35/35 PASS**.

### F7 — H1 restriction tests are schema-preserving

**Root cause:** some H1 restriction tests compared a raw v1 expected checkpoint to a current-v2
candidate and therefore rejected at schema inequality before exercising the intended identity
restriction.

**Fix:** direct-ID, gameplay, and competing-owner restriction candidates are now independently
valid raw v1 checkpoints. The allowed receipt-ID-only normalization is also raw v1 → raw v1 at
the same generation. Mixed-schema rejection has its own separate regression.

**Regression proof:**
- valid v1 receipt-ID-only normalization → **Committed**;
- otherwise-valid v1 direct-ID mutation → **Failed**, bytes unchanged;
- otherwise-valid v1 gameplay mutation → **Failed**, bytes unchanged;
- otherwise-valid v1 competing-owner mutation → **Failed**, bytes unchanged;
- v1 expected + v2 candidate → **Failed** in the dedicated mixed-schema test.

`AstraFoundationChecks` increased from **19/19** to **20/20 PASS** because the mixed-schema
prohibition is now an independent focused case.

### F8 — complete retry matrix for every business command

Focused retry matrices now cover `LaunchBusiness`, `ReinvestBusiness`,
`SetBusinessPricing`, and `CloseBusiness`.

For every command the regression performs:
1. initial successful commit;
2. immediate retry;
3. cold restore;
4. retry after restore;
5. a later unrelated successful commit;
6. retry of the original command again.

Every retry returns the original committed semantics: revision, operation ID, minutes consumed,
and the contractually meaningful `SimulationOutcome` fields are compared, including timeline,
cue/playback, cash delta, career fields, history, grants, ledger entries, and skill deltas.
Checkpoint byte identity after retry proves no duplicate cash, ledger, history, business mutation,
entity/operation allocation, receipt, or revision.

Hostile ownership regressions also cover same `CommandId` with changed payload for every
business command, direct/batch root collision, batch/direct collision, and disjoint child-looking
direct IDs.

### Final Save/H1 contract after corrections

- `SaveSchema.CurrentVersion == 2`;
- synthetic v0 → frozen historical v1 → v2;
- historical v1 → v2;
- current v2 → v2;
- H1 normalization is same physical schema, same revision, receipt-ID-only;
- first normal gameplay commit after a v1 load writes revision +1 as physical v2.

Launch/reinvest/pricing/closure behavior itself is unchanged: validation boundaries, shared-wallet
economy, ledger categories, no-refund closure, global identity, immutable business read model,
and the frozen M1 API remain intact.

### Correction regression evidence

Engine-free CI run **#108** on correction implementation/test head
`de981f4153d532dc3bc71e83a7fcf3cdb17b08b3`:

- documentation validation: **PASS**;
- Simulation: **49/49 PASS**;
- Business ownership: **23/23 PASS** (previous baseline **12/12**);
- H1/H2/R2: **20/20 PASS** (previous baseline **19/19**);
- M2 restore/provenance: **35/35 PASS**;
- Presentation adapter: **11/11 PASS**;
- Content catalog: **8/8 PASS**;
- Static Unity foundation: **31/31 PASS**;
- frozen M1 public API: **31/31 PASS**;
- Content bridge: **8/8 PASS**;
- M7 static: **43/43 PASS**;
- Unity runner contract static: **30/30 PASS**;
- repository security/release hygiene: **28/28 PASS**;
- .NET builds: **0 warnings / 0 errors**;
- clean-worktree gate: **PASS**.

### Correction status

`FINAL CHANGED-HEAD UNITY ACCEPTANCE: NOT RUN — waiting for clean Astra re-audit`

`M8-T01 IMPLEMENTATION CORRECTED — ASTRA RE-AUDIT REQUIRED`

`M8-T02 NOT READY — blocked by M8-T01`

`SAFE TO RE-SEND PR #24 TO ASTRA IMPLEMENTATION AUDIT`

Do not merge PR #24. Do not start M8-T02.


---

## Post-merge restore-boundary corrections

Date: 2026-10-05  
Owner model: GPT-5.6 Sol  
Merged starting main: `669ad94d192d10079846741214775e304f008b41`  
Branch: `fix/m8-t01-restore-boundary`  
Follow-up PR: **#25 — `fix: harden M8 restore-boundary validation`**  
Save impact: **none — SaveVersion remains 2**

PR #24 was already merged before this correction pass. It was not reopened or reused.

### Required workflow / skills

Loaded before editing:

- `startup-life-session-orchestrator`;
- `startup-life-gameplay-guardian`;
- `unity-game-director`;
- `unity-gameplay-systems`;
- `unity-mcp-bridge`;
- `unity-qa-release`;
- `unity-game-economy`.

The community Unity skills remain pinned to
`tea-x-random/unity-game-skills@dafb97ef00f94e64e42e6260bc6b3af74cc83dad`.

### Finding A — intrinsic business command payload validation

Persisted business receipts are now structurally validated before content compatibility can
short-circuit restore classification.

The intrinsic rules enforced independently of `ContentCatalog` are:

- `LaunchBusiness`: nonempty definition `ContentId`, positive startup `Amount`, empty `Name`,
  and nonempty exact revision `AppearanceId`; the existing launch provenance/content checks remain.
- `ReinvestBusiness`: nonempty business instance `ContentId`, `Amount > 0`, empty `Name`,
  empty `AppearanceId`.
- `SetBusinessPricing`: nonempty business instance `ContentId`, a defined `PricingPosture`
  integer, empty `Name`, empty `AppearanceId`.
- `CloseBusiness`: nonempty business instance `ContentId`, `Amount == 0`, empty `Name`,
  empty `AppearanceId`.

The helper is content-independent and runs from
`BusinessStateValidation.ValidateStructure` before catalog compatibility classification.

Focused regressions prove:

- closure `Amount = 1` → **Corrupt** with complete content and with unrelated start/background content unavailable;
- closure nonempty `Name` → **Corrupt** under both content conditions;
- closure nonempty `AppearanceId` → **Corrupt** under both content conditions;
- reinvest nonempty `Name` or `AppearanceId` independently → **Corrupt** under both content conditions;
- pricing nonempty `Name` or `AppearanceId` independently → **Corrupt** under both content conditions.

A file/store-level regression uses a checksum-valid intrinsically corrupt closure receipt as
primary, a valid checkpoint as backup, and a catalog where unrelated character-start content is
unavailable. Primary classifies **Corrupt**, backup classifies **Valid**, and
`AtomicFileSaveStore.Read()` returns **RecoveredBackup**. Primary and backup bytes remain unchanged
during the read.

### Finding B — malformed frozen-v1 parsing containment

`HistoricalV1Codec.RejectImpossibleV1WireMembers` now catches only
`System.Xml.XmlException` from the JSON→XML infoset reader and translates it to the existing
validation-family `ArgumentException`.

There is no broad `Exception` catch and fatal/runtime failures are not swallowed.

Focused regressions prove:

- schema-1 envelope + matching checksum + malformed JSON payload →
  `DeserializeAndValidate(...)` returns **Corrupt**;
- no `XmlException` escapes the serializer validation boundary;
- malformed frozen-v1 primary + valid backup →
  `AtomicFileSaveStore.Read()` returns **RecoveredBackup**;
- corrupt primary bytes are not rewritten during read;
- valid backup bytes are not overwritten.

### Save/H1 and accepted business contracts

Unchanged:

- `SaveSchema.CurrentVersion == 2`;
- synthetic v0 → frozen historical v1 → v2;
- historical v1 → v2;
- current v2 → v2;
- H1 normalization remains same physical schema, same revision, receipt-ID-only;
- first normal commit after v1 restore remains in-memory v2 → revision +1 → physical v2.

Accepted business semantics are unchanged: launch, reinvest, pricing, closure, one active per type,
maximum four active, no closure refund, arrears launch/reinvest block, closure allowed with arrears,
ledger attribution, global entity allocation, and immutable business read model.

Frozen M1 types/API remain untouched.

### Focused and full engine-free regression

Engine-free CI run **#111** on source/test head
`9c997599c4e547f5d1a581a4d7a9c004f5e61ab0` passed.

Focused new checks:

- intrinsic business payload corruption before compatibility: **PASS**;
- intrinsically corrupt primary + valid backup recovery: **PASS**;
- malformed frozen-v1 containment + backup recovery: **PASS**.

Full regression:

- documentation validation: **PASS**;
- Simulation: **49/49 PASS**;
- Business ownership: **26/26 PASS** (previous merged-main baseline **23/23**);
- H1/H2/R2: **20/20 PASS**;
- M2 restore/provenance: **35/35 PASS**;
- Presentation adapter: **11/11 PASS**;
- Content catalog: **8/8 PASS**;
- Static Unity foundation: **31/31 PASS**;
- frozen M1 public API: **31/31 PASS**;
- Content bridge: **8/8 PASS**;
- M7 static: **43/43 PASS**;
- Unity runner contract static: **30/30 PASS**;
- repository security/release hygiene: **28/28 PASS**;
- .NET builds: **0 warnings / 0 errors**;
- clean-worktree gate: **PASS**.

### Unity / acceptance status

No final changed-head Unity acceptance was performed in this correction pass. The source correction
must go through Astra source re-audit first.

`FINAL M8-T01 UNITY ACCEPTANCE: PENDING SOURCE RE-AUDIT`

`M8-T01 SOURCE CORRECTED — ASTRA RE-AUDIT REQUIRED`

`M8-T02 NOT READY — blocked by M8-T01`

`SAFE TO SEND FOLLOW-UP PR TO ASTRA SOURCE RE-AUDIT`

Do not merge PR #25. Do not start M8-T02.

---

## Final frozen-v1 parser containment regression proof

Date: 2026-10-06 (+07:00)  
Executor: Codex (current session; no model switch claimed)  
Verified starting main: `435670877c382b06d6c0a99a6231b2e995ba0a74`  
Branch: `test/m8-t01-frozen-v1-xmlexception`  
Follow-up PR: [#26 — test: prove frozen v1 parser containment](https://github.com/vitoo16/Startup/pull/26)  
Production-code diff: **NONE**  
Save impact: **none — SaveVersion remains 2**

### Previous Astra MEDIUM finding and correction

The earlier EOF-truncated fixture, `{"SaveVersion":1,"Receipts":[`, did not prove the
`HistoricalV1Codec` XmlException containment boundary. The JSON-to-XML reader can
synthesize end elements at EOF, allowing root inspection to finish; later DTO rejection
can return Corrupt even without the containment catch.

The replacement payload is exactly:

```text
{"SaveVersion":1,"Receipts":[?]}
```

The test project independently constructs the same JSON infoset reader with
`JsonReaderWriterFactory.CreateJsonReader` and `XmlDictionaryReaderQuotas.Max`.
It enters the root, asserts the SaveVersion member, skips it, asserts the Receipts member,
and asserts `Throws<XmlException>` around the scan. Skipping Receipts encounters the
invalid array-item token before any DTO deserialization. The successful assertion emits:

```text
PROOF malformed-token root-member reader: XmlException before DTO deserialization
```

That same payload is wrapped with `JsonSaveSerializer.Wrap` as schema 1 / generation 0,
which computes its correct checksum. Direct `DeserializeAndValidate` returns **Corrupt**
without an escaping XmlException. The same envelope is written as primary, with an
independently validated **Valid** checkpoint as backup. `AtomicFileSaveStore.Read()`
returns **RecoveredBackup** with the expected backup RunId. Byte equality assertions
confirm both primary and backup remain unchanged during read.

The production catch remains the existing narrow XmlException-to-ArgumentException
translation. The serializer's validation-family filter contains ArgumentException, not
XmlException. Therefore removing that translation lets this reader exception escape before
DTO decoding, failing the direct Corrupt assertion. This is logical/test-wise mutation
dependence; no production mutation experiment or production visibility change was performed.

### Focused first, then full engine-free CI

GitHub Actions ran the focused BusinessOwnershipChecks suite first:

- [run 37344851334](https://github.com/vitoo16/Startup/actions/runs/37344851334):
  **SUCCESS**, branch head `0aa49d059e3693ca860ac6bf8570bf914ba3e284`;
- PR merge checkout: `379b1e8f8e8e4dd8b5ec1e6c9b7c7a7e78ea8e40`;
- raw-reader proof emitted; business suite **26/26 PASS**;
- build: **0 warnings / 0 errors**.

A temporary focused-only entry point in `scripts/Test-Simulation.ps1` was then restored
byte-for-byte to starting-main blob `3c0b4bf78d00b9e73286673d5c41b097b14396b7`.
There is no runner or workflow change in the final PR diff.

Full engine-free [run 37344984560](https://github.com/vitoo16/Startup/actions/runs/37344984560)
completed **SUCCESS** on tested source head
`0f317954592e06ebb3ed2bad7de84d68636d16bf`, using PR merge checkout
`a7a2abea63cee86db13246aa810f579257fee320` against the verified starting main.

Actual full results:

- Simulation: **49/49 PASS**;
- Business ownership: **26/26 PASS**, including the raw-reader proof;
- H1/H2/R2: **20/20 PASS**;
- M2 restore/provenance: **35/35 PASS**;
- Presentation: **11/11 PASS**;
- Content: **8/8 PASS**;
- Static Unity: **31/31 PASS**;
- frozen M1 API: **31/31 PASS**;
- Content bridge: **8/8 PASS**;
- M7 static: **43/43 PASS**;
- runner contract: **30/30 PASS** (static only);
- security/release: **28/28 PASS**;
- .NET builds: **0 warnings / 0 errors**;
- documentation validation: **PASS**;
- clean-worktree gate: **PASS**.

### Unchanged contracts and session closeout

Save/H1 remains: SaveVersion 2; synthetic v0 → frozen v1 → v2; historical v1 → v2;
current v2 → v2. H1 normalization stays identity-only, same physical schema and same
revision. A normal gameplay commit after v1 restore advances revision by one and writes
physical v2 from the migrated in-memory v2 state.

No production file, business behavior, migration, AtomicFileSaveStore semantics, H1 logic,
or frozen M1 API changed.

Skills used: startup-life-session-orchestrator; startup-life-gameplay-guardian;
unity-game-director; unity-gameplay-systems; unity-mcp-bridge; unity-qa-release;
unity-game-economy. Community skills were read from
`tea-x-random/unity-game-skills@dafb97ef00f94e64e42e6260bc6b3af74cc83dad`.  
Tests: focused and full GitHub Actions evidence above; final evidence-head CI is reported
on PR #26.  
Visual evidence: not applicable; final Unity acceptance was not run.  
Save impact: none; SaveVersion 2 and accepted Save/H1 contracts unchanged.  
Known limitations: final independent Astra source re-audit and changed-head Unity acceptance
remain required; this correction does not close M8-T01.  
Files changed: `tools/BusinessOwnershipChecks/Program.cs`;
`docs/evidence/M8-T01/SESSION.md` (append only).  
Repository execution: GitHub source and Actions; no local checkout changes.

`FINAL M8-T01 UNITY ACCEPTANCE: PENDING FINAL ASTRA SOURCE RE-AUDIT`

`M8-T01 TEST PROOF CORRECTED — FINAL ASTRA SOURCE RE-AUDIT REQUIRED`

`M8-T02 NOT READY — blocked by M8-T01`

`SAFE TO SEND TEST-ONLY PR TO ASTRA FINAL SOURCE RE-AUDIT`

Do not merge PR #26 automatically. Do not start M8-T02.


---

## M8-T01 employment launch scope correction

Date: 2026-10-06 (+07:00)  
Executor: Codex (current session; no model switch claimed)  
Starting merged main: `dbdb41f051814ac6e7fc13f27115940a59dd6fcb`  
Proposed branch: `codex/m8-t01-defer-employment-eligibility` (not created)

### Finding and authorized boundary

The final source audit identified the employment-dependent `FullTimeRequired` launch
rejection as a MEDIUM scope violation. The user explicitly classified it as outside
M8-T01 and requested the correction. Employment, operating-window and required-owner-minute
eligibility belong to M8-T02, which remains blocked on final M8-T01 Unity acceptance.

### Correction and regression

`SimulationEngine.LaunchBusiness` now validates ownership/investment without the
employment-dependent launch rejection. The existing manager-unsupported rejection and
ownership, active-count/type, investment, cash and arrears guards are retained.

The launch regression now proves that an otherwise valid FullTimeRequired launch while
employed commits, charges the exact investment, consumes zero minutes, preserves employment
and the simulation instant, and survives a byte-identical cold restore. This is an ownership
checkpoint only: operating windows, owner minutes, business demand, revenue and daily
settlement remain unimplemented.

### Verification and session closeout

Owner model: Codex (current implementation executor).  
Skills used: startup-life-session-orchestrator; startup-life-gameplay-guardian;
unity-game-director; unity-gameplay-systems; unity-game-economy; unity-mcp-bridge;
unity-qa-release. Community skills loaded at pinned revision
`dafb97ef00f94e64e42e6260bc6b3af74cc83dad`.  
Acceptance criteria: remove the employment launch scope violation; prove employed ownership,
conservation/no-time-advance and restore; preserve save/parser/H1 and accepted business contracts.  
Tests: local isolated snapshot BusinessOwnershipChecks 26/26; SimulationChecks 49/49;
AstraFoundationChecks 20/20; RestoreInvariantChecks 35/35; PresentationChecks 11/11;
ContentCatalogChecks 8/8: all PASS. M1 public API static gate: 31/31 PASS. .NET Standard production
assemblies and the local .NET 10 test runner build with 0 warnings/errors. The host has SDK 10
only, so the scratch runner target was changed to net10.0 solely for local verification;
repository project targets remain unchanged. Local NuGet vulnerability lookup was disabled
because network access is unavailable; repository CI remains authoritative for the unchanged
.NET 8 runner. Changed-head repository CI: NOT RUN; GitHub publication awaits explicit approval.  
Visual evidence: not applicable; no UI/scene/asset change.  
Save impact: no persisted-field change; SaveVersion remains 2; no new migration.
Existing v0/v1/v2 migration, H1 normalization and parser containment are retained.  
Known limitations: final changed-head Unity import/compile, EditMode and PlayMode acceptance
remain pending. M8-T01 remains unchecked; M8-T02 is not started.  
Files changed: `Assets/StartupLife/Scripts/Simulation/SimulationEngine.cs`;
`tools/BusinessOwnershipChecks/Program.cs`; `docs/evidence/M8-T01/SESSION.md` (append only).  
Repository execution: isolated verification snapshot; the user's older Unity checkout and its
existing changes are preserved. GitHub source-upload auto-review rejected external publication
without destination-specific approval. No GitHub branch, commit or PR was created.
Commit: none; the reviewable patch is prepared for the exact starting merged main.
