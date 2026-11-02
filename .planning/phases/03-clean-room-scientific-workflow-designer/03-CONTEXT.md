# Phase 3 Context - Clean-room Scientific Workflow Designer

## Provenance and locked direction

This phase implements the direction locked by Phase 02.1, including
`02.1-PHASE-3-RECOMMENDATION.md`, `02.1-CONTEXT.md` D-01 through D-11, and
the Phase 02.1 clean-room and UAT records. Spike 004 remains background
research only; its former cards-first and on-demand graph direction is
superseded.

- **D-01/D-04:** Build one independently authored workflow board as the
  primary scientific-workspace representation. Do not restore cards-first
  navigation, a graph mode, or a graph toggle as an alternate primary
  workspace.
- **D-02/D-03:** Maintain an MIT-only clean room. Do not copy or adapt
  IvoryOS source, UI, styles, text, layout, assets, API shapes, names, logos,
  or branding. Preserve independent provenance for terminology, interactions,
  visuals/layout, APIs/data contracts, assets, and branding.

## Authority and scientific-record boundary

The Designer is proposal-only. The local Helix Agent remains the only
authority that can address drivers or execute physical commands after
deterministic capability, safety, lease, resource, budget, and termination
validation. The board must not dispatch commands, grant approval, or confirm
execution.

Retain exact immutable published/approved identities, revision or frozen
references, independent procedure and plan revisions, sample custody and
lineage, append-only history, and transformation facts. Never infer a sample,
analytical result, or physical execution from a planned step.

## Required workflow-board behavior

- Provide an Agent-declared logical capability palette and typed inspector for
  bindings, units, defaults, provenance, quality/review state, and resources.
- Model explicit If/Else true/false ports and evidence placeholders; model
  Fork/Join with Agent resource and lease diagnostics.
- Resolve draft changes before revision, route, or procedure changes: require
  Save, Discard, or Keep editing. Keep editing retains the original draft
  context; never silently autosave, retarget, approve, or apply a draft to a
  reviewed run.
- Show a persistent, truthful reconciliation strip. Held, reconciling,
  faulted, uncertain, missing, or contradictory Agent evidence is read-only
  across pointer, keyboard, shortcut, inspector, and route affordances, and
  offers recovery/evidence navigation rather than an execution,
  acknowledgement, or confirmation control.
- Make the lifecycle legible as Draft -> Agent preflight -> scientist approval
  -> observed execution, without representing a UI proposal as authority.

## Acceptance and promotion gate

Keep the legacy workspace available as the comparator until this gate is
complete. Before any primary-workspace change or promotion, Phase 3 must
perform a task-based installed-Tauri comparison for setup, experiment, plan,
run, results, and samples/analysis; capture keyboard-only and screen-reader
evidence, outcomes, and evidence locations for every UAT row; and obtain a
future explicit scientist approval. A prototype, automated check, or design
preference cannot satisfy this gate.
