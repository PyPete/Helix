---
phase: "02"
slug: "scientific-setup-foundation"
status: draft
shadcn_initialized: false
preset: none
created: "2026-10-30"
---

# Phase 02 — UI Design Contract

> Visual and interaction contract for the fumehood scientist's revisioned setup and sample-fact work. All mutations are proposals to the local Helix Agent; only an acknowledged server result changes the UI to committed state.

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none — reuse the existing Helix desktop shell and hand-authored CSS; `components.json` is absent at repository and `helix-desktop/` roots. |
| Preset | not applicable |
| Component library | React 18 desktop shell with rc-dock and Allotment; use native HTML form, button, dialog, table, and list semantics. |
| Icon library | none installed; use concise text labels and existing visual affordances, never icon-only controls unless `aria-label` and visible tooltip are supplied. |
| Font | system UI stack already used by the desktop; use tabular numerals for revision IDs, timestamps, hashes, quantities, and units. |

No shadcn initialization is requested for this phase: it is an existing-shell extension, and no third-party registry or block is permitted.

---

## Spacing Scale

| Token | Value | Usage |
|-------|-------|-------|
| xs | 4px | inline icon/text gap; status-dot gap |
| sm | 8px | compact field and row gaps |
| md | 16px | form groups, card padding, standard control gaps |
| lg | 24px | panel section padding and grouped history separation |
| xl | 32px | adjacent workspace-region gap |
| 2xl | 48px | major page-region separation |
| 3xl | 64px | page-level empty-state breathing room |

Exceptions: pointer targets are at least 32px high and destructive/primary confirmation buttons at least 36px high; do not add mobile-oriented 44px spacing to the desktop density.

---

## Typography

| Role | Size | Weight | Line Height |
|------|------|--------|-------------|
| Body | 14px | 400 | 1.5 |
| Label | 14px | 600 | 1.3 |
| Heading | 20px | 600 | 1.2 |
| Display | 28px | 600 | 1.2 |

Only weights 400 and 600 are used. IDs, hashes, quantities, units, dates, and event sequence numbers use a monospace/tabular treatment without adding another type scale.

---

## Color

| Role | Value | Usage |
|------|-------|-------|
| Dominant (60%) | #101820 | shell background and primary work surfaces |
| Secondary (30%) | #1C2A35 | dock panels, cards, tables, navigation, and read-only evidence blocks |
| Accent (10%) | #22B8A7 | primary committed-action buttons, focused field ring, selected row/node, and links to exact revision/sample evidence |
| Destructive | #D9534F | discard-draft, irreversible record-removal if introduced, and destructive-dialog confirm only |

Accent reserved for: Publish named revision, Save draft when valid, Assemble frozen references, Confirm collection, Record custody change, Record transformation, selected scientific entity, keyboard focus, and exact-ID evidence links. Status is never communicated by color alone: pair a color with a textual state and, where appropriate, an icon/shape.

Semantic status colors are restrained: success #2E9B62, warning/uncertain #D99A24, error #D9534F, and neutral #8FA3B3. These indicate Agent-reported record state, never device permission, calibration truth, or execution authority.

---

## Layout and Navigation Contract

- Extend the existing `HelixShell`, ribbon/activity-bar, rc-dock, and panel conventions. Add distinct entry points: **Apparatus**, **Procedures**, **Plans**, **Assembly**, and **Samples**. Retain existing execution/workflow surfaces without redefining them.
- Use a stable two-region detail pattern: a left list/history region (minimum 280px) and a main editing/detail region. A resizable dock may host a detail pane only when opened by an explicit row action. No always-visible contextual inspector is authorized.
- Use compact evidence headers on every detail view: entity name/label, immutable aggregate ID, status chip (`Draft`, `Published`, `Frozen`, `Confirmed`, `Pending acknowledgement`, or `Uncertain`), latest published revision name/ID where relevant, and last Agent acknowledgement time.
- Stable IDs are copyable, displayed in a truncated monospace line with a Copy control, and reveal the complete value in a tooltip/detail dialog. Never make a mutable label the only identifier.
- Tables retain headers while scrolling; long labels wrap at word boundaries, long identifiers truncate with Copy/View full, and histories use pagination or incremental “Load more” rather than unbounded panels.
- Phase 02.1 fence: do not implement a production canvas-first workspace, graph/cards representation toggle, or persistent contextual inspector. Existing `WorkflowBuilder`/React Flow patterns may only be reused for a minimal apparatus topology preview/editor within the current shell; topology remains distinct from executable WSL. Record any canvas/graph/inspector preference as **Pending Phase 02.1 comparison and explicit approval**.

---

## Scientific Surfaces and Interactions

### Apparatus versions and typed topology (APP-01, APP-02)

- **Apparatus list:** show name, draft/published status, latest named revision, component count, binding-validation summary, last acknowledgement, and a `New apparatus` action. Selection never changes the active revision silently.
- **Draft editor:** require apparatus name plus typed component, port, connection, role, and device-binding fields. Add-component/add-port/add-connection controls open typed forms; endpoint selectors only offer ports in the active draft. A topology preview is supplementary, not the source of truth; form/table rows expose the canonical type, endpoints, role, and stable IDs.
- **Binding panel:** Agent discovery is read-only. Each binding shows discovered logical device ID, alias, type, advertised capabilities, discovery freshness, and bound component. `Bind device` proposes a binding; it must not call a driver, send a command, or claim that the device is safe/ready.
- **Assertions panel:** capability constraints and calibration/inspection metadata are revision-owned assertions. Each assertion shows assertion type, measured/allowed value, exact unit, observed/valid-through dates when provided, source reference, recorded-by, and interpretation (`Recorded assertion — not live verification`). Validate required fields and bounded unit vocabulary inline. Never badge an assertion as “calibrated”, “safe”, or “authorized” without an Agent-provided authoritative result.
- **Validation:** render an ordered error summary above the editor and field-level diagnostics adjacent to affected data. Invalid port compatibility, missing role, unavailable binding, bad unit, or stale draft token blocks Save/Publish. Preserve entered values; do not discard or auto-correct scientific input.
- **Draft/version actions:** `Save draft` is enabled only after local required-field validation; display `Saving…` then `Awaiting Agent acknowledgement…`. Only Agent acknowledgement changes the header to `Draft saved`. `Publish named revision` opens a confirmation dialog that requires a non-empty revision name and shows the exact draft base revision, validation summary, and statement: “Publishing creates an immutable revision. It does not execute equipment.” On success show immutable revision ID, content hash, actor, and recorded time. Published revisions are read-only with `Create draft from revision`.
- **Conflict/uncertain:** stale save/publish returns `Draft changed elsewhere` with Reload and Compare actions; never overwrite/rebase automatically. On lost response show `Outcome uncertain — no success recorded in this window`, keep editing disabled for that operation, show its operation ID, and offer `Check outcome`. Do not retry with a new ID.

### Independently revisioned procedures and plans (APP-03)

- Procedure and Plan have separate lists, draft badges, history, publication controls, and immutable detail pages; they are not subpanels of apparatus topology.
- **Procedure:** structural editing produces draft changes only. For a published procedure used by a plan, `Derive procedure revision` starts a distinct draft and visibly records `Derived from: procedure revision <exact ID>`. The pinned revision remains unchanged and readable. Publish requires a named immutable revision.
- **Plan:** select one published procedure revision, then enter experiment-specific parameter values and exact units. The plan header always shows `Pins procedure: <revision name> · <exact ID>` and its own plan draft/published revision independently. `Adopt newer procedure` is explicit, shows old/new IDs and parameter compatibility diagnostics, and requires a plan draft save/publish; it is never performed by opening a newer procedure.
- Use a revision history table with Name, Revision ID, Source/parent revision, Published by, Recorded time, Content hash, and `View immutable`. History rows never offer Edit or Delete.

### Frozen assembly references (APP-04)

- **Assembly view:** before later campaign execution, present a read-only “Execution reference assembly” card. On open, defaults are labelled `Latest published at assembly start`, never merely “latest”. Display exact apparatus, plan, plan-pinned procedure, and policy revision IDs/names/hashes; the procedure selection must match the plan pin.
- `Assemble frozen references` opens a confirm dialog listing every exact ID and warns: “Later publications will not update this assembly. This action creates no campaign and sends no physical command.” A missing/unavailable authoritative policy reference is an explicit blocking integration state, not an editable free-text substitute.
- On acknowledged success, render a `Frozen` chip and immutable manifest ID, exact source hashes/schema versions, actor, and time. Remove edit/latest-refresh controls. A failed consistency check calls out the conflicting plan/procedure IDs and directs the scientist to explicitly revise the plan. No execution/start button appears in this phase.

### Samples: separate collection confirmation, custody, and lineage (SAMP-01, SAMP-02)

- **Sample list/detail:** show human label, immutable Helix sample ID, current custody/location projection, last event sequence/time, and lineage indicator. Label edits never alter the ID and must appear as an attributed history event.
- **Planned collection reference:** a planned procedure step/execution reference can be shown as `Planned reference only — collection not confirmed`. It has no success/collected badge and never creates a sample automatically.
- **Confirm collection:** primary CTA is `Confirm sample collection`. The dialog requires occurrence time and location, permits label and planned-step/execution reference, displays the authenticated actor source as Agent-determined, and states: “Confirm only after actual collection. This records a new immutable sample fact; it does not infer collection from a procedure step.” After submission show `Awaiting Agent acknowledgement…`; only acknowledgement shows `Confirmed` with server-assigned sample ID, recorded time, actor, and initial custody event. Lost acknowledgement is `Collection outcome uncertain`; do not display a confirmed sample, create a second sample, or auto-resubmit; offer `Check outcome` by operation ID.
- **Custody:** `Record custody change` requires current expected sequence, new location/custodian, occurrence time, and optional reason. Current custody is explicitly labelled `Derived from append-only history`. History lists sequence, event type, occurrence time, recorded time, actor, location/custodian, reason, and supersedes link. On conflict, show server current state and Reload; never silently merge two moves.
- **Transformation:** `Record transformation` opens a modal with operation, one-or-more parent samples, one-or-more new child labels, occurrence time, and quantity/unit rows when relevant. Preview all parent→activity→child links before confirmation. Require positive finite quantities where supplied and exact supported units. On acknowledgement show the immutable transformation ID and child sample IDs; child samples are labelled `Created by transformation`, not `Collected`. Invalid/self/cyclic/duplicate lineage diagnostics remain visible until corrected.
- **Lineage:** provide an accessible hierarchical/table lineage view (ancestor/descendant switch) with linked stable IDs, transformation operation, actor, occurrence/recorded timestamps, and quantity/unit. This is the Phase 02 minimum; no graph-first lineage redesign is authorized pending Phase 02.1.

---

## State Contract and Copywriting

| Element | Copy |
|---------|------|
| Primary CTA | Confirm sample collection |
| Apparatus empty | No apparatus versions yet |
| Apparatus empty body | Create an apparatus draft to record typed components, ports, connections, roles, and device bindings. |
| Procedure/plan empty | No published procedures or plans yet |
| Procedure/plan empty body | Create a draft, then publish a named immutable revision before it can be pinned or assembled. |
| Sample empty | No confirmed samples yet |
| Sample empty body | A planned step is not a sample. Confirm actual collection to create an immutable sample record. |
| Loading | Loading Agent-backed scientific records… |
| Offline/error | Scientific records could not be loaded from the local Helix Agent. Check the local Agent connection, then Retry. |
| Validation error | Resolve the highlighted scientific record errors before saving or publishing. Nothing has been sent for execution. |
| Stale draft | This draft changed after you opened it. Reload to review the current version or Compare before making another save. |
| Mutation pending | Awaiting Agent acknowledgement. This workstation has not recorded a committed result yet. |
| Mutation uncertain | Outcome uncertain. Do not repeat this action. Check outcome using operation ID {id}. |
| Assembly blocked | A valid authoritative policy revision is required to freeze execution references. No assembly was created. |
| Publish confirmation | Publish “{name}” as an immutable revision? This does not execute equipment. |
| Freeze confirmation | Freeze these exact references for a future execution? Later publications will not update this assembly, and no campaign will start. |
| Collection confirmation | Confirm that this sample was actually collected? This creates an immutable fact and cannot be inferred from a planned step. |
| Discard draft confirmation | Discard unsaved draft changes? Published revisions and recorded sample facts will not be changed. |

`Retry` only reloads read data. Mutation retry is permitted only with the original operation identity after a result lookup confirms no commit; the UI never fabricates client success.

---

## UI Considerations

Applicable state considerations resolved: 24 covered, 2 backstop, 0 unresolved.

| Category | Element(s) | Status | Resolution / Reason |
|----------|------------|--------|---------------------|
| empty | Apparatus, procedure/plan, and sample lists | ✅ covered | Render the documented entity-specific empty copy and its next valid action. |
| loading | Lists, revision history, discovery, manifest, custody, lineage | ✅ covered | Use loading copy/skeleton rows; preserve last known read data with a stale indicator rather than blanking it. |
| error | Agent-backed reads and typed mutations | ✅ covered | Render actionable retry for reads; retain form values and diagnostics for mutation failures. |
| uncertain | Save, publish, assembly, collection, custody, transformation acknowledgements | ✅ covered | Show operation ID and Check outcome; never show success, auto-replay, or create a new mutation ID. |
| confirmation | Publish, freeze, collection, discard draft, transformation | ✅ covered | Require an explicit dialog showing exact IDs/effects and a labelled confirmation button. |
| zero-one-many | Components/ports/connections, bindings/assertions, parents/children, histories | ✅ covered | Zero uses inline add action; one remains editable/readable; many use scrollable tables/pagination with stable headers. |
| partial | Device discovery and last-known records | ✅ covered | Mark discovery freshness and “Showing last available discovery”; never promote cached facts to live validation. |
| overflow | IDs, hashes, labels, diagnostics, quantity/unit rows | ✅ covered | Wrap labels; truncate IDs/hashes with tooltip and Copy/View full; use paged histories. |
| long-text | Revision names, metadata/source references, reasons | ✅ covered | Wrap in detail panels, constrain list columns, and expose full text in a detail dialog. |
| accessibility | Forms, dialogs, topology preview, lineage | ✅ covered | Native labels/errors, keyboard-operable controls, visible focus, focus-trapped dialogs, text/table alternative for any topology preview, and no color-only state. |
| authority boundary | Bindings/assertions/assembly | ✅ covered | These controls say proposal/record/freeze; they provide no driver or execution control and do not claim authorization. |
| visual regression | Existing shell extension | 🧪 backstop | Verify dock sizing, 125–200% scaling, focus order, and high-contrast readability in real Tauri desktop acceptance. |
| native boundary | Representative topology and lineage payloads | 🧪 backstop | Verify bounded native IPC accepts representative valid records and reports oversized payloads without data loss. |

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| none | none | not applicable — no shadcn initialization and no third-party registry declared |

---

## Quality Dimensions

| Dimension | Contractual evidence |
|-----------|---------------------|
| 1 Copywriting | All calls to action use scientific verbs; empty, error, validation, conflict, uncertain, and confirmation copy is defined above. |
| 2 Visual hierarchy | Existing shell navigation, entity evidence headers, stable list/detail layout, tables, and explicit immutable IDs make current scientific state scannable. |
| 3 Color | 60/30/10 palette and bounded semantic statuses are explicit; status includes text/shape and accent is reserved for named actions/evidence. |
| 4 Typography | Exactly four sizes and two weights are declared; evidence values use legible tabular/monospace treatment. |
| 5 Spacing | Seven multiple-of-four tokens and desktop-target exceptions are declared. |
| 6 Interaction and integrity | Explicit publication/freeze/collection confirmations, acknowledgement/uncertainty handling, conflict behavior, append-only history, and Agent-only authority are binding. |
| 7 Component provenance and safety | No design-system package is present; existing desktop primitives are reused, no registries/blocks are admitted, and Phase 02.1 redesign work is expressly fenced. |

## Checker Sign-Off

- [x] Dimension 1 Copywriting: PASS
- [x] Dimension 2 Visuals: PASS
- [x] Dimension 3 Color: PASS
- [x] Dimension 4 Typography: PASS
- [x] Dimension 5 Spacing: PASS
- [x] Dimension 6 Registry Safety: PASS
- [x] Dimension 7 Inventory Provenance: PASS

**Approval:** approved 2026-10-30 — 7/7 dimensions verified.

**Nonblocking checker recommendations:** Control labels should name their scientific object; define a primary visual anchor for each primary surface. These recommendations do not alter the approved contract, locked context decisions, resolved UI Considerations, or Phase 02.1 design fences.
