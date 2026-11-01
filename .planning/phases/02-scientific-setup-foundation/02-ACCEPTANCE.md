# Phase 02 Acceptance Evidence

## Automated evidence

| Area | Evidence | Status |
| --- | --- | --- |
| Native Agent endpoint | The owned disposable Agent fixture binds `127.0.0.1:5080`; the native target remains fixed-loopback. | partial — focused endpoint assertion passed before the full restart scenario exposed its runtime-role failure. |
| Authenticated SQL tracer | `ScientificEndToEndTests` exercises Bearer-authenticated apparatus creation, idempotent replay/conflict, restart readback, and zero device dispatch. | failed — the constrained runtime role cannot read `command_dispatches`, causing the reconciliation worker to stop the host and the next authenticated request to return 503. |
| APP-04 foundation | Earlier 02-39–02-42 tests cover immutable policy content, trusted test-only approval, SQL freeze/reconstruction, and canonical binding; production authority remains unavailable. | verified foundation only — not production approval or campaign execution. |
| Native guard suite | `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` remains required separately from live evidence. | pending rerun after the Agent host failure is corrected. |

## Requirement and decision boundary

- APP-01, APP-02, APP-03, SAMP-01 and SAMP-02 have their prior focused Agent, SQL, and desktop evidence; this final report does not replace it with a green aggregate result.
- APP-04 proves the narrow test-approved immutable binding contract only. Production approval issuance/wiring and campaign start, resume, and recovery belong to Phase 3.
- D-01 through D-08 remain implemented by the completed setup, procedure/plan, sample custody, and transformation plans. No evidence here authorizes physical dispatch.
- The Phase 02.1 canvas/graph/cards/inspector comparison remains pending. Four unclassified edge rows and fourteen descriptor-less prohibition rows remain flagged, not silently resolved.

## Human-needed native and UI checks

These checks have not been performed in an authenticated installed Tauri application and are **not passed**: five entries and restored tabs; apparatus assertions, derived procedure and plan pins; immutable history; confirmation/custody/transformation/lineage; unavailable production authority; original-key uncertainty; 125–200% scaling; dock sizing; keyboard/focus/high contrast; long IDs; and representative/oversized native payloads.

## Blocking verification result

`powershell -NoProfile -File scripts/verification/phase02-e2e.ps1 -Mode Integration` currently fails. The failure is deliberate evidence, not a waiver: the test runtime role is denied access to `command_dispatches` while the production reconciliation worker starts. The worker stops the owned host, so the post-restart authenticated setup request returns HTTP 503. Do not mark Phase 02 accepted until the runtime role/reconciliation composition boundary is resolved and Integration and Acceptance both pass.

## Phase 3 handoff

Phase 3 must wire a production approval authority and demonstrate approved campaign start/resume/recovery against the exact frozen binding. It must not treat test authority evidence, a policy-unavailable screen, or this report as production campaign approval.
