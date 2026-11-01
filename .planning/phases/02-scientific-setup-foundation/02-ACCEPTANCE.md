# Phase 02 Acceptance Evidence

## Automated evidence

| Area | Evidence | Status |
| --- | --- | --- |
| Native Agent endpoint | The owned disposable Agent fixture binds `127.0.0.1:5080`; the native target remains fixed-loopback. | verified — Integration completed. |
| Authenticated SQL tracer | `ScientificEndToEndTests` exercises Bearer-authenticated apparatus creation, idempotent replay/conflict, restart readback, and zero device dispatch. | verified — reconciliation/security-audit workers use the separate owner connection while the runtime role remains restricted. |
| APP-04 foundation | Earlier 02-39–02-42 tests cover immutable policy content, trusted test-only approval, SQL freeze/reconstruction, and canonical binding; production authority remains unavailable. | verified foundation only — not production approval or campaign execution. |
| Native guard suite | `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` runs separately; Integration executes exactly one explicitly ignored live fixture. | verified in Integration. |

## Requirement and decision boundary

- APP-01, APP-02, APP-03, SAMP-01 and SAMP-02 have their prior focused Agent, SQL, and desktop evidence; this final report does not replace it with a green aggregate result.
- APP-04 proves the narrow test-approved immutable binding contract only. Production approval issuance/wiring and campaign start, resume, and recovery belong to Phase 3.
- D-01 through D-08 remain implemented by the completed setup, procedure/plan, sample custody, and transformation plans. No evidence here authorizes physical dispatch.
- The Phase 02.1 canvas/graph/cards/inspector comparison remains pending. Four unclassified edge rows and fourteen descriptor-less prohibition rows remain flagged, not silently resolved.

## Human-needed native and UI checks

These checks have not been performed in an authenticated installed Tauri application and are **not passed**: five entries and restored tabs; apparatus assertions, derived procedure and plan pins; immutable history; confirmation/custody/transformation/lineage; unavailable production authority; original-key uncertainty; 125–200% scaling; dock sizing; keyboard/focus/high contrast; long IDs; and representative/oversized native payloads.

## Blocking verification result

Integration now passes. Acceptance remains blocked: the Agent suite executes 65 tests, with 61 passing and two `DurableRunspaceTests` failing because they expect an intentionally disabled legacy execution path. The full suite's two scientific endpoint DI failures were fixed after that run and their focused tests pass. Do not mark Phase 02 accepted until the owning safety/runspace work updates or retires the stale legacy expectations and Acceptance passes end-to-end.

## Phase 3 handoff

Phase 3 must wire a production approval authority and demonstrate approved campaign start/resume/recovery against the exact frozen binding. It must not treat test authority evidence, a policy-unavailable screen, or this report as production campaign approval.
