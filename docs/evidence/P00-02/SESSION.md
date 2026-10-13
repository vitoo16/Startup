# P00-02 Session — Pixel originality grammar & provenance preflight

**Owner model:** GPT-6 Technical Director  
**Date:** 2026-10-14  
**Baseline:** `main` after P00-01, at `dcb0795d3cc75743ad4e050ad01c9e10147eb530`.  
**Domain:** art direction, independent rights clearance, static QA, code.  

- **Skills used:** `startup-life-session-orchestrator`, `startup-life-asset-quality-gate`, `vietnam-art-direction`, `2d-pixel-perfect`. Read canonical GDD, MVP historic plan, active production roadmap, art/IP contracts and project skill inventory.
- **Acceptance criteria:** document three independent initial concepts for each Golden category; define and initialize provenance v1 manifest without false clearance; enforce approval rights, score, active style and registry cross-reference; preserve M13 validator and active style; include deterministic unit cases in CI.
- **Tests:** Added deterministic Python stdlib fixture suite and engine-free CI preflight. Their actual execution status must be stated separately from script presence; do not mark green until run on exact commit.
- **Visual evidence:** NOT RUN. No assets generated, imported, independently approved or captured on real devices.
- **Save impact:** NONE. No game simulation/save code modified.
- **Known limitations:** Human review/terms/legal clearance, Unity imported assets, original pixel palette and PPU, real animated Golden, device proof and runtime scene-reference scan remain pending. The preflight validates evidence references structurally; it cannot prove the authenticity of external evidence.
- **Files changed:** `docs/production/P00_PIXEL_IP_LOCK.md`; `Assets/StartupLife/Art/_ArtDirection/provenance/manifest.json` plus Unity meta; `scripts/Validate-AssetProvenance.py`; `tools/AssetProvenanceChecks/test_asset_provenance.py`; `.github/workflows/engine-free-ci.yml`; this session log.
- **Commit:** This session document ships in the same atomic Git commit; inspect Git history for SHA.
