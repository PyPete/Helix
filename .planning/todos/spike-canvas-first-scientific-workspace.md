---
created: 2026-10-30T09:20:57.522Z
title: Spike canvas-first scientific workspace
area: ui
severity: major
status: pending
type: interactive-design-spike
files:
  - helix-desktop/src/shell/HelixShell.tsx
  - .planning/PROJECT.md
  - .planning/phases/01-agent-safety-boundary/01-UI-SPEC.md
---

## Problem

At capture, the prior UI review supplied research and recommendations, not an implemented experiment or validated redesign. Three disposable comparisons have since been built; evaluate whether the preferred direction better supports apparatus understanding, protocol authoring, and safe execution observation than the current ribbon/dock shell. Positive design preferences are recorded below, not conclusive usability winners or production approval; retain the existing UI as the comparison baseline.

Severity `major` was confirmed for backlog triage; it does not establish a product defect. This is a pending project TODO, not a completed spike or authorization to redesign production UI.

## Solution

Run an interactive UI design spike using isolated, disposable variants and representative simulated laboratory tasks. Compare alternatives with the scientist before proposing production work. Do not implement UI or build a prototype as part of this capture.

### Actionable spike questions

1. Does an apparatus-centered canvas make device relationships, sample context, and experiment progress clearer than the ribbon/dock baseline? What should remain dockable, and what loses discoverability when moved onto the canvas?
2. Are workflow sequence cards easier to author, reorder, and inspect for linear protocols? At what branching, looping, or parallelism complexity does a graph become necessary? Can cards and graph views preserve the same protocol identities and semantics without hiding control flow?
3. What belongs in a contextual inspector for selected apparatus, steps, samples, and results? Can selection changes preserve drafts, provenance, units, quality flags, and safety-review context without implying execution authority?
4. What must a persistent execution/safety strip expose across all views: Agent connection, actual run state, admission/review status, lease/resource constraints, pause/abort availability, and pending reconciliation? How does it distinguish a proposal, an approved revision, and actual physical execution?
5. How should AI suggestions appear as editable, reviewable proposals with visible uncertainty and provenance, without looking like approved commands? Can the scientist explain why an action is blocked and what evidence is missing?

### Research references

- **IvoryOS:** [repository UI subtree](https://github.com/ivoryos-ai/IvoryOS/tree/main/ivoryos) · [repository README](https://github.com/ivoryos-ai/IvoryOS). Inspect scientific automation interaction patterns for task organization, protocol authoring, and execution observation; record applicable ideas and mismatches with Helix.
- **IvoryOS-client:** [repository](https://github.com/ivoryos-ai/IvoryOS-client). API-only/no UI; use as an API/integration reference, not as evidence for a visual interaction pattern.
- **ivoryos-next:** [repository](https://github.com/ivoryos-ai/ivoryos-next). Inspect available UI/workspace approaches and compare them with IvoryOS and the Helix baseline.
- Record repository URLs, revisions/dates, screenshots where available, and source-grounded findings during the spike. The capture does not claim these references were newly inspected or their features verified.

### Non-negotiable Helix constraints

- The local Helix Agent alone owns device drivers and physical execution. Canvas gestures, ribbon controls, workflow edits, and LLM outputs remain untrusted proposals.
- Preserve explicit deterministic capability, safety, lease, resource, budget, and termination validation at the Agent boundary. UI review or an AI suggestion is never execution authorization; bounded campaigns require an approved policy.
- Preserve safety review and immutable approved revisions; distinguish draft intent from approved intent and observed device state. Do not add UI paths that bypass admission.
- Show uncertainty, stale/disconnected state, interrupted execution, and reconciliation requirements visibly. Recovery reconciles physical state instead of suggesting that replay is safe. Closing the UI must not imply that durable Agent execution stopped.
- Keep samples, results, decisions, provenance, units, and quality status traceable; quarantined or unreviewed analytical evidence must not appear accepted.
- Use simulated devices and synthetic data only. No physical commands or production redesign are in this spike's scope.

## Acceptance and evidence outcomes

These remain evaluation criteria, not claims that the required evidence is complete. Prototype construction and limited automated/DOM checks are recorded in the linked spike READMEs; detailed human comparative evidence remains outstanding.

| Given | When | Then | Required evidence |
|---|---|---|---|
| The same apparatus and linear protocol in baseline and canvas variants | A scientist locates equipment, edits a step, and traces the expected sample | Compare discoverability, task completion, errors, and perceived clarity without choosing a winner in advance | Annotated variant screenshots, task script, observation notes, and comparison matrix |
| Equivalent linear and branching/parallel protocol examples | The scientist authors and reviews sequence-card and graph variants | Control flow and stable step identities remain explicit; document where each representation succeeds or fails | Side-by-side protocol examples, control-flow checks, and representation recommendation with unresolved questions |
| A selected device, protocol step, sample, or result | The scientist changes selection and reviews the contextual inspector | Relevant context, draft state, provenance, units, quality, and safety implications remain understandable | Inspector state captures and task observations, including selection/draft preservation |
| A run is blocked, active, paused, disconnected, or awaiting reconciliation | The scientist navigates between canvas, protocol, and inspector views | The execution/safety strip retains truthful Agent state and uncertainty; no stale state implies readiness or success | State matrix and screenshots for each scenario; review of pause/abort and blocked-action affordances |
| An AI proposal or edited draft is not admitted, or evidence is unreviewed/quarantined | The scientist attempts to progress toward execution or a scientific decision | Proposal/review/authorization boundaries and missing evidence are unmistakable; no UI gesture grants device authority | Walkthrough notes, boundary checklist, and screenshots of proposal, safety review, blocked, and reconciliation states |
| Reference research and interactive comparison are complete | The scientist reviews the findings | Produce a recommendation, rejected alternatives, trade-offs, limitations, and follow-up questions; keep production changes separately approval-gated | Source/revision ledger, decision summary, and proposed next delivery slice only if justified |

## Next action

Evaluate the three existing disposable comparisons using their shared task guidance; collect human task timings, explanation accuracy, native interaction/accessibility and safety-state evidence. Prototype construction is complete; detailed evaluation and a separately scoped, approved production plan remain pending. Stop at a reviewed spike report rather than treating preferences or automated checks as implementation authorization. No further builds are requested by this documentation update.

## Feedback and preferred direction — 2026-10-30

- [001: Canvas workspace comparison](../spikes/001-canvas-workspace-comparison/README.md): user said **"this looks really good"**. Canvas-first direction received positive visual feedback, not a validated head-to-head winner.
- [002: Protocol representation comparison](../spikes/002-protocol-representation-comparison/README.md): user said **"I like the workflow graph a lot but too be honest, being able to alternative between both would be brilliant"**. Graph preferred as primary; cards remain first-class with an equally preserved toggle. Stable IDs, selection and saved/unsaved edits must survive switching without changing canonical control-flow semantics.
- [003: Inspector layout comparison](../spikes/003-inspector-layout-comparison/README.md): exact quote **"workflow graph and contextual inspector are quite nice"**. Contextual inspector favored; permanent panels retained as a comparison/advanced possibility. This is positive design preference, not full experiential validation, a conclusive usability winner, or tested safety/accessibility.

All three verdicts remain **PENDING**. Source research and limited automated/DOM evidence are attributed in the READMEs, including parent-reported independent 39/39 regression checks and 003 DOM smoke. Human task timings remain uncollected. This authorized documentation update records feedback in the canonical TODO but does not complete it or approve production changes.

## Tracking

- GSD pending registration: `pending/2026-10-30-spike-canvas-first-scientific-workspace.md`.
- Registry: `MANIFEST.md`.
- Duplicate check: no existing todos directory or pending entries at capture time (`init.todos` returned zero).
- Status remains pending until the interactive spike is actually run and reviewed; research recommendations alone are not completion evidence.
