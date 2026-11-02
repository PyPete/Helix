# Phase 3 Source Coverage Audit

SOURCE | ID | Feature/Requirement | Plan | Status | Notes
--- | --- | --- | --- | --- | ---
GOAL | — | Original proposal-only board, clean-room provenance, retained comparator, and post-UAT scientist gate | 03-01..05 | COVERED | Promotion remains a future human disposition; this phase makes no primary-selection change.
REQ | UX-01 | Six scientific status/provenance views let a scientist understand current status and provenance | 03-01..05 | COVERED | Exact evidence or explicit unavailable/read-only state is required for every view.
RESEARCH | Live seam | HelixShell→WorkspaceLayout supports health, existing stream values, and known-ID evidence reads only | 03-01, 03-02 | COVERED | Health is explicitly threaded; stream/local state is source-labelled; no new backend/domain contract.
RESEARCH | Health | `AgentHealthState` exact healthy is only a native eligibility observation, never a complete reconciliation outcome | 03-01 | COVERED | Positive health with absent lifecycle/evidence remains read-only.
RESEARCH | Local lifecycle/selection | `selectedExecutionId` and `durableLifecycle` are renderer/event-derived presentation, not Agent authority | 03-02 | COVERED | Tests require labels and read-only behavior.
RESEARCH | Stream | Disconnected, partial, errored, empty, or unmatched command stream is unavailable evidence | 03-02 | COVERED | Tests cover each fail-closed branch.
RESEARCH | Operation | No shell-wide retained operation exists; only original-operation reconciliation can use existing hook | 03-02 | COVERED | No global last-operation state is created.
RESEARCH | Command evidence | Use existing known-ID cursor evidence without deriving lifecycle, approval, or missing scientific record | 03-02 | COVERED | Ordered immutable evidence is preserved; absent/partial remains read-only.
RESEARCH | Unsupported records | No shared six-view record, reviewed result, capability metadata beyond descriptor, or resource/lease diagnostics | 03-02, 03-03 | COVERED | Named unavailable variants; never synthetic IDs/statuses or Phase 2 changes.
RESEARCH | No dependencies | Reuse React, rc-dock, Vitest, Testing Library, Tauri, and existing native hooks | 03-01..05 | COVERED | No package-manager task.
RESEARCH | UAT | Installed Tauri plus real Agent records and future explicit scientist disposition are mandatory | 03-05 | COVERED | Automated suite cannot close the gate.
CONTEXT | D-01/D-04 | One original workflow board; no cards-first/graph alternate primary workspace | 03-01, 03-03 | COVERED | Board is the one new representation; legacy surfaces remain comparator only.
CONTEXT | D-02/D-03 | MIT-only clean-room and continuing provenance | 03-01, 03-03, 03-05 | COVERED | Independently authored terminology/components and auditable provenance record.
CONTEXT | D-05 | Agent-only execution authority; board is proposal/read-model only | 03-01..04 | COVERED | No driver, approval, acknowledgement, or execution-confirmation control.
CONTEXT | D-06/D-07 | Persistent truthful reconciliation, read-only across all interactions | 03-01, 03-02, 03-04 | COVERED | Missing/unhealthy/contradictory evidence remains evidence/recovery-only.
CONTEXT | D-08 | Save/Discard/Keep editing draft resolver | 03-04 | COVERED | Acknowledged local-proposal save and exact context retention tested.
CONTEXT | D-09 | Immutable IDs/revisions/provenance; no inferred sample/result/execution | 03-02, 03-03, 03-04 | COVERED | Exact source pass-through or named unavailable variants.
CONTEXT | D-10/D-11 | Installed comparator/a11y UAT and future explicit scientist approval | 03-01, 03-05 | COVERED | Comparator remains reachable; no premature promotion.
