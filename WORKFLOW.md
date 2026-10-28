# Helix Workflow Handoffs

This repository uses the GSD Path v2 phase contracts. `.project/STATE.md` is the
source of truth for the active phase and status; phase skills and their helpers
own validation and transitions. Do not edit phase state by hand. Project
constraints and program scope are recorded in `.project/CHARTER.md`.

## Program-scope research

For program-level research before a milestone intent exists:

- **Research input:** `.project/CHARTER.md`.
- **Settled brownfield input:** `.project/research/evidence-codebase.md`, when
  present. Researchers use it as the codebase map and do not replace it with
  assumptions.
- **Research manifest:** `.project/research/RESEARCH.md`. It records every
  standard dimension as dispatched or skipped and assigns every charter
  `[RESEARCH]` question to a dispatched dimension.
- **Research evidence:** `.project/research/evidence-<dimension>.md`, one file
  for each dispatched dimension, following the installed evidence template.
- **Handoff:** after the research evidence and manifest pass the phase gate and
  state is `research/done`, hand off to `gsd-path-decide`. The decider consumes
  the manifest and its evidence; research records evidence and does not make
  project decisions.

Research outputs must use these paths. The active GSD Path skill, validated
state, and phase helpers define the exact transition and any later phase
artifacts; this file does not authorize skipping a phase gate.
