# Startup Life — Local-first CI/CD

After merging this branch, all eight historical workflows are manual-only; no PR/push/scheduled cloud Actions runs. This change preserves the original 43 engine-free commands and 24 C03 art/source checks, including all original Python receipt assertions. It does not declare a passing build.

Windows dependencies: Git LFS, Node 24, pwsh 7, Python 3 with Pillow 11.3.0, .NET 8. Native Unity additionally needs Unity CLI and a licensed Unity 6000.3.25f1 Editor with graphics available.

    git config core.hooksPath .githooks
    node scripts/local-ci.mjs --profile quick
    node scripts/local-ci.mjs --profile full
    node scripts/local-ci.mjs --profile unity

Quick: all engine-free documentation/security/static Unity/simulation/asset Python and .NET gates. Full: Quick + all six separate C03 source-only/pose stress/visual review and SHA assertions. Unity: Full + current EditMode/graphical PlayMode with actual NUnit/JUnit artifacts.

The versioned `ci/workflow-steps.json` contains verbatim GitHub run step bodies, executed on Windows by `ci/Run-WorkflowStep.ps1`; keep this snapshot synchronized when changing the original workflow. Bash Python and dotnet steps and Python heredoc assertions are translated to native PowerShell invocations without dropping assertions.

Local test reports, logs and artifacts go under ignored `.local-ci/runs/`; failing jobs remain FAILED. A pre-push hook can run Quick by opt-in. Local checks cannot be consumed by hosted GitHub branch protection.

Android Unity Build Profile publication still needs explicit `scripts/Invoke-UnityBuild.ps1 -Profile ... -OutputPath ... -EvidenceDirectory ...` and physical Android QA. iOS archive/IPA/TestFlight requires a Mac, signing, and real device testing; Windows cannot certify these. No production deploy or release staging is implied by the test profile.

Static art evidence must never be mistaken for owner/artistic/IP approval. Missing Unity screenshot/device evidence stays blocked. All original visual review approvals remain human gates.
