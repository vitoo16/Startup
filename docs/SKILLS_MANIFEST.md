# STARTUP LIFE — SKILLS MANIFEST

## Tier 1 — Official Unity Skills
Source:
https://github.com/Unity-Technologies/skills

Install:
```bash
npx skills add Unity-Technologies/skills
```

Keep available:
- new-unity-project
- unity-cli
- unity-package-management
- ui
- ui-ugui
- localization
- sprite-editor
- relevant build/test skills from the installed collection

Policy:
official skill wins when an official and community skill conflict on Unity-specific behavior.

---

## Tier 2 — Game production pack
Source:
https://github.com/tea-x-random/unity-game-skills

This is community-maintained. Review scripts and pin a known-good revision.

Recommended set:
- unity-game-director
- unity-mcp-bridge
- unity-mcp-skill
- unity-project-setup
- unity-gameplay-systems
- unity-game-economy
- unity-art-direction
- unity-asset-designer
- unity-asset-pipeline
- unity-scene-composition
- unity-animation
- unity-ui-designer
- unity-debug-profiler
- unity-qa-release
- unity-localization
- unity-image-generator (when generating art)
- unity-audio-generator (later)

OpenAI/Codex-style install according to the upstream project:
```bash
git clone https://github.com/tea-x-random/unity-game-skills.git
cd unity-game-skills
python3 scripts/install_skills.py --platform openai --force
python3 scripts/validate_repository.py
```

Unity MCP dependency in the upstream workflow:
`com.coplaydev.unity-mcp`

Security:
- review community scripts before granting tool access;
- pin revision;
- never commit API keys;
- keep generation API keys in environment variables.

---

## Tier 3 — AutoSkills supplement
Source:
https://www.autoskills.sh/

Check:
```bash
npx autoskills --dry-run
```

As of 2026-09-30 Unity is not listed as a supported technology entry.
Use AutoSkills only as a supplement for detected supporting technology.

---

## Tier 4 — Project-specific skills
Required from repository:
- startup-life-session-orchestrator
- startup-life-gameplay-guardian
- vietnam-art-direction
- startup-life-asset-quality-gate

These project-specific skills are mandatory when their trigger matches even if a generic Unity skill is also active.
