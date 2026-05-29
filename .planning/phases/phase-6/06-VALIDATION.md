---
phase: 6
phase_slug: phase-6
date: 2026-05-29
---

# Phase 6 Validation Strategy

## Scope
Validates the Agent REST/WebSocket API, WslCompiler completions, Rete.js workflow builder, execution panel, live trend charts, safety panel, and workflow library.

## Validation Architecture

### Unit tests (Vitest)
- WorkflowBuilder: node creation, edge wiring, WSL JSON export
- ExecutionPanel: compile-then-start flow, pause/resume/abort, RunspaceEvent display
- TrendCharts: renders data points, handles empty data
- SafetyPanel: renders interlock rules, shows triggered/safe state
- workflowLibrary: save/load/list/duplicate round-trip with Tauri fs mock

### Integration tests (.NET)
- WslCompiler: compile valid document returns RunspaceDefinition; invalid returns diagnostics
- WslCompiler loop expansion: loop nodes produce N repeated step sequences
- WslCompiler safety injection: guard nodes injected when ISafetyEngine returns interlocks
- REST endpoints: POST /workflows/compile, POST /runspaces, GET /runspaces/{id}, GET /devices

### Acceptance (manual)
- Drag two nodes onto canvas, wire them, export WSL JSON, submit to Agent — GET /runspaces/{id} returns Running
- WebSocket /ws delivers at least one RunspaceEvent and one TrendValue during live execution
- Live scalar trend chart updates during execution
- Safety panel shows at least one interlock rule with current value and state
- Workflow save/load round-trip: save, reload app, re-open — all nodes and edges present

## Dimension Coverage
| Dimension | Coverage |
|-----------|---------|
| D1 Unit tests | Vitest component tests in each plan |
| D2 Integration | dotnet test WslCompiler + REST endpoint tests |
| D3 E2E | Manual acceptance tests above |
| D4 Build | npm run build + dotnet build in every plan verify step |
| D5 Regression | Full suite npm run test -- --run in 06-08 |
| D6 Type safety | TypeScript strict mode; no `any` in new files |
| D7 API contract | must_haves truths cross-reference C# endpoint types to TypeScript fetch calls |
| D8 Nyquist | All 7 requirements (C2-C6, C9, D4) covered by at least one plan's must_haves |
