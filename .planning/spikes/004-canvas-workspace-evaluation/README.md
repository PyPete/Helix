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

## First Interactive Decision

**When the Agent is awaiting reconciliation, should the persistent execution/safety strip expose Pause/Hold/Abort controls directly, or show truthful held state plus a route to the evidence/control panel?**

- **Status + route (recommended):** keeps the strip persistent and unmistakable while placing consequential controls beside their admission/evidence context.
- **Direct controls:** makes operator intervention immediately available from every view, with explicit confirmation and durable acknowledgement requirements.
- **Hybrid:** show only `Hold` in the strip; expose `Pause` and `Abort` in the contextual control panel.

This decision affects layout only. It never changes Agent-side authority, deterministic admission, immutable approval records, or reconciliation behavior.

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