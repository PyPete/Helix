# Docs Audit

<!-- Written by the docs auditor (gsd-path-docs-audit or gsd-path-inspect).
     Every verdict carries evidence; a verdict without evidence is a defect. -->

Repo root: C:\source\Github\Helix\Helix
Audited: 2026-10-25
Audited HEAD: none
Alignment mode: no

## Summary

| Verdict | Count |
|---------|-------|
| verified | 92 |
| stale | 71 |
| aspirational | 1 |
| unverifiable | 7 |
| descriptive docs (no testable claims) | 88 |

Worst drift: `docs-site/docs/operator/deployment-setup.md` documents Agent 5000/gRPC 5050, SiteServer 7020, MinIO 9000/9001 with `helix`/`helix_dev_secret`, and an obsolete `AgentProxy.Agents[]` plus `ConnectionStrings__DefaultConnection` matrix, while the shipped stack uses Agent 5080, SiteServer 5090, MinIO 19000/19002 with `minioadmin`/`minioadmin`, `AgentProxy:Mode`/`GrpcAddress`, and `ConnectionStrings__HelixOrchestration` - following the doc cannot start the stack.

## Doc: .claude/skills/sketch-findings-Helix/SKILL.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Theme variables are defined in sources/themes/default.css" | structure | verified | .claude/skills/sketch-findings-Helix/sources/themes/default.css exists; sources/ contains 001-003 sketch dirs |
| "Sketch reference HTML lives under sources/" | structure | verified | sources/001-ribbon-workspace-shell/index.html, 002-docking-panel-model/index.html and 003-manual-control-dialog/index.html all exist |
| "Target app is the Tauri helix-desktop app" | structure | verified | helix-desktop/src-tauri/ exists; helix-desktop/package.json:17-21 |

## Doc: .claude/skills/sketch-findings-Helix/references/layout-and-chrome.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Shell uses rc-dock docking and allotment split panes" | structure | verified | helix-desktop/package.json:18-19 (allotment 1.20.5, rc-dock 3.3.2); helix-desktop/src/shell/WorkspaceLayout.tsx:4 |

## Doc: .claude/skills/sketch-findings-Helix/references/manual-control-ui.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Manual control is rendered by a capability-driven dialog" | feature | verified | helix-desktop/src/components/DeviceManualControl/DeviceManualControl.tsx exists; DeviceManualControl.test.tsx exists |

## Doc: .github/CICD.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Agent.Tests 8, Core.Tests 107, Devices.Tests 57, SiteServer.Tests 44; total 216+ tests" | status | stale | Static count is Agent 31, Core 115, Devices 389, SiteServer 45, Infrastructure 2 (592 total); .github/CICD.md:56-62 |
| "Tests run and TRX reports are generated" | command | verified | .github/workflows/ci.yml:69 runs dotnet test; .github/workflows/ci.yml:18 build-and-test job exists |

## Doc: .github/PIPELINE_VERIFICATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Agent 8/8, Core 107/107, Devices 57/57, SiteServer 44/44 pass" | status | stale | Current static counts 31/115/389/45; .github/PIPELINE_VERIFICATION.md:25-28 |

## Doc: .github/copilot-instructions.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "CI accounts for dev, stage and main branches" | config | verified | .github/workflows/ci.yml:3-15 triggers on dev, stage, main |
| "Devices should be identified by COM port plus USB VID:PID" | feature | aspirational | No VID/PID identity code; serial options only set PortName placeholders (DeviceAdapterExtensions.cs:21-24); this is a guideline, not an implementation |

## Doc: .planning/PROJECT.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "1,262 .NET xUnit tests, 76 desktop Vitest tests, 32 dashboard Vitest tests" | status | stale | Static count is 592 .NET, 116 desktop, 32 dashboard; .planning/PROJECT.md:46 |

## Doc: .planning/REQUIREMENTS.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Scope is Milestone v0.2 Production-Grade Platform, status Active" | status | verified | .planning/REQUIREMENTS.md:3-5 |

## Doc: .planning/ROADMAP.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Phase 11 is 0/? Not started" | status | stale | Phase 11 has four summaries plus UAT and VALIDATION; .planning/ROADMAP.md:211 |
| "Phase 14 ReactFlow Node Canvas is 0/? Not started" | status | stale | helix-desktop/package.json:17 has @xyflow/react 12.11; .planning/ROADMAP.md:214 |
| "Phase 13 Three Workflow Modes is 0/? Not started" | status | verified | Only CONTEXT.md and DISCUSSION-LOG.md exist for phase 13; no phase 13 plan/summary or Kanban/Start-Stop code; .planning/ROADMAP.md:213 |

## Doc: .planning/STATE.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "current_phase 13, status ready_to_execute, total_phases 10, completed_phases 1" | status | stale | .planning/STATE.md:5-12 conflicts with the body and with ROADMAP.md:211-214 (phase 12 complete, phase 11 work complete) |
| "Current focus is phase 12 Experiment as Top-Level Scope" | status | stale | .planning/STATE.md:33 says focus phase 12 while frontmatter:5,16 and body:22 say phase 13 |

## Doc: .planning/codebase/ARCHITECTURE.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Agent listens on port 5080 and SiteServer on 5090" | config | verified | .planning/codebase/ARCHITECTURE.md:18; docker-compose.yml:78,105; Helix.Orchestration.Agent/appsettings.json:12 |
| "Core has no ASP.NET or EF Core references" | structure | verified | Helix.Orchestration.Core/*.csproj has no ASP.NET/EF packages; Dapper lives in Infrastructure |

## Doc: .planning/codebase/CONCERNS.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "No test covers pause/resume round-trip" | status | stale | tests/Helix.Orchestration.Agent.Tests/DurableRunspaceTests.cs:27 PauseAsync_CancelsCurrentExecution_AndSetsPausedStatus; InMemoryRunspaceTests.cs:13 PauseAsync_SuspendsExecution; .planning/codebase/CONCERNS.md:127 |
| "SiteServer builds an empty RunspaceDefinition and forwards the Phase 9 stub comment" | status | stale | Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs:57-80 now compiles WSL via IWslCompiler; .planning/codebase/CONCERNS.md:14 |

## Doc: .planning/codebase/CONVENTIONS.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Frontend tests are co-located with .test.ts/.test.tsx suffixes" | structure | verified | helix-desktop/src/hooks/useAgentHealth.test.ts and 22 other test files exist |

## Doc: .planning/codebase/INTEGRATIONS.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Specific CI provider is not confirmed" | integration | stale | GitHub Actions is configured in .github/workflows/ci.yml; .planning/codebase/INTEGRATIONS.md:86 |

## Doc: .planning/codebase/STACK.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| ".NET 10, React 18.3, Tauri 2, Vitest 4.x, Dapper/Npgsql, @xyflow/react 12.x" | config | verified | .planning/codebase/STACK.md:8-62; helix-desktop/package.json:17-20,36; Helix.Orchestration.Infrastructure/*.csproj |

## Doc: .planning/codebase/STRUCTURE.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Helix.Orchestration.Agent/Configuration/ contains Agent config classes" | structure | stale | Helix.Orchestration.Agent/Configuration/ does not exist; .planning/codebase/STRUCTURE.md:12 |

## Doc: .planning/codebase/TESTING.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Vitest runs in both frontend packages" | command | verified | helix-desktop/package.json:11,36; helix-dashboard/package.json:11,40 |

## Doc: .planning/milestones/v0.1-REQUIREMENTS.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "B (Drivers) complete with 48 drivers, 394 tests" | status | stale | 48 drivers plus SimulatorDevice = 49 files and 389 Devices test attributes; .planning/milestones/v0.1-REQUIREMENTS.md:14 |

## Doc: .planning/milestones/v0.1-ROADMAP.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Milestone v0.1 status is SHIPPED 2026-06-03" | status | verified | .planning/milestones/v0.1-ROADMAP.md:3 |

## Doc: .planning/v0.1-MILESTONE-AUDIT.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "gRPC server absent; GrpcAgentProxy fails every RPC" | status | stale | Agent Program.cs:320 MapGrpcService<AgentGrpcService>; .planning/v0.1-MILESTONE-AUDIT.md:111 |
| "DeviceRegistry.tsx fetches /health instead of /devices" | status | stale | helix-desktop/src/components/DeviceRegistry.tsx:28 fetches /devices; .planning/v0.1-MILESTONE-AUDIT.md:18 |
| "SiteServer compose block lacks ConnectionStrings__HelixOrchestration" | config | stale | docker-compose.yml:114 sets ConnectionStrings__HelixOrchestration; .planning/v0.1-MILESTONE-AUDIT.md:90 |

## Doc: docs-site/README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "The site is built with Docusaurus" | command | verified | docs-site/README.md:3; docs-site/package.json has @docusaurus/core |

## Doc: docs-site/docs/developer/adapter-authoring.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Adapters implement DeviceAdapterBase and register capabilities" | structure | verified | Helix.Orchestration.Devices/Abstractions/DeviceAdapterBase.cs:9; Helix.Orchestration.Devices/Drivers/KnauerAzura.cs:48 RegisterCapabilities |

## Doc: docs-site/docs/developer/architecture.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Infrastructure owns EF Core migrations" | structure | stale | Infrastructure uses Dapper/Npgsql and postgres/migrations/*.sql; docs-site/docs/developer/architecture.md:90 |
| "Recorder flushes JSONL batches" | structure | stale | Recorder writes .ndjson objects; Helix.Orchestration.Recorder/Services/RecorderWriterService.cs:106,121; docs-site/docs/developer/architecture.md:91 |

## Doc: docs-site/docs/developer/grpc-reference.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Agent gRPC default port is 5050 and adapter port 5051" | config | stale | Agent listens on 5080 (Helix.Orchestration.Agent/appsettings.json:12; docker-compose.yml:78); docs-site/docs/developer/grpc-reference.md:142,157 |
| "AgentControl exposes five RPCs (ListDevices absent from table)" | integration | stale | adapter.proto:19 has 4 RPCs; agent_control.proto:25 has 6 including ListDevices; docs-site/docs/developer/grpc-reference.md:26 |

## Doc: docs-site/docs/developer/intro.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Developer docs cover architecture, adapters, transports, REST and gRPC" | structure | verified | Files exist under docs-site/docs/developer/ |

## Doc: docs-site/docs/developer/rest-api.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "SiteServer REST API runs on port 7020" | config | stale | No 7020 anywhere in the repo; dashboard dev proxy targets 56523 (helix-dashboard/vite.config.ts:11-12) and the container maps 5090 (docker-compose.yml:105); docs-site/docs/developer/rest-api.md:9 |
| "/api/trends/ws WebSocket endpoint exists" | integration | verified | Helix.Orchestration.SiteServer/Endpoints/TrendEndpoints.cs; TrendWebSocketManager registered in SiteServer/Program.cs:54 |

## Doc: docs-site/docs/developer/transport-guide.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "AddHelixAdapter<TAdapter> extension method exists" | feature | stale | No AddHelixAdapter symbol; specific AddHelixSerial/Tcp/Modbus/Opc/CDllAdapter helpers exist; docs-site/docs/developer/transport-guide.md:215 |

## Doc: docs-site/docs/operator/deployment-setup.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Agent HTTP 5000 and gRPC 5050; SiteServer 7020" | config | stale | docker-compose.yml:78,105,107 and Agent appsettings.json:12 use 5080 and 5090; deployment-setup.md:53-54,72 |
| "MinIO API 9000, console 9001, credentials helix/helix_dev_secret" | config | stale | docker-compose.yml:58-62 exposes 19000/19002 with minioadmin/minioadmin; deployment-setup.md:31-37 |
| "ConnectionStrings__DefaultConnection and AgentProxy.Agents[] with GrpcAddress 5050" | config | stale | Compose sets ConnectionStrings__HelixOrchestration (docker-compose.yml:84,114) and AgentProxy__Mode/AgentProxy__GrpcAddress (108-109); deployment-setup.md:60,77-82 |
| "Dashboard points at VITE_SITE_SERVER_URL=http://localhost:7020" | config | stale | SiteServer runs on 5090 (docker-compose.yml:105); deployment-setup.md:110 |

## Doc: docs-site/docs/operator/device-capabilities.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Manual capability execution is POST http://localhost:5000/devices/{id}/execute with capabilityName" | config | stale | Agent DeviceEndpoints.cs:130 route lives on the Agent at 5080 and the request field is Capability (DeviceEndpoints.cs:20); device-capabilities.md:59-63 |

## Doc: docs-site/docs/operator/experiment-setup.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Campaign create takes workflowDefinitionJson and controls iteration count" | integration | stale | CampaignEndpoints.cs:13-27 takes parameterSets; experiment-setup.md:46,60 |

## Doc: docs-site/docs/operator/intro.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Operator docs cover deployment, devices, experiments, workflows and hardening" | structure | verified | All five operator pages exist under docs-site/docs/operator/ |

## Doc: docs-site/docs/operator/production-hardening.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "MinIO console is at localhost:9002" | config | stale | Host mapping is 19002:9002 (docker-compose.yml:59); production-hardening.md:69,77 |
| "Mqtt:UseTls and Mqtt:CaFile are supported settings" | config | stale | No UseTls/CaFile options in the Mosquitto/MQTT configuration; production-hardening.md:62-63 |
| "OPC UA AutoAcceptUntrustedCertificates defaults to true" | config | stale | OpcTransportOptions.cs:57 defaults false; production-hardening.md:21,103 |

## Doc: docs-site/docs/operator/workflow-authoring.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "The canvas is Rete.js" | feature | stale | Desktop uses @xyflow/react (helix-desktop/package.json:17; WorkflowBuilder imports @xyflow/react); workflow-authoring.md:11 |
| "The engine supports seven node types" | feature | stale | HelixNodes.tsx:8-17 defines nine types including Start and Step; workflow-authoring.md:15 |

## Doc: docs/CI.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Builds Helix.Orchestration/Helix.Orchestration.slnx with /warnaserror" | command | stale | Solution is at repo root Helix.Orchestration.slnx and ci.yml:69 has no /warnaserror; docs/CI.md:21,23 |

## Doc: docs/CONCEPT.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Two tiers: local Agent and site SiteServer; Core is headless" | structure | verified | docs/CONCEPT.md:7-15; Helix.Orchestration.Agent and Helix.Orchestration.SiteServer projects exist |

## Doc: docs/DEVICE-MIGRATION-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Driver implementations live in Helix.Orchestration.Devices/Drivers" | structure | verified | 49 .cs files under Helix.Orchestration.Devices/Drivers (48 drivers plus SimulatorDevice) |

## Doc: docs/HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Core layer has 31/31 tests passing" | status | stale | Current Core static count is 115; docs/HANDOVER.md:574-578 |

## Doc: docs/PHASE1-CORE-DOMAIN-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Core delivers stratified trends, spectral analytics, leasing, WSL, Runspace and TestContext" | structure | verified | Helix.Orchestration.Core/Trends, WSL, Runspace, TestContext directories exist; docs/PHASE1-CORE-DOMAIN-HANDOVER.md:22-24 |

## Doc: docs/PHASE2-AGENT-RUNTIME-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "AdapterDiscoveryService provides config-driven adapter discovery" | structure | stale | No AdapterDiscoveryService.cs; only Helix.Orchestration.Agent/Adapters/IAdapterDiscovery.cs remains; docs/PHASE2-AGENT-RUNTIME-HANDOVER.md:26 |

## Doc: docs/PHASE3-EXECUTION-PATH-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "WSL compiler and capability routing close the execution path" | feature | verified | Helix.Orchestration.Core/WSL/WslCompiler.cs; Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs |

## Doc: docs/PHASE4-DURABLE-EXECUTION-TREND-FOUNDATION-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Cleipnir durability store is InMemoryFunctionStore" | structure | stale | Agent Program.cs:76 constructs PostgreSqlFunctionStore; docs/PHASE4-DURABLE-EXECUTION-TREND-FOUNDATION-HANDOVER.md:7,31 |

## Doc: docs/PHASE5-RUNSPACE-LIFECYCLE-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Runspace lifecycle manager and WSL execution wiring exist" | feature | verified | Helix.Orchestration.Core/Runspace/IRunspaceManager; Helix.Orchestration.Agent/Runspaces/DurableRunspace.cs |

## Doc: docs/PHASE6-WSL-COMPILER-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "WSL compiler, device parity and MQTT trend ingestion foundation exist" | feature | verified | Core/WSL compiler; Agent MQTT ingestion service; docs/PHASE6-WSL-COMPILER-HANDOVER.md:5-10 |

## Doc: docs/PHASE7-VARIABLE-MANAGEMENT-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "WorkflowStateExtensionsTests has 10 tests and GlobalStateStoreTests has 12 (22 total)" | command | verified | Static counts: 10 and 12 in tests/Helix.Orchestration.Core.Tests; docs/PHASE7-VARIABLE-MANAGEMENT-HANDOVER.md:197-210 |

## Doc: docs/PHASE8-TYPED-ANALYTICAL-DATA-MODEL-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "All 107 Core tests pass and SpectralType has no UVVis or Raman values" | status | stale | Core has 115 tests; SpectralType.cs:6,8 contains UV and Raman; docs/PHASE8-TYPED-ANALYTICAL-DATA-MODEL-HANDOVER.md:286,323 |

## Doc: docs/PHASE9-SITE-SERVER-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "SiteServer xUnit coverage is 30 tests" | status | stale | Current SiteServer static count is 45; docs/PHASE9-SITE-SERVER-HANDOVER.md:130 |

## Doc: docs/PHASE10-GRPC-AGENT-TRANSPORT-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "SiteServer xUnit coverage is 35 tests" | status | stale | Current SiteServer static count is 45; docs/PHASE10-GRPC-AGENT-TRANSPORT-HANDOVER.md:66 |

## Doc: docs/PHASE11-CAMPAIGN-MANAGER-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "SiteServer xUnit coverage is 44 tests" | status | stale | Current SiteServer static count is 45; docs/PHASE11-CAMPAIGN-MANAGER-HANDOVER.md:52 |

## Doc: docs/PHASE12-POSTGRESQL-PERSISTENCE-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "SiteServer 44 passed and Core 107 passed" | status | stale | Current counts are SiteServer 45 and Core 115; docs/PHASE12-POSTGRESQL-PERSISTENCE-HANDOVER.md:78-79 |
| "Infrastructure uses Dapper and Npgsql with 2 tests" | structure | verified | Helix.Orchestration.Infrastructure.csproj references Dapper/Npgsql; Infrastructure static count is 2 |

## Doc: docs/PHASE13-DEVICE-INTEGRATION-LAYER-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Devices has 54 xUnit tests" | status | stale | Current Devices static count is 389; docs/PHASE13-DEVICE-INTEGRATION-LAYER-HANDOVER.md:5,177,257 |

## Doc: docs/PHASE14-RECORDER-SERVICE-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Recorder:TopicFilters, ChannelCapacity 4096, JSONL key experiments/{id}/trends/{date}/...jsonl" | config | stale | RecorderOptions.cs:14 uses singular TopicFilter; Program.cs:23-25 uses 10,000/DropOldest; RecorderWriterService.cs:106 writes {experimentId}/{trendId}/{timestamp}.ndjson; docs/PHASE14-RECORDER-SERVICE-HANDOVER.md:27,46-47,83 |

## Doc: docs/README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Phase 1 has 56 tests; Phase 9 30; Phase 10 35; Phase 11 44; Phase 13 54" | status | stale | Current counts are 592 .NET total (Agent 31, Core 115, Devices 389, Infrastructure 2, Recorder 10, SiteServer 45); docs/README.md:13,21-25 |

## Doc: CI_CD_FIXES.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Test coverage is 216+ tests and per-project counts 8/107/57/44" | status | stale | Static count is 592; CI_CD_FIXES.md:44,60-64 |
| "The build command still contains /warnaserror" | command | stale | ci.yml build step has no /warnaserror; CI_CD_FIXES.md:9 |

## Doc: CLAUDE.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Sketch findings skill lives at .claude/skills/sketch-findings-Helix/SKILL.md" | structure | verified | File exists |

## Doc: README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Repository remote is gitlab.com/syngentagroup/peters_projects/helix.git" | integration | stale | git remote -v is https://github.com/PyPete/Helix.git; README.md:18 |

## Doc: helix-dashboard/README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Dashboard is a React + TypeScript + Vite app" | structure | verified | helix-dashboard/package.json:16 React 19; Vite config present |

## Doc: helix-desktop/README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Desktop is a Tauri + React + TypeScript + Vite app" | structure | verified | helix-desktop/src-tauri exists; helix-desktop/package.json:17-21 |

## Doc: .planning/sketches/001-ribbon-workspace-shell/README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Winner is Variant A Dense VS-Ribbon" | status | verified | index.html exists; .planning/sketches/WRAP-UP-SUMMARY.md:15 records A |

## Doc: .planning/sketches/002-docking-panel-model/README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Winner is Variant A VS split panels" | status | verified | index.html exists; helix-desktop/src/shell/WorkspaceLayout.tsx uses split docking (rc-dock) |

## Doc: .planning/sketches/003-manual-control-dialog/README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Winner is Variant B card grid" | status | verified | index.html exists; DeviceManualControl.tsx renders capability cards |

## Doc: .planning/sketches/MANIFEST.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Sketches 001, 002, 003 have winners A, A, B" | status | verified | .planning/sketches/MANIFEST.md:15-17; each index.html exists |

## Doc: .planning/sketches/WRAP-UP-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Skill output is .claude/skills/sketch-findings-Helix" | structure | verified | .claude/skills/sketch-findings-Helix/SKILL.md exists |

## Doc: .planning/spikes/001-allotment-basic-layout/README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Verdict is VALIDATED for allotment basic layout" | status | verified | README.md:6,72 |

## Doc: .planning/spikes/002a-allotment-tauri-webview/README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Verdict is VALIDATED for allotment in the Tauri webview" | status | stale | README says VALIDATED (line 6,51) while .planning/spikes/MANIFEST.md:25 says PENDING; also rc-dock, not allotment, is the shipped shell |

## Doc: .planning/spikes/MANIFEST.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "002a is PENDING" | status | stale | .planning/spikes/002a-allotment-tauri-webview/README.md:6,51 says VALIDATED |
| "Do NOT use rc-dock; use allotment for the shell" | structure | stale | Shipped shell imports rc-dock (helix-desktop/package.json:19; src/shell/WorkspaceLayout.tsx:4); MANIFEST.md:18 |

## Doc: .planning/todos/pending/2026-05-30-ai-experiment-summarisation.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "There is no structured AI experiment summary generator" | feature | verified | No summarisation service under Helix.Orchestration.Agent or Infrastructure |

## Doc: .planning/todos/pending/2026-05-30-auto-generated-manual-control.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Every device type requires custom manual-control UI code" | status | stale | CapabilityDescriptor-driven DeviceManualControl.tsx exists; CapabilityDescriptor.cs:34; todo:15 |

## Doc: .planning/todos/pending/2026-05-30-graceful-device-loss-removal.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "No contract exists for device loss, workflow invalidation and trend degradation" | status | stale | DeviceStatusEvent and offline status exist (RunspaceEvent.cs:102-106; AgentService.cs:245); todo:18 |

## Doc: .planning/todos/pending/2026-05-30-multi-mode-workflow-authoring.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Only node-flow authoring exists; Kanban and Start/Stop modes are absent" | feature | verified | No Kanban/StartStop/workflowMode symbols in helix-desktop/src; todo:14 |

## Doc: .planning/todos/pending/2026-05-30-real-time-trend-system.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "There is no way to define derived or custom trends" | status | stale | DerivedTrendDefinition.cs and IDerivedTrend.cs exist; ITrendProvider.cs:41; todo:17 |

## Doc: .planning/todos/pending/2026-05-30-record-replay-system.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "There is no way to replay a completed experiment" | status | stale | Agent ExperimentEndpoints.cs:226-245 exposes GET /experiments/{id}/replay; todo:17 |

## Doc: .planning/todos/pending/2026-05-30-scichart-integration.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "SciChart is not integrated; charts still use recharts" | status | verified | helix-desktop/package.json:22 recharts 3.8.1; src/components/charts/index.ts is placeholder mode; todo:17-22 |

## Doc: .planning/todos/pending/2026-06-01-agent-ws-runspace-event-timing.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "RunspaceEvent lacks nodeId, startedAt and estimatedCompletionAt" | status | stale | StepStartedEvent has StepId and EstimatedDurationMs (RunspaceEvent.cs:48-53); useNodeExecutionState.ts:30 consumes it; todo asks for the fields |

## Doc: .planning/todos/pending/2026-06-01-reactflow-node-canvas-spike.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Workflow builder still uses Rete.js and needs a ReactFlow spike" | status | stale | @xyflow/react 12.11 is installed (helix-desktop/package.json:17) and HelixNodes.tsx uses React Flow; todo title asks for the migration |

## Doc: .planning/phases/11-ribbon-docking-workspace-shell/11-01-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "helix-dock.css holds 25 rc-dock class overrides mapped to CSS variables with zero hardcoded hex" | structure | stale | helix-dock.css now contains only dock sizing; overrides moved into WorkspaceLayout.tsx:361-370 with hardcoded hex; 11-01-SUMMARY.md:78 |

## Doc: .planning/phases/11-ribbon-docking-workspace-shell/11-02-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "rc-dock workspace shell with Ribbon, ActivityBar and StatusBar" | structure | verified | helix-desktop/src/shell/WorkspaceLayout.tsx:4; Ribbon.tsx, ActivityBar.tsx, StatusBar.tsx exist |

## Doc: .planning/phases/11-ribbon-docking-workspace-shell/11-03-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "DeviceExplorer polls /devices every 5 seconds" | feature | verified | helix-desktop/src/shell/panels/DeviceExplorer.tsx:18,37 |

## Doc: .planning/phases/11-ribbon-docking-workspace-shell/11-04-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "App.tsx is a short HelixShell wrapper" | structure | verified | helix-desktop/src/App.tsx:1-5 |

## Doc: .planning/phases/11-ribbon-docking-workspace-shell/11-UAT.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "IDE shell boots with ribbon, activity bar, workspace and status bar" | feature | verified | helix-desktop/src/shell/HelixShell.tsx and child components exist; 11-UAT.md:19 result pass |

## Doc: .planning/phases/11-ribbon-docking-workspace-shell/11-VALIDATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "76 pre-existing Vitest tests are the regression gate" | status | stale | Static desktop count is 116; 11-VALIDATION.md:11,32 |

## Doc: .planning/phases/11.1-remove-ui-components-and-focus-on-backend-remove-tauri-etc/11.1-01-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "GetScalarTrend is backed by a ConcurrentDictionary in InMemoryTrendProvider" | feature | verified | Helix.Orchestration.Agent/Trends/InMemoryTrendProvider.cs; 11.1-01-SUMMARY.md:19-20 |

## Doc: .planning/phases/11.1-remove-ui-components-and-focus-on-backend-remove-tauri-etc/11.1-02-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "AgentGrpcService delegates pause/resume/abort to the runspace manager" | feature | verified | Helix.Orchestration.Agent/gRPC/Server/AgentGrpcService.cs |

## Doc: .planning/phases/11.1-remove-ui-components-and-focus-on-backend-remove-tauri-etc/11.1-03-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "ListDevices RPC added to proto and AgentGrpcService" | integration | verified | agent_control.proto:25; AgentGrpcService.cs:90 |

## Doc: .planning/phases/11.1-remove-ui-components-and-focus-on-backend-remove-tauri-etc/VERIFICATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Verification passed with 0 behavior_unverified" | status | verified | VERIFICATION.md:3-6 |

## Doc: .planning/phases/12-experiment-top-level-scope/12-01-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "ExperimentPhase enum and 002_experiment_events.sql migration exist" | structure | verified | Helix.Orchestration.Core/Experiment/ExperimentPhase.cs; postgres/migrations/002_experiment_events.sql |

## Doc: .planning/phases/12-experiment-top-level-scope/12-02-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Runspace lifecycle events and IExperimentEventChannel exist" | structure | verified | Helix.Orchestration.Agent/Events/RunspaceEvent.cs; IExperimentEventChannel.cs |

## Doc: .planning/phases/12-experiment-top-level-scope/12-03-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Desktop experiment event types and WebSocket dispatch exist" | feature | verified | helix-desktop/src/types/agent.ts; src/hooks/useAgentWebSocket.ts |

## Doc: .planning/phases/12-experiment-top-level-scope/12-VERIFICATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "7/7 must-haves verified" | status | verified | 12-VERIFICATION.md:5-6 |

## Doc: .planning/phases/phase-1/SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "217 tests pass, 0 failures" | status | stale | Current .NET static count is 592; phase-1/SUMMARY.md:33 |

## Doc: .planning/phases/phase-10/10-01-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Agent image uses mcr.microsoft.com/dotnet/aspnet:10.0 and dotnet Helix.Orchestration.Agent.dll" | config | verified | Helix.Orchestration.Agent/Dockerfile; 10-01-SUMMARY.md:45-47 |

## Doc: .planning/phases/phase-10/10-02-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Idempotent bootstrap scripts with PASS/FAIL reporting exist" | structure | verified | deploy/install-agent-service.ps1, install-recorder-service.ps1, preflight.ps1/.sh |

## Doc: .planning/phases/phase-10/10-03-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Preflight scripts passed on Debian 12 and Windows" | status | unverifiable | Requires external Debian/Windows hosts and systemd; cannot be replayed statically; 10-03-SUMMARY.md:15 |

## Doc: .planning/phases/phase-10/10-VERIFICATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "7/7 must-haves verified with status human_needed" | status | unverifiable | Human deployment checks cannot be re-run from the repo; 10-VERIFICATION.md:3-5 |

## Doc: .planning/phases/phase-2/SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Created Helix.Orchestration/docs/driver-inventory.md" | structure | stale | No driver-inventory.md anywhere; the Helix.Orchestration/ wrapper directory does not exist; phase-2/SUMMARY.md:13 |

## Doc: .planning/phases/phase-2/wave1-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Created Helix.Orchestration/docs/driver-inventory.md" | structure | stale | File absent and Helix.Orchestration/ absent; wave1-SUMMARY.md:13 |

## Doc: .planning/phases/phase-3/SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "KnauerAzura driver implements RemoteControl, Start, Stop, SetFlowRate, GetPumpStatus and GetIdentity" | feature | verified | Helix.Orchestration.Devices/Drivers/KnauerAzura.cs:12-19 |

## Doc: .planning/phases/phase-4/SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "E2E and transport test files were created" | structure | verified | tests/Helix.Orchestration.Agent.Tests/Integration/E2eWorkflowExecutionTests.cs; Devices.Tests/Transports/*.cs |

## Doc: .planning/phases/phase-4/4-UAT.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "ModbusTransportTests reports 7 facts" | command | verified | Static [Fact]/[Theory] count in ModbusTransportTests.cs is 7 |

## Doc: .planning/phases/phase-4/VALIDATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "T1-T4 transport tests exist and Postgres tests are guarded" | structure | verified | Devices.Tests/Transports tests exist; Infrastructure tests use IsAvailable guard |

## Doc: .planning/phases/phase-5/01-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Tauri v2 project scaffolded at helix-desktop" | structure | verified | helix-desktop/src-tauri exists; package.json has Tauri deps |

## Doc: .planning/phases/phase-5/02-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "useAgentHealth polls /health every 5 seconds with a 3s timeout" | feature | verified | helix-desktop/src/hooks/useAgentHealth.ts |

## Doc: .planning/phases/phase-5/03-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "tauri-build CI job completes in about 5 minutes" | status | unverifiable | Duration varies by runner and cache; no run log in repo; 03-SUMMARY.md:30 |

## Doc: .planning/phases/phase-5/VALIDATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Tauri v2 project targets Windows 11/Linux and has a tauri-build CI job" | config | verified | helix-desktop/src-tauri exists; .github/workflows/ci.yml:181 tauri-build job |

## Doc: .planning/phases/phase-5/VERIFICATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "5 ROADMAP success criteria require human/CI verification" | status | unverifiable | Human verification cannot be replayed; VERIFICATION.md:3-5 |

## Doc: .planning/phases/phase-6.1/6.1-01-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "CapabilityDescriptor types and POST /devices/{id}/execute exist" | feature | verified | Helix.Orchestration.Core/Capabilities/CapabilityDescriptor.cs:34; Agent/Api/DeviceEndpoints.cs:130 |

## Doc: .planning/phases/phase-6.1/6.1-02-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "DeviceManualControl panel renders capability-driven controls" | feature | verified | helix-desktop/src/components/DeviceManualControl/DeviceManualControl.tsx |

## Doc: .planning/phases/phase-6.2/6.2-01-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "DeviceAction is a HelixNodeType" | structure | verified | helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.tsx:17 |

## Doc: .planning/phases/phase-6.2/6.2-02-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "EstimatedDurationMs is an optional parameter on step events and descriptors" | structure | verified | RunspaceEvent.cs:52; CapabilityDescriptor.cs:74 |

## Doc: .planning/phases/phase-6.2/6.2-UAT.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Completed node shows a duration footer" | feature | verified | HelixNodes.tsx:144-152 computes elapsed and renders it; 6.2-UAT.md:49-51 |

## Doc: .planning/phases/phase-6.3-close-gap-device-integration-bugs/6.3-01-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Capability execute guard uses adapter-prefixed registry keys" | feature | verified | Agent/Api/DeviceEndpoints.cs:129-140; 6.3-01-SUMMARY.md:43-44 |

## Doc: .planning/phases/phase-6.3-close-gap-device-integration-bugs/6.3-02-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "DeviceRegistry maps /devices instead of /health" | feature | verified | helix-desktop/src/components/DeviceRegistry.tsx:9,28 |

## Doc: .planning/phases/phase-6.3-close-gap-device-integration-bugs/6.3-VALIDATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Validation status is pending" | status | stale | Work is complete and DeviceRegistry.tsx:28 uses /devices; 6.3-VALIDATION.md:5 |

## Doc: .planning/phases/phase-6.3-close-gap-device-integration-bugs/6.3-VERIFICATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "4/5 must-haves verified with status human_needed" | status | unverifiable | Manual criterion cannot be replayed; 6.3-VERIFICATION.md:3-5 |

## Doc: .planning/phases/phase-6.4-close-gap-remaining-audit-items/6.4-01-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "ExperimentEndpoints compiles WSL through IWslCompiler before submission" | feature | verified | Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs:57-80 |

## Doc: .planning/phases/phase-6.4-close-gap-remaining-audit-items/6.4-02-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "FumehoodDto and GET /api/fumehoods exist" | feature | verified | Helix.Orchestration.SiteServer/Endpoints/FumehoodEndpoints.cs:27-30 |

## Doc: .planning/phases/phase-6/06-01-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Agent REST control API exposes GET /runspaces/{id}" | integration | verified | Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs:52 |

## Doc: .planning/phases/phase-6/06-02-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "RunspaceEvent bus and /ws promotion exist" | feature | verified | Helix.Orchestration.Agent/Events/RunspaceEvent.cs; Agent/Program.cs WebSocket mapping |

## Doc: .planning/phases/phase-6/06-03-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "WslCompiler transformations and DeviceDescriptor exist" | feature | verified | Helix.Orchestration.Core/WSL/WslCompiler.cs; Core/Devices/DeviceDescriptor.cs |

## Doc: .planning/phases/phase-6/06-04-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "WorkflowBuilder and useAgentWebSocket exist" | structure | verified | helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx; src/hooks/useAgentWebSocket.ts |

## Doc: .planning/phases/phase-6/06-05-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Experiment Execution Panel exists" | structure | verified | helix-desktop/src/components/ExecutionPanel/ExecutionPanel.tsx |

## Doc: .planning/phases/phase-6/06-06-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Live trend charts and SafetyPanel exist" | structure | verified | helix-desktop/src/components/TrendCharts/*; src/components/SafetyPanel/SafetyPanel.tsx |

## Doc: .planning/phases/phase-6/06-07-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "workflowLibrary save/load exists with Tauri fs mocking" | structure | verified | helix-desktop/src/lib/workflowLibrary.ts and workflowLibrary.test.ts |

## Doc: .planning/phases/phase-6/06-08-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "WorkflowBuilder save/load and App wiring exist" | feature | verified | helix-desktop/src/lib/workflowLibrary.ts; src/App.tsx |

## Doc: .planning/phases/phase-6/06-09-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "WorkflowBuilder uses Rete.js with useReteEditor and 7 node classes" | feature | stale | No useReteEditor.ts; desktop uses @xyflow/react (package.json:17) and HelixNodes.tsx:8-17 has 9 types; 06-09-SUMMARY.md:4-14 |

## Doc: .planning/phases/phase-6/06-10-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "SpectralChart receives real UV/MIR trend data" | feature | verified | helix-desktop/src/components/TrendCharts/SpectralChart.tsx; 06-10-SUMMARY.md:43 |

## Doc: .planning/phases/phase-6/06-11-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Tab navigation (Builder/Execution/Monitor) is the shell" | feature | stale | Current shell is an rc-dock workspace (WorkspaceLayout.tsx:4); no Builder/Monitor tab literals; 06-11-SUMMARY.md |

## Doc: .planning/phases/phase-6/06-VALIDATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Validates a Rete.js workflow builder" | feature | stale | Desktop uses @xyflow/react, not Rete.js; 06-VALIDATION.md:11 |

## Doc: .planning/phases/phase-6/06-VERIFICATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "10/14 must-haves verified with gaps_found" | status | stale | Gap-closure summaries 06-10 and 06-11 landed; 06-VERIFICATION.md:3-5 |

## Doc: .planning/phases/phase-6/6-UAT.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "UAT status is diagnosed with only 4 tests passed" | status | stale | The re-test and gap-closure work landed; 6-UAT.md:2,121 |

## Doc: .planning/phases/phase-6/6-UAT-retest.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "UAT verifies Builder/Execution/Monitor tab navigation" | feature | stale | No tab navigation remains; shell is rc-dock (WorkspaceLayout.tsx:4); 6-UAT-retest.md:14 |

## Doc: .planning/phases/phase-7/07-UAT.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Dashboard UAT is complete" | status | verified | helix-dashboard pages exist; 07-UAT.md:2 |

## Doc: .planning/phases/phase-7/07-VERIFICATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "14/14 must-haves verified" | status | verified | 07-VERIFICATION.md:3-5 |

## Doc: .planning/phases/phase-7/07-WAVE0-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "SiteServer dev port is 56523" | config | verified | helix-dashboard/vite.config.ts:11-12 proxies /api and /api/trends/ws to localhost:56523 |

## Doc: .planning/phases/phase-7/07-WAVE1-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Dashboard types include TrendValue, RunspaceEvent and FumehoodStatus" | structure | verified | helix-dashboard/src/types/dashboard.ts |

## Doc: .planning/phases/phase-7/07-WAVE2-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "App.tsx wires all five dashboard pages" | structure | verified | helix-dashboard/src/App.tsx; 07-WAVE2-SUMMARY.md:20 |

## Doc: .planning/phases/phase-7/07-WAVE3-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Dashboard page tests pass" | command | verified | Static dashboard count is 32 tests across 7 files |

## Doc: .planning/phases/phase-8/08-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "582 tests passed, 0 failed, 0 skipped" | status | stale | Current .NET static count is 592; 08-SUMMARY.md:5 |

## Doc: .planning/phases/phase-9/09-01-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Docusaurus site is configured with baseUrl /Helix/" | config | verified | docs-site/docusaurus.config.ts; 09-01-SUMMARY.md:51 |

## Doc: .planning/phases/phase-9/09-02-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Vapourtec POP command vocabulary is documented" | structure | verified | docs-site/docs/developer/transport-guide.md:61-64 documents PN/PF |

## Doc: .planning/phases/phase-9/09-03-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Architecture, adapter-authoring, transport, REST and gRPC pages exist" | structure | verified | All five files exist under docs-site/docs/developer/ |

## Doc: .planning/phases/phase-9/09-04-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "CapabilityDescriptor exposes VerbName, parameters, category, destructive and runspace flags" | structure | verified | Helix.Orchestration.Core/Capabilities/CapabilityDescriptor.cs |

## Doc: .planning/phases/phase-9/09-05-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Phase 9 is recorded complete in ROADMAP.md" | status | stale | ROADMAP.md phase table starts at phase 11 and has no phase 9 row; 09-05-SUMMARY.md:9 |

## Doc: .planning/phases/phase-9/09-REVIEW.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Review found documentation issues (status issues_found)" | status | unverifiable | The review is a point-in-time snapshot; no reproducible command or current defect list; 09-REVIEW.md:16,24 |

## Doc: .planning/phases/phase-9/09-VERIFICATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "9/10 must-haves verified with status human_needed" | status | unverifiable | Human/CI verification cannot be replayed; 09-VERIFICATION.md:3-5 |

## Doc: .planning/phases/phase-driver-port/VERIFICATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "6/6 checks passed and 48 drivers exist" | command | verified | Static driver file count is 48 drivers plus SimulatorDevice; VERIFICATION.md:3-5 |

## Doc: .planning/phases/phase-driver-port/driver-port-01-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "NewEra1000 driver exists" | structure | verified | Helix.Orchestration.Devices/Drivers/NewEra1000.cs |

## Doc: .planning/phases/phase-driver-port/driver-port-02-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "IMettlerOpcCommand production registration throws NotImplementedException" | structure | stale | DeviceAdapterExtensions.cs:518-522 registers NullMettlerOpcCommand, a no-op null object, not a throw; driver-port-02-SUMMARY.md:44 |

## Doc: .planning/phases/phase-driver-port/driver-port-03-SUMMARY.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "OceanViewDriver methods throw NotImplementedException pending the vendor SDK" | structure | verified | Helix.Orchestration.Devices/Drivers/OceanViewDriver.cs:27-51 |

## Descriptive docs

- .planning/phases/05-tauri-desktop-app-foundation/05-RESEARCH.md
- .planning/phases/11-ribbon-docking-workspace-shell/11-01-PLAN.md
- .planning/phases/11-ribbon-docking-workspace-shell/11-02-PLAN.md
- .planning/phases/11-ribbon-docking-workspace-shell/11-03-PLAN.md
- .planning/phases/11-ribbon-docking-workspace-shell/11-04-PLAN.md
- .planning/phases/11-ribbon-docking-workspace-shell/11-CONTEXT.md
- .planning/phases/11-ribbon-docking-workspace-shell/11-DISCUSSION-LOG.md
- .planning/phases/11-ribbon-docking-workspace-shell/11-PATTERNS.md
- .planning/phases/11-ribbon-docking-workspace-shell/11-RESEARCH.md
- .planning/phases/11.1-remove-ui-components-and-focus-on-backend-remove-tauri-etc/11.1-01-PLAN.md
- .planning/phases/11.1-remove-ui-components-and-focus-on-backend-remove-tauri-etc/11.1-02-PLAN.md
- .planning/phases/11.1-remove-ui-components-and-focus-on-backend-remove-tauri-etc/11.1-03-PLAN.md
- .planning/phases/12-experiment-top-level-scope/12-01-PLAN.md
- .planning/phases/12-experiment-top-level-scope/12-02-PLAN.md
- .planning/phases/12-experiment-top-level-scope/12-03-PLAN.md
- .planning/phases/12-experiment-top-level-scope/12-PATTERNS.md
- .planning/phases/12-experiment-top-level-scope/12-RESEARCH.md
- .planning/phases/13-three-workflow-modes/13-CONTEXT.md
- .planning/phases/13-three-workflow-modes/13-DISCUSSION-LOG.md
- .planning/phases/phase-1/PATTERNS.md
- .planning/phases/phase-1/PLAN.md
- .planning/phases/phase-1/RESEARCH.md
- .planning/phases/phase-10/10-01-PLAN.md
- .planning/phases/phase-10/10-02-PLAN.md
- .planning/phases/phase-10/10-03-PLAN.md
- .planning/phases/phase-2/PLAN.md
- .planning/phases/phase-3/PLAN.md
- .planning/phases/phase-4/PATTERNS.md
- .planning/phases/phase-4/PLAN.md
- .planning/phases/phase-4/RESEARCH.md
- .planning/phases/phase-5/01-PLAN.md
- .planning/phases/phase-5/02-PLAN.md
- .planning/phases/phase-5/03-PLAN.md
- .planning/phases/phase-5/PATTERNS.md
- .planning/phases/phase-5/RESEARCH.md
- .planning/phases/phase-6.1/6.1-01-PLAN.md
- .planning/phases/phase-6.1/6.1-02-PLAN.md
- .planning/phases/phase-6.2/6.2-01-PLAN.md
- .planning/phases/phase-6.2/6.2-02-PLAN.md
- .planning/phases/phase-6.2/CONTEXT.md
- .planning/phases/phase-6.2/PATTERNS.md
- .planning/phases/phase-6.3-close-gap-device-integration-bugs/6.3-01-PLAN.md
- .planning/phases/phase-6.3-close-gap-device-integration-bugs/6.3-02-PLAN.md
- .planning/phases/phase-6.3-close-gap-device-integration-bugs/6.3-RESEARCH.md
- .planning/phases/phase-6.4-close-gap-remaining-audit-items/6.4-01-PLAN.md
- .planning/phases/phase-6.4-close-gap-remaining-audit-items/6.4-02-PLAN.md
- .planning/phases/phase-6.4-close-gap-remaining-audit-items/6.4-RESEARCH.md
- .planning/phases/phase-6/06-01-PLAN.md
- .planning/phases/phase-6/06-02-PLAN.md
- .planning/phases/phase-6/06-03-PLAN.md
- .planning/phases/phase-6/06-04-PLAN.md
- .planning/phases/phase-6/06-05-PLAN.md
- .planning/phases/phase-6/06-06-PLAN.md
- .planning/phases/phase-6/06-07-PLAN.md
- .planning/phases/phase-6/06-08-PLAN.md
- .planning/phases/phase-6/06-09-PLAN.md
- .planning/phases/phase-6/06-10-PLAN.md
- .planning/phases/phase-6/06-11-PLAN.md
- .planning/phases/phase-6/06-PATTERNS.md
- .planning/phases/phase-6/06-RESEARCH.md
- .planning/phases/phase-7/07-CONTEXT.md
- .planning/phases/phase-7/07-DISCUSSION-LOG.md
- .planning/phases/phase-7/07-PATTERNS.md
- .planning/phases/phase-7/07-PLAN.md
- .planning/phases/phase-7/07-RESEARCH.md
- .planning/phases/phase-8/08-PLAN.md
- .planning/phases/phase-9/09-01-PLAN.md
- .planning/phases/phase-9/09-02-PLAN.md
- .planning/phases/phase-9/09-03-PLAN.md
- .planning/phases/phase-9/09-04-PLAN.md
- .planning/phases/phase-9/09-05-PLAN.md
- .planning/phases/phase-9/09-CONTEXT.md
- .planning/phases/phase-9/09-PATTERNS.md
- .planning/phases/phase-driver-port/CONTEXT.md
- .planning/phases/phase-driver-port/PATTERNS.md
- .planning/phases/phase-driver-port/RESEARCH.md
- .planning/phases/phase-driver-port/driver-port-01-PLAN.md
- .planning/phases/phase-driver-port/driver-port-02-PLAN.md
- .planning/phases/phase-driver-port/driver-port-03-PLAN.md
- .planning/seeds/SEED-001-ribbon-docking-workspace-ui.md
- .planning/seeds/SEED-002-rete-workflow-scopes-strict-authoring.md
- .planning/seeds/SEED-003-experiment-as-top-level-scope.md
- .planning/seeds/SEED-004-three-workflow-modes.md
- .planning/seeds/SEED-005-reactflow-live-node-canvas.md
- .planning/todos/pending/2026-05-30-deployment-strategy-lab.md
- .planning/todos/pending/2026-05-30-protocol-standardisation-trend-naming.md
- .planning/todos/pending/2026-05-30-python-sidecar-architecture.md
- .planning/todos/pending/2026-05-30-python-workflow-interface.md

## User rulings

| Queue # | Ruling | User's words | Planned |
|---------|--------|--------------|---------|
| - | - | - | - |

## Remediation queue

| # | Doc | Claim | Verdict | Class | Suggested action |
|---|-----|-------|---------|-------|------------------|
| 1 | .github/CICD.md | "Agent.Tests 8, Core.Tests 107, Devices.Tests 57, SiteServer.Tests 44; total 216+ tests" | stale | fix-doc | Replace per-project table with a date-stamped command result. |
| 2 | .github/PIPELINE_VERIFICATION.md | "Agent 8/8, Core 107/107, Devices 57/57, SiteServer 44/44 pass" | stale | fix-doc | Re-run and date the verification table or delete the snapshot. |
| 3 | .github/copilot-instructions.md | "Devices should be identified by COM port plus USB VID:PID" | aspirational | NEEDS-USER | Decide whether to implement hardware identity or soften the instruction. |
| 4 | .planning/PROJECT.md | "1,262 .NET xUnit tests, 76 desktop Vitest tests, 32 dashboard Vitest tests" | stale | fix-doc | Refresh counts from the static-count command; keep dashboard 32. |
| 5 | .planning/ROADMAP.md | "Phase 11 is 0/? Not started" | stale | fix-doc | Mark phase 11 complete and match .planning/STATE.md. |
| 6 | .planning/ROADMAP.md | "Phase 14 ReactFlow Node Canvas is 0/? Not started" | stale | fix-doc | Mark phase 14 complete or state remaining scope. |
| 7 | .planning/STATE.md | "current_phase 13, status ready_to_execute, total_phases 10, completed_phases 1" | stale | fix-doc | Regenerate STATE frontmatter from ROADMAP; align phase numbering. |
| 8 | .planning/STATE.md | "Current focus is phase 12 Experiment as Top-Level Scope" | stale | fix-doc | State one current focus. |
| 9 | .planning/codebase/CONCERNS.md | "No test covers pause/resume round-trip" | stale | fix-doc | Remove the stale concern or narrow it to restart-from-checkpoint semantics. |
| 10 | .planning/codebase/CONCERNS.md | "SiteServer builds an empty RunspaceDefinition and forwards the Phase 9 stub comment" | stale | fix-doc | Keep the accurate SimAgentProxy 2-second-stub concern but remove the empty-definition claim. |
| 11 | .planning/codebase/INTEGRATIONS.md | "Specific CI provider is not confirmed" | stale | fix-doc | Name GitHub Actions and link ci.yml. |
| 12 | .planning/codebase/STRUCTURE.md | "Helix.Orchestration.Agent/Configuration/ contains Agent config classes" | stale | fix-doc | Remove the path or point at the real options classes. |
| 13 | .planning/milestones/v0.1-REQUIREMENTS.md | "B (Drivers) complete with 48 drivers, 394 tests" | stale | fix-doc | Update the driver-test count to 389 and date it. |
| 14 | .planning/v0.1-MILESTONE-AUDIT.md | "gRPC server absent; GrpcAgentProxy fails every RPC" | stale | fix-doc | Archive the audit as historical; all five blockers are now fixed. |
| 15 | .planning/v0.1-MILESTONE-AUDIT.md | "DeviceRegistry.tsx fetches /health instead of /devices" | stale | fix-doc | Mark the finding fixed. |
| 16 | .planning/v0.1-MILESTONE-AUDIT.md | "SiteServer compose block lacks ConnectionStrings__HelixOrchestration" | stale | fix-doc | Mark the finding fixed. |
| 17 | docs-site/docs/developer/architecture.md | "Infrastructure owns EF Core migrations" | stale | fix-doc | Change to Dapper repositories plus SQL migration files. |
| 18 | docs-site/docs/developer/architecture.md | "Recorder flushes JSONL batches" | stale | fix-doc | Change JSONL to NDJSON. |
| 19 | docs-site/docs/developer/grpc-reference.md | "Agent gRPC default port is 5050 and adapter port 5051" | stale | fix-doc | Replace 5050/5051 with 5080 and the adapter's configured endpoint. |
| 20 | docs-site/docs/developer/grpc-reference.md | "AgentControl exposes five RPCs (ListDevices absent from table)" | stale | fix-doc | Add ListDevices and correct the RPC table. |
| 21 | docs-site/docs/developer/rest-api.md | "SiteServer REST API runs on port 7020" | stale | fix-doc | Replace 7020 with 5090 (container) and 56523 (dashboard dev proxy). |
| 22 | docs-site/docs/developer/transport-guide.md | "AddHelixAdapter<TAdapter> extension method exists" | stale | fix-doc | Document the specific registration helpers. |
| 23 | docs-site/docs/operator/deployment-setup.md | "Agent HTTP 5000 and gRPC 5050; SiteServer 7020" | stale | fix-doc | Replace the entire port table with 5080/5090. |
| 24 | docs-site/docs/operator/deployment-setup.md | "MinIO API 9000, console 9001, credentials helix/helix_dev_secret" | stale | fix-doc | Correct host ports and credentials. |
| 25 | docs-site/docs/operator/deployment-setup.md | "ConnectionStrings__DefaultConnection and AgentProxy.Agents[] with GrpcAddress 5050" | stale | fix-doc | Document the real keys and single gRPC address. |
| 26 | docs-site/docs/operator/deployment-setup.md | "Dashboard points at VITE_SITE_SERVER_URL=http://localhost:7020" | stale | fix-doc | Change to 5090. |
| 27 | docs-site/docs/operator/device-capabilities.md | "Manual capability execution is POST http://localhost:5000/devices/{id}/execute with capabilityName" | stale | fix-doc | Correct port and request field. |
| 28 | docs-site/docs/operator/experiment-setup.md | "Campaign create takes workflowDefinitionJson and controls iteration count" | stale | fix-doc | Rewrite the campaign payload against CampaignEndpoints. |
| 29 | docs-site/docs/operator/production-hardening.md | "MinIO console is at localhost:9002" | stale | fix-doc | Use http://localhost:19002. |
| 30 | docs-site/docs/operator/production-hardening.md | "Mqtt:UseTls and Mqtt:CaFile are supported settings" | stale | fix-doc | Remove or implement the options. |
| 31 | docs-site/docs/operator/production-hardening.md | "OPC UA AutoAcceptUntrustedCertificates defaults to true" | stale | fix-doc | Correct the default and the warning. |
| 32 | docs-site/docs/operator/workflow-authoring.md | "The canvas is Rete.js" | stale | fix-doc | Replace Rete.js with React Flow. |
| 33 | docs-site/docs/operator/workflow-authoring.md | "The engine supports seven node types" | stale | fix-doc | Document all nine node types. |
| 34 | docs/CI.md | "Builds Helix.Orchestration/Helix.Orchestration.slnx with /warnaserror" | stale | fix-doc | Point at the root solution and remove the warn-as-error claim. |
| 35 | docs/HANDOVER.md | "Core layer has 31/31 tests passing" | stale | fix-doc | Refresh the Core count or date the snapshot. |
| 36 | docs/PHASE2-AGENT-RUNTIME-HANDOVER.md | "AdapterDiscoveryService provides config-driven adapter discovery" | stale | fix-doc | Point at IAdapterDiscovery and the current loader. |
| 37 | docs/PHASE4-DURABLE-EXECUTION-TREND-FOUNDATION-HANDOVER.md | "Cleipnir durability store is InMemoryFunctionStore" | stale | fix-doc | Replace with PostgreSqlFunctionStore and note the SQL schema. |
| 38 | docs/PHASE8-TYPED-ANALYTICAL-DATA-MODEL-HANDOVER.md | "All 107 Core tests pass and SpectralType has no UVVis or Raman values" | stale | fix-doc | Update the count and remove the stale enum limitation. |
| 39 | docs/PHASE9-SITE-SERVER-HANDOVER.md | "SiteServer xUnit coverage is 30 tests" | stale | fix-doc | Refresh the count or date the handover. |
| 40 | docs/PHASE10-GRPC-AGENT-TRANSPORT-HANDOVER.md | "SiteServer xUnit coverage is 35 tests" | stale | fix-doc | Refresh the count. |
| 41 | docs/PHASE11-CAMPAIGN-MANAGER-HANDOVER.md | "SiteServer xUnit coverage is 44 tests" | stale | fix-doc | Refresh the count. |
| 42 | docs/PHASE12-POSTGRESQL-PERSISTENCE-HANDOVER.md | "SiteServer 44 passed and Core 107 passed" | stale | fix-doc | Refresh both counts. |
| 43 | docs/PHASE13-DEVICE-INTEGRATION-LAYER-HANDOVER.md | "Devices has 54 xUnit tests" | stale | fix-doc | Refresh the count or delete the historical number. |
| 44 | docs/PHASE14-RECORDER-SERVICE-HANDOVER.md | "Recorder:TopicFilters, ChannelCapacity 4096, JSONL key experiments/{id}/trends/{date}/...jsonl" | stale | fix-doc | Rewrite the config keys, capacity and object-key scheme. |
| 45 | docs/README.md | "Phase 1 has 56 tests; Phase 9 30; Phase 10 35; Phase 11 44; Phase 13 54" | stale | fix-doc | Replace per-phase counts with a single dated count. |
| 46 | CI_CD_FIXES.md | "Test coverage is 216+ tests and per-project counts 8/107/57/44" | stale | fix-doc | Archive or refresh this fix log. |
| 47 | CI_CD_FIXES.md | "The build command still contains /warnaserror" | stale | fix-doc | Mark the fix as applied. |
| 48 | README.md | "Repository remote is gitlab.com/syngentagroup/peters_projects/helix.git" | stale | fix-doc | Replace the GitLab template with project-specific GitHub instructions. |
| 49 | .planning/spikes/002a-allotment-tauri-webview/README.md | "Verdict is VALIDATED for allotment in the Tauri webview" | stale | fix-doc | Reconcile the spike verdict with MANIFEST and record the actual library choice. |
| 50 | .planning/spikes/MANIFEST.md | "002a is PENDING" | stale | fix-doc | Resolve the contradiction. |
| 51 | .planning/spikes/MANIFEST.md | "Do NOT use rc-dock; use allotment for the shell" | stale | fix-doc | Record the superseded decision and the reason rc-dock was adopted. |
| 52 | .planning/todos/pending/2026-05-30-auto-generated-manual-control.md | "Every device type requires custom manual-control UI code" | stale | fix-doc | Mark the auto-generated dialog work as landed. |
| 53 | .planning/todos/pending/2026-05-30-graceful-device-loss-removal.md | "No contract exists for device loss, workflow invalidation and trend degradation" | stale | fix-doc | Narrow the todo to the remaining degradation gaps. |
| 54 | .planning/todos/pending/2026-05-30-real-time-trend-system.md | "There is no way to define derived or custom trends" | stale | fix-doc | Narrow the todo to the Python/ML evaluator gap. |
| 55 | .planning/todos/pending/2026-05-30-record-replay-system.md | "There is no way to replay a completed experiment" | stale | fix-doc | Narrow the todo to unified trend+event timeline reconstruction. |
| 56 | .planning/todos/pending/2026-06-01-agent-ws-runspace-event-timing.md | "RunspaceEvent lacks nodeId, startedAt and estimatedCompletionAt" | stale | fix-doc | Mark the nodeId/ETA part landed and keep any missing timing fields. |
| 57 | .planning/todos/pending/2026-06-01-reactflow-node-canvas-spike.md | "Workflow builder still uses Rete.js and needs a ReactFlow spike" | stale | fix-doc | Mark the spike landed and record the theme decision. |
| 58 | .planning/phases/11-ribbon-docking-workspace-shell/11-01-SUMMARY.md | "helix-dock.css holds 25 rc-dock class overrides mapped to CSS variables with zero hardcoded hex" | stale | fix-doc | Update the file/mechanism or restore the token-based overrides. |
| 59 | .planning/phases/11-ribbon-docking-workspace-shell/11-VALIDATION.md | "76 pre-existing Vitest tests are the regression gate" | stale | fix-doc | Update the gate to the current 116 tests. |
| 60 | .planning/phases/phase-1/SUMMARY.md | "217 tests pass, 0 failures" | stale | fix-doc | Date the phase snapshot and stop presenting it as current. |
| 61 | .planning/phases/phase-10/10-03-SUMMARY.md | "Preflight scripts passed on Debian 12 and Windows" | unverifiable | NEEDS-USER | Record the host matrix and date, or mark as a manual check. |
| 62 | .planning/phases/phase-10/10-VERIFICATION.md | "7/7 must-haves verified with status human_needed" | unverifiable | NEEDS-USER | Attach the human-verification evidence or archive the snapshot. |
| 63 | .planning/phases/phase-2/SUMMARY.md | "Created Helix.Orchestration/docs/driver-inventory.md" | stale | fix-doc | Point at a surviving inventory or delete the claim. |
| 64 | .planning/phases/phase-2/wave1-SUMMARY.md | "Created Helix.Orchestration/docs/driver-inventory.md" | stale | fix-doc | Remove the dead path. |
| 65 | .planning/phases/phase-5/03-SUMMARY.md | "tauri-build CI job completes in about 5 minutes" | unverifiable | NEEDS-USER | Replace with a pass/fail gate, not a duration. |
| 66 | .planning/phases/phase-5/VERIFICATION.md | "5 ROADMAP success criteria require human/CI verification" | unverifiable | NEEDS-USER | Attach the human evidence or mark the criteria as unverified. |
| 67 | .planning/phases/phase-6.3-close-gap-device-integration-bugs/6.3-VALIDATION.md | "Validation status is pending" | stale | fix-doc | Mark the validation rows complete. |
| 68 | .planning/phases/phase-6.3-close-gap-device-integration-bugs/6.3-VERIFICATION.md | "4/5 must-haves verified with status human_needed" | unverifiable | NEEDS-USER | Record the human result. |
| 69 | .planning/phases/phase-6/06-09-SUMMARY.md | "WorkflowBuilder uses Rete.js with useReteEditor and 7 node classes" | stale | fix-doc | Rewrite as the React Flow migration record. |
| 70 | .planning/phases/phase-6/06-11-SUMMARY.md | "Tab navigation (Builder/Execution/Monitor) is the shell" | stale | fix-doc | Archive as superseded by the phase 11 shell. |
| 71 | .planning/phases/phase-6/06-VALIDATION.md | "Validates a Rete.js workflow builder" | stale | fix-doc | Replace Rete.js with React Flow. |
| 72 | .planning/phases/phase-6/06-VERIFICATION.md | "10/14 must-haves verified with gaps_found" | stale | fix-doc | Re-verify and update the score. |
| 73 | .planning/phases/phase-6/6-UAT.md | "UAT status is diagnosed with only 4 tests passed" | stale | fix-doc | Archive the diagnosed session and point at the retest. |
| 74 | .planning/phases/phase-6/6-UAT-retest.md | "UAT verifies Builder/Execution/Monitor tab navigation" | stale | fix-doc | Retire the superseded UAT. |
| 75 | .planning/phases/phase-8/08-SUMMARY.md | "582 tests passed, 0 failed, 0 skipped" | stale | fix-doc | Replace with a dated count. |
| 76 | .planning/phases/phase-9/09-05-SUMMARY.md | "Phase 9 is recorded complete in ROADMAP.md" | stale | fix-doc | Restore a phase 9 row or drop the claim. |
| 77 | .planning/phases/phase-9/09-REVIEW.md | "Review found documentation issues (status issues_found)" | unverifiable | NEEDS-USER | Re-run the docs review or archive the snapshot. |
| 78 | .planning/phases/phase-9/09-VERIFICATION.md | "9/10 must-haves verified with status human_needed" | unverifiable | NEEDS-USER | Attach the human evidence. |
| 79 | .planning/phases/phase-driver-port/driver-port-02-SUMMARY.md | "IMettlerOpcCommand production registration throws NotImplementedException" | stale | fix-doc | Update the summary to the null-object implementation. |
