# Phase 3 Clean-Room Provenance Record

## Scope and status

This record documents the provenance boundary for the independently authored Phase 3 workflow board. It is an auditable project record, not a legal opinion or evidence of installed-app UAT. Phase 3 is grounded in Helix requirements and Helix-owned source records. No IvoryOS source code, UI, styles, text, layout, assets, APIs, names, logos, branding, or interaction/visual expression is copied or adapted.

## Provenance by area

| area | Helix-authored basis and sources reviewed | independently authored boundary | excluded external expression |
| --- | --- | --- | --- |
| Terminology | Phase 3 context and Helix record contracts; `ScientificWorkflowBoard.tsx`, `EvidenceView.tsx`, and domain-facing labels | Apparatus, proposal, frozen reference, command evidence, provenance, custody, and lineage vocabulary remains Helix-specific and tied to exact source records. | IvoryOS names, labels, copy, and terminology. |
| Interactions | Phase 3 D-05 through D-09; `ScientificWorkflowBoard.tsx`, `DraftContextDialog.tsx`, `useDraftContextResolver.ts`, and `scientificWorkflowProposalStore.ts` | Board navigation is proposal/read-model intent. Dirty-context changes use Save, scoped Discard, or Keep editing; no board action gains device or approval authority. | IvoryOS control behavior, gestures, workflows, and interaction sequencing. |
| Visual and layout | Phase 3 context, board components, and existing Helix desktop conventions | One original workflow-board composition presents six evidence views with truthful read-only/unavailable states. Visual hierarchy and layout are authored for Helix. | IvoryOS visual composition, screen arrangement, styles, component shapes, and responsive expression. |
| API and data contract | Helix-owned Agent/native hooks and contracts, including existing evidence/read-model paths; Phase 3 source audit | Existing immutable IDs, revisions, frozen references, status, custody, and lineage are displayed only as sourced; no new board authority or inferred record is introduced. | IvoryOS API shapes, schemas, endpoints, identifiers, and data contracts. |
| Assets and branding | Helix desktop assets and existing project branding | No external assets or branding are used; any board-specific visual assets remain Helix-owned. | IvoryOS assets, icons, marks, logos, names, and branding. |

## Reviewed Helix sources

- `.planning/phases/03-clean-room-scientific-workflow-designer/03-CONTEXT.md` — locked D-01 through D-11 direction and clean-room boundary.
- `.planning/phases/03-clean-room-scientific-workflow-designer/03-SOURCE-AUDIT.md` — requirement and decision traceability.
- `.planning/phases/03-clean-room-scientific-workflow-designer/03-01-SUMMARY.md` through `03-04-SUMMARY.md` — implementation provenance and prior decisions.
- `.planning/phases/02.1-scientific-workspace-ui-rework/02.1-CLEAN-ROOM-PROVENANCE.md` — inherited source-review boundary.
- `helix-desktop/src/components/ScientificWorkflowBoard/` — independently authored board and evidence presentation.
- `helix-desktop/src/hooks/` and `helix-desktop/src/lib/` — existing Helix-owned proposal, evidence, and context-resolution seams used by the board.

## Excluded external expression

No IvoryOS expression is included or adapted. Specifically excluded are source code, UI, styles, text, layout, assets, API/data-contract shapes, names, logos, branding, interactions, and visual composition. External material, if consulted as background, is not a source for implementation expression.

## Review and UAT linkage

The six-view installed-Tauri UAT template is `03-INSTALLED-TAURI-UAT.md`. It is deliberately not evidence of completed review. A scientist conducting UAT must review this boundary and record any discrepancy with actual source/location details before recording a future disposition. The UAT remains pending until all six rows contain actual session evidence.
