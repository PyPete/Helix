# Phase 02 Scientific Contract Approval

**Decision:** Approved

**Recorded:** 2026-10-30

## Approval Source

- **Scientist response:** `approved`
- **Approved source:** [02-PLAN-CONTRACT.md](./02-PLAN-CONTRACT.md)
- **Source SHA-256:** `67a40715b53e584677f674251378d76391ca379ce75c770142f78b47e0475f10`
- **Approval scope:** The exact proposed contract; no implied laboratory safety, calibration, conservation, depletion, regulatory-signature, or campaign-approval policy.

## Approved Contract Groups

1. **Revision identity and lifecycle** — Server-generated UUID identities for aggregates, revisions, samples, transformations, and manifests; one active editable draft; immutable named publications; explicit expected-token saves; and source provenance for drafts derived from historical publications.
2. **Apparatus catalog and bindings** — Component types `Vessel`, `Pump`, `Valve`, `Sensor`, `Collector`, and `DeviceModule`; roles `Source`, `Transfer`, `Control`, `Observe`, and `Collection`; port media `Fluid`, `Gas`, and `Signal`; directions `In`, `Out`, and `Bidirectional`; non-empty component roles; matching media/direction/cardinality; distinct endpoints; and a discovered DeviceId binding distinct from editable labels. Physical loops and unbound components remain allowed. A discovered identity may not be claimed by multiple physical instances.
3. **Exact units and assertions** — The case-sensitive unit table `1`, `mL`, `L`, `g`, `mg`, `s`, `min`, `Cel`, `kPa`, and `mL/min`, with the approved dimensions and no implicit conversion. Capability constraints, calibration records, and inspection records are attributable recorded assertions only; expiry is displayed as an expired record and never supplies live safety authorization or calibration.
4. **Procedures and experiment plans** — Independently revisioned WSL procedures and plans, typed declared parameters/targets, deep-copy instantiation without interpolation or conversion, and explicit derived-procedure adoption. A plan-pinned older procedure takes precedence over a latest-procedure default; a newer procedure is shown but may be used only after explicit adoption and plan publication.
5. **Canonical content** — `scientific-setup/1` UTF-8 compact JSON with ordinal recursive key sorting, stable-ID ordering only for semantically unordered topology collections, retained WSL order, invariant decimals, fixed UTC timestamps, lowercase UUIDs, explicit nulls, rejected duplicate properties, and Agent-side lowercase SHA-256. The six pinned C-01 through C-06 byte vectors/digests remain the approved primitive conformance vectors. Draft-only presentation coordinates and selection remain outside hashed scientific content.
6. **Samples and transformations** — Authenticated explicit sample confirmation, immutable Helix-assigned UUIDs, append-only dated attributed custody/label events projected by server sequence, reported backdating flagged as evidence, future occurrence rejection, and correction with a reason plus same-stream prior-event reference. Split, Pool, and Processing create new child IDs atomically from distinct existing parents; optional quantities are positive and dimension-compatible when supplied. No depletion, conservation, retention, inventory, or signature rule is inferred.
7. **Authority and provenance** — Typed scoped Core ports, actor-bound idempotency receipts, Agent-resolved identity, parameterized/scoped persistence, fixed native routes/commands, and rejection of renderer-provided actor/hash/approval assertions. P-01–P-03 remain approved only for immutable policy content/reference resolution and separate approval-provenance validation; they do not authorize campaign approval or execution.

## Approved Numerical Budgets

| Budget | Approved value |
| --- | ---: |
| Setup draft/publication request | 64 KiB |
| Setup response | 256 KiB |
| Sample/transformation request | 16 KiB |
| Sample response | 128 KiB |
| JSON depth | 32 |
| Components | 100 |
| Ports per component | 16 |
| Connections | 256 |
| Assertions | 100 |
| Procedure nodes | 128 |
| Transformation parents / children | 32 / 32 |
| Page default / maximum | 25 / 100 |
| Lineage traversal depth / returned entries | 32 / 1,000 |
| Query timeout | 5 seconds |

## Baseline Disposition Carried Forward

- The Phase 1 smoke, migration, clean build, UI, and Tauri gates remain waived or unverified, never passed evidence.
- Current baseline failures remain distinct from Phase 2 evidence: Core test subset failed 2 of 144 tests; Agent subset failed 2 of 50 tests; desktop Vitest failed 20 of 125 tests. Infrastructure passed 4 tests. Native guard tests discovered 7 focused tests but do not constitute authenticated native integration acceptance.
- The measured maximal representative topology fixture is 141,625 bytes, above the approved 64 KiB setup-request candidate. The approved response does **not** silently widen the bound. The 02-18 tracer must prove a bounded representation/operation shape or present an explicit coherent adjustment before schema stabilization. Procedure (3,517 bytes) and transformation (442 bytes) representative fixtures fit their candidate request budgets.
- `NU1603`, `NU1902`, and `MSB3277` remain visible Phase 1 baseline concerns when emitted; no warning suppression or waiver-to-pass conversion is approved.

## Explicit Non-Approvals and Handoff

- No apparatus record, recorded assertion, policy content registration, or desktop action authorizes physical dispatch. Fresh Agent capability, safety, lease, resource, budget, and termination admission remains mandatory.
- No production approval provider, approval button, self-approver, or campaign approval/execution workflow is approved here. Production policy authority remains unavailable/fail-closed; the trusted authority is test-only evidence for the consumer contract.
- Phase 02.1 canvas-first redesign, graph/cards toggle, and persistent contextual inspector remain outside this decision pending their separate evidence and explicit approval.
- The P-04 restored tracer remains mandatory. This approval/preflight work does not substitute for authenticated desktop → native session broker → Agent → isolated PostgreSQL tracer evidence.
