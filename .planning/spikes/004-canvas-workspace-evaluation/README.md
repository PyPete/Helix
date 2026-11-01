---
spike: "004"
idea: ivoryos-scientific-workspace
name: canvas-workspace-evaluation
type: interactive-design-evaluation
validates: "Given the built 001–003 simulated workspace comparisons, when a scientist performs the shared tasks and reviews safety states, then Helix can capture comparative evidence without treating preference as execution authorization or a production decision."
verdict: PENDING
related:
  - ../001-canvas-workspace-comparison/README.md
  - ../002-protocol-representation-comparison/README.md
  - ../003-inspector-layout-comparison/README.md
tags: [ui, evaluation, simulation, safety, human-review]
---

# Spike 004: Canvas Workspace Evaluation

## Scope

This is the human-review gate for the existing disposable 001–003 comparisons. It creates no product code and does not declare a winner. All fixtures remain simulated; UI interactions, drafts, and AI suggestions are proposals only. The local Helix Agent remains the sole authority for physical execution and admission.

## Status

**PENDING — interactive review required.** The canonical TODO remains pending: [canvas-first scientific workspace](../../todos/spike-canvas-first-scientific-workspace.md). This artifact reconciles the already-built comparisons with the outstanding evidence rather than rebuilding or redesigning them.

## Evaluation sequence

1. Launch the disposable comparison host: `node .planning/spikes/server.mjs`.
2. Complete the same apparatus-orientation and linear-protocol task in ribbon/dock and canvas variants; record discoverability, completion, errors, and explanation accuracy.
3. Switch between graph and sequence-card representations for the same stable protocol identities, including a branching/parallel example; record whether control flow stays explicit.
4. Change device, step, sample, and result selection while a draft is pending; record whether the contextual inspector preserves draft, provenance, units, quality, and safety-review context.
5. Review blocked, active, paused, disconnected, AI-proposal, unreviewed-result, and reconciliation scenarios. Confirm the persistent execution/safety strip remains truthful and that no gesture appears to dispatch a physical command.
6. Export the fixture event log and attach task observations, screenshots, timings, and the state matrix here before updating any verdict.

## Decision Log

### 2026-11-01 - Reconciliation strip: status + route

The persistent execution/safety strip shows the truthful reconciliation/held status and routes the scientist to the detailed safety/evidence view. It exposes no direct `Pause`, `Hold`, or `Abort` controls while reconciliation is pending. Consequential controls remain beside their admission evidence, require their existing confirmations/acknowledgements, and never imply that the UI can authorize execution.

## Next Interactive Decision

**When a scientist changes contextual-inspector selection while the current entity has an unsaved draft, how should the spike preserve that draft and its safety/provenance context?**

- **Persistent per-entity draft tray (recommended):** switch selection immediately; retain a clearly labeled, non-autosaved draft for the prior stable entity ID, with its provenance, units, quality flags, and review state.
- **Resolve-before-switch:** keep selection fixed until the scientist explicitly keeps or discards the draft.
- **Shared unsaved draft:** retain one workspace-level draft while selection changes, with an explicit reassignment step before it can be reviewed.

No option may silently save, retarget, approve, dispatch, or mutate a reviewed fixture/run. This decision tests inspector clarity and traceability with simulated data only.

## Evidence checklist

- [ ] Annotated screenshots for baseline and canvas tasks
- [ ] Task timings, completion notes, errors, and clarity observations
- [ ] Graph/card control-flow comparison using identical stable step IDs
- [ ] Inspector selection/draft-preservation observations
- [ ] Execution/safety state matrix including uncertainty and reconciliation
- [ ] AI proposal, safety-review, blocked, and unreviewed/quarantined evidence walkthrough
- [ ] Exported fixture event log and observer notes
- [ ] Reviewed comparison summary, limitations, and separately approval-gated next slice

## Investigation Trail

- 2026-11-01: Reconciled the canonical pending TODO with built spikes 001–003. No production source was changed and no human comparative evidence has been recorded in this artifact.

## Results

No comparative verdict yet. Existing positive feedback is preserved as preference only; it is not usability validation, an execution authorization, or approval for production redesign.