# Docs Audit

Repo root: C:\Users\u643664\source\repos\CSharpGitLab\Helix
Audited: 2026-10-27
Audited HEAD: d3c412e82781662b971a81fa35782e3ab4571adb
Alignment mode: no

## Summary

| Verdict | Count |
|---------|-------|
| verified | 18 |
| stale | 35 |
| aspirational | 1 |
| unverifiable | 0 |
| descriptive docs (no testable claims) | 0 |

Worst drift: `docs-site/docs/operator/deployment-setup.md` describes a stack with obsolete Agent/SiteServer ports, MinIO host ports and credentials, connection-string key, and Agent proxy settings; following its configuration does not match the checked-in Compose stack.

## Doc: .github/CICD.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Agent.Tests 8, Core.Tests 107, Devices.Tests 57, SiteServer.Tests 44; total 216+ tests" | status | stale | .github/CICD.md:56-62 gives old counts; current static test inventory is 31/115/389/45 in the respective Agent/Core/Devices/SiteServer test projects. |
| "Tests run and TRX reports are generated" | command | verified | .github/workflows/ci.yml:69 runs `dotnet test`; job at lines 18 onward collects test results. |

## Doc: .github/PIPELINE_VERIFICATION.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Agent 8/8, Core 107/107, Devices 57/57, SiteServer 44/44 pass" | status | stale | .github/PIPELINE_VERIFICATION.md:25-28 lists old totals; current static counts are 31/115/389/45. |

## Doc: .github/copilot-instructions.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "CI accounts for dev, stage and main branches" | config | verified | .github/workflows/ci.yml:3-15 configures push/PR triggers for dev, stage and main. |
| "Devices should be identified by COM port plus USB VID:PID" | feature | aspirational | .github/copilot-instructions.md states this guidance; current serial adapter options use port names and no VID/PID identity implementation was found (Helix.Orchestration.Devices/Transports/Serial/SerialTransportOptions.cs). |

## Doc: CI_CD_FIXES.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Test coverage is 216+ tests and per-project counts 8/107/57/44" | status | stale | CI_CD_FIXES.md:44,60-64 retains old counts; static project counts are now 31/115/389/45 for Agent/Core/Devices/SiteServer. |
| "The build command still contains /warnaserror" | command | stale | CI_CD_FIXES.md:9 says the CI build command includes it; .github/workflows/ci.yml:69's test command and build step do not include `/warnaserror`. |

## Doc: CLAUDE.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Sketch findings skill lives at .claude/skills/sketch-findings-Helix/SKILL.md" | structure | verified | CLAUDE.md:5 points to the path; `.claude/skills/sketch-findings-Helix/SKILL.md` exists in the audited tree. |

## Doc: README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Repository remote is gitlab.com/syngentagroup/peters_projects/helix.git" | integration | stale | README.md:18 names GitLab; sidecar `git remote -v` reports `https://github.com/PyPete/Helix.git`. |

## Doc: docs-site/README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "The site is built with Docusaurus" | structure | verified | docs-site/README.md:3 and docs-site/package.json dependency `@docusaurus/core`. |

## Doc: docs-site/docs/developer/adapter-authoring.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Adapters implement DeviceAdapterBase and register capabilities" | structure | verified | Helix.Orchestration.Devices/Abstractions/DeviceAdapterBase.cs defines the base; Helix.Orchestration.Devices/Drivers/KnauerAzura.cs registers capabilities. |

## Doc: docs-site/docs/developer/architecture.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Infrastructure owns EF Core migrations" | structure | stale | docs-site/docs/developer/architecture.md:90 says EF Core; Helix.Orchestration.Infrastructure/Helix.Orchestration.Infrastructure.csproj:12-13 references Dapper/Npgsql, and SQL migrations are under postgres/migrations. |
| "Recorder flushes JSONL batches" | structure | stale | docs-site/docs/developer/architecture.md:91 says JSONL; Helix.Orchestration.Infrastructure/Storage/IObjectStorageSink.cs:19 and RecorderWriterService write `.ndjson` objects. |

## Doc: docs-site/docs/developer/grpc-reference.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Agent gRPC default port is 5050 and adapter port 5051" | config | stale | docs-site/docs/developer/grpc-reference.md:142,157 conflicts with Helix.Orchestration.Agent/appsettings.json:12 and docker-compose.yml:78, which use 5080. |
| "AgentControl exposes five RPCs (ListDevices absent from table)" | integration | stale | docs-site/docs/developer/grpc-reference.md:26 omits ListDevices; Helix.Orchestration.Contracts/Protos/agent_control.proto defines it in addition to the other control RPCs. |

## Doc: docs-site/docs/developer/intro.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Developer docs cover architecture, adapters, transports, REST and gRPC" | structure | verified | The five linked pages exist under docs-site/docs/developer/. |

## Doc: docs-site/docs/developer/rest-api.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "SiteServer REST API runs on port 7020" | config | stale | docs-site/docs/developer/rest-api.md:9 says 7020; docker-compose.yml:105 configures 5090 (the dashboard dev proxy separately targets 56523 in helix-dashboard/vite.config.ts:11-12). |
| "/api/trends/ws WebSocket endpoint exists" | integration | verified | Helix.Orchestration.SiteServer/Program.cs:53-83 registers WebSockets and accepts the trend WebSocket; TrendWebSocketManager is registered. |

## Doc: docs-site/docs/developer/transport-guide.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "AddHelixAdapter<TAdapter> extension method exists" | feature | stale | docs-site/docs/developer/transport-guide.md:215 names the generic helper; Helix.Orchestration.Devices/Hosting/DevicesServiceCollectionExtensions.cs defines specific AddHelix*Adapter helpers, not AddHelixAdapter. |

## Doc: docs-site/docs/operator/deployment-setup.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Agent HTTP 5000 and gRPC 5050; SiteServer 7020" | config | stale | docs-site/docs/operator/deployment-setup.md:53-54,72 conflicts with docker-compose.yml:78,105 and Agent appsettings.json:12 (5080/5090). |
| "MinIO API 9000, console 9001, credentials helix/helix_dev_secret" | config | stale | docs-site/docs/operator/deployment-setup.md:31-37 conflicts with docker-compose.yml:58-62, which maps host ports 19000/19002 and configures MinIO credentials separately. |
| "ConnectionStrings__DefaultConnection and AgentProxy.Agents[] with GrpcAddress 5050" | config | stale | docs-site/docs/operator/deployment-setup.md:60,77-82 conflicts with docker-compose.yml:84,108-114 (`ConnectionStrings__HelixOrchestration`, `AgentProxy__Mode`, `AgentProxy__GrpcAddress`). |
| "Dashboard points at VITE_SITE_SERVER_URL=http://localhost:7020" | config | stale | docs-site/docs/operator/deployment-setup.md:110; checked-in SiteServer Compose port is 5090 at docker-compose.yml:105. |

## Doc: docs-site/docs/operator/device-capabilities.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Manual capability execution is POST http://localhost:5000/devices/{id}/execute with capabilityName" | config | stale | docs-site/docs/operator/device-capabilities.md:59-63; Agent/Api/DeviceEndpoints.cs maps execution at the Agent endpoint and its request DTO uses `Capability`; Agent listens on 5080 (appsettings.json:12). |

## Doc: docs-site/docs/operator/experiment-setup.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Campaign create takes workflowDefinitionJson and controls iteration count" | integration | stale | docs-site/docs/operator/experiment-setup.md:46,60; Helix.Orchestration.SiteServer/Endpoints/CampaignEndpoints.cs:13-27 accepts parameter sets instead. |

## Doc: docs-site/docs/operator/intro.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Operator docs cover deployment, devices, experiments, workflows and hardening" | structure | verified | The five referenced operator pages are present under docs-site/docs/operator/. |

## Doc: docs-site/docs/operator/production-hardening.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "MinIO console is at localhost:9002" | config | stale | docs-site/docs/operator/production-hardening.md:69,77; docker-compose.yml:59 maps host 19002 to container 9002. |
| "Mqtt:UseTls and Mqtt:CaFile are supported settings" | config | stale | docs-site/docs/operator/production-hardening.md:62-63; no such settings are defined by the shipped MQTT options/configuration. |
| "OPC UA AutoAcceptUntrustedCertificates defaults to true" | config | stale | docs-site/docs/operator/production-hardening.md:21,103; Helix.Orchestration.Devices/Transports/Opc/OpcTransportOptions.cs:57 initializes it to false. |

## Doc: docs-site/docs/operator/workflow-authoring.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "The canvas is Rete.js" | feature | stale | docs-site/docs/operator/workflow-authoring.md:11; helix-desktop/package.json includes @xyflow/react and the workflow builder imports React Flow. |
| "The engine supports seven node types" | feature | stale | docs-site/docs/operator/workflow-authoring.md:15; helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.tsx:8-17 defines nine types. |

## Doc: docs/CI.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Builds Helix.Orchestration/Helix.Orchestration.slnx with /warnaserror" | command | stale | docs/CI.md:21,23; solution is root Helix.Orchestration.slnx and .github/workflows/ci.yml:69 runs tests without `/warnaserror`. |

## Doc: docs/CONCEPT.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Two tiers: local Agent and site SiteServer; Core is headless" | structure | verified | docs/CONCEPT.md:7-15 describes the two-tier design; Agent, SiteServer and Core projects exist. |

## Doc: docs/DEVICE-MIGRATION-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Driver implementations live in Helix.Orchestration.Devices/Drivers" | structure | verified | Helix.Orchestration.Devices/Drivers contains the migrated driver implementations and SimulatorDevice. |

## Doc: docs/HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Core layer has 31/31 tests passing" | status | stale | docs/HANDOVER.md:574-578 is an old point-in-time count; current static Core test inventory is 115. |

## Doc: docs/PHASE1-CORE-DOMAIN-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Core delivers stratified trends, spectral analytics, leasing, WSL, Runspace and TestContext" | structure | verified | Corresponding Core/Trends, WSL, Runspace, leasing and TestContext implementations exist. |

## Doc: docs/PHASE10-GRPC-AGENT-TRANSPORT-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "SiteServer xUnit coverage is 35 tests" | status | stale | docs/PHASE10-GRPC-AGENT-TRANSPORT-HANDOVER.md:66 records the older total; current static SiteServer test inventory is 45. |

## Doc: docs/PHASE11-CAMPAIGN-MANAGER-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "SiteServer xUnit coverage is 44 tests" | status | stale | docs/PHASE11-CAMPAIGN-MANAGER-HANDOVER.md:52; current static SiteServer test inventory is 45. |

## Doc: docs/PHASE12-POSTGRESQL-PERSISTENCE-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "SiteServer 44 passed and Core 107 passed" | status | stale | docs/PHASE12-POSTGRESQL-PERSISTENCE-HANDOVER.md:78-79; current static inventories are SiteServer 45 and Core 115. |
| "Infrastructure uses Dapper and Npgsql with 2 tests" | structure | verified | Infrastructure.csproj references Dapper/Npgsql; two Infrastructure test cases are present in tests/Helix.Orchestration.Infrastructure.Tests. |

## Doc: docs/PHASE13-DEVICE-INTEGRATION-LAYER-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Devices has 54 xUnit tests" | status | stale | docs/PHASE13-DEVICE-INTEGRATION-LAYER-HANDOVER.md:5,177,257 gives an old count; current static Devices test inventory is 389. |

## Doc: docs/PHASE14-RECORDER-SERVICE-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Recorder:TopicFilters, ChannelCapacity 4096, JSONL key experiments/{id}/trends/{date}/...jsonl" | config | stale | docs/PHASE14-RECORDER-SERVICE-HANDOVER.md:27,46-47,83; RecorderOptions.cs:14 uses singular TopicFilter, writer options use 10,000/DropOldest, and IObjectStorageSink.cs:19 specifies `.ndjson` keys. |

## Doc: docs/PHASE2-AGENT-RUNTIME-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "AdapterDiscoveryService provides config-driven adapter discovery" | structure | stale | docs/PHASE2-AGENT-RUNTIME-HANDOVER.md:26 names the removed type; no AdapterDiscoveryService exists, while Helix.Orchestration.Agent/Adapters/IAdapterDiscovery.cs is the surviving abstraction. |

## Doc: docs/PHASE3-EXECUTION-PATH-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "WSL compiler and capability routing close the execution path" | feature | verified | Helix.Orchestration.Core/WSL/WslCompiler.cs and Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs implement the compiler and execution path. |

## Doc: docs/PHASE4-DURABLE-EXECUTION-TREND-FOUNDATION-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Cleipnir durability store is InMemoryFunctionStore" | structure | stale | docs/PHASE4-DURABLE-EXECUTION-TREND-FOUNDATION-HANDOVER.md:7,31; Agent/Program.cs configures PostgreSqlFunctionStore. |

## Doc: docs/PHASE5-RUNSPACE-LIFECYCLE-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Runspace lifecycle manager and WSL execution wiring exist" | feature | verified | Core/Runspace/IRunspaceManager and Agent/Runspaces/DurableRunspace.cs exist. |

## Doc: docs/PHASE6-WSL-COMPILER-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "WSL compiler, device parity and MQTT trend ingestion foundation exist" | feature | verified | Core/WSL compiler, Agent MQTT ingestion service and device implementations are present. |

## Doc: docs/PHASE7-VARIABLE-MANAGEMENT-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "WorkflowStateExtensionsTests has 10 tests and GlobalStateStoreTests has 12 (22 total)" | status | verified | Static `[Fact]`/`[Theory]` counts in tests/Helix.Orchestration.Core.Tests are 10 and 12 respectively. |

## Doc: docs/PHASE8-TYPED-ANALYTICAL-DATA-MODEL-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "All 107 Core tests pass" | status | stale | docs/PHASE8-TYPED-ANALYTICAL-DATA-MODEL-HANDOVER.md:286; current static Core test inventory is 115. |
| "SpectralType has no UVVis or Raman values" | status | stale | docs/PHASE8-TYPED-ANALYTICAL-DATA-MODEL-HANDOVER.md:323; Helix.Orchestration.Core/Trends/SpectralType.cs:8 includes Raman (UVVis remains absent). |

## Doc: docs/PHASE9-SITE-SERVER-HANDOVER.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "SiteServer xUnit coverage is 30 tests" | status | stale | docs/PHASE9-SITE-SERVER-HANDOVER.md:130 records 30; current static SiteServer test inventory is 45. |

## Doc: docs/README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Phase 1 has 56 tests; Phase 9 30; Phase 10 35; Phase 11 44; Phase 13 54" | status | stale | docs/README.md:13,21-25 repeats historical counts; current static totals are 592 .NET tests (Agent 31, Core 115, Devices 389, Infrastructure 2, Recorder 10, SiteServer 45). |

## Doc: helix-dashboard/README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Dashboard is a React + TypeScript + Vite app" | structure | verified | helix-dashboard/package.json declares React and Vite; Vite config and TypeScript sources are present. |

## Doc: helix-desktop/README.md

| Claim | Type | Verdict | Evidence |
|-------|------|---------|----------|
| "Desktop is a Tauri + React + TypeScript + Vite app" | structure | verified | helix-desktop/src-tauri exists; helix-desktop/package.json declares React, Vite and Tauri dependencies. |

## Descriptive docs

None. All 40 inventoried documents contain at least one testable claim.

## User rulings

| Queue # | Ruling | User's words | Planned |
|---------|--------|--------------|---------|
| 3 | fix-doc | Fix docs by default | T005 |
| 17 | fix-doc | Fix docs by default | T006 |
| 19 | fix-doc | Fix docs by default | T007 |
| 21 | fix-doc | Fix docs by default | T017 |
| 23 | fix-doc | Fix docs by default | T008 |
| 24 | fix-doc | Fix docs by default | T009 |
| 25 | fix-doc | Fix docs by default | T010 |
| 26 | fix-doc | Fix docs by default | T011 |
| 27 | fix-doc | Fix docs by default | T012 |
| 28 | fix-doc | Fix docs by default | T013 |
| 30 | fix-doc | Fix docs by default | T014 |
| 36 | fix-doc | Fix docs by default | T015 |
| 37 | fix-doc | Fix docs by default | T016 |

## Remediation queue

| # | Doc | Claim | Verdict | Class | Suggested action |
|---|-----|-------|---------|-------|------------------|
| 1 | .github/CICD.md | "Agent.Tests 8, Core.Tests 107, Devices.Tests 57, SiteServer.Tests 44; total 216+ tests" | stale | fix-doc | Replace the historic counts with dated current counts and the counting method. |
| 2 | .github/PIPELINE_VERIFICATION.md | "Agent 8/8, Core 107/107, Devices 57/57, SiteServer 44/44 pass" | stale | fix-doc | Re-run and date the verification table or label it as historical. |
| 3 | .github/copilot-instructions.md | "Devices should be identified by COM port plus USB VID:PID" | aspirational | NEEDS-USER | Decide whether to implement USB identity or state this as a desired guideline. |
| 17 | docs-site/docs/developer/architecture.md | "Infrastructure owns EF Core migrations" | stale | NEEDS-USER | Resolve the architecture description against the Dapper/SQL implementation and clarify the intended persistence design. |
| 18 | docs-site/docs/developer/architecture.md | "Recorder flushes JSONL batches" | stale | fix-doc | Update the format and object-key description to match `.ndjson` output. |
| 19 | docs-site/docs/developer/grpc-reference.md | "Agent gRPC default port is 5050 and adapter port 5051" | stale | NEEDS-USER | Reconcile documented ports with Agent configuration before changing either side. |
| 20 | docs-site/docs/developer/grpc-reference.md | "AgentControl exposes five RPCs (ListDevices absent from table)" | stale | fix-doc | Add the implemented ListDevices RPC and refresh the RPC count. |
| 21 | docs-site/docs/developer/rest-api.md | "SiteServer REST API runs on port 7020" | stale | NEEDS-USER | Confirm the intended public/development port and align documentation or deployment config. |
| 22 | docs-site/docs/developer/transport-guide.md | "AddHelixAdapter<TAdapter> extension method exists" | stale | fix-doc | Replace the nonexistent generic extension with the actual registration APIs. |
| 23 | docs-site/docs/operator/deployment-setup.md | "Agent HTTP 5000 and gRPC 5050; SiteServer 7020" | stale | NEEDS-USER | Confirm intended deployment ports and reconcile the guide with Compose. |
| 24 | docs-site/docs/operator/deployment-setup.md | "MinIO API 9000, console 9001, credentials helix/helix_dev_secret" | stale | NEEDS-USER | Confirm intended host mappings and credentials; then align the deployment guide and stack. |
| 25 | docs-site/docs/operator/deployment-setup.md | "ConnectionStrings__DefaultConnection and AgentProxy.Agents[] with GrpcAddress 5050" | stale | NEEDS-USER | Confirm the supported proxy/connection configuration and reconcile guide and runtime. |
| 26 | docs-site/docs/operator/deployment-setup.md | "Dashboard points at VITE_SITE_SERVER_URL=http://localhost:7020" | stale | NEEDS-USER | Confirm the intended dashboard endpoint and update the guide/config consistently. |
| 27 | docs-site/docs/operator/device-capabilities.md | "Manual capability execution is POST http://localhost:5000/devices/{id}/execute with capabilityName" | stale | NEEDS-USER | Confirm intended public API base path and align the example with the Agent route/request contract. |
| 28 | docs-site/docs/operator/experiment-setup.md | "Campaign create takes workflowDefinitionJson and controls iteration count" | stale | NEEDS-USER | Reconcile the documented campaign input with the current parameter-set API and intended contract. |
| 29 | docs-site/docs/operator/production-hardening.md | "MinIO console is at localhost:9002" | stale | fix-doc | Document the actual host-mapped console port 19002. |
| 30 | docs-site/docs/operator/production-hardening.md | "Mqtt:UseTls and Mqtt:CaFile are supported settings" | stale | NEEDS-USER | Confirm whether MQTT TLS settings are intended; implement them or remove the unsupported configuration advice. |
| 31 | docs-site/docs/operator/production-hardening.md | "OPC UA AutoAcceptUntrustedCertificates defaults to true" | stale | fix-doc | Correct the stated default to false and retain the security warning. |
| 32 | docs-site/docs/operator/workflow-authoring.md | "The canvas is Rete.js" | stale | fix-doc | Update the authoring guide to the shipped React Flow canvas. |
| 33 | docs-site/docs/operator/workflow-authoring.md | "The engine supports seven node types" | stale | fix-doc | Update the documented node list/count to match HelixNodes. |
| 34 | docs/CI.md | "Builds Helix.Orchestration/Helix.Orchestration.slnx with /warnaserror" | stale | fix-doc | Correct the solution path and describe the current CI command. |
| 35 | docs/HANDOVER.md | "Core layer has 31/31 tests passing" | stale | fix-doc | Mark the test count as historical or refresh it with a date and method. |
| 36 | docs/PHASE2-AGENT-RUNTIME-HANDOVER.md | "AdapterDiscoveryService provides config-driven adapter discovery" | stale | NEEDS-USER | Decide whether to restore the described discovery service or revise this historical implementation summary. |
| 37 | docs/PHASE4-DURABLE-EXECUTION-TREND-FOUNDATION-HANDOVER.md | "Cleipnir durability store is InMemoryFunctionStore" | stale | NEEDS-USER | Reconcile the phase snapshot with the currently configured PostgreSQL store and clarify whether the snapshot is historical. |
| 39 | docs/PHASE9-SITE-SERVER-HANDOVER.md | "SiteServer xUnit coverage is 30 tests" | stale | fix-doc | Label the count as historical or refresh with a date. |
| 40 | docs/PHASE10-GRPC-AGENT-TRANSPORT-HANDOVER.md | "SiteServer xUnit coverage is 35 tests" | stale | fix-doc | Label the snapshot historical or update the count with a date. |
| 41 | docs/PHASE11-CAMPAIGN-MANAGER-HANDOVER.md | "SiteServer xUnit coverage is 44 tests" | stale | fix-doc | Label the snapshot historical or update the count with a date. |
| 42 | docs/PHASE12-POSTGRESQL-PERSISTENCE-HANDOVER.md | "SiteServer 44 passed and Core 107 passed" | stale | fix-doc | Label the snapshot historical or update both counts with a date. |
| 43 | docs/PHASE13-DEVICE-INTEGRATION-LAYER-HANDOVER.md | "Devices has 54 xUnit tests" | stale | fix-doc | Label the snapshot historical or update the count with a date. |
| 44 | docs/PHASE14-RECORDER-SERVICE-HANDOVER.md | "Recorder:TopicFilters, ChannelCapacity 4096, JSONL key experiments/{id}/trends/{date}/...jsonl" | stale | fix-doc | Update option names, queue behavior/capacity and object format/key to match the recorder implementation. |
| 45 | docs/README.md | "Phase 1 has 56 tests; Phase 9 30; Phase 10 35; Phase 11 44; Phase 13 54" | stale | fix-doc | Identify phase test counts as dated snapshots or remove them from the current index. |
| 46 | CI_CD_FIXES.md | "Test coverage is 216+ tests and per-project counts 8/107/57/44" | stale | fix-doc | Archive or refresh this fix log's counts. |
| 47 | CI_CD_FIXES.md | "The build command still contains /warnaserror" | stale | fix-doc | Mark the old command issue as resolved or update the referenced command. |
| 48 | README.md | "Repository remote is gitlab.com/syngentagroup/peters_projects/helix.git" | stale | fix-doc | Replace the GitLab template remote with the project remote. |
| 80 | docs/PHASE8-TYPED-ANALYTICAL-DATA-MODEL-HANDOVER.md | "All 107 Core tests pass" | stale | fix-doc | Label as a dated historical run or refresh the current count. |
| 81 | docs/PHASE8-TYPED-ANALYTICAL-DATA-MODEL-HANDOVER.md | "SpectralType has no UVVis or Raman values" | stale | fix-doc | Mark the enum statement as historical; Raman is now present. |
