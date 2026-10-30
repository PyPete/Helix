---
phase: 01-agent-safety-boundary
plan: "05"
status: complete
---

# Phase 01 Plan 01-05 Summary

## Delivered

- Replaced the renderer-side Agent client with typed native invocations for session status, logout/revocation, command proposal/policy/dispatch/evidence, stable operation status, bounded committed-evidence cursor reads, and experiment start/status/control.
- Validates UUIDs, canonical capability names, bounded JSON and cursor inputs before reaching native code. Mutations retain their caller-generated operation ID and return `Held` when acknowledgement is unknown; none is retried.
- Extended the fixed loopback native broker allowlist with session, experiment and cursor operations. It accepts only the main window, fixed `127.0.0.1:5080`, no proxy/redirects, bounded request/response/session-pipe data, and never serializes the opaque token.
- Removed arbitrary connection configuration. The native configuration commands now return a fixed-target migration diagnostic only; the UI ignores legacy remote settings and displays redacted verified-session identity, expiry and restoration failures.
- Migrated `useExperiment` from renderer `fetch` calls to native operations. It cancels stale selections/unmount work, keeps completed/uncertain executions selected, refreshes authoritative read status only, and surfaces unknown acknowledgements without redispatch.

## Client and Caller Inventory

| Surface | Native operation(s) | Safety behavior |
|---|---|---|
| `ConnectionConfig` | `agent_session_status`, `bootstrap_agent_session` | Fixed target, no credential display/storage, visible restoration error/expiry. |
| `useExperiment` | `agent_experiment_start`, `agent_experiment_status`, `agent_experiment_control` | Stable operation identity, safe status refresh only, cancellation/sequence guard, no mutation retry. |
| `DeviceManualControl` | Existing `executeApprovedSetFlow` command sequence | Unknown acknowledgement becomes held evidence with the stable command ID. |
| Future Plan 12 callers | health/discovery/event subscriptions | Explicitly deferred by this plan. |

## Verification

- `npm --prefix helix-desktop test -- --run src/lib/agentClient.test.ts src/components/ConnectionConfig.test.tsx` — 13 passed.
- `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` — 5 passed.
- `npm --prefix helix-desktop run build` — passed (Vite emitted the pre-existing large-chunk warning only).

## Notes

- The full desktop test suite still has unrelated failures in device manual-control and workflow-builder suites. The stale connection-configuration suite was updated and now passes its fixed-target/session cases.
