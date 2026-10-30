# Phase 2: Scientific Setup Foundation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in `02-CONTEXT.md` — this log preserves the alternatives considered.

**Date:** 2026-10-30
**Phase:** 2-Scientific Setup Foundation
**Areas discussed:** Revision lifecycle, Procedure vs plan, Sample lineage, Folded TODO

---

## Revision lifecycle

| Option | Description | Selected |
|--------|-------------|----------|
| Every save | Each save creates a new immutable revision. | |
| Publish explicitly | Keep edits in a draft; the scientist publishes a named immutable revision. | ✓ |
| One active draft | One working draft based on the latest published version. | ✓ |
| Parallel drafts | Multiple named drafts can be compared before publishing. | |
| Explicitly select | Scientist chooses each setup revision when preparing a campaign. | |
| Default to latest | Preselect latest published versions but show them before campaign start. | ✓ |
| When assembled | Pin the selected versions when the campaign is assembled. | ✓ |
| At approval/start | Allow version refresh until approval/start, then pin. | |

**User's choices:** Explicit publication, one active draft, default to latest published, pin at campaign assembly.
**Notes:** Later publications must not silently alter an assembled campaign.

---

## Procedure vs plan

| Option | Description | Selected |
|--------|-------------|----------|
| Plan instantiates procedure | Plan selects a specific published procedure revision and supplies experiment-specific choices. | ✓ |
| Independent artifacts | Procedure and plan remain separate documents; a link does not inherit steps. | |
| Parameters only | Plan keeps the procedure step structure fixed and changes only run-specific values. | |
| Fork a variant | Plan-specific structural changes fork a procedure variant. | ✓ |
| Reusable revision | A fork is a derived procedure revision that other plans may select. | ✓ |
| Plan-scoped variant | Structural changes stay local to one plan. | |
| Versioned in plan | Intended values and units live in the plan revision. | ✓ |
| Entered per run | Values are entered for each campaign and recorded separately. | |

**User's choices:** Plans instantiate specific procedure revisions; structural changes fork reusable derived procedure revisions; experiment-specific values and units are versioned in the plan.
**Notes:** Preserve the independent revision identity of the procedure and plan.

---

## Sample lineage

| Option | Description | Selected |
|--------|-------------|----------|
| Scientist confirms | Planned collection is intent; a scientist separately confirms actual collection. | ✓ |
| Confirm a proposal | Step completion creates a pending sample candidate for scientist confirmation. | |
| Generated ID + label | Helix assigns immutable identity; scientist may add a human-readable label. | ✓ |
| Scientist-assigned ID | Scientist-provided accession/label is the stable identity. | |
| Append-only events | Preserve dated, attributable collection and custody/location changes. | ✓ |
| Current custodian only | Keep only the current owner/location. | |
| Transformation event | Record operation, parent/child samples, actor, and relevant quantities/units. | ✓ |
| Parent-child links | Store lineage links with an optional note, without a structured transformation event. | |

**User's choices:** Explicitly confirm actual collection; assign immutable Helix IDs with optional labels; retain append-only custody events; record transformations as attributable events with quantities/units when relevant.

---

## Folded TODO

| Option | Description | Selected |
|--------|-------------|----------|
| Fold into Phase 2 | Include the canvas-first workspace TODO as design input for setup-facing decisions. | ✓ |

**User's choice:** Fold it in.
**Notes:** Treat canvas-first/contextual-inspector and graph-with-cards-toggle preferences as promising but unvalidated; retain pending human review and separate approval for any production redesign.

## the agent's Discretion

- Concrete domain model, persistence, APIs, migration strategy, and test approach remain for research/planning to determine.

## Deferred Ideas

- Complete human comparative evaluation of the workspace spikes and separately approve any production redesign; the TODO remains pending under Phase 02.1.
