# Evidence — codebase

Repo root: C:\source\Github\Helix\Helix
Scanned: 2026-10-25
Checks run: Revision inspected `1d2458bdb0756a8f4f8df304587e2d39a8b976ec` (`fix(docs): transcode four Markdown files to UTF-8`), branch `dev`, 336 commits ahead of `origin/main`; 865 tracked files; 14 `.csproj` (8 product, 6 test) in `Helix.Orchestration.slnx`. No project commands were executed — no `dotnet`, `npm`, `node`, `cargo`, `docker`, `make`, `pytest`, or setup/test/build/release runner was invoked. This is a static read only. The only commands used were read-only inspection: `git log`, `git status --porcelain`, `git rev-list --count`, `git ls-files`, `Select-String`, and file reads/globs. Every claim about runtime behaviour, build success, or test results is marked `unverifiable` and states what would have to run.

## Map

- **Stack**: Three deployable .NET 10 (`net10.0`) services (`Helix.Orchestration.Agent`, `Helix.Orchestration.SiteServer`, `Helix.Orchestration.Recorder`) plus shared libraries (`Core`, `Contracts`, `Devices`, `Infrastructure`, `Simulation`) and two front ends (`helix-desktop` = React 18 + Tauri 2, `helix-dashboard` = React 19 + Vite 8) and a Docusaurus 3 docs site. Backing services: Postgres 16, MinIO, Mosquitto 2. One Python gRPC adapter stub.
- **Entry points**: Three ASP.NET Core minimal-API hosts started from their own `Program.cs` — `Helix.Orchestration.Agent/Program.cs` (Kestrel `http://localhost:5080`, route groups `/runspaces`, `/experiments`, `/workflows`, `/devices`, `/health`, `/ws`, plus a gRPC `AgentControl` service), `Helix.Orchestration.SiteServer/Program.cs` (`/api/*` REST + Swagger + MQTT-over-WebSocket, default port 5000 / compose 5090), and `Helix.Orchestration.Recorder/Program.cs` (no HTTP, a `BackgroundService`). Front ends start via `npm run dev` (Vite) and `npm run tauri` in `helix-desktop`; `npm run dev` / `npm run build` in `helix-dashboard`; docs via `docs-site` Docusaurus scripts. Endpoint registration lives in `Api/*Endpoints.cs` `MapGroup` extension methods, not in `Program.cs` bodies.
- **Architecture**: There is a real, working HTTP execution path (compile → queue → in-memory BFS runspace) and a separate, fully-registered "durable" Cleipnir `IWorkflowExecutor` path that nothing invokes. The SiteServer — the service the browser/desktop UI actually talks to for experiment submission — bridges into the Agent over gRPC, but that gRPC method creates a runspace and never starts it. The Agent owns devices and runspaces; SiteServer owns campaigns, the WSL compiler instance, and the MQTT trend bridge; the Recorder consumes run data. So the UI-facing submit path is a dead end, while the path that works is only reachable by calling the Agent's own `/runspaces` endpoint directly, which the desktop ribbon does not do. Cross-service seams are gRPC (control), MQTT (trends), and Postgres (persistence).
- **Conventions**: C# uses file-scoped namespaces (`namespace X;`), PascalCase types/members, `record` types for DTOs and events, constructor injection, and static extension classes exposing `MapXEndpoints(this WebApplication app)` with fluent `WithTags`/`WithName`/`WithSummary`/`Produces`; endpoint handlers catch exceptions and return `Results.Problem(...)` rather than throwing across the boundary. Tests are xUnit `[Fact]`/`[Theory]` in `tests/<Project>.Tests/` mirroring the source project name, with descriptive `Method_State_Expectation` names; front-end tests are Vitest files colocated as `*.test.tsx`/`*.test.ts`. Commits are Conventional Commits with the phase as scope: `feat(11.1-01):`, `docs(12):`, `fix(12-gaps):`.
- **Maturity**: Uneven. The .NET core compiles into a coherent service surface with 592 `[Fact]/[Theory]` attributes across 6 test projects; the desktop has 116 Vitest cases in 23 files and the dashboard 32 in 7. But large areas are scaffolding: the durable executor and `WslExecutionAdapter` are wired-but-unreachable, the Python adapter cannot import, the Tauri Rust backend is two stub functions, device wiring is a 644-line hardcoded registry with null-object stubs, and integration tests silently early-return without Docker. CI builds/tests/releases everything (`ci.yml`), which raises apparent maturity above actual behaviour.
- **Recent activity**: The last 20 commits split cleanly into two phases. Commit `89b940a` and the surrounding wave (`9be58cb`, `80f5a4e`, `f3a351e`, `9452e0d`) are "phase 11.1" backend gap closure — WSL compiler, gRPC device/pause-resume, fumehood status, campaign persistence, trend-provider stubs — followed by verification/state docs (`c3c5b1c`, `ea225b9`). The most recent cluster is phase 12/13 planning and documentation (`2c782b1`, `07373c8`, `2108fe3` "stamp step events with ExperimentId", `cf81485`, `02da9ab`, `53996bb`) ending in a docs-encoding fix (`1d2458b`). Net: recent work is documentation and lifecycle/step-event plumbing, not new execution capability.

## Finding: 1. Two orchestration engines exist; the durable one is unreachable

- **Claim**: `Program.cs` registers Cleipnir (`IFunctionStore`, `FunctionsRegistry`) at `Helix.Orchestration.Agent/Program.cs:79-82` and `IWorkflowExecutor → WorkflowExecutor` at `:247`, plus `WslExecutionAdapter` at `:248`. Nothing in production code resolves `IWorkflowExecutor`. The only `/runspaces` implementation goes through `IRunspaceManager`, which `Program.cs:89` binds to the in-memory manager.
- **Source**: static read, `Helix.Orchestration.Agent/Program.cs:79-89,237-248`; `Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs:24`.
- **Confidence**: High (registration and absence of consumers are directly observable).
- **Why it matters here**: Two competing mental models of "how work runs" exist in one process. Any new work must first decide which one is real. The durable path also requires a `WorkflowRegistry` entry that no code creates (see #3), so enabling it is not a config flip — it would throw.

## Finding: 2. SiteServer's experiment submit creates a runspace but never starts it

- **Claim**: `AgentGrpcService.SubmitExperiment` deserializes the definition, calls `_runspaceManager.CreateAsync(...)`, and returns `Accepted = true`. It never calls `StartAsync`. Pause/Resume/Abort are implemented (`:58-90`) but only against an already-running runspace.
- **Source**: `Helix.Orchestration.Agent/gRPC/Server/AgentGrpcService.cs:21-34` (only `CreateAsync` at line 27, confirmed by grep).
- **Confidence**: High (single method body).
- **Why it matters here**: This is the single most load-bearing defect. `POST /api/experiments` on SiteServer (`Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs:54-82`, gRPC call at `:80`) returns a `runspaceId` and HTTP 200 while nothing executes. Any demo, campaign, or UI flow routed through SiteServer reports success and does nothing. It also makes the pause/resume/abort surface look complete while operating on a `Created` runspace.

## Finding: 3. Durable executor would throw even if invoked — no workflows are registered

- **Claim**: `WorkflowExecutor` resolves work by name via `WorkflowRegistry.Get(name)`; `WorkflowRegistry` is registered (`Program.cs:82`) but no code calls `Register(...)` for any production workflow. `WslExecutionAdapter`/`DurableRunspace` are referenced only by tests and the DI line at `:248`.
- **Source**: static read, `Helix.Orchestration.Agent/Program.cs:82,247-248`; cross-checked consumer set by grep for `IWorkflowExecutor`/`WorkflowRegistry`.
- **Confidence**: High on "no production registration site"; Medium on the exact exception text (not executed).
- **Why it matters here**: Confirms #1 is not a dormant-but-ready feature. Turning it on requires implementing the work-registration layer, not just calling `Start`.

## Finding: 4. Desktop ribbon Run/Pause/Abort hit endpoints that do not exist

- **Claim**: `HelixShell.tsx` posts to `${baseUrl}/api/runspaces/start`, `/api/runspaces/pause`, `/api/runspaces/abort`. The Agent maps `/runspaces` with `POST /`, `POST /{id}/pause`, `POST /{id}/resume`, `POST /{id}/abort` — no `/api` prefix, no `start`, no id-less pause/abort.
- **Source**: `helix-desktop/src/shell/HelixShell.tsx:58-72`; `Helix.Orchestration.Agent/Api/RunspaceEndpoints.cs:24,73,102,131`.
- **Confidence**: High (route table vs fetch strings).
- **Why it matters here**: Ribbon buttons silently 404 (the `fetch` results are never checked). Meanwhile `ExecutionPanel.tsx` uses the *correct* paths (`/workflows/compile`, `/experiments`, `/experiments/{id}/pause|resume|abort`), which proves the contract is known elsewhere in the same app — the ribbon is stale, not the API. It also means "desktop can run workflows" is false as shipped.

## Finding: 5. Agent's own `POST /experiments` runs an empty stub, not the workflow

- **Claim**: The endpoint starts an experiment, then builds `RunspaceDefinition(..., Nodes: Array.Empty<WslNode>(), Edges: Array.Empty<WslEdge>(), ...)` and starts *that*.
- **Source**: `Helix.Orchestration.Agent/Api/ExperimentEndpoints.cs:36-51` (stub definition at `:39-44`).
- **Confidence**: High.
- **Why it matters here**: A second apparently-working entry point that cannot do work. Combined with #2 and #4, there are three distinct "start an experiment" surfaces and all three are inert for different reasons. This is the kind of thing that makes a test suite pass while the product does nothing.

## Finding: 6. Experiment history is always empty — `SaveAsync` has no production caller

- **Claim**: `IExperimentRepository.SaveAsync` is implemented but never called outside tests. SiteServer's history endpoint reads the repository, and fumehood `activeExperimentName` is derived from it, so both are permanently empty/blank.
- **Source**: static read + grep across `Helix.Orchestration.SiteServer` and `Helix.Orchestration.Infrastructure`; reader at `Helix.Orchestration.SiteServer/Endpoints/ExperimentEndpoints.cs:40-51`.
- **Confidence**: High (absence of callers is grep-verifiable).
- **Why it matters here**: The persistence layer is built and wired but never written to. This is why "campaign persistence / experiment history" claims in planning can look satisfied by code presence while the feature is empty at runtime.

## Finding: 7. Step events race the runspace activation, and completion is never recorded

- **Claim (a)**: `ExperimentEndpoints` calls `runspace.StartAsync(ct)` and only afterwards `experimentManager.SetActiveRunspace(...)`. `InMemoryRunspace.StartAsync` fires the run body without awaiting it, so early step/completion events observe a null `ActiveRunspaceId` and stamp `ExperimentId = null`, which the persistence worker drops.
- **Claim (b)**: `ExperimentManager.CompleteAsync` (`:103`) has zero callers, so experiments stay in `Running` and no completion event is persisted.
- **Source**: `Helix.Orchestration.Agent/Api/ExperimentEndpoints.cs:46-51`; `Helix.Orchestration.Agent/Experiments/ExperimentManager.cs:103`; event ingestion at `Helix.Orchestration.Agent/AgentService.cs:138-139` writes lifecycle events to `IExperimentEventChannel`.
- **Confidence**: High on the call sites and absence of `CompleteAsync` callers; Medium on the precise dropped-event sequence (would need a run to confirm ordering).
- **Why it matters here**: Explains the otherwise contradictory planning note that marks gaps closed while itself observing "step rows absent". It is a real ordering bug, not a missing feature, and it is invisible to unit tests that call the manager directly.

## Finding: 8. MQTT trend contract is broken in both directions

- **Claim (a)**: The Agent subscribes to filter `helix/trends/#` (`appsettings.json:19-21`) and parses topics by `Split('/')` expecting 4 segments (`helix/trends/{kind}/{name}`).
- **Claim (b)**: SiteServer publishes `helix/{siteId}/{fumehoodId}/{experimentId}/trends/{kind}/{trendId}` — 7 segments, with `trends` at index 4.
- **Claim (c)**: The Recorder expects exactly 7 segments with `trends` at index 4.
- **Source**: `Helix.Orchestration.Agent/appsettings.json:16-22`; `Helix.Orchestration.Agent/Trends/MqttTrendIngestionService.cs:35,57,105`; `Helix.Orchestration.SiteServer/Mqtt/MqttTrendBridge.cs:16,98-109`; Recorder topic parser.
- **Confidence**: High (string literals and split indices).
- **Why it matters here**: The SiteServer topic does not even match the Agent's `helix/trends/#` filter (the second segment is the site id, not `trends`), so Agent ingestion receives nothing from the bridge at all. Two services in the same repo define incompatible schemas for the same bus. This is the clearest "half-built integration" seam.

## Finding: 9. The scalar trend producer is dead in production

- **Claim**: `TrendAggregator.ProcessAsync` exists and calls `trendProvider.PushScalar(...)`, but it is only invoked by tests. The driver's `StreamTrendsAsync` is likewise only iterated by tests. `TrendAggregator` is registered as a singleton (`Program.cs:88`) but never driven.
- **Source**: `Helix.Orchestration.Agent/Streaming/TrendAggregator.cs:30,55-67`; `Helix.Orchestration.Agent/Program.cs:86-88`; caller set by grep.
- **Confidence**: High.
- **Why it matters here**: The live-trend story depends on a pipeline whose input stage has no production driver. Combined with #8, the trend feature has a producer with no caller and a transport with no matching consumer.

## Finding: 10. SiteServer's trend bridge only sees startup trends

- **Claim**: `MqttTrendBridge.ExecuteAsync` enumerates `_trendProvider.GetAllTrends()` once and subscribes to those observables; there is no subscription to a "trend added" stream, so trends created after startup are never bridged.
- **Source**: `Helix.Orchestration.SiteServer/Mqtt/MqttTrendBridge.cs:67-82`.
- **Confidence**: High (single enumeration, no dynamic hook).
- **Why it matters here**: In a sim-first product, trends are created as experiments run — i.e. after startup — so the bridge would publish almost nothing even if #8 were fixed.

## Finding: 11. Campaign iterations submit empty definitions and self-report Completed

- **Claim**: Each iteration builds `RunspaceDefinition` with `Nodes: Array.Empty<WslNode>()`, `Edges: Array.Empty<WslEdge>()`, submits it, then immediately sets the iteration to `CampaignIterationStatus.Completed` with message `"Submitted"` and `EndedAt = now`. The campaign is then marked `Completed` at `:133`.
- **Source**: `Helix.Orchestration.SiteServer/Campaigns/CampaignManager.cs:100-115,133`.
- **Confidence**: High.
- **Why it matters here**: Campaigns are the "many experiments" abstraction and they are a facade: no nodes, no waiting, no result. The status vocabulary (`Completed`) actively misleads any consumer or report. It also depends on #2, so even a correct definition would not run.

## Finding: 12. Safety interlocks are implemented but never armed

- **Claim**: `SafetyEngine` exposes `RegisterInterlock`, `EvaluateInterlocksAsync`, and `StartInterlockMonitoringAsync`; no production code calls any of them. `ISafetyEngine` is registered (`Program.cs:85`) and `WslCompiler` injects `SafetyGuard` nodes (`:56-61,228-247`), but the executor ignores guard semantics and nothing registers rules or starts monitoring.
- **Source**: `Helix.Orchestration.Core/Safety/SafetyEngine.cs:32,47,121-125`; `Helix.Orchestration.Core/WSL/WslCompiler.cs:56-61,228-247`; `Helix.Orchestration.Agent/Program.cs:85`.
- **Confidence**: High on absence of production callers; Medium on executor guard handling (read, not run).
- **Why it matters here**: For a lab-automation product this is the highest-consequence gap: the safety layer exists in code, appears in compiled output, and is inert. A reader auditing "is it safe?" from file presence would answer yes, wrongly.

## Finding: 13. Compiled Loop/Branch/Parallel/guard constructs exceed what the executor honours

- **Claim**: `WslCompiler` expands `Loop`, `Branch`, and `Parallel` into node graphs and injects `SafetyGuard` nodes. The in-memory BFS executor does not interpret `Condition`, fork/join, or guard semantics.
- **Source**: `Helix.Orchestration.Core/WSL/WslCompiler.cs:56-61,228-247`; `Helix.Orchestration.Agent/Program.cs:108-236` (inlined BFS executor).
- **Confidence**: Medium-High (compiler verified; executor semantics read, not exercised).
- **Why it matters here**: The compiler is the more sophisticated half. Its output implies control flow the runtime cannot enforce, which is a silent correctness risk rather than an error. This is `unverifiable` as to exact runtime behaviour without executing a compiled Loop/Branch workflow.

## Finding: 14. Device integration is a 644-line hardcoded registry with stubs and unused helpers

- **Claim**: `DeviceAdapterExtensions.cs` is 644 lines, contains 64 `AddSingleton` calls and 34 `COM*` references, hardcodes IP addresses, and registers null-object device stubs around `:562-618`. Dedicated helpers `AddHelixSerialAdapter`, `AddHelixTcpAdapter`, `AddHelixOpcAdapter` exist but have zero call sites.
- **Source**: `Helix.Orchestration.Agent/Adapters/Devices/DeviceAdapterExtensions.cs` (line count/add counts measured with `Select-String`/`Get-Content`); helper call sites by grep.
- **Confidence**: High on counts; High on zero call sites.
- **Why it matters here**: Device support is the product's reason to exist, and it is configured by editing one giant file rather than by configuration. The unused helpers suggest an abandoned refactor. It also means the device set is fixed at compile time, which blocks any multi-site story.

## Finding: 15. Restore is not reproducible

- **Claim**: No `global.json` (SDK not pinned), no `packages.lock.json` or other lockfiles, wildcard NuGet versions (`4.*`, `2.*`), and `nuget.config` clears sources and leaves only nuget.org.
- **Source**: glob for `global.json`/`*.lock.json`/`packages.lock.json` (none found); `Directory.Packages.props`/`.csproj` version strings; `nuget.config`.
- **Confidence**: High.
- **Why it matters here**: A build today and a build next month can differ without any commit. Any "it builds" claim is therefore time-dependent — `unverifiable` without a restore+build.

## Finding: 16. Test suite is real but environment-conditional, and the advertised counts are unsupported

- **Claim**: 592 `[Fact]/[Theory]` attributes statically; desktop 116 cases across 23 files; dashboard 32 cases across 7 files. Postgres repository tests `return` early when Docker/Postgres is unavailable (silent pass), and they apply only `001_initial.sql`, not later migrations. `CICD.md:62` claims "216+" tests; `.planning/PROJECT.md:46` claims "1,262 .NET xUnit tests, 76 helix-desktop ... 32 helix-dashboard".
- **Source**: attribute counts via `Select-String`; `tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs:116-117,145-146`; `.github/CICD.md:62`; `.planning/PROJECT.md:46`.
- **Confidence**: High on static counts and the early-return pattern; the doc numbers cannot be reconciled with the 592 attributes and are `unverifiable` without a run.
- **Why it matters here**: Two documents disagree by ~5× about test volume, and the integration tests can pass vacuously. Test-count claims in planning should not be treated as evidence of coverage.

## Finding: 17. Tauri backend is a two-function stub

- **Claim**: The Rust side exposes only `load_config` (ignores the `app` argument, returns defaults) and a no-op `save_config`. There are no gRPC commands despite comments implying them; the frontend uses only `fs`/`store` plugins.
- **Source**: static read, `helix-desktop/src-tauri/src/*.rs`; plugin usage grep in `helix-desktop/src`.
- **Confidence**: High.
- **Why it matters here**: The desktop is effectively a browser app in a shell. Config persistence is fake, so "desktop settings survive restart" is false. This matters for any decision about whether desktop is a real product surface.

## Finding: 18. Python gRPC pump adapter does not correspond to the proto

- **Claim**: `pump_adapter.py` imports a `generated` package that does not exist in the repo and reads fields (`trend_id`, `trend_kind`, `timestamp`) that are absent from `adapter.proto`. No .NET `DeviceAdapter` client exists.
- **Source**: `adapters/` static read; `adapter.proto` field list; grep for `DeviceAdapter` client in .NET.
- **Confidence**: High on mismatch; `unverifiable` as to whether it was ever runnable.
- **Why it matters here**: This is aspirational code checked in as if it were an integration. It will not import, and there is no counterpart to talk to. Treat the "Python adapter" as a design sketch.

## Finding: 19. Config/port drift between services and deployment

- **Claim**: `docker-compose.yml` maps MinIO host ports `19000:9000` and `19002:9002` while comments say 9000/9002 (`:58-59` vs `:9`). SiteServer `AgentProxy:Mode` defaults to `sim` in `appsettings.json:12` but compose overrides to `grpc` (`:108`). SiteServer always registers the simulation layer regardless of mode (`Program.cs:27`). The dashboard Vite proxy targets port `56523`, which nothing in the repo listens on (SiteServer default 5000, compose 5090).
- **Source**: `docker-compose.yml:9,58-59,108`; `Helix.Orchestration.SiteServer/appsettings.json:11-12`; `Helix.Orchestration.SiteServer/Program.cs:27`; `helix-dashboard/vite.config.ts`.
- **Confidence**: High on literals.
- **Why it matters here**: "Run it with compose and it works" is not supportable from the files. In grpc mode SiteServer still holds simulated devices and a simulated trend provider, so the mode switch is not a clean boundary — it changes *some* services, which is worse than either pure mode.

## Finding: 20. Agent is the only service with a `/health`; deploy assumes otherwise

- **Claim**: The Agent maps `/health`. `docker-compose.yml:90-91` comments that the Agent has one; SiteServer has no `/health` endpoint, and compose notes the Recorder has none. `useAgentHealth` in desktop polls `/health` — correct for Agent.
- **Source**: `Helix.Orchestration.Agent/Program.cs` health mapping; `Helix.Orchestration.SiteServer/Program.cs`; `docker-compose.yml:90-91,122,148`.
- **Confidence**: High.
- **Why it matters here**: Health-gated orchestration will treat SiteServer as unhealthy. Minor, but it is the kind of drift that turns into a flaky deploy.

## Finding: 21. The product tree is clean at HEAD; only `.project/` is untracked

- **Claim**: Measured with Git for Windows 2.56.0.windows.3, `git status --porcelain` returns exactly one line, `?? .project/`. There are zero modified entries: every tracked product file matches HEAD.
- **Source**: `git status --porcelain` (one line, `?? .project/`); `git rev-parse --abbrev-ref HEAD` = `dev`; `git log -1` = `1d2458b`.
- **Confidence**: High (measured).
- **Why it matters here**: The tree is not dirty, but a helper or agent that resolves `git` to the MSYS2-bundled 2.34.1 on PATH sees a false-dirty tree of phantom line-ending/BOM modifications, because that build does not handle `core.autocrlf` for this checkout. Such an agent will fail "worktree must be clean" gates and misread noise as work, so pipeline invocations must prepend `C:\Program Files\Git\cmd` to PATH.

## Finding: 22. Build output is committed

- **Claim**: 23 files under `helix-dashboard/coverage/` and 9 `test-results*` files are tracked. No `dist/`, `bin/`, `obj/`, or `node_modules/` is tracked.
- **Source**: `git ls-files` filtered by path.
- **Confidence**: High.
- **Why it matters here**: These artifacts are rewritten by local test runs, so they can dirty a tree that would otherwise be clean, and they must not be cited as product source.

## Finding: 23. Governance docs are absent or template-level

- **Claim**: No `AGENTS.md`; no `WORKFLOW.md`. `README.md` is an unedited GitLab starter template. `CLAUDE.md` is 6 lines. `.project/` contains only the pipeline state and this inspection's two research artifacts (pipeline `gsd-path/v2`, phase `inspect`); before this run it held only `STATE.md`.
- **Source**: glob/read of repo root; `.project/STATE.md:1-28`.
- **Confidence**: High.
- **Why it matters here**: There is no authoritative statement of architecture or contribution rules in the repo. Any "the project intends X" claim has to be reconstructed from code and `.planning/`, which is exactly the trap this inspection is meant to avoid. Recorded as a finding, not a blocker.

## Finding: 24. `CLAUDE.md`/`CICD.md`/`PIPELINE_VERIFICATION.md` describe intent that the code does not meet

- **Claim**: `.github/CICD.md` and `.github/PIPELINE_VERIFICATION.md` describe a verification pipeline and test volume; `.github/workflows/ci.yml` does build .NET, dashboard, desktop tests, docs, Tauri bundles, and releases (`:69,112,139,141-157,181-191`). The pipeline itself is real; the *verified behaviour* it implies is not, because of #2/#5/#7 and #16.
- **Source**: `.github/workflows/ci.yml:69,112,139,141-157,181-191`; `.github/CICD.md:62`; `.github/PIPELINE_VERIFICATION.md`.
- **Confidence**: High on CI contents; the "implies verified behaviour" link is inference.
- **Why it matters here**: A green pipeline here proves compilation and test execution, not that submit→execute→trend works end to end. Do not read CI success as product correctness.

## Finding: 25. Planning contradicts both itself and the code

- **Claim (a)**: `.planning/STATE.md:124` states "Phase 11.1 complete ... All 8 backend execution GAPs closed", and `:149` marks 11.1 Complete.
- **Claim (b)**: `.planning/ROADMAP.md:53-57` describes Phase 11.1 as "remove ui components and focus on backend (remove tauri etc)", inserted after Phase 11.
- **Claim (c)**: CI builds and releases Tauri desktop bundles and both front ends — the opposite of (b).
- **Claim (d)**: `.planning/phases/12-.../12-VERIFICATION.md` itself records "step rows absent (confirmed gap)" while marking I4/I5 closed — consistent with finding #7.
- **Source**: `.planning/STATE.md:124,141-149,227`; `.planning/ROADMAP.md:35-57`; `.github/workflows/ci.yml:181-191`; phase 12 verification file.
- **Confidence**: High on the quoted text; the resolution (which document is current) is not determinable from the repo.
- **Why it matters here**: `.planning/` cannot be used as truth. Three artifacts disagree: STATE says gaps closed, ROADMAP says remove the UI, CI ships the UI. Any define-session must pick a direction explicitly rather than inherit one.

## Finding: 26. Simulation is always on in SiteServer, even in gRPC mode

- **Claim**: `AddHelixSimulation()` is called unconditionally at `Program.cs:27`, before `AddHelixAgentProxy` reads the mode. In grpc mode SiteServer therefore still registers a `SimulatedTrendProvider`, sim devices, and `SimCapabilityInvoker` alongside the real proxy.
- **Source**: `Helix.Orchestration.SiteServer/Program.cs:27,30`; `docker-compose.yml:108`.
- **Confidence**: High.
- **Why it matters here**: "Sim vs real" is not a switch but a superposition. This can mask real-device failures and is likely why several seams (#8, #10) went unnoticed — the sim path supplies plausible data locally.

---

## Apparent intent

Inference, marked as such. These are the goals the code seems built for, not confirmed requirements.

1. **A lab orchestration platform** where a WSL DSL is compiled to a node graph, executed by an agent attached to physical instruments, observed via live trends, and driven from a desktop operator console or web dashboard, with a recorder capturing runs. The presence of `SafetyEngine`, interlock rules, and `SafetyGuard` injection implies a regulated/physical-risk environment, not a toy.
2. **A deliberate move from monolith to split services** — Agent (execution edge) vs SiteServer (control plane) vs Recorder (capture) — with gRPC and MQTT as the seams. The two-engine situation (#1–#3) looks like a migration that stopped mid-flight: the durable Cleipnir design was adopted and wired, but the in-memory executor remained the only live path.
3. **"Backend-only" was, at some point, a real strategy** (#25b) — remove the UI and Tauri. The CI file and the large desktop/dashboard codebases suggest that decision was reversed or never executed. Which is current is a define-session question.
4. **Sim-first development** (#26) — build the whole control plane against simulation, then attach real devices. This explains why so many seams are plausible-looking but unexercised.
5. **The UI was built against a contract that existed in someone's head**, then partially corrected. `ExecutionPanel` matches the Agent; `HelixShell` does not. That is the signature of a fix applied to one component and not propagated.

---

## Open questions for define

Each maps to a finding; these are the decisions that cannot be settled from code.

1. **Which execution engine is canonical?** In-memory BFS (#1) or durable Cleipnir (#1, #3)? If durable, the missing `WorkflowRegistry` registration layer is real work, not a switch.
2. **Is SiteServer the submit authority?** If yes, `SubmitExperiment` must start the runspace (#2) and campaign iterations must submit real definitions (#11). If no, SiteServer's `/api/experiments` should be removed or marked read-only, and desktop should talk to the Agent directly.
3. **Is the desktop a shipping surface?** Ribbon is broken (#4), Tauri config is a stub (#17), and planning says remove it (#25). One of these must win.
4. **What is the MQTT topic contract?** 4-part Agent grammar or 7-part SiteServer/Recorder grammar (#8)? Whoever owns it must also own filtering and late-subscription (#10).
5. **Is safety in scope for this milestone?** If yes, interlocks must actually be registered and monitored (#12) and guards honoured by the executor (#13); that is a milestone, not a cleanup.
6. **Are experiments meant to persist?** If yes, `SaveAsync` needs a caller (#6), the stamping race must be fixed (#7), and `CompleteAsync` must be invoked (#7). Today the history feature is structurally empty.
7. **Which `.planning` artifact is authoritative?** STATE (#25a) and ROADMAP (#25b) contradict each other and the CI file. The define session should retire or reconcile them rather than treat either as truth.
8. **What is the supported run mode?** Compose-with-grpc (#19, #26) or local-sim? The current default (`sim` in appsettings, `grpc` in compose, simulation always on) is not a coherent answer.
9. **Is device support configurable?** The 644-line hardcoded registry (#14) implies no. If multi-device or multi-site is in scope, this is an architectural item.
10. **What counts as "done" for a test claim?** Given silent-skip integration tests and a 5× count discrepancy (#16), the project needs a stated definition of test evidence.

---

## Blocked areas

- **Build/test/run verification.** Blocked by constraint (no project commands). Consequence: every claim about compilation, test pass rate, runtime ordering, and gRPC/MQTT behaviour is `unverifiable`. To verify: `dotnet restore` + `dotnet build Helix.Orchestration.slnx`, `dotnet test Helix.Orchestration.slnx`, `npm ci && npm test -- --run` in `helix-desktop` and `helix-dashboard`, then a compose bring-up with an end-to-end submit → step-event → trend assertion. Restore reproducibility is itself doubtful (#15).
- **Exact runtime event ordering** for the step-stamping race (#7): static reading gives the mechanism; only a run confirms which events are lost.
- **Which `.planning` document is current** (#25): not determinable from the repo. Needs a human/define decision.
- **Historical intent** for the Cleipnir adoption and the "remove UI" reversal: absent from the repo (no `AGENTS.md`/`WORKFLOW.md`, template README, 6-line `CLAUDE.md`). Would need commit archaeology beyond `git log -1` or author input.
- **Working-tree cleanliness under MSYS2 git** (#21): not a repo problem — the tree is clean — but any tooling resolving `git` to MSYS2 2.34.1 reports phantom line-ending/BOM changes. Would need PATH to prepend `C:\Program Files\Git\cmd`, not a normalization commit.
- **Whether the Python adapter ever worked** (#18): `unverifiable`; it cannot import as checked in.
- **`.project/` contents beyond `STATE.md`**: no prior research or template artifacts existed before this run; only `STATE.md` plus the two artifacts this inspection produced.
