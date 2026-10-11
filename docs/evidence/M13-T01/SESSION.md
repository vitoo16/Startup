# M13-T01 SESSION — Golden art contracts

Owner: GPT-6 (Technical Director and primary implementer)
Base main: `c889787a47dcfa996a8c6e90bede7efa7f3564ff`.

## Skills loaded
`startup-life-session-orchestrator`, `vietnam-art-direction`,
`startup-life-asset-quality-gate`, `unity-art-direction`,
`unity-asset-designer`, `unity-asset-pipeline`,
`unity-scene-composition`.

## Acceptance this slice targets
- Canonical art spec in YAML 1.2 JSON subset: fixed 2.6-head chibi, PPU100, ortho5, 1080x1920, joint-layer names, soft upper-left key, source/master limits, mobile goals.
- Palette roles match existing `MobileTheme.asset` exactly; specific scene decor palette is separate.
- Five **unapproved** Golden asset briefs for character, home, office, business/kiosk and Life UI, all grounded in 2–4 contemporary Vietnamese visual anchors and blank authored sign strategy.
- A composition contract shares camera, palette references, target screen sizes, focal path, foreground occlusion limit, visual layers, negative space.
- Empty approved registry prevents treating unfinished or generated art as a production dependency.
- `scripts/Validate-GoldenArt.py` uses stock Python3 and is added to engine-free CI. It deliberately fails if an unverified brief claims approval, palette/scale/style drift, missing anchoring or weakened scorecard threshold.

## Gates and limitations
Tests: Python static validator + existing engine-free CI/CodeQL on exact head required.
Visual evidence: **NOT RUN**. No concept render, rig, PSB, AnimationClip, Unity imported asset or BeautyCell created.
Save impact: None.
Approval: **No Golden asset approved.** Selected candidate must score 15/16 with no zero AND receive independent approval after technical import and visual check.
Known limitations: Provisional numbers are art-direction decisions, not measured production results. Generated candidate prompts and production art are M13-T02+ follow-ups. CI only establishes contract consistency; Unity screenshot/phone/device acceptance deferred.

Files: `Assets/StartupLife/Art/_ArtDirection/art-spec.yaml`,
`composition.yaml`, `golden-targets.json`, `Art/Approved/registry.json`,
`docs/ART_BIBLE_M13.md`, `scripts/Validate-GoldenArt.py`,
`.github/workflows/engine-free-ci.yml`, this evidence.
