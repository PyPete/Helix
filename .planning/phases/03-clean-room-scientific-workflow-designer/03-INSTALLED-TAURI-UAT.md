# Phase 3 Installed-Tauri Comparative and Accessibility UAT

## Gate status

`PENDING_INSTALLED_TAURI_UAT`

This is a six-row evidence template, not an outcome. No installed-Tauri comparison, keyboard-only test, screen-reader test, or scientist disposition is claimed here. Do not replace a pending marker with an assumed, synthetic, prototype, or automated result. Complete each row from one real installed-Tauri session with a local authenticated Helix Agent and representative records. Keep the legacy comparator available throughout.

## Session metadata to complete

| field | value |
| --- | --- |
| operator / scientist | PENDING_UAT — record the actual scientist/operator |
| session date and timezone | PENDING_UAT — record actual date/time and timezone |
| installed app build / commit | PENDING_UAT — record exact build or commit used |
| Helix Agent identity / environment | PENDING_UAT — record actual local Agent and environment (never include secrets) |
| assistive technology and version | PENDING_UAT — record actual screen reader and version |
| clean-room provenance reviewed | PENDING_UAT — record review result and any discrepancy in evidence location |

## Required six-view task evidence

`PENDING_UAT` means not run; it is permitted only while that row remains `NOT_RUN`. For each row, replace each pending cell with observed session evidence and set row state to `COMPLETE` only when every evidence field is actual, non-placeholder evidence. Use an evidence location that the scientist can inspect (for example, a secured session log or capture directory); do not put credentials or sensitive data in this record.

| view | installed-Tauri task | legacy comparator mount | representative record identity | revision or frozen reference | provenance or status | original board observed outcome | legacy comparator observed outcome | keyboard-only evidence | screen-reader evidence | evidence location | row state |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| setup | Open setup and inspect the same representative apparatus/setup record in both workspaces. | Existing Scientific Setup dock entry — verify it remains reachable. | PENDING_UAT — capture actual Agent-backed apparatus identity. | PENDING_UAT — capture exact apparatus revision. | PENDING_UAT — capture source-backed status and provenance. | PENDING_UAT — record actual task result, including any unavailable/read-only state. | PENDING_UAT — record actual comparator result. | PENDING_UAT — record tab order, focus targets, actions, and fail-closed behavior. | PENDING_UAT — record announced identity, revision, status, and read-only state. | PENDING_UAT — record inspectable evidence path. | NOT_RUN |
| experiment | Inspect the representative experiment and its observed command/evidence correlation in both workspaces; do not infer execution from a proposal. | Existing Experiment Timeline panel — verify it remains reachable. | PENDING_UAT — capture actual experiment and command-evidence identities, or documented absence. | PENDING_UAT — capture exact execution/evidence reference, or documented absence. | PENDING_UAT — capture observed outcome, actor/admission evidence, or unavailable state from source. | PENDING_UAT — record actual evidence inspection and recovery-route result. | PENDING_UAT — record actual comparator result. | PENDING_UAT — record keyboard access to evidence and recovery routes. | PENDING_UAT — record announced command-evidence status and recovery route. | PENDING_UAT — record inspectable evidence path. | NOT_RUN |
| plan | Inspect the same representative plan and exercise Save, Discard, and Keep editing before a typed context change. | Existing Procedure Plans dock entry — verify it remains reachable. | PENDING_UAT — capture actual Agent-backed plan identity/revision. | PENDING_UAT — capture exact plan-pinned procedure revision. | PENDING_UAT — capture source-backed publication and mutation status. | PENDING_UAT — record all three actual resolver outcomes and retained-context behavior. | PENDING_UAT — record actual comparator result. | PENDING_UAT — record keyboard-only invocation/result for Save, Discard, and Keep editing. | PENDING_UAT — record dialog names, choices, and retained-context announcement. | PENDING_UAT — record inspectable evidence path. | NOT_RUN |
| run | Inspect the representative frozen assembly/run reference in both workspaces; confirm the board cannot start a campaign. | Existing Frozen Assembly panel — verify it remains reachable. | PENDING_UAT — capture actual assembly identity. | PENDING_UAT — capture exact apparatus, plan, procedure, policy revisions, and preview token where present. | PENDING_UAT — capture source-backed frozen/draft and policy status. | PENDING_UAT — record actual identities and confirm no execution-authorizing control is offered. | PENDING_UAT — record actual comparator result. | PENDING_UAT — record keyboard traversal and non-authorizing evidence-route result. | PENDING_UAT — record announced frozen references and status. | PENDING_UAT — record inspectable evidence path. | NOT_RUN |
| results | Inspect a representative reviewed result if one exists; otherwise verify the truthful unavailable state in both workspaces. | Existing samples/results availability path — verify it remains reachable. | PENDING_UAT — capture actual result identity or record that none is available. | PENDING_UAT — capture exact result revision/source artifact or record that none exists. | PENDING_UAT — capture reviewed status or source-backed unavailable state. | PENDING_UAT — record actual result/unavailable presentation; never invent a result. | PENDING_UAT — record actual comparator result. | PENDING_UAT — record keyboard access to result/unavailable evidence. | PENDING_UAT — record announced result status and source evidence. | PENDING_UAT — record inspectable evidence path. | NOT_RUN |
| samples-analysis | Inspect representative sample, custody, and lineage evidence; distinguish actual collection from a planned step. | Existing Samples and SampleLineage panels — verify they remain reachable. | PENDING_UAT — capture actual Agent-backed sample and event identities. | PENDING_UAT — capture exact server sequence, event, and transformation activity references where present. | PENDING_UAT — capture source-backed origin, custody, append-only history, and lineage status. | PENDING_UAT — record actual sample/analysis outcome and ensure no planned collection is presented as fact. | PENDING_UAT — record actual comparator result. | PENDING_UAT — record keyboard traversal through sample, custody, and lineage evidence. | PENDING_UAT — record announced identity, custody, lineage, and collection status. | PENDING_UAT — record inspectable evidence path. | NOT_RUN |

## Scientist review and evidence completion

1. Start the local authenticated Helix Agent with representative, non-sensitive records. Do not record secrets.
2. Launch the desktop from the repository using `npm --prefix helix-desktop run tauri -- dev` and use the installed Tauri application for all observations; browser/prototype-only review is not a substitute.
3. For each of the six tasks above, inspect the original board and the named legacy comparator using the same record/reference. Record actual IDs, revisions/frozen references, provenance/status, both outcomes, and evidence location.
4. Repeat each task using keyboard-only navigation. Record focus order/target and actual action/read-only result.
5. Repeat each task with the declared screen reader. Record actual announcements and any discrepancy.
6. Confirm the legacy comparator remains reachable and the primary workspace selection was not changed by this UAT package.
7. Complete all cells with actual evidence, mark rows `COMPLETE`, and run `node .planning/phases/03-clean-room-scientific-workflow-designer/03-validate-evidence.cjs`. Validation does not perform or attest the human tests.
8. Only after all six rows are complete, the scientist records an explicit approval or rejection in `03-PROMOTION-DECISION.md`. That disposition does not itself change primary workspace selection.

## Promotion gate

Automated tests, a build, a prototype, preference, or incomplete evidence cannot complete UAT, approve a design, or promote the board. The initial disposition remains pending. The legacy comparator remains available; this plan makes no primary-selection change.
