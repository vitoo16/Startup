# M10-T02 — Business readiness & Coffee Kiosk mobile feedback

**Development-first; not milestone acceptance.** Owner GPT-6 Technical Director.  
**Parent:** main `55ca13bc3ad4a19f144d19b1bdb49d9fb5f7e4a7`.

## Implementation
- New detached `BusinessOperationsProjection` reads committed portfolio, owner-day allocation and authored launch eligibility under **one GameSession revision lock**. No simulation mutation, duplicate economics or receipt changes.
- Launch buttons reflect source-authoritative refusal conditions: missing character, same active business type, portfolio cap, personal arrears, insufficient wallet cash, job vs full-time owner-time collision and unsupported manager mode.
- Owned-business view presents owner-time allocation/capacity plus fixed helper wage as **already included** in mandatory costs. Paused means not operational even if time projection is otherwise available.
- Unity uGUI readouts remain inside the existing ScrollRect. No hand-edited scene/prefab YAML, no flattened character motion substituted as gameplay.
- One shared `BusinessPortfolioRules.MaxActiveBusinesses` in current simulation and read model; frozen historical V2 engine untouched.

## Gates & deferred work
- **Skills:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-mcp-bridge, unity-game-economy, unity-qa-release, ui, ui-ugui, unity-ui-designer, vietnam-art-direction.
- **Tests:** source engine-free EconomicChecks added; Unity PlayMode/UI/device tests deferred to final QA. Record exact-head CI result once available.
- **Visual:** NOT RUN — phone scroll/notch, font and overlap require final Unity visual gate.
- **Save impact:** None. No new economic transactions, save fields, migrations, or replay semantics.
- **Known limitations:** Launch options are *advisory* until committed CAS; owner-time capacity is not a wallet-funded sales forecast. Final localization table for explanatory labels remains M12 work; M10-T02 visual/animation and insolvency UX are not complete.
- **Files:** BusinessOperationsProjection.cs (+ .meta), Core/Business.cs, SimulationEngine.cs (current only), GameSession.cs, StartupLifeBootstrapper.cs, LifeScreenViewController.cs, BusinessManagementOverlay.cs, tools/EconomicChecks/Program.cs, this document.
