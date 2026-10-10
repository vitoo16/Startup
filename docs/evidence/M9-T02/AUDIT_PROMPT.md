# M9-T02 independent audit prompt

Audit the new local HEAD in C:\Users\viett\Startup. Work directly in this
original repository; do not clone, create a worktree, merge, or close M9-T02.
Start by recording `git rev-parse HEAD` and verify it matches the SHA supplied
by the human. Read AGENTS.md, load startup-life-session-orchestrator and all
required domain skills, then read the milestone contract and acceptance criteria.

Audit the full diff from 7c3c581557040fc38153a83da7b8d036323b24dc to the pinned
commit. Focus on receipt-backed economic finalization in GameSession and cold
replay, transaction-start invoice ordering, candidate isolation/validation/CAS,
failed writes and rejected/duplicate commands, Study/midnight lazy epoch opening,
and preservation of historical v2 and valid pre-fix v3 saves. Verify the fixture
provenance and hashes, frozen source bytes, and meaningful regression coverage.

Review the real uGUI business controls: 96-unit target sizing, layout and scroll
reachability, event wiring, Vietnamese text, safe area, and overlap/clipping.
Check that acceptance thresholds and runtime validation were not weakened.

Evidence is committed in docs/evidence/M9-T02/runtime-20261011. Verify its final
SHA256SUMS, NUnit/JUnit identities/counts, exit codes, source hashes, save hashes,
real portrait images, and dynamic TMP atlas restoration. Latest original-repo
execution is EditMode 9/9, PlayMode 14/14, graphical 1/1 PASS. Distinguish earlier
engine-free evidence and pre-commit byte-identical runtime verification from a
new committed-head CI/fresh-import/device verdict. Do not infer untested coverage.

Read TRANSACTION_FINALIZATION_FIX.md, BUSINESS_TOUCH_TARGET_FIX.md and the
evidence README for chronology and limitations. Re-run the canonical simulation
and Unity gates directly on the original repo when needed, preserving user saves
and source bytes. Report concrete P0/P1/P2 findings with file/line, triggering
scenario, expected/actual behavior, impact, and required correction. Separate
source correctness, executed runtime evidence, and outstanding milestone gates.
Return PASS, FAIL or NOT VERIFIED for each gate, with remaining blockers. This
request authorizes a read-only independent audit; do not modify implementation,
commit, push, merge, or declare the full milestone closed.
