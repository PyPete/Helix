# Phase 01 — Walking Skeleton

## User story

**As a** scientist, **I want to** control simulated devices through the local Helix Agent with safety checks, pause/hold/abort controls, restart reconciliation, and an attributable command timeline, **so that** I can execute experiments safely without losing evidence or blindly repeating uncertain actions.

## Scope and approved exceptions

The skeleton is the permanent architecture, not a disposable experiment. Plan 01's FIRST task alone has the user-approved exception to the five-file task limit. Every expansion task has at most five exact modification paths. No exception applies to later tasks or plans.

No Site Server, apparatus modeling, analytical results, autonomous optimization, or real-device execution is introduced. Deterministic control does not need AI-SPEC.md. No package installation or package upgrades are planned.

## Architecture and responsibilities

| Layer | Permanent responsibility | Boundary |
|---|---|---|
| React desktop | Propose commands, approve explicit simulation limits, request controls, display committed evidence | Untrusted input; never owns device authority or credentials |
| Tauri native broker | Typed operation allowlist, fixed loopback WinHTTP, native-memory session, OS-user bootstrap | Main local webview only; no renderer-selected URL, headers, credential or actor |
| Agent | Authenticate every route, evaluate proposals, serialize admission, own dispatch/control/recovery | Sole mutation authority |
| Core | Immutable command contracts and deterministic admission evaluator | No host, database or driver composition |
| PostgreSQL | Session references, approved policy, immutable facts, durable coordination/cursor and simulator effect state | Parameterized SQL, least-privilege runtime role, no fact update/delete |
| Agent-owned SimulatorDevice | Apply parameter-aware deterministic effect and separately expose durable observations | Only authorized dispatcher can mutate |

Permanent path: existing HelixShell → agentClient → scoped Tauri commands → fixed 127.0.0.1:5080 Agent → admission/ledger → SimulatorDevice → durable observation/evidence → native read polling → timeline. Read polling uses the committed cursor; notifications are not authority.

## Transport and enrollment decision (planner discretion)

Use Windows WinHTTP through a small Rust FFI adapter to the maintained OS API, not custom HTTP framing. No Rust dependency or protobuf generation is added. Rust's currently declared tonic stack is not treated as a functioning generated client. C# gRPC remains an authenticated compatibility surface; unsafe raw definition submissions are explicitly rejected. WinHTTP requests disable proxy use and redirects and target exactly 127.0.0.1:5080. Enforce bounded bodies/timeouts and cancellation. Non-Windows builds return an explicit unsupported-platform error and cannot dispatch; wider platform support is not claimed.

Session bootstrap is NOT an unauthenticated Agent HTTP endpoint. Agent runs as the scientist's interactive Windows account at the same elevation level as desktop and exposes a local named pipe with PipeOptions.CurrentUserOnly, Asynchronous and FirstPipeInstance. It verifies WindowsIdentity/GetImpersonationUserName against the configured allowed SID before issuing a session. Reject remote pipe clients and mismatched identities; require a locally resolvable client process ID through the maintained Windows named-pipe API, not just matching a claimed SID. Native opens that pipe using its OS process token; a renderer-provided scientist name is display context, not identity. This validates OS-account identity, not resistance to malicious code already running as the same OS user. That residual limitation is disclosed, not silently expanded into a stronger identity claim.

Agent creates 256-bit random opaque sessions with framework RandomNumberGenerator, persists only SHA-256 hashes and actor/session references, and returns the credential through the user-restricted pipe to native memory only. Session lifetime is 15 minutes, at most four active sessions per SID. Renewal is through the same OS-authenticated pipe, never per-command approval. Logout/revocation expires server state; Agent startup revokes old sessions. Native refreshes session after restart, but never resubmits uncertain mutations. Session tokens never enter renderer storage, logs, URLs, evidence, or configuration files.

This is a costly discretion choice; no new one-way decision is introduced. Running Agent as LocalSystem or a different account fails enrollment. Existing service registrations must be inspected and changed explicitly; source edits do not prove installed binaries/configuration changed.

## Execution contracts this phase produces

All new names below are planner-owned contracts, not assertions about existing exports.

`Helix.Orchestration.Core/Execution/CommandIntent.cs` defines immutable CommandIntent, CommandEvidence, SimulationPolicy, ExecutionControlState and cancellable ICommandLedger, ICommandDispatcher, ISimulatorObservation ports. Use stable GUID command/execution/run/step/correlation/evidence IDs, canonical parameter JSON and units, verified actor SID/display name/session reference, approved policy revision, definition revision/hash, expected postcondition, check evidence, context/control/lease revisions, reservation/debit references, occurred-at UTC and canonical per-execution sequence. Legacy records lacking actor/correlation stay read-only and explicitly Not recorded.

ICommandDispatcher exposes ProposeAsync and DispatchAsync with trusted actor supplied by Agent authentication, never the body. ProposeAsync persists proposal/check feedback and returns Allowed, Rejected or Held with stable codes. DispatchAsync repeats checks using fresh context and serialized reservations. Invalid canonical identity reuse returns conflict, identical reuse returns recorded status without a second effect.

ICommandLedger owns atomic admission/intent commits, append evidence, acquire/release reservations, durable control revision, dispatch-start fence, outcome commits, cursor updates and paged history. All canonical stream append operations serialize an execution stream row and assign sequence in the same transaction. GUID or global sequence allocation alone is not commit ordering.

SimulationPolicy must be approved by the validated scientist and persisted before any dispatch. Offer concrete editable simulation defaults: allowed SetFlow/GetFlow, SetTemperature/GetTemperature, SetMode/GetMode on registered simulator IDs; SetFlow 0–10 mL/min in 0.1 steps, SetTemperature 20–80 °C in 1 °C steps, SetMode Isocratic/Gradient/Purge; one reservation per simulator, exclusive device lease 30 seconds renewed by Agent, 100 mutation commands, 10-minute execution wall-time limit, 2-second safety/health freshness and 2-second observation deadline. Approval freezes all values and version. Missing/unapproved/expired/unknown limits never mean unlimited. These simulation-only defaults are shown for scientist approval; they are not undisclosed campaign authoring requirements.

Canonical capability vocabulary is exactly SetFlow/GetFlow (flowRate, mL/min), SetTemperature/GetTemperature (temperature, °C) and SetMode/GetMode (mode enum). Bridge alone adds/removes the registered device namespace; case and parameter keys remain canonical across discovery, policy, compiler, native DTO and simulator. Existing SimulatorDevice advertises SetPumpMode and lacks GetTemperature/GetMode. Plan 03-02 deliberately replaces both SetPumpMode registration and rich metadata with SetMode, adds both getters and their read-only metadata, and excludes Start/Stop from this approved command policy. No executable legacy alias is retained. Historical legacy facts remain immutable; old stored proposals get an explicit unsupported-name diagnostic directing reauthoring/reapproval, never silent identity/hash rewriting. Plan 03 discovery→policy→real dispatcher tests prove every canonical setter/getter mapping; Plan 10 compiler and Plan 08 actual native acceptance use the same vocabulary.

Admission checks: capability, safety, lease, resource, budget, termination, plus device health. Each emits Passed/Failed/Unknown with check code, reason, timestamp and context revision. Ordinary missing/disallowed/malformed/out-of-range/nonfinite/wrong-unit/wrong-step capability parameters reject (D-04). Known invalid resources/exhausted budget reject. Unsafe/unknown safety, lease ownership or health hold (D-05). Unknown resources/accounting also hold. Paused, held, aborted, expired policy or closed termination fence never dispatch. All checks run at proposal/compile and immediately before effect (D-06). Compile feedback is not an authorization token.

Controls are durable desired state Running/Paused/Held/Aborted plus separate hold reasons and cursor. Hold reasons include scientist, safety, health, lease, reconciliation and accounting; clearing one never clears others. Pause blocks future dispatch at a safe boundary. Abort is terminal and persisted before cancellation. A cancellation acknowledgement is not proof that an in-flight action had no effect.

Serialize final dispatch-start and control acknowledgement under the same per-execution gate and database fence. Recheck fresh safety/health and lease generation at the final boundary. A stop committed before dispatch-start wins; a stop arriving after dispatch-start is recorded as in-flight and blocks subsequent commands. Transactional reservations and revision checks prevent competing instances consuming one lease/capacity/budget. Use a single active Agent owner lock; fail startup/dispatch if ownership is lost. Do not hold a SQL transaction across an external effect.

## Effect and recovery invariant

Simulator state persists independently of Agent memory with simulator generation, canonical value/units, last applied command/effect ID and observation timestamp. A setter actually updates that state; read observations never invoke arbitrary capabilities. Exact normalized decimals/enums match; no guessed floating tolerance. Same value from another command is not a matching effect.

Commit immutable intent/checks/reservations before effect, then separate immutable dispatch-start and observation/outcome evidence. On restart, intent without trustworthy outcome creates Held pending reconciliation before dispatch is possible (D-07). Agent queries the stored simulator observation (D-08). Only matching command ID, generation, expected value/units and fresh readable state confirms that effect, advances its durable cursor and clears its reconciliation reason. Append a prominent recovery event and resume only if no independent hold/pause/abort exists. Mismatch/unreadable state holds for scientist review and cannot auto-retry (D-09). Unknown commands never replay. Outcome committed before lost acknowledgement remains idempotent. Charge an uncertain attempt conservatively until reviewed; release exclusive resource/lease only under durable disposition rules, never to bypass unresolved effect.

## Evidence and UI invariant

Durable history includes proposals, checks, rejection, hold, dispatch, observation, recovery, controls, abort, denied-access attempts and append-only annotations. Facts cannot be edited/deleted; Add review note is a distinct attributed event and does not release a hold (D-10–D-12). Sequence is chronological authority; timestamps remain displayed facts. Provide independent selection of completed executions and a manual/security stream. A denied attempt has claimed identity separate from verified actor.

Denied-attempt outage contract (D-03/D-12): DesktopSessionService owns an OS-user-protected append-only spool with 64 MiB active capacity, maximum 8 KiB redacted record and at most 32 admitted requests in flight. Each accepted request reserves a record slot before authentication/body parsing, whether SQL is currently healthy or not; one separate preallocated shutdown-marker slot is reserved. Each denial has stable attempt/evidence ID, occurred-at UTC, Agent generation, local spool ordinal/source ID, bounded route/method/reason and claimed-versus-verified identity, never a token/body. Flush the immutable record durably before responding when SQL is unavailable. No ring overwrite, record eviction or secret logging is allowed. Capacity reservation exhaustion quiesces API listeners before admitting further requests, finishes already-reserved records and records the saturation marker; further connections are refused, not accepted unaudited API attempts. Writable protected disk and enough reserved capacity for admitted requests are startup/runtime preconditions; unexpected disk persistence failure fail-stops acceptance and is reported unverified, never called a successfully audited denial. New dispatch is blocked throughout SQL outage/saturation.

Plan 14 implements idempotent recovery in the session service, driven by SessionBootstrapWorker before serving/reopening and periodically with cancellation/bounded backoff: ingest original spool records through ICommandLedger append by stable evidence ID/content hash, preserve original occurrence/identity/ordinal/source provenance, assign canonical security-stream commit sequence, and append a recovery event. Retrying after SQL commit but before local acknowledgement cannot duplicate facts; changed content under the same ID is a blocking integrity fault. Write durable local acknowledgement only after SQL commit. Retain immutable originals until acknowledged checksum/ID proof; rotate acknowledged segments to protected archive, never discard unacknowledged records. Reclaim active capacity only after that proof. An interrupted drain resumes from unacknowledged IDs. Reopen listeners only after successful drain, safe reserved headroom and database/ownership checks; do not auto-resume held commands. Plan 07 promised security history reads ingested committed evidence plus preserved provenance through ICommandLedger; SQL outage is explicit unavailable history, not an empty or falsely complete response.

UI-SPEC is binding: Start simulation; No command evidence yet with explanatory copy; specified session/rejection/uncertain-effect text and Abort simulation confirmation. Session/verified scientist visible; no retry of uncertain effects. Six required checks visibly Passed/Failed/Unknown with health alongside. Runtime results are authoritative. Critical events have text plus semantic icons/alerts and persistent emphasis; routine success is quiet (D-13). Native/session/loading failures disable dispatch without falsely completing controls.

All 38 explicit UI considerations are must-haves: empty/no interlocks never implies safety; loading labels per surface; readable errors; populated and partial evidence; bounded overflow with visible critical status; zero/one/many semantics; wrapping long text and selectable monospace identifiers. Preserve hand-authored dark CSS, Inter/Segoe UI, four sizes 11/13/14/20, weights 400/600, spacing 4/8/16/24/32/48/64 and existing semantic colors. No component/registry packages.

## Deployment and schema

Supported skeleton deployment is coherent native desktop + simulator-only Agent on the same Windows user and host, with PostgreSQL reachable locally. Agent validates effective listeners, rejects overrides to wildcard/nonloopback addresses, and does not expose anonymous health/stream routes. Docker compose must not start/publish a network Agent in this profile; it may provide PostgreSQL. Systemd path is explicitly unsupported by this Windows-native profile, not falsely authenticated. Document this in existing launch artifacts without changing Site Server architecture.

Apply 003_agent_command_ledger.sql and additive 004_agent_ledger_constraints.sql with an explicit versioned migration runner to both new and existing databases. Never change an already-applied migration checksum. Program's migration-only startup entry applies/checks schema without exposing routes or dispatch. Fail on missing migration, insufficient privileges, unavailable database or incompatible schema; never silently return from required tests. Runtime role cannot update/delete canonical facts; coordinator/session tables have narrowly scoped update rights. Preserve preexisting experiment evidence as legacy history without inventing actor/correlation values. No ORM push pattern applies to Npgsql/Dapper, but real migration application is still an acceptance gate.

## Dependency map

| Plan | Needs | User-visible expansion | Wave |
|---|---|---|---|
| 01 | Existing desktop/Agent and real PostgreSQL | Real SetFlow round trip with session, checks, immutable evidence and restart observation | 1 |
| 02 | 01 contracts | Full parameter/policy checks and fresh safety/E-stop | 2 |
| 03 | 01 ledger/effect | Durable pause/hold/abort and restart reconciliation matrix | 2 |
| 05 | 01 native client | Complete native operations, local settings/session and experiment hook | 2 |
| 09 | 02,03 | Contending reservations and immutable/idempotent PostgreSQL proof | 3 |
| 10 | 02,03 | Canonical read-only variables and non-authorizing compile feedback | 3 |
| 12 | 05 | Health/event reconnect and all actual discovery/palette native callers | 3 |
| 04 | 10,03 | Actual nonempty REST/gRPC canonical submissions and durable lifecycle | 4 |
| 14 | 09,01 | Bounded fail-closed audit capacity and SQL-outage denial recovery | 4 |
| 06 | 10,03,04,05,12 | Usable submission after actual endpoint producer, approved policy and controls UI | 5 |
| 07 | 10,03,05,12,14 | Immutable history/note/safety endpoints and mounted native timeline cursor | 5 |
| 11 | 04,10,03 | Actual host/alternative executor closure and evidence publication ordering | 5 |
| 13 | 07,06 | Complete outcome-first timeline, notes and explicit safety/evidence UI states | 6 |
| 08 | 11,06,13,09,12,14 | Coherent deployment/schema and full actual Tauri/SQL restart acceptance | 7 |

Same-wave plans have disjoint modification paths; production ownership conflicts are intentionally sequenced. Plans consume tested interfaces, not reflexively chained summaries.

Revision 1 preserves the single approved tracer and splits only oversized refinements: 14 plans, 28 tasks, seven waves; each expansion manifest is at most nine files and each expansion task at most five. Calibration queried again: factor 1, zero samples, low confidence. Added boundaries carry honest per-plan reading/test costs; estimates are not merely redistributed original totals. Threats T-01-07/13/14/16/24 move with their owning behavior and do not duplicate; new T-01-33–36 cover refinement/recovery concerns.

## Validation strategy and known limits

Level-1 verification for the selected OS transport: official Microsoft WinHTTP Sessions Overview documents the maintained Open/Connect/OpenRequest/SendRequest/ReceiveResponse/QueryHeaders/ReadData/CloseHandle lifecycle, and .NET 10 PipeOptions documentation confirms CurrentUserOnly checks same account AND elevation on Windows. Sources: https://learn.microsoft.com/en-us/windows/win32/winhttp/winhttp-sessions-overview and https://learn.microsoft.com/en-us/dotnet/api/system.io.pipes.pipeoptions?view=net-10.0. Context7/ctx7 was unavailable in research; official-document fallback is cited, not proof of working Rust FFI/native builds. Use owned RAII handles, correct Win32 ABI/types, bounded reads/timeouts and safe cleanup; OS HTTP implementation supplies framing. Do not copy the documentation's external-server/default-proxy example into the fixed no-proxy/no-redirect broker.

First tracer starts with failing real-stack smoke and complementary focused CommandAdmissionTests/agent_security native cases created inside the same first task. Create scripts/verification/phase01-smoke.ps1 and native broker smoke entry. Launch real Agent child, built Tauri main app and PostgreSQL; verify main-webview action→actual IPC→session→approve policy→SetFlow→durable observation→rendered timeline, restart and read same evidence. Mocks, standalone broker or TestServer alone cannot pass tracer. Warm focused feedback targets under 30 seconds (measure, not claimed), cold native builds/SQL smoke measured separately. Infrastructure.csproj adds inward Core ProjectReference in tracer before building both persistence implementations. All filters require nonzero tests; these are execution gates, not current passes.

Real-Tauri smoke implementation contract: use existing `npm --prefix helix-desktop run tauri -- build --debug` and the built Windows main binary, not a separate test implementation. A test-only child-process environment may enable WebView2 remote debugging on a dynamically reserved loopback port, through WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS; never change release configuration to enable it. PowerShell uses .NET HttpClient and ClientWebSocket with the maintained Chromium DevTools JSON protocol to attach only to this child webview, inspect DOM, activate visible controls and assert rendered timeline/verified session/unknown hold. No new test package, custom HTTP framing or fake invoke transport. Keep credentials native-only even during testing; read only display DTOs/DOM and SQL command identities. Remove the debug environment after child cleanup; production launch runbook forbids remote-debug flags. The same native module's standalone smoke provides additional diagnostics and direct wrong-scope/target/error tests, but cannot substitute for the actual Tauri UI round trip. Missing WebView2/native tooling fails visibly.

Plan 01 Program composition also provides `--migrate-only`: apply/check numbered migration versions using existing Npgsql ledger migration code, verify runtime grants, then exit without creating listeners, sessions, workers or dispatch. Plan 08's PowerShell migration gate calls this actual entry, so no nonexistent ORM CLI is assumed.

Docker daemon was unavailable during research. Supply PGHOST/PGPORT/PGDATABASE/PGUSER/PGPASSWORD for a disposable real database or start Docker Desktop. Provision .NET 10-compatible SDK, Windows native build tools and existing Rust/npm dependencies. No test may accept missing database/native binary as green. Database/process smoke can exceed 60 seconds; record measured duration, use unit/native/renderer focused suites for quick feedback.

## Assumptions and prohibition recall

Three source edge assumptions SAFE-01/SAFE-02/SAFE-03 are unclassified and unresolved; retain them in the audit as flagged-unverified. They are not converted to implicit coverage or backstops. Classified SAFE-04 adjacency, empty and ordering are implemented explicitly: intent/checks before effect and outcome before notification; no command evidence empty state; per-execution commit sequence and stable deduplication. These do not resolve the three unclassified assumptions.

Descriptor-less project prohibitions must be recalled through the projectProhibitions contract. If contract is unavailable or returns unresolved entries, preserve flagged-unverified and report to orchestrator; never auto-dismiss. Project-local explicit prohibitions remain binding: sole Agent authority, deterministic gates, no blind replay, immutable evidence, native-only credentials, simulated-only execution and no scope expansion. All code symbols in this skeleton are proposed unless PATTERNS.md identifies an existing symbol.

## Acceptance and handoff

All SAFE-01–SAFE-04 and D-01–D-13 are traced in SOURCE-AUDIT.md. ASVS1 threats with high/critical severity block acceptance until mitigation tests pass. Native/build/database and deployed-runtime checks are execution gates, not planning claims. Plans and skeleton are uncommitted planning output; orchestrator owns checker, state transitions and commit. Do not edit STATE.md or application sources during planning.
