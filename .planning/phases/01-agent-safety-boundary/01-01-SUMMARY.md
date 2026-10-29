# Plan 01-01 Execution Summary

## Status

**Partially implemented; tracer remains blocked.** Docker Desktop provisioned
the existing Compose PostgreSQL service and it reported healthy. The required
end-to-end tracer remains unavailable because its required Agent session,
durable ledger, native broker, actual-Tauri smoke script, and renderer evidence
path are not yet implemented.

## Work performed

- Started the existing `postgres` Compose service; it is healthy on the local
  mapped database port.
- Added immutable Core command intent/evidence/policy/control contracts and
  the Agent-only dispatcher/ledger/observation ports.
- Added a deterministic seven-check admission evaluator. A failed check rejects
  and an absent or unknown check holds; all check evidence is retained.
- Added four focused admission tests covering allowed SetFlow, failed
  capability, unknown health, and a missing lease state.
- Added PostgreSQL command-stream, intent, and evidence tables plus an atomic
  Dapper ledger implementation that serializes per-execution sequence allocation
  with immutable intent/evidence insertion. The migration was applied to the
  disposable Docker database and all three tables were confirmed present.

## Verification

Passed: `dotnet test tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj --no-restore --filter FullyQualifiedName~CommandAdmissionTests --verbosity minimal`
with 4 tests. `dotnet build Helix.Orchestration.Core/Helix.Orchestration.Core.csproj --no-restore --verbosity minimal` also passed.
`dotnet build Helix.Orchestration.Infrastructure/Helix.Orchestration.Infrastructure.csproj --no-restore --verbosity minimal` passed after adding the ledger.

Not run: mandatory `scripts/verification/phase01-smoke.ps1 -Scenario Tracer`
because the script does not exist yet. Native `agent_security` tests and Agent
authentication tests also do not exist yet. These gates are not claimed as
passing.

## Required unblock

Complete the remaining explicitly scoped 01-01 implementation: simulator-only
Agent composition, OS-paired session authentication, PostgreSQL ledger and
migration, dispatch/reconciliation/control endpoints, native WinHTTP broker and
ACL, renderer evidence/timeline path, and the real Tauri/PostgreSQL smoke.

## Agent integration update

- Registered fail-closed bearer-session middleware before Agent route mapping.
- Added opaque SHA-256-backed session validation and revocation, plus trusted actor propagation into authenticated command proposal and dispatch endpoints.
- Added an Agent-owned dispatcher that appends held admission evidence through the durable ledger rather than attempting an effect with incomplete fresh safety, lease, resource, budget, termination, and health context.
- `AgentAuthenticationTests`: 3 passed. Agent project build passed (existing dependency/platform warnings remain).

## Native broker update

- Added a Windows `PipeOptions.CurrentUserOnly` session-bootstrap worker. It issues an opaque 15-minute session only over the local named pipe.
- Added the main-window-scoped Tauri broker. It retains the token only in native `Mutex` state and uses WinHTTP FFI against fixed `127.0.0.1:5080` for the authenticated health request.
- Added explicit Tauri command permission, main-window capability scope, strict local CSP, and typed renderer IPC wrappers with no renderer credential.
- Verified `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` (2 passed) and Agent build (passed with existing warnings).

## Renderer evidence update

- Added a typed immutable command-evidence row to the timeline, including verified actor, command ID, committed state, ordered evidence sequence, and expandable admission checks.
- Timeline renderer tests: 6 passed. Desktop build passed.
- Added the required tracer script and ran it. It correctly failed because actual Tauri-webview automation and durable evidence retrieval are still absent; this gate is not claimed as passing.

## Native operation completion

- Added native main-window-only proposal, policy approval, dispatch, and evidence-read broker commands. The broker keeps its bearer token in Rust memory and only calls the fixed loopback Agent target.
- Added the Agent's simulation-only SetFlow dispatch path: seven passing checks and immutable PostgreSQL admission evidence are committed before the simulator mutation; unsupported capabilities remain rejected.
- Replaced direct renderer mutation with the brokered Set Flow UI flow and added a real-process tracer that starts the Agent and Tauri WebView, clicks its visible control using Windows UI Automation, and verifies PostgreSQL evidence before and after an Agent restart.

## Current verification

- Passed: Agent build; `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` (3 passed); focused `CommandAdmissionTests` (4 passed); desktop production build.
- The first tracer execution reached Docker provisioning but failed only because it was launched from `helix-desktop/` and the script then resolved the migration relative to that caller directory. The script now resolves the repository root from its own path. The corrected real-process tracer has not yet been rerun, so no passing tracer result is claimed here.
