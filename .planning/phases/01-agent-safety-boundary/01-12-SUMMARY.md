---
phase: 01-agent-safety-boundary
plan: "12"
status: complete
---

# Phase 01 Plan 01-12 Summary

## Delivered

- Replaced desktop health reads and the display-compatible `useAgentWebSocket` transport with authenticated native broker operations. Evidence polling retains its committed cursor, deduplicates by command identity and sequence, rejects stale/unmounted results, and reports unavailable/partial state explicitly.
- Added native, session-bound simulator discovery commands. The fixed loopback broker validates device identifiers before requesting discovery or capabilities and continues to prevent renderer-selected destinations.
- Migrated every discovery caller: `DeviceRegistry`, `DeviceExplorer`, `DevicePalette`, and `WorkflowBuilder`. They expose loading, failed-session, empty, retained-partial, and long-name-safe states while limiting authoring metadata to the simulator.
- Added native-client/component tests for health, cursor subscription, device/capability discovery, registry, explorer, palette, and workflow builder integration.

## Caller Inventory

| Surface | Native operation(s) | Unavailable behavior |
|---|---|---|
| `useAgentHealth` | `agent_status` | Unhealthy with the exact session/broker error |
| `useAgentWebSocket` | `agent_evidence_cursor` | Disconnected error; committed cursor and existing rows retained |
| `DeviceRegistry` | `agent_devices` | Alert plus last discovery, or loading/empty state |
| `DeviceExplorer` | `agent_devices` | Alert plus last discovery, or loading/empty state |
| `DevicePalette` | `agent_devices`, `agent_device_capabilities` | Explicit loading/error/empty/partial labels |
| `WorkflowBuilder` | `agent_devices` | Alert without a capability picker until discovery succeeds |

## Verification

- `npm --prefix helix-desktop test -- --run src/hooks/useAgentHealth.test.ts src/hooks/useAgentWebSocket.test.ts src/lib/agentClient.test.ts` — 17 passed.
- `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` — 6 passed.
- `npm --prefix helix-desktop run build` — passed; Vite emitted its existing large-chunk warning only.
