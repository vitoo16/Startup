# Original-repository verification evidence

Owner: Codex. Skills: startup-life-session-orchestrator; unity-cli; ui; ui-ugui;
vietnam-art-direction; startup-life-gameplay-guardian.

Latest run: 2026-10-11 02:55 Asia/Bangkok, directly in the original repository.
Unity 6000.3.25f1; CLI 1.0.0-beta.12. EditMode 9/9, PlayMode 14/14 and graphical
portrait flow 1/1 PASS. Both execution exits 0. NUnit/JUnit identities agree.
The 397 tracked Unity input hashes match the preceding successful original-repo
run and the production inputs committed with this evidence. This is pre-commit
verification of those same bytes, not a post-commit fresh-import or CI run.

Entry points: REPORT.md, environment.json, verdict.json, test-matrix.json,
unity-tests/*results*.xml, graphical/results*.xml, visual/*.png.
source-hashes-before.json identifies the tested source. source-differences.json
records only Unity's generated TMP atlas change; font-restoration.json records
restoration of its exact pre-run bytes. User-save before/after hashes agree;
private user-save backups and original token-bearing logs are excluded.
normal-save.json is a synthetic save produced by the graphical test.

prior-engine-free/ contains earlier transaction-repair regression evidence,
not new execution in this commit-packaging session. See
../TRANSACTION_FINALIZATION_FIX.md for its provenance and bounded Astra review.
The graphical failure described in that historical record was superseded by
the UI fix and two passing original-repository graphical runs; see
../BUSINESS_TOUCH_TARGET_FIX.md. The full independent M9-T02 audit remains open.

Text copies redact user-home paths and Unity access tokens as disclosed in
REDACTION.txt. SHA256SUMS.txt covers the final in-repository evidence bytes.
Screenshots are evidence under docs, not production scene assets; Git LFS
stores PNGs according to the repository's existing attributes.

Save impact: no schema change; existing saves unchanged. Known limitations:
no full business-panel screenshot set, physical-device acceptance, complete
historical migration matrix under Unity, or committed-head fresh import/CI.
Files changed: transaction/UI fixes, their regression tests and compatibility
fixture, attributes and session records, this evidence directory and audit prompt.
Commit: packaged together with the verified fixes. No push, merge or milestone close.
