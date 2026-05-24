# Documentation

This folder contains project documentation for Helix.Orchestration.

## Files

### Architecture & Concepts
- [`CONCEPT.md`](CONCEPT.md) — Project concept, architecture principles, two-tier design (Agent / Site Server), execution and data-flow diagrams, technology stack, and roadmap.
- [`HANDOVER.md`](HANDOVER.md) — Full infrastructure handover: architecture direction, technology choices, WSL/Runspace/Campaign design decisions, and captured learnings.
- [`CI.md`](CI.md) — CI/CD workflow behavior, branch model (`dev`/`stage`/`main`), and recommended GitHub branch protection checks.

### Phase Handovers
- [`PHASE1-CORE-DOMAIN-HANDOVER.md`](PHASE1-CORE-DOMAIN-HANDOVER.md) — Phase 1 complete. Covers: stratified trend model, MS/MS and NMR spectral analytics, device leasing, WSL authoring + RunspaceDefinition, Runspace abstraction, TestContext. 56 tests passing.
- [`PHASE2-AGENT-RUNTIME-HANDOVER.md`](PHASE2-AGENT-RUNTIME-HANDOVER.md) — Phase 2 foundation. Covers: contracts protos, agent scaffolding, adapter discovery/proxy layer, runspace manager, streaming skeleton, control-plane scaffolding, and Python pump adapter assets.
- [`PHASE3-EXECUTION-PATH-HANDOVER.md`](PHASE3-EXECUTION-PATH-HANDOVER.md) — Phase 3 completion. Covers: WSL compiler path closure, adapter capability routing, gRPC host wiring, lease expiry background cleanup, and remaining production gaps.

## Notes
- Keep long-form project decisions and handovers here.
- Add new phase handover docs as `PHASE{N}-{TOPIC}-HANDOVER.md` and update this index.
- Add new architecture decision records as `ADR-{NNN}-{topic}.md`.
