---
phase: 01-agent-safety-boundary
plan: "01"
status: complete
---

# Phase 01 Plan 01-01 Summary

## Delivered

- A real desktop Set Flow action traverses the native broker, authenticated Agent admission path, PostgreSQL command ledger, and simulator.
- The main-window ACL now grants precisely the native broker operations used by the approved Set Flow path: propose, policy approval, dispatch, and durable evidence read, as well as bootstrap/status.
- The tracer scopes UI Automation to the `npm tauri dev` process tree it started and tears down that entire tree. It cannot click a title-matched stale Tauri window carrying a dead Agent session.
- The tracer verifies the allowed `SetFlow` evidence record (seven checks), verified actor, single immutable evidence row, and no duplicate evidence after Agent restart.

## Root-cause correction

The native `allow-agent-broker` ACL granted only `bootstrap_agent_session` and `agent_status`. Tauri correctly denied the renderer's `agent_propose` invocation before WinHTTP was reached, leaving PostgreSQL empty; session-ready gating could not expose that missing authority. The repaired ACL allows exactly the five remaining broker operations used by the Set Flow sequence, and only from the existing `main` window capability. The native transport now explicitly adds its authenticated headers with WinHTTP's required add flag. The tracer also fixes a separate repeat-run defect: `Stop-Process` terminated only npm, leaving Cargo/Vite/the native child alive and allowing a later run to select an old title-matched window with a dead Agent session.

## Verification

- `powershell -NoProfile -File scripts/verification/phase01-smoke.ps1 -Scenario Tracer` — passed (real evidence command `474eac0b-19dc-4f6e-9a55-c18cfdb2a17f`).
- `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` — 3 passed.
- `npm run build` in `helix-desktop` — passed.
- `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~CommandAdmissionTests --verbosity minimal` — 4 passed.
- `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore --filter FullyQualifiedName~AgentAuthenticationTests --verbosity minimal` — 3 passed.
