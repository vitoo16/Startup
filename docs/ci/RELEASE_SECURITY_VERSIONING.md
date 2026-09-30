# Startup Life — Release security and versioning contract

Date: 2026-09-30  
Status: **foundation policy; release signing/version injection not yet enabled**

This contract defines what must be true before Startup Life produces distributable mobile artifacts. It does not turn the current prototype/foundation into a release candidate.

## 1. Secrets never enter Git

Never commit:

- Unity license credentials or service-account secrets;
- Android keystores or passwords;
- Apple signing certificates/private keys;
- App Store Connect `.p8` keys;
- provisioning profiles;
- production analytics/ad/service credentials;
- `GoogleService-Info.plist` or equivalent production service files;
- local `.env`, `secrets.json`, `*.private.*`, or credential directories.

Use repository/environment secret stores and inject at execution time. A secret committed even once must be treated as leaked and rotated; deleting the latest file is not sufficient.

The repository keeps examples/templates only when they contain no real credentials.

## 2. GitHub Actions trust boundary

- First-party actions must be pinned to reviewed commit SHAs rather than floating major tags.
- Normal verification uses `permissions: contents: read`.
- Do not use `pull_request_target` for code-executing verification.
- Self-hosted Unity workflows are manual-only until runner hardening and access policy are explicitly approved.
- Untrusted forks must never receive signing, store, or Unity license credentials through a privileged workflow.

## 3. Artifact boundary

There are four different artifact classes; never describe one as another:

1. **Engine-free report** — documentation/static/.NET evidence only.
2. **Unity test report** — proves the pinned Unity Editor executed tests; not a player build.
3. **Unity mobile build/export** — Android player artifact or iOS Xcode project plus provenance; not device QA.
4. **Signed distribution artifact** — signed Android bundle/APK or archived/exported iOS IPA; requires signing evidence and remains separate from store acceptance/device QA.

Every build/export must retain Unity CLI provenance and relevant logs.

## 4. Marketing version

`PlayerSettings.bundleVersion` is the human-facing app version. The generated Unity project currently carries the template value `1.0`; that value is **not** a public release commitment.

Before the first externally distributed build, choose an intentional release version and change it through a reviewed Unity/project-settings workflow. Do not silently bump marketing version on every CI run.

Recommended development progression once distribution begins:

```text
0.1.0  internal playable
0.2.0  vertical slice
0.x.y  subsequent internal/beta releases
1.0.0  only when the product is actually ready for 1.0
```

This is a release convention, not a locked gameplay rule. Astra does not need to redesign gameplay around version numbers.

## 5. Platform build numbers

Store build numbers are machine-generated and monotonically increasing per uploaded version:

- Android `versionCode` → CI/release counter;
- iOS `buildNumber` (`CFBundleVersion`) → CI/release counter.

Preferred source once release builds are enabled: a CI-controlled positive integer such as `GITHUB_RUN_NUMBER`, optionally combined with an explicitly documented floor/offset if store history already contains larger numbers.

The build number must not depend on wall-clock formatting, developer-local counters, or manual edits that can collide across machines.

**Current state:** the generic Build Profile wrapper intentionally does not mutate Android/iOS build numbers yet because accepted mobile Build Profiles and the Unity Editor-owned release entrypoint have not been committed. Therefore the current runner contract is smoke-test infrastructure, not store-upload infrastructure.

## 6. Signing

Foundation/device smoke builds should avoid production signing whenever possible.

When signing is introduced:

- Android keystore material comes only from CI secret storage and is materialized into a temporary runner path;
- Apple signing/App Store Connect material exists only on the controlled macOS release runner/keychain or CI secret store;
- temporary key/profile files are deleted during cleanup;
- logs never print secret values;
- signing is isolated from ordinary pull-request verification.

Do not pass signing secrets as hard-coded workflow literals. Even environment-backed CLI arguments can appear in process listings, so release signing needs an explicit threat review before enablement.

## 7. Release gate

A distributable build is not accepted until all applicable evidence exists:

- [ ] canonical docs/static foundation green;
- [ ] Unity import/compile green on the exact commit;
- [ ] EditMode + PlayMode reports green;
- [ ] committed, reviewed platform Build Profile;
- [ ] unique platform build number injected and reported;
- [ ] build/export provenance retained;
- [ ] signing evidence where distribution requires it;
- [ ] physical-device safe-area/input/lifecycle/save-resume checks;
- [ ] performance and localization gates for the target milestone.

No task checkbox should be advanced merely because a workflow file or signing placeholder exists.
