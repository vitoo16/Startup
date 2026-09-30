# ADR-001 — Engine-independent simulation and assembly boundaries

Status: implementation contract proposed under M1-T01; dependency gate pending. Date: 2026-09-30.

Context: Work, study, salary, and restore must run without a loaded Unity scene. The approved plan specifies nine assemblies.

Decision: Core owns plain types, immutable definitions, results, and ports; Simulation references Core; Application references Core and Simulation. Content and Infrastructure each reference Core. Presentation composes Application, Content, Infrastructure, and Core. Editor and test assemblies depend inward and never ship as player dependencies. No runtime assembly references Editor/tests; Core/Simulation/Application disallow engine references.

Consequence: ScriptableObjects convert to immutable Core definitions at startup. Infrastructure implements Core save ports, avoiding an Application/Infrastructure cycle. Presentation cannot write simulation state. Compiler-enforced asmdefs and a scene-free fixture prove the boundary in M1-T02.

Verification: The [contract dependency table](../architecture/FIRST_PLAYABLE_CONTRACTS.md) is acyclic by review. Runtime asmdefs/Unity compilation remain unverified. See [implementation plan](../IMPLEMENTATION_PLAN_MVP.md).
