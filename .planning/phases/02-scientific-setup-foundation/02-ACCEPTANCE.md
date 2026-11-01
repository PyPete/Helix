# Phase 02 Acceptance Evidence

## Automated evidence

| Area | Evidence | Status |
| --- | --- | --- |
| Native Agent endpoint | The owned disposable Agent fixture binds `127.0.0.1:5080`; the native target remains fixed-loopback. | verified — Integration completed. |
| Authenticated SQL tracer | `ScientificEndToEndTests` exercises Bearer-authenticated apparatus creation, idempotent replay/conflict, restart readback, and zero device dispatch. | verified — reconciliation/security-audit workers use the separate owner connection while the runtime role remains restricted. |
| APP-04 foundation | Earlier 02-39–02-42 tests cover immutable policy content, trusted test-only approval, SQL freeze/reconstruction, and canonical binding; production authority remains unavailable. | verified foundation only — not production approval or campaign execution. |
| Native guard suite | `cargo test --manifest-path helix-desktop/src-tauri/Cargo.toml agent_security` runs separately; Integration executes exactly one explicitly ignored live fixture. | verified — 14 passed, 1 ignored live fixture. |
| Aggregate Agent suite | `dotnet test tests/Helix.Orchestration.Agent.Tests/Helix.Orchestration.Agent.Tests.csproj --no-restore` | verified — 65 passed. |
| Agent warning-as-error build | `dotnet build Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj --no-restore -warnaserror` | verified — 0 warnings, 0 errors. |
| C# formatting baseline | `dotnet format Helix.Orchestration.slnx --verify-no-changes --no-restore` | verified — clean after the repository formatter correction. |
| Desktop renderer tests | `npm --prefix helix-desktop test -- --run` | verified — 37 files, 155 tests passed. |
| Desktop production build | `npm --prefix helix-desktop run build` | verified — TypeScript and Vite production build completed. |

## Requirement and decision boundary

- APP-01, APP-02, APP-03, SAMP-01 and SAMP-02 have their prior focused Agent, SQL, and desktop evidence; this final report does not replace it with a green aggregate result.
- APP-04 proves the narrow test-approved immutable binding contract only. Production approval issuance/wiring and campaign start, resume, and recovery belong to Phase 3.
- D-01 through D-08 remain implemented by the completed setup, procedure/plan, sample custody, and transformation plans. No evidence here authorizes physical dispatch.
- The Phase 02.1 canvas/graph/cards/inspector comparison remains pending. Four unclassified edge rows and fourteen descriptor-less prohibition rows remain flagged, not silently resolved.

## Human-needed native and UI checks

These checks have not been performed in an authenticated installed Tauri application and are **not passed**: five entries and restored tabs; apparatus assertions, derived procedure and plan pins; immutable history; confirmation/custody/transformation/lineage; unavailable production authority; original-key uncertainty; 125–200% scaling; dock sizing; keyboard/focus/high contrast; long IDs; and representative/oversized native payloads.

## Automated acceptance result

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verification/phase02-e2e.ps1 -Mode Acceptance` completed end-to-end on 2026-11-01. It ran the owned authenticated Agent/isolated SQL scenario (3 tests and exactly one explicitly invoked ignored native fixture), the ordinary native guard suite (14 passed, 1 intentionally ignored live fixture), the aggregate Agent suite (65 passed), a warning-free Agent build, the clean formatting gate, desktop Vitest (37 files/155 tests), and the desktop production build. The known Vite large-chunk advisory is non-failing and does not change the checked exit.

This completes automated Phase 02 acceptance. It does not convert the unperformed authenticated installed-Tauri checks below into passed evidence.

## Phase 3 handoff

Phase 3 must wire a production approval authority and demonstrate approved campaign start/resume/recovery against the exact frozen binding. It must not treat test authority evidence, a policy-unavailable screen, or this report as production campaign approval.
