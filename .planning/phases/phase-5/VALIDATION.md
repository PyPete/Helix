---
phase: phase-5
type: validation
requirements:
  - C1
  - C7
  - C8
nyquist_coverage: true
---

# Phase 5 — VALIDATION.md

**Phase:** Tauri Desktop App — Foundation  
**Generated:** 2026-05-28

---

## Requirement Coverage

### C1 — Tauri v2 project at helix-desktop/ targeting Windows 11 + Linux

| Test | Type | Command | Pass Condition |
|------|------|---------|----------------|
| Scaffold exists | structural | `Test-Path helix-desktop/package.json` | exits 0 |
| package.json has tauri dep | unit | `grep "@tauri-apps/api" helix-desktop/package.json` | match found |
| TypeScript compiles (strict) | build | `cd helix-desktop && npm run build` | exit 0, 0 errors |
| Rust compiles | build | `cd helix-desktop/src-tauri && cargo check` | exit 0 |
| CI job exists | structural | `grep "tauri-build" .github/workflows/ci.yml` | match found |

**Wave 0 test files:** none (structural checks only)  
**Wave 1 tests:** `helix-desktop/src/hooks/useAgentHealth.test.ts`, `helix-desktop/src/components/*.test.tsx`

---

### C7 — Agent connection configuration screen (host, port, gRPC/REST toggle)

| Test | Type | Command | Pass Condition |
|------|------|---------|----------------|
| ConnectionConfig renders | unit | `npm run test -- --run components/ConnectionConfig` | all pass |
| Host input renders with default | unit | ConnectionConfig.test.tsx | default `localhost` in input |
| Port input renders with default | unit | ConnectionConfig.test.tsx | default `5080` in input |
| Validation error on invalid port | unit | ConnectionConfig.test.tsx | error shown for port=0 |
| onSave called with valid config | unit | ConnectionConfig.test.tsx | callback received updated connection |
| Config persists across sessions | integration | plugin-store mock | store.set called with agentConnection |

**Wave 0 test file:** `helix-desktop/src/components/ConnectionConfig.test.tsx`

---

### C8 — Device registry view: list of adapters, connection status, health check results

| Test | Type | Command | Pass Condition |
|------|------|---------|----------------|
| DeviceRegistry renders adapters | unit | `npm run test -- --run components/DeviceRegistry` | all pass |
| Healthy adapter shown | unit | DeviceRegistry.test.tsx | "Healthy" text in rendered output |
| Unhealthy adapter shown | unit | DeviceRegistry.test.tsx | "Unhealthy" text in rendered output |
| Error state on fetch failure | unit | DeviceRegistry.test.tsx | error message in role="alert" element |
| Network error handled | unit | DeviceRegistry.test.tsx | no unhandled rejection |
| Standalone mode (no SiteServer ref) | structural | `grep -r "SiteServer" helix-desktop/src/` | zero matches |
| useAgentHealth polls /health | unit | `npm run test -- --run hooks/` | all pass |
| Poll timeout set to 3s | unit | useAgentHealth.test.ts | AbortSignal.timeout(3000) called |

**Wave 0 test file:** `helix-desktop/src/components/DeviceRegistry.test.tsx`, `helix-desktop/src/hooks/useAgentHealth.test.ts`

---

## Full Test Run Command

```bash
cd helix-desktop
npm run test -- --run
```

Expected: all tests pass, 0 failures, 0 skips.

---

## CI Validation

The `tauri-build` job in `.github/workflows/ci.yml` validates C1 end-to-end on Linux:
- `npm run tauri build -- --bundles appimage,deb` must exit 0
- Artifact `helix-desktop-linux` must be uploaded

---

## Known Gaps

- **gRPC device registry** (full C8): Phase 5 uses REST `GET /health` only. The `DeviceAdapter.HealthCheck` and `GetCapabilities` gRPC RPCs are not called — Agent has no gRPC services wired (`Agent/gRPC/Server/` is empty). This will be addressed in Phase 6.
- **Windows dev verification**: `npm run tauri dev` on Windows 11 cannot be run in CI (no Windows runner in tauri-build job). This is a manual checkpoint in the human-verify step of Phase 5 execution.
