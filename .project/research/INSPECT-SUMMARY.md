# Inspect Summary — Helix

Repo root: C:\source\Github\Helix\Helix
Scanned: 2026-10-25
Revision: `1d2458bdb0756a8f4f8df304587e2d39a8b976ec` on `dev`, 336 commits ahead of `origin/main`
State at close: `inspect/done` (validated)

This is the phase handoff summary. The evidence lives in [evidence-codebase.md](evidence-codebase.md) and [DOCS-AUDIT.md](DOCS-AUDIT.md); this file adds nothing that is not derived from them.

## What this project is

**Helix** is a lab-automation orchestration platform: a WSL workflow DSL compiled to a node graph, executed by an agent attached to instruments, streamed as trends, observed from a desktop console or dashboard, with a recorder capturing runs.

- **Stack**: .NET 10 (`net10.0`) — 3 services (Agent, SiteServer, Recorder) + 5 libraries, 6 test projects; `helix-desktop` React 18 + Tauri 2; `helix-dashboard` React 19 + Vite 8; Docusaurus 3 site; Postgres 16, MinIO, Mosquitto 2.
- **Shape**: split brain. A working in-memory BFS execution path exists, but the registered Cleipnir "durable" executor is unreachable, and the UI-facing submit path (SiteServer → gRPC) creates runspaces without starting them.
- **Entry points**: three minimal-API hosts plus Tauri/Vite front ends; endpoint maps live in `Api/*Endpoints.cs`.
- **Maturity**: uneven. Real test surface (592 .NET `[Fact]/[Theory]`, 116 desktop, 32 dashboard) but large scaffolding areas: no callers for `IWorkflowExecutor`, stub Tauri backend, 644-line hardcoded device registry, vacuous integration tests without Docker.

## What demonstrably works vs. what is claimed

- 92 of 171 doc claims verified; 71 are stale, 1 aspirational, 7 unverifiable — all 236 inventory paths accounted for, 79 remediation rows queued.
- Three independent "start an experiment" surfaces are **all inert for different reasons**: SiteServer gRPC submit never calls `StartAsync`; the desktop ribbon posts to nonexistent `/api/runspaces/*` routes; the Agent's own `POST /experiments` starts an empty stub definition.
- The safety layer is the highest-consequence gap: interlocks are implemented, `SafetyGuard` nodes are compiled in, and nothing arms or monitors them.
- MQTT trend grammars are mutually incompatible (4-segment Agent vs 7-segment SiteServer/Recorder), and the scalar trend producer has no production caller.
- Worst docs drift: `docs-site/docs/operator/deployment-setup.md` — ports, credentials, and config all wrong (doc: 5000/7020/9000-9001; shipped: 5080/5090/19000-19002).
- The working tree is genuinely clean at HEAD; an MSYS2 git on `PATH` manufactures phantom line-ending churn. Only `.project/` is untracked.

## Open questions define must settle

1. Which engine is canonical (in-memory BFS vs Cleipnir)
2. Whether SiteServer is the submit authority
3. Whether desktop is a shipping surface
4. The MQTT topic contract (4-part vs 7-part)
5. Whether safety is in this milestone
6. Whether experiments must persist
7. Which `.planning/` artifact is authoritative
8. The supported run mode (compose-grpc vs local-sim)
9. Whether device support becomes configurable
10. What counts as test evidence

## Artifacts

- [evidence-codebase.md](C:\source\Github\Helix\Helix\.project\research\evidence-codebase.md) — Map, 26 findings, apparent intent, 10 define questions, blocked areas.
- [DOCS-AUDIT.md](C:\source\Github\Helix\Helix\.project\research\DOCS-AUDIT.md) — 236 paths, 171 claims, 79-row remediation queue.

## Blockers carried into define

- **No verify sidecar was possible.** `isolation.py isolate-verify` refused because no canonical milestone branch was bound (`primary branch must be canonical gsd-path/M00N, found dev`). Every claim about compilation, test results, and runtime behaviour is therefore `unverifiable` and static-only.
- **`origin/main` is 336 commits behind `dev`.** The router wants `bind-initial`, which expects HEAD equal to `origin/main` (`2c74d65`). Reconciling that is a define/router decision, not an inspect defect.

**Define is next.**
