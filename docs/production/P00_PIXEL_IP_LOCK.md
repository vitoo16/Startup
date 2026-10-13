# P00-02 — Original Pixel Style / Asset Provenance Preflight

**Date:** 2026-10-14  
**Status:** TECHNICAL CONTRACT RECORDED; artistic/IP sign-off PENDING; no pixel asset approved or integrated.  
**Owner:** GPT-6 Technical Director.  
**Authorities:** `PRODUCTION_PIXEL_STYLE_CONTRACT.md`, `PRODUCTION_ASSET_IP_POLICY.md`, `ART_BIBLE_M13.md` (historical active M13 implementation until coherent W03 migration).

## Hard boundaries

- Production style candidate: `vn_everyday_pixel_v1`. Current live M13 YAML/composition/registry are still `vietnam_cozy_chibi_cutout_v1`. This **must not** be treated as a hybrid shipping style. W03 owns the synchronized migration.
- Original design **process** can be specified now; palette hex, PPU, native sprite dimensions, pixel camera policy and import settings remain prototypes until phone captures and independent approval. No values may be silently copied from another title. UI/TMP text remains native-resolution.
- No generated, licensed or contractor work is automatically original, exclusively owned, trademark-clear or production-approved. Independent human rights/art review and evidence remain mandatory.
- A Golden asset is not accepted because its files exist: 8 score dimensions, >=15/16 total, each >=1, distinct reviewer, true Unity import and physical-device BeautyCell. Character motion requires actual moving limb joints/poses, not whole-body translation.

## Three *independently initiated* exploration directions per Golden proof

These are **brief alternatives**, not images or approved candidates. Draw from our GDD and observed functional needs, not external game screenshots.

| Golden target | A — primary exploration | B — distance alternative | C — distance alternative |
| --- | --- | --- | --- |
| Main character | Large rectangular hair mass and asymmetric sling strap; soft-square cheeks | Tapered hair sections, off-centre jacket closure and mismatched shoe accents | Compact side-swept fringe, contrasting elbow patch and angular messenger pocket |
| Night workspace | L-shaped lamp arm framing a short desk; helmet hook above book shelf | Narrow stacked wall storage, pull-out desk tray and small fan | Folding desk beside window, cube wall shelves and a backpack peg |
| Workplace | Cluster of low staggered monitors around central aisle | Long diagonal bench with shared stationary shelves | Compact paired desks arranged around a presentation board |
| Sidewalk coffee kiosk | Tall slim service hatch, two-height shelf and blank QR plinth | Wide canopied short counter with pickup corner | Folded side-panel kiosk and inset takeaway window |
| Life UI | Asymmetric receipt-edge dividers; header tab tucked to one side | Loose-leaf side registration with clipped data panels | Staggered notebook column tabs and compact stamp feedback |

Select only after originality comparison, phone-scale hierarchy review and convergence into **one** visual grammar. Ordinary receipts, notebooks, office props and chibi proportions are generic concepts; uniqueness cannot be inferred from them. Keep color, shape combination and motion decisions authored rather than copied. Run a similarity-distance review against relevant products **without incorporating their expressive art as source inputs**.

## Provenance schema v1 (engine-free preflight)

Manifest: `Assets/StartupLife/Art/_ArtDirection/provenance/manifest.json`  
Validation entrypoint: `python3 scripts/Validate-AssetProvenance.py`  
Fixtures: `python3 -m unittest discover -s tools/AssetProvenanceChecks -p 'test_*.py'`

The manifest has `schema = startup-life.asset-provenance.v1`, `proposed_style_id = vn_everyday_pixel_v1` and a collection named `assets`. Each candidate uses the required fields in `PRODUCTION_ASSET_IP_POLICY.md`, plus `unity_path` and `scorecard` for the approval join. Missing evidence fields are `null` before approval, **not invented references**.

For a future approved record:
- `asset_id`, `type`, `style_id`, `status`, `creator_type`, `creator_or_licensor_ref`, `created_utc` and `process_reference` are required for all candidate records.
- Approval additionally requires real existing source file(s), Unity output file(s), `unity_path` inside `Assets/StartupLife/Art/Approved/`, scoped commercial-use terms, license reference, similarity and technical-review IDs, physical-device BeautyCell ID, `independent_reviewer` with `human:` reference distinct from producer, `approval_date_utc`, and complete 8-value scorecard >=15/16 with no zeros.
- AI-assisted candidates must retain model/tool-and-terms evidence. Evidence references may be access-controlled identifiers; do not check private contracts, signatures, phone numbers, payment receipts, or API tokens into public Git.
- Registry join is **bidirectional** on `asset_id` with matching style and Unity path. `REVOKED`, `REJECTED`, `QUARANTINED` and `RIGHTS_PENDING` records may never be referenced by registry.
- While M13 is active, the empty manifest is valid as a preparatory preflight; a pixel `APPROVED` record is rejected if M13 is still the current active style. This avoids premature activation.

For W03: update the active specs, composition, theme, registry style and Golden validator **atomically** and include the approval join in full static and runtime reference checks. This P00-02 preflight does *not* replace a human originality/IP review, a source-file legal review, real Unity screenshots, a scene reference scan, or phone verification.

## Acceptance / risk disposition

| Check | Result |
| --- | --- |
| Active M13 style preserved; zero approved registry entries | Verified in source inspection at session baseline |
| Distinct concept directions for five Golden targets | Authored briefs, not visual or legal approval |
| Provenance manifest and CI validator | Added in P00-02; CI result requires a run on exact commit |
| W03 coherent pixel conversion | **NOT STARTED** |
| Human originality/legal review and Unity/device Golden proof | **NOT RUN** |

**No release/IP clearance or art acceptance is granted by this document.**
