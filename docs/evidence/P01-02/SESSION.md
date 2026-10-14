# P01-02 — Headless Unity verdict integrity

**Date:** 2026-10-14  
**Owner model:** GPT-6 Technical Director  
**Main baseline:** `9f8a74d3e3494acac424af1914d6cecb0765a20f`.  
**Status:** IMPLEMENTED_UNVERIFIED — Unity runner still not provisioned; no device evidence.

## Outcome

Removed a real false-positive condition from `scripts/Test-UnityHeadless.ps1`: exit code 0 plus existing NUnit/JUnit files could previously pass even when both files were leftovers or contained an empty/invalid verdict. Now stale per-mode XML/logs are removed before launching Unity, and the successful invocation must return valid matching NUnit 3 and JUnit reports with at least one actual case, **all passing and none skipped/inconclusive**.

The strict shared reader `scripts/Read-UnityTestVerdict.ps1` reads the two outputs without another Unity implementation, and `tools/UnityReportChecks.ps1` generates disposable engine-free pass/negative fixtures. Static runner checks and normal CI were expanded without relaxing existing validators, fixture hashes, thresholds or Unity runner labels. `docs/ci/UNITY_RUNNER_CONTRACT.md` now describes the stricter acceptance.

## Closeout

- **Skills used:** `startup-life-session-orchestrator` and `unity-cli` (official), plus historical architecture/QA policies from `docs/SKILLS_MANIFEST.md`.
- **Acceptance criteria:** reject stale/empty/malformed/inconsistent test reports; retain real Unity execution path and both platform workflow routes; deterministic CI fixture coverage.
- **Tests:** Added 9 engine-free report parser cases and CI step; exact-head GitHub CI must be checked before claiming PASS. Unity EditMode/PlayMode **NOT RUN here**; no self-hosted Editor was invoked.
- **Visual evidence:** NONE; no physical-device screenshots, no Unity BeautyCell.
- **Save impact:** NONE. No gameplay code, serializer, content or historical fixtures changed.
- **Known limitations:** Runner provisioning, Unity compilation, mobile Build Profiles, Android/iOS export, signed packages and physical devices still need real hardware/Editor gates. This does not change the Android-first/iOS-deferred ADR.
- **Files changed:** `scripts/Test-UnityHeadless.ps1`, `scripts/Read-UnityTestVerdict.ps1`, `tools/UnityReportChecks.ps1`, `scripts/Test-UnityRunnerContractStatic.ps1`, `.github/workflows/engine-free-ci.yml`, `docs/ci/UNITY_RUNNER_CONTRACT.md`, `docs/evidence/P01-02/SESSION.md`.
- **Commit:** Refer to commit containing this record.
