# Startup Life — Original Asset, Copyright, Licensing & Provenance Policy

**Status:** ACTIVE production acceptance policy from 2026-10-14; individual assets remain **UNAPPROVED** until evidenced. **Owner:** GPT-6 Technical Director; creative/IP gate requires independent human review where indicated. Applies to visual artwork, UI screens, character rigs, animation clips, sound, music, fonts, titles, logos, marketing art, and AI-assisted reference work.

## 1. What we can and cannot promise

The requested target is **a recognisably original cute-pixel product, independent of other apps and not using anyone's protected artwork or brand without permission**.

No finite search, image model or similarity checker can prove 100% global originality across every internet work, app, game or jurisdiction. A creation can independently share unprotectable general elements with other products. We can enforce: clean original source, traceable rights, no deliberate imitation, independent comparison review, release clearance, auditable receipts and remediation when uncertain. This is a production process, not legal advice or a warranty of non-infringement.

**Never market a candidate as "copyright-free" merely because it was AI-generated.** The ability to commercially use content, obtain enforceable exclusive rights, and avoid infringement are different questions. Confirm relevant platform/tool terms and obtain professional legal advice for high-value brand assets or disputed ownership.

## 2. Absolute prohibitions for production candidates

- No prompts asking for the art style, exact composition, sprite, UI or logo of a **named game/app, franchise or living artist**; no "make it the same but change colors."
- No tracing, redrawing, overpainting, re-coloring, pixelating, silhouette borrowing or near-derivative transformation of third-party screenshots, sprite sheets, icon packs or illustrations.
- No use of third-party character names, logos, branded signs, QR merchant identities, trademarked product packaging, sound marks, UI wordmarks or watermarked media as first-party decoration.
- No asset from search/social/media/commercial packs without documented rights suitable for the **intended** redistribution, derivative editing and platform distribution (iOS, Android, advertising/store media).
- No scraping copyrighted artwork into model-style references as a substitute for original art direction. Visual research is for competitive distance/checking, not copy construction.
- No font/audio/texture/plugin with unverifiable license or incompatible attribution, share-alike, embedding, sublicense or commercial terms. Store licenses with versions and asset relationships.
- No production-ready flag from an unverified generation response, UI mockup, blank provenance field or unsupported similarity score. An asset's creator cannot unilaterally approve their own originality.

These prohibitions cover **AI and human creators equally**.

## 3. Approved source categories

| Source route | Minimum required evidence | Extra gate |
| --- | --- | --- |
| Hand-drawn / first-party | Original editable layered source, author/ownership declaration, iteration history, dates, clear use rights | Independent similarity/brand check |
| AI-assisted, based on original project brief | Prompt & model/tool version, generation/seed or batch refs when available, declared user-supplied inputs, selection/rework logs and resulting human refinement | Tool terms review; independently reviewed; no high-confidence ownership/exclusivity claim solely from AI output |
| Third-party licensed asset | Exact vendor/author, purchase/permission record, commercial redistribution/modification rights, attribution constraints, version & license file | Rights review; no use until CLEAR |
| Contractor deliverable | Written scope, originality and non-infringement warranties where lawful, assignment or sufficient perpetual commercial rights, subcontractor disclosures | Signed agreement and source handoff |

If an ingredient of a composite (e.g., font inside a mockup, image texture, reference pack) is unlicensed, **the composite is not approved**.

## 4. Per-asset provenance manifest and statuses

Every candidate gets a stable `asset_id`, stored in source-control in a dedicated production provenance manifest (create under `Assets/StartupLife/Art/_ArtDirection/provenance/`; first implementation must define and validate schema). Do not put API secrets or personal confidential data into Git.

Required conceptual fields:

```yaml
asset_id: sl-character-main-001
type: character|environment|ui|icon|audio|font|marketing
style_id: vn_everyday_pixel_v1
status: DRAFT|QUARANTINED|RIGHTS_PENDING|READY_FOR_ART_QA|APPROVED|REJECTED|REVOKED
creator_type: first_party|contractor|licensed_third_party|ai_assisted
creator_or_licensor_ref: <recorded project identity>
created_utc: <ISO-8601>
source_files: [<versioned editable layers or audio project>]
output_files: [<unity import paths, never raw candidate in runtime scene>]
process_reference: <prompt/brief version + refinement log>
model_tool_and_terms_ref: <only if AI-assisted>
input_reference_rights: [<original|licensed link records>]
third_party_dependencies: [<font, texture, sample licenses>]
license_scope: <commercial app, store media, modifications, platforms>
license_evidence_ref: <access-controlled link/record; no secrets in Git>
similarity_review_ref: <evidence ID>
technical_import_review_ref: <evidence ID>
visual_beautycell_ref: <actual device capture ID>
independent_reviewer: <role/ref>
approval_date_utc: <ISO-8601 or null>
rejection_or_replacement_reason: <only if applicable>
```

The existing `Assets/StartupLife/Art/Approved/registry.json` is currently **empty**. Retain the existing 8-dimension Golden gate (>=15/16, no zero) and make it impossible to add an entry without `status=APPROVED` in provenance, clearance reference, real Unity screenshot, import/rig verification, and distinct reviewer sign-off. Do not populate approval registry as part of documentation-only work.

## 5. Originality-distance review (not automatic legal clearance)

1. **Design-space mapping:** for each Golden character, UI system, environment and logo, record three different *independently conceived* silhouette/composition options. No named-title derivation or tracing.
2. **Market similarity check:** reviewer inspects relevant mobile-life/business/2D/pixel products and known icons/trademarks for potential conflict. Images/urls only for **risk comparison**; do not download them as production source.
3. **Distance dimensions:** silhouette/face; clothing and accessory cluster; recurring prop/icon motifs; visual hierarchy and exact panel shape; palette+lighting as a whole; composition and branding; game/store title; animation poses. Similarity in **one generic idea** is not by itself copying, but high similarity in multiple expressive elements triggers concern.
4. **Decision:** `CLEAR` only when source process and review are documented, `REVISE` if concerns are resolvable, `QUARANTINED` for unclear rights, `REJECTED` for direct derivation. A search with no hit never guarantees uniqueness.
5. **Escalation:** high-value mascot, logo/title and disputed work require legal/brand consultation before any store or marketing publication. Do not assert legal ownership or trademark clearance from an AI judgment.

Comparisons can use off-repo reference URLs/notes and internally created thumbnails; store only permitted excerpts or metadata. Do not bulk store third-party copyrighted screenshots in the product repository.

## 6. Pixel-art UI/animation-specific controls

- All avatars, outfit pieces and scene characters must come from a common original rig-layer architecture; genuine joint/pose changes are required for locomotion and gestures.
- UI icons must be source-designed, not modified marketplace icon packs with superficial pixel filters; consistent pixel grid alone is not sufficient differentiation.
- Pure geometry like tabs, buttons and grids is generic; differentiate actual combination through original component layout, named states, motion rhythm and hierarchy while maintaining accessibility.
- Preserve legibility of Vietnamese TMP diacritics and VND formatting. Do not bake automatically generated fake Vietnamese lettering into accepted image assets.
- For audio, check melody/arrangement/sample clearance as well as any generator's commercial-use terms; first-party performances and commissioned work preferred.

## 7. CI gates and enforcement backlog

Proposed implementation in production W01–W03, **not present/verified merely because this policy is written**:

1. Define `provenance.v1.json` schema and an approved-registry join: every referenced approved asset maps to exactly one clearance record; every `APPROVED` record has Unity path, source, license, reviewer, score and screenshot evidence.
2. Extend `scripts/Validate-GoldenArt.py` and static Unity checks to reject approved-registry records with missing metadata or mismatched style IDs; scan production scene references for raw-source directories.
3. Static scans can flag external branding names, third-party file hashes or ambiguous source metadata, but **cannot** algorithmically establish non-infringement. Require independent artistic/human review.
4. Gate production builds on manifest validity and missing/revoked rights. CI checks do not auto-approve images; human visual reviews remain explicit.
5. Record the contract/license evidence outside public Git if it contains personal contact/payment information; check in only stable non-sensitive references.

## 8. Remediation and release-stop procedure

When potential copying, disputed rights or unexpectedly similar visuals are discovered:

- Set the candidate `QUARANTINED` immediately and block any approval/build referencing it; do not silently rename it.
- Identify import references, atlases, prefabs, marketing captures and release packages.
- Replace with a clean-room **new first-party asset**, not an edited/recolored derivative of the suspect file.
- Re-run similarity, provenance, Unity import, rig, screenshots and build asset audit.
- If already distributed, escalate incident handling with product/legal owner for takedown/replacement/notification decisions; preserve the evidence trail.

**Release stops** if any shipping asset has `RIGHTS_PENDING`, `QUARANTINED`, `REJECTED`, `REVOKED`, or missing required clearance. Explicit risk acceptance cannot be inferred from a green engine-free CI run.

## 9. Sources of authority and ownership

The production style contract is `PRODUCTION_PIXEL_STYLE_CONTRACT.md`; historical visual design evidence `ART_BIBLE_M13.md` remains archived. Existing `startup-life-asset-quality-gate` and `vietnam-art-direction` skills remain mandatory. The independent reviewer must be someone **other than the producing model/agent**; a real human/IP professional is required for unresolved legal rights or branding questions. The approved registry owns runtime assets, and source-control changes must be accompanied by schema/provenance/quality evidence.

The purpose of this policy is original commercial art with documented clearance, **not** an impossible guarantee that no visual anywhere in the world resembles the game.
