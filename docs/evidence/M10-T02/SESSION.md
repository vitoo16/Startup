# M10-T02 — Contracted Coffee Kiosk helper, development-first slice

**Owner:** GPT-6 Technical Director / implementation.  
**Base main:** `2b3d5c171c50d0fb28d859d99e6b7362a05c87d3`

## Implementation
- Author a reusable immutable `FixedBusinessHelperDefinition` with contractual daily wage, additional units, and efficiency permyriad.
- Coffee Kiosk authored contract: solo capacity 20; helper contribution 20 at 10000/10000 efficiency; unchanged total capacity 40.
- **Critical conservation:** Contracted wage 90,000 VND is ALREADY included in the kiosk's 150,000 VND mandatory daily charge. Remaining non-helper fixed cost 60,000 VND. Do not create a second payable or alter the existing V3 invoice/ledger/replay semantics.
- Expose fixed helper terms through `BusinessSnapshot` for later mobile UI/animation. Live simulation remains based on `FullCapacityUnits`, which is now transparently computed from solo and fixed-helper capacity. No new authoritative save fields.
- No manager automation, recruitment market, hourly scheduling, or asset claims.

## Session
- **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-game-economy, unity-mcp-bridge, unity-qa-release, ui, ui-ugui, unity-ui-designer, vietnam-art-direction, startup-life-asset-quality-gate, unity-animation.
- **Tests:** Engine-free CI, source compatibility checks and CodeQL required on exact head. No Unity runtime execution requested in this development-first phase.
- **Save impact:** No DTO migration; same V3 150k expense and 40-unit capacity, no historical replay changes; original frozen V2 source untouched.
- **Visual evidence:** NOT RUN (helper rig, work animation and kiosk scene remain M13–M14).
- **Known limitations:** Fixed helper is mandatory and preassigned; insolvency behavior remains based on current v3 contractual payment/capacity policy; final visual/kiosk QA and wider M10-T02 acceptance deferred.
- **Files changed:** EconomicDefinitions.cs; M9T02FunctionalEconomy.cs; Business.cs; GameSession.cs; EconomicChecks/Program.cs; this SESSION.md.
