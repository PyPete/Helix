# Plan 01-06 Summary: Desktop safety boundary

## Delivered

- `ExecutionPanel` submits the original nonempty WSL with a scientist-approved, frozen simulation-policy revision and stable operation/execution/correlation IDs. It renders all seven runtime admission checks, keeps missing evidence explicitly `Unknown`, blocks retries after incomplete evidence, and provides separate acknowledged Pause, Hold, and confirmed Abort requests.
- `DeviceManualControl` now offers all simulator capabilities as typed Agent proposals, validates bounded manual values, and shows only durable evidence as completed. Held, rejected, unavailable-session, and uncertain outcomes do not trigger retries.
- `AgentSession` is mounted in `HelixShell`, exposes only the native verified scientist identity, and provides safe restore/sign-out paths without renderer token access. The shell retains the selected execution for timeline access after terminal events and removes direct browser `fetch` lifecycle calls.
- Native execution-control broker calls use the Agent `/executions/{executionId}/control/{state}` boundary and return the durable acknowledgement state/revision to the renderer.

## Verification

- `npm --prefix helix-desktop test -- --run src/components/ExecutionPanel/ExecutionPanel.test.tsx src/components/DeviceManualControl/DeviceManualControl.test.tsx src/components/AgentSession/AgentSession.test.tsx src/shell/HelixShell.test.tsx` — 14 passed.
- `npm --prefix helix-desktop run build` — passed.
- `cargo check` in `helix-desktop/src-tauri` — passed.

## Scope note

The full desktop Vitest suite still contains pre-existing tests for the superseded direct-fetch Device Explorer/Workflow Builder paths. Those tests fail because preserved, uncommitted native-client migration changes intentionally remove that direct browser transport; they are outside Plan 01-06's execution/manual-control/session task files.
