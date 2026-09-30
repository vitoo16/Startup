# M0-T03 — Static foundation verification

Date: 2026-09-30  
Owner: GPT-5.6 Sol  
Scope: repository-verifiable Unity foundation only  
Status: **PASS for static checks; M0-T03 remains incomplete**

## Skills used

- `startup-life-session-orchestrator`
- `new-unity-project`
- `unity-cli`
- `unity-package-management`
- `unity-project-setup`
- `unity-mcp-bridge`
- `unity-qa-release`

The community skills were read from the project-pinned revision `dafb97ef00f94e64e42e6260bc6b3af74cc83dad`. No live Editor/MCP action is claimed in this connector session.

## What the static gate proves

`scripts/Test-UnityFoundationStatic.ps1` verifies repository state without pretending to execute Unity:

- exact Unity Editor patch `6000.3.25f1` and revision `e1dba0a9aba4`;
- Force Text serialization and Visible Meta Files;
- Startup Life product identity and portrait reference resolution;
- landscape autorotation disabled and Input System active;
- required package versions pinned in `manifest.json`;
- every manifest direct dependency agrees with `packages-lock.json` at depth 0;
- MobileBaseline is the first enabled build scene;
- Input System and Localization configuration objects are registered;
- the baseline builder explicitly configures portrait, orthographic camera, 1080x1920 CanvasScaler, safe area, Vietnamese glyph validation, and localization keys;
- generated Unity folders are not tracked;
- tracked Unity assets have `.meta` sidecars;
- the reviewed community skill revision remains pinned and required community skill contents match that pin.

## CI evidence

PR #2 initial verification run: `36669865170` — **success**.

- Documentation validation: passed; 4/4 canonical sources, 49 expected task IDs, 26 Markdown files checked.
- Static Unity foundation: **31/31 passed**.
- Engine-free .NET build: **0 warnings / 0 errors**.
- Simulation regression suite: **35/35 passed**.
- Clean-worktree guard: passed.
- Artifact: `engine-free-foundation-reports`, ID `11077538083`.
- Artifact SHA-256: `1d70eb728d235d87045b4b63cce66b7279d056a47e21f7201a15d354f5c38cd9`.

## What this does NOT prove

- Unity imports/compiles the current head.
- EditMode or PlayMode tests execute successfully in Unity.
- MCP connects/reconnects after compilation.
- the portrait baseline renders correctly in the Game view or on a device.
- Android/iOS modules are installed.
- Android/iOS builds succeed.
- device lifecycle/save-resume behavior works.

Those remain runtime/platform gates and block M0-T03/M0-T04 completion.

## Visual evidence

None in this slice. Static repository verification cannot replace the required portrait screenshot/device evidence.

## Save impact

None.

## Files introduced by this verification slice

- `scripts/Test-UnityFoundationStatic.ps1`
- `.github/workflows/engine-free-ci.yml` integration
- this evidence record
- `docs/handoffs/FOUNDATION_MODEL_HANDOFFS.md`

## Model handoff

Astra foundation/architecture audit and the optional Gemini read-only coverage inventory are queued in `docs/handoffs/FOUNDATION_MODEL_HANDOFFS.md`. Neither is recorded as executed.
