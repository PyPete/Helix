# Documentation

This folder contains project documentation for Helix.Orchestration.

## Files

### Architecture & Concepts
- [`CONCEPT.md`](CONCEPT.md) — Project concept, architecture principles, two-tier design (Agent / Site Server), execution and data-flow diagrams, technology stack, and roadmap.
- [`HANDOVER.md`](HANDOVER.md) — Full infrastructure handover: architecture direction, technology choices, WSL/Runspace/Campaign design decisions, device integration layer architecture (`Helix.Orchestration.Devices`), transport fault-tolerance model, OPC UA swap path, and captured learnings.
- [`CI.md`](CI.md) — CI/CD workflow behavior, branch model (`dev`/`stage`/`main`), and recommended GitHub branch protection checks.

### Phase Handovers
- [`PHASE1-CORE-DOMAIN-HANDOVER.md`](PHASE1-CORE-DOMAIN-HANDOVER.md) — Phase 1 complete. Covers: stratified trend model, MS/MS and NMR spectral analytics, device leasing, WSL authoring + RunspaceDefinition, Runspace abstraction, TestContext. 56 tests passing.
- [`PHASE2-AGENT-RUNTIME-HANDOVER.md`](PHASE2-AGENT-RUNTIME-HANDOVER.md) — Phase 2 foundation. Covers: contracts protos, agent scaffolding, adapter discovery/proxy layer, runspace manager, streaming skeleton, control-plane scaffolding, and Python pump adapter assets.
- [`PHASE3-EXECUTION-PATH-HANDOVER.md`](PHASE3-EXECUTION-PATH-HANDOVER.md) — Phase 3 completion. Covers: WSL compiler path closure, adapter capability routing, gRPC host wiring, lease expiry background cleanup, and remaining production gaps.
- [`PHASE4-DURABLE-EXECUTION-TREND-FOUNDATION-HANDOVER.md`](PHASE4-DURABLE-EXECUTION-TREND-FOUNDATION-HANDOVER.md) — Phase 4 foundation. Covers: durable runspace delegation via `IWorkflowExecutor`, Agent DI wiring for Cleipnir runtime dependencies, in-memory spectral and diode-array trend providers, trend aggregation routing, and scalar trend helper additions.
- [`PHASE5-RUNSPACE-LIFECYCLE-HANDOVER.md`](PHASE5-RUNSPACE-LIFECYCLE-HANDOVER.md) — Phase 5 lifecycle wiring. Covers: lease renewal API, runspace manager implementation, workflow leasing context extension, and WSL-to-durable-runspace execution adapter integration.
- [`PHASE6-WSL-COMPILER-HANDOVER.md`](PHASE6-WSL-COMPILER-HANDOVER.md) — Phase 6 foundation. Covers: WSL compiler + diagnostics + device binding registry, Vapourtec R4 protocol parity uplift against legacy POP command model (`FW/KP/GA/HF/HT/HP`), MQTT trend ingestion background service and configuration, and expanded xUnit device tests.
- [`PHASE7-VARIABLE-MANAGEMENT-HANDOVER.md`](PHASE7-VARIABLE-MANAGEMENT-HANDOVER.md) — Phase 7 complete. Covers: cohesive variable management system with workflow-scoped, global/campaign-scoped, and device read variables. JSON serialization for complex types, WSL variable substitution with compile-time validation and runtime resolution, typed access helpers, and comprehensive test coverage (22 tests passing).
- [`PHASE8-TYPED-ANALYTICAL-DATA-MODEL-HANDOVER.md`](PHASE8-TYPED-ANALYTICAL-DATA-MODEL-HANDOVER.md) — Phase 8 complete. Covers: typed analytical data model refactor (Option B — permanent envelope + typed metadata). Introduces `Spectrum1D`/`Spectrum2D` structural envelopes and technique-specific metadata records for NMR 1D/2D (HSQC, COSY), MS full-scan, MS/MS tandem fragmentation, and Mid-IR / ATR-FTIR. Updates trend interfaces, providers, test context API, MQTT ingestion, and TrendAggregator routing. Fixes `InMemoryRunspace.AbortAsync` hang on unstarted runspaces. 107 tests passing.
- [`PHASE9-SITE-SERVER-HANDOVER.md`](PHASE9-SITE-SERVER-HANDOVER.md) — Phase 9 complete. Covers: Site Server API host foundation (REST + Swagger/OpenAPI), simulation-first runtime wiring (`SimulatedTrendProvider`, `SimReactor`, `SimPump`, `SimSpectrometer`), `IAgentProxy` + `SimAgentProxy` seam, MQTT trend bridge publishing to technology-agnostic topics, local Mosquitto/docker-compose setup (MQTT + WebSocket), and SiteServer xUnit coverage (30 tests passing).
- [`PHASE10-GRPC-AGENT-TRANSPORT-HANDOVER.md`](PHASE10-GRPC-AGENT-TRANSPORT-HANDOVER.md) — Phase 10 complete. Covers: config-driven Site Server agent transport selection (`sim`/`grpc`), gRPC control-plane proxy implementation (`GrpcAgentProxy`), generated gRPC client enablement in Contracts, `IAgentControlClient` adapter for testability, and SiteServer xUnit coverage uplift (35 tests passing).
- [`PHASE11-CAMPAIGN-MANAGER-HANDOVER.md`](PHASE11-CAMPAIGN-MANAGER-HANDOVER.md) — Phase 11 complete. Covers: Site Server-owned campaign orchestration (`CampaignManager`), sequential iteration strategy seam, campaign REST endpoints (`/api/campaigns`), in-memory lifecycle tracking, and expanded SiteServer xUnit coverage (44 tests passing).
- [`PHASE12-POSTGRESQL-PERSISTENCE-HANDOVER.md`](PHASE12-POSTGRESQL-PERSISTENCE-HANDOVER.md) — Phase 12 complete. Covers: dedicated Infrastructure project (`Helix.Orchestration.Infrastructure`), PostgreSQL repositories for experiments/campaigns, initial SQL migration (`campaigns`, `campaign_iterations`, `experiments`), docker-compose postgres service wiring, and container-backed integration test scaffolding.
- [`PHASE13-DEVICE-INTEGRATION-LAYER-HANDOVER.md`](PHASE13-DEVICE-INTEGRATION-LAYER-HANDOVER.md) — Phase 13 foundation. Covers: shared transport and adapter host layer (`Helix.Orchestration.Devices`), transport abstraction (`IDeviceTransport`), fault tolerance design (ReconnectPolicy, CircuitBreaker, TransportWatchdog), serial/Modbus/TCP/OPC/C-DLL protocol implementations, composition-based adapter architecture, OPC UA library swap path, test seams for hardware-free validation, and 54 xUnit tests passing.
- [`PHASE14-RECORDER-SERVICE-HANDOVER.md`](PHASE14-RECORDER-SERVICE-HANDOVER.md) — Phase 14 complete. Covers: standalone Recorder Worker Service (`Helix.Orchestration.Recorder`), MQTT subscription and event capture, MinIO object storage via `IObjectStorageSink` abstraction, JSONL batch flushing with hierarchical key scheme, configurable flush policy (count/time), bounded channel architecture with drop-on-full, and xUnit coverage for parsing and writer logic. Known gaps: Parquet support, TLS/authentication, replay consumer.

### Migration Handovers
- [`DEVICE-MIGRATION-HANDOVER.md`](DEVICE-MIGRATION-HANDOVER.md) — POP.DeviceCore → Helix.Orchestration.Devices migration. Covers: two-layer split rationale (pure device vs. Agent transport), transport contract design, domain model replacements, base class hierarchy, wave-1 driver scaffold status, known parity gaps per driver (`VapourtecR4`, `KnauerAzura`, `SystagOpcUaClient`), capability adapter wiring, DI lifetime considerations, and recommended next steps.

## Notes
- Keep long-form project decisions and handovers here.
- Add new phase handover docs as `PHASE{N}-{TOPIC}-HANDOVER.md` and update this index.
- Add new architecture decision records as `ADR-{NNN}-{topic}.md`.

