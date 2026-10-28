# Synthesis

## Settled

- Primary operator surface â€” build the local `helix-desktop` Tauri/React client; the web dashboard is parked, not a co-target (CHARTER.md Â§Full scope: in/out; evidence-codebase.md Â§Finding: The repository contains two independent frontends with different stacks).
- Execution authority â€” all physical commands remain on the Agent capability, lease, validation, safety, and audit path (CHARTER.md Â§Constraints).
- Program delivery shape â€” pair usable desktop redesign work with core scientific functionality in vertical slices (CHARTER.md Â§Constraints; CHARTER.md Â§Corrections).
- Scientific traceability â€” version and retain definitions, executions, methods, mappings, result revisions, calculations, decisions, and approvals that affect conclusions or physical work (CHARTER.md Â§Constraints).
- Apparatus truth model â€” keep apparatus design, physical verification, and live observations distinct; a diagram proves none of the latter (CHARTER.md Â§Full scope: out (vetoes); evidence-similar.md Â§Finding: Comparator lesson â€” Opentrons explicitly separates configured fixtures from physical verification).
- Documentation ruling â€” treat current runtime/code as authoritative and update conflicting documentation unless an approved milestone deliberately changes the contract (CHARTER.md Â§Constraints).
- Existing desktop substrate â€” retain HelixShell, Agent connection/status surfaces, rc-dock layout, and contextual device/trend/workflow/execution/safety/log modules as reusable infrastructure, not as the organising campaign UX (evidence-similar.md Â§Finding: The desktop already has a local operator shell with connection and status surfaces; Â§Finding: The dock workspace supports reusable panels and persisted layouts; Â§Finding: Existing workspace modules cover device inspection, trends, workflow authoring, execution, safety, and logs).

## Decisions

### Durable activity orchestration and external-result waits

- **Decision**: Extend and validate the existing Agent/Cleipnir PostgreSQL path: model activities as registered durable actions, use persisted stable campaign/activity/flow IDs, and wait for correlated external-result messages rather than introduce another orchestration engine.
- **Runner-up**: Temporal .NET â€” it supports durable waits but loses because it adds a service/history/worker deployment and migration beyond the charterâ€™s existing Agent/PostgreSQL/Cleipnir baseline.
- **Evidence**: evidence-stack.md Â§Finding: Cleipnir provides existing primitives for durable activities, message waits, and effect boundaries; Â§Finding: Helix has Cleipnir-backed execution components, but runtime wiring and runspace recovery are not yet equivalent to end-to-end durability; Â§Finding: Temporal .NET covers durable workflows and asynchronous external completion but carries a larger migration footprint; CHARTER.md Â§Constraints.
- **Confidence**: high

### Physical-effect idempotency and restart recovery

- **Decision**: Use stable logical activity and command deduplication keys with Cleipnir effect boundaries, and on recovery require the Agent to reacquire leases and revalidate capability/safety before any subsequent command; reconcile uncertain physical outcomes explicitly rather than claim exactly-once execution.
- **Runner-up**: Treat durable flow records/`AtMostOnce` effects as proof of exactly-once device execution â€” it loses because no atomic boundary with an instrument is evidenced and leases/auxiliary state are in memory.
- **Evidence**: evidence-stack.md Â§Finding: Effect capture and stable invocation IDs help prevent duplicate effects but do not guarantee exactly-once physical execution; Â§Finding: In-memory leases and auxiliary state remain independent safety and recovery gaps; evidence-codebase.md Â§Finding: Durable execution is mixed with explicitly in-memory runtime state.
- **Confidence**: high

### Sample identity and analytical-result import

- **Decision**: Issue a stable, scannable sample accession at confirmed collection; retain descriptive metadata, campaign/run lineage, and external references in linked records. Import results through versioned source-specific mappings that validate required sample linkage, result/method fields, and units while preserving source provenance.
- **Runner-up**: A universal label payload and CSV schema â€” it loses because representative evidence supports stable identity with linked metadata and finds no cross-instrument CSV contract.
- **Evidence**: evidence-domain.md Â§Finding: Sample identity belongs on the specimen; descriptive metadata and lineage belong in linked records; Â§Finding: No universal analytical-result CSV column contract was found.
- **Confidence**: medium

### Result review, calculation, and artifact evidence

- **Decision**: Persist an arriving analytical result as an immutable, provenance-bearing revision in an unreviewed state; require approval of that exact revision before it drives calculations or decisions. Preserve versioned calculation definition/implementation, inputs, units/factors, output, and validation/test evidence separately from analytical-method validation; retain identifiable raw artifacts for accurate, ready retrieval.
- **Runner-up**: Treat receipt as approval and retain only derived values â€” it loses because review is distinct from receipt and replay/review requires source data, calculation context, and provenance.
- **Evidence**: evidence-domain.md Â§Finding: Result receipt and result review are distinct states in a representative regulated workflow; Â§Finding: Calculation and analytical-method validation are evidenced separately; Â§Finding: Retain identifiable source analytical records and make them accurately retrievable; CHARTER.md Â§Full scope: in.
- **Confidence**: high

### Campaign-centred desktop information architecture

- **Decision**: Organise the desktop around apparatus and campaign contexts spanning Setup, Experiment, Plan, Run, Results, and Samples & Analysis; reuse existing panels contextually, but implement a durable campaign history rather than extending the active-experiment timeline as if it already covered the loop.
- **Runner-up**: Keep the current dock/timeline as the primary information architecture â€” it loses because the timeline only represents one active experiment and lacks samples, external waits, revisions, approvals, calculations, and decisions.
- **Evidence**: evidence-similar.md Â§Finding: The current experiment timeline is an active-experiment event list, not the charter's full campaign history; Â§Finding: Existing workspace modules cover device inspection, trends, workflow authoring, execution, safety, and logs; CHARTER.md Â§Full scope: in; CHARTER.md Â§Constraints.
- **Confidence**: high

### Apparatus-editor path and commercial configuration

- **Decision**: Use the in-tree React Flow canvas only as a wave-1 apparatus-editor candidate behind explicit performance, accessibility, real-canvas test, package/runtime, and commercial gates; keep attribution visible unless React Flow Pro authorization is obtained. If it fails a gate, pause the direction; resume or change it only with demonstrated equivalent-or-better operator quality, accessibility, commercial suitability, packaging/CI viability, and explicit approval.
- **Runner-up**: Commit immediately to a new editor or hide React Flow attribution without authorization â€” both lose because a replacement would be speculative while the latter has no evidenced commercial authorization.
- **Evidence**: evidence-pitfalls.md Â§Finding: The settled codebase evidence does not identify a preferred apparatus editor; Â§Finding: React Flow is a plausible in-tree canvas candidate, but there is no realistic apparatus performance evidence; Â§Finding: React Flow has keyboard and screen-reader support, but its availability is not an accessibility audit; Â§Finding: The current canvas unit tests replace React Flow, so they do not validate its real interactions; Â§Finding: Tauri packaging and three-platform build coverage exist, but release signing and target-machine behavior remain separate checks; Â§Finding: React Flowâ€™s core MIT license permits commercial use, but hiding its attribution is a paid Pro condition; CHARTER.md Â§Full scope: out (vetoes).
- **Confidence**: medium

## For the planner

- **Wave-1 blockers**: Prove the Cleipnir/Agent submission and recovery path end-to-end with stable IDs, correlated duplicate/out-of-order result ingress, restart recovery, lease reacquisition, and safety revalidation; define artifact-store topology/retention and result-ingress contract; run realistic React Flow apparatus benchmarks, assistive-technology and keyboard/contrast checks, browser-level interaction tests, packaged target-machine/offline checks, and resolve attribution/Pro authorization; replace DeviceExplorerâ€™s synthetic â€œhealthyâ€ state with an observed-status projection. (evidence-stack.md Â§Finding: Helix has Cleipnir-backed execution components, but runtime wiring and runspace recovery are not yet equivalent to end-to-end durability; Â§Finding: Cleipnir message waits can pause for an external result, but Helix result correlation and ingress guarantees remain unresolved; evidence-domain.md Â§Finding: Retain identifiable source analytical records and make them accurately retrievable; evidence-pitfalls.md Â§Findings: React Flow is a plausible in-tree canvas candidate, but there is no realistic apparatus performance evidence; React Flow has keyboard and screen-reader support, but its availability is not an accessibility audit; Tauri packaging and three-platform build coverage exist, but release signing and target-machine behavior remain separate checks; evidence-similar.md Â§Finding: DeviceExplorer marks every listed adapter healthy without checking returned health data.)
- **Walking skeleton**: In the desktop, configure simulated two pumps, mixer, reactor, and sampling point; bind distinct pumps with design/verification/live-state separation; execute one Agent-authorised reaction; complete a manual collection attestation and issue a scannable sample ID; durably await then import and approve one correlated HPLC revision with raw provenance; calculate a defined metric; record/approve a decision and validate then execute one proposed next experiment; close/restart the UI or Agent during the wait and replay the entire campaign without duplicate physical action or retained lease. (CHARTER.md Â§Program success criteria; evidence-stack.md Â§Finding: Effect capture and stable invocation IDs help prevent duplicate effects but do not guarantee exactly-once physical execution.)
- **Pitfalls → tasks**: In-memory runspace/lease/global-state/trend services â†’ recovery-and-safety integration task; generic message wait without Helix correlation/deduplication â†’ result-ingress and revision-contract task; Recorder `DropOldest` buffering â†’ explicitly specify campaign-event durability/backpressure/observability task; mocked React Flow tests â†’ real-canvas browser and accessibility E2E task; unbenchmarked custom canvas/live state â†’ representative graph/update performance task; unsign/notarized or runtime-variable packages â†’ release validation task; `hideAttribution` without Pro evidence â†’ commercial configuration task; DeviceExplorerâ€™s assumed health â†’ live-status projection task; active-experiment-only timeline â†’ durable campaign-history task. (evidence-stack.md Â§Findings: Helix has Cleipnir-backed execution components, but runtime wiring and runspace recovery are not yet equivalent to end-to-end durability; Cleipnir message waits can pause for an external result, but Helix result correlation and ingress guarantees remain unresolved; evidence-codebase.md Â§Finding: Recorder buffering can discard old events without the shown warning path; evidence-pitfalls.md Â§Findings: The current canvas unit tests replace React Flow, so they do not validate its real interactions; React Flow is a plausible in-tree canvas candidate, but there is no realistic apparatus performance evidence; Tauri packaging and three-platform build coverage exist, but release signing and target-machine behavior remain separate checks; React Flowâ€™s core MIT license permits commercial use, but hiding its attribution is a paid Pro condition; evidence-similar.md Â§Findings: DeviceExplorer marks every listed adapter healthy without checking returned health data; The current experiment timeline is an active-experiment event list, not the charter's full campaign history.)

## User rulings

- The owner ruled that, if the apparatus-editor candidate fails its quality, accessibility, or commercial gate, the direction remains paused until a resume or replacement demonstrates equivalent-or-better operator quality, accessibility, commercial suitability, packaging/CI viability, and receives explicit approval. (Owner ruling, 2026-10-27; CHARTER.md open questions; CHARTER.md vetoes; evidence-pitfalls.md assigned-question answer.)

## Still unknown

- Exact apparatus-editor acceptance thresholds (representative graph size/update rate, accessibility standard/test matrix, target machines, signing expectation, and budget/authorization for Pro) are wave-1 gates to define and validate before committing to the editor. (evidence-pitfalls.md assigned-question answer.)
- Artifact storage topology, retention duration, and durable analytical-result ingress/correlation semantics â€” wave-1 spike and contract; representative evidence requires retrievability/provenance but does not settle topology or Helix-specific ingress. (evidence-domain.md Â§Finding: Retain identifiable source analytical records and make them accurately retrievable; evidence-stack.md Â§Finding: Cleipnir message waits can pause for an external result, but Helix result correlation and ingress guarantees remain unresolved.)
