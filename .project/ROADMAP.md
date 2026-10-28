# Roadmap — Helix Vision Mark II

<!-- Written and owned by gsd-path-roadmap. Rolling-wave: milestone-granularity
     scope for EVERY milestone; waves and tasks are planned only for the active
     milestone. Never edit a shipped entry except its Status, Archive, and
     Integrated fields; never edit an abandoned entry at all. Lives at
     .project/ top
     level and persists across milestones; it never archives. -->

## Milestones

### M001 — durable-local-campaign-skeleton

Goal: A scientist can start one Agent-authorised simulated activity from a minimal local desktop campaign workspace and recover its durable pending external-evidence wait without a duplicate physical command or retained lease.
Depends on: []
Surfaces: local helix-desktop campaign workspace; Agent external-evidence ingress endpoint
Status: active
Archive: null
Integrated: null

Scope: in
- Deliver the smallest campaign-centred local desktop slice: start one bounded simulated activity, see its durable identity, lifecycle, command/audit history, and pending external-evidence state, while retaining the existing shell and connection/status surfaces as infrastructure.
- Extend the existing Agent/PostgreSQL/Cleipnir path, rather than adding an orchestration engine, with stable campaign, activity, flow, and command deduplication identities and a correlated external-evidence wait.
- Prove recovery for that activity: after desktop closure or Agent restart while waiting, restore the durable state, accept a correlated completion signal once, explicitly surface uncertain physical outcomes, and reacquire lease plus revalidate capability and safety before a subsequent command.
- Establish the Agent-only capability, lease, validation, safety, and audit boundary for the demonstrator and align documentation with its shipped runtime and recovery contract.

Scope: out
- Apparatus configuration or visual editing, sample collection and accessions, analytical-result revisions, calculations, decisions, proposals, and plan revisions; these are delivered by M002 and M003.
- General activity types, campaign-history replay, broad result-source support, and desktop redesign beyond the demonstrator; these are delivered by M004.
- Browser/dashboard work, direct AI or remote-driver device control, and any SiteServer, internet, LIMS, or external-analysis requirement for authorised local work; all are charter vetoes.

Success criteria
1. In the local helix-desktop campaign workspace, a scientist starts the bounded simulated activity and observes its durable campaign/activity identity, pending state, and Agent audit history.
2. The Agent external-evidence ingress endpoint accepts one signal correlated to that pending activity and rejects or safely deduplicates a repeated signal without claiming that an offline instrument was controlled or completed automatically.
3. After the desktop is closed or the Agent restarts during the wait, the campaign workspace restores the pending or completed state; recovery neither repeats the physical command nor leaves a device lease held, and any later command follows renewed lease, capability, and safety checks.
4. The delivered documentation describes the supported local execution, ingress, recovery, and authority boundary and removes or labels conflicting historic claims.

Risks
- Existing Cleipnir integration, in-memory runspace state, and in-memory leases may not provide the demonstrated recovery semantics when wired end to end.
- Durable correlation, deduplication, ordering, and explicit uncertain-outcome handling may require a contract not present in the current Agent.

Open questions
- What durable ingress correlation, deduplication, ordering, and uncertain-physical-outcome contract governs the demonstrator and its later analytical-result use?

### M002 — apparatus-and-evidence-loop-demonstrator

Goal: A scientist can configure the bounded simulated apparatus, run it through the recovered Agent path, collect a sample, and review one provenance-bearing HPLC result and calculation in the local desktop.
Depends on: [M001]
Surfaces: local helix-desktop apparatus and campaign workspace; Agent analytical-result ingress endpoint
Status: pending
Archive: null
Integrated: null

Scope: in
- Deliver the paired Setup, Experiment, Run, Results, and Samples & Analysis slice for one simulated two-pump, mixer, reactor, and sampling-point apparatus; bind two distinct pump instances and keep design, physical verification, and observed live status visibly distinct.
- Validate the in-tree React Flow apparatus-editor candidate with realistic representative performance, real-canvas interaction, keyboard/assistive-technology/contrast, package/runtime and commercial-attribution gates before relying on it; pause the direction if a gate fails.
- Run one simulated reaction through M001's Agent path, issue a stable scannable sample accession at confirmed manual collection, and retain campaign/run lineage and the collection attestation.
- Accept one source-specific HPLC result as an immutable provenance-bearing unreviewed revision with identifiable retrievable raw artifact, required linkage/method/result/unit validation, and approval of that exact revision.
- Show one defined versioned calculation's definition/implementation, inputs, units/factors, output, and validation evidence after the approved revision, and update documentation for the bounded apparatus, sample, result, and artifact contracts.

Scope: out
- Treating an apparatus diagram as proof of physical connectivity, safety, or observed state; design, verification, and observation remain separate.
- A universal label payload, CSV schema, or external analytical-system replacement; only the bounded, versioned source mapping is supported here.
- Proposal generation, decision policy, approved next-experiment execution, and general apparatus-model administration; these are delivered by M003 and M004.

Success criteria
1. In the local helix-desktop apparatus workspace, a scientist configures the simulated two pumps, mixer, reactor, and sampling point, binds distinct pumps, and can distinguish design, recorded verification, and observed live status.
2. In the local helix-desktop campaign workspace, the scientist runs the simulated reaction through the Agent, completes the collection attestation, and receives a stable scannable accession linked to its campaign and source run.
3. The Agent analytical-result ingress endpoint accepts the bounded mapped HPLC revision only with required linkage, fields, and units, preserves retrievable raw provenance, and safely handles duplicate or out-of-order delivery as specified by M001's contract.
4. In the local helix-desktop Results and Samples & Analysis surfaces, the scientist sees the imported revision as unreviewed, approves the exact revision, and then inspects the defined calculation with its versioned inputs, units/factors, output, and validation evidence.
5. Documentation identifies the supported apparatus, sample, HPLC mapping, review, artifact, and recovery limits.

Risks
- The React Flow candidate may fail operator-quality, accessibility, packaging/CI, or commercial gates; no lower-quality substitute may be silently adopted.
- Artifact storage topology and retention may not support accurate, ready retrieval of the imported source evidence.
- Existing device-health display is synthetic and must not be presented as observed status.

Open questions
- What representative graph size/update rate, accessibility test matrix, target-machine/signing expectation, and React Flow attribution or Pro authorization define the editor gate?
- What artifact-store topology and retention policy provide the required identifiable, retrievable raw-result evidence?

### M003 — governed-adaptive-experiment-loop

Goal: Scientists can turn approved evidence into an explainable, deterministically validated, policy-approved next simulated experiment that executes only through the Agent.
Depends on: [M001, M002]
Surfaces: local helix-desktop campaign planning and approval workspace
Status: pending
Archive: null
Integrated: null

Scope: in
- Add explainable rule, optimiser, and AI proposal sources over approved scientific evidence, retaining proposal evidence and explanations as versioned decision-support records.
- Deterministically validate proposal feasibility against the apparatus, resources, and safety constraints, record the decision and configured human approval state, and create a traceable plan revision.
- Permit only an approved proposal to become a simulated execution candidate, routed through the Agent capability, lease, validation, safety, and audit path, and document the policy and authority boundary.

Scope: out
- Direct or unrestricted AI-to-device control, automatic approval, or any bypass of Agent authority; these are charter vetoes.
- Automatic offline-instrument scheduling or completion claims without external evidence.
- Generalisation of activity, apparatus, sample, result, and campaign-replay administration; this is delivered by M004.

Success criteria
1. In the local helix-desktop planning and approval workspace, a scientist can inspect a rule, optimiser, or AI proposal's evidence and explanation and see deterministic apparatus, resource, and safety validation.
2. In that workspace, a scientist can approve or reject the proposal according to configured policy; approval records a decision and produces a traceable plan revision.
3. An approved proposal can start its next simulated experiment only through the Agent's capability, lease, validation, safety, and audit path, visible from the campaign workspace.
4. Documentation describes supported proposal sources, validation, approval policy, and the prohibition on decision-support authority over devices.

Risks
- Proposal explanation or validation may be mistaken for physical authority unless the Agent boundary is explicit in the interface and runtime.
- Resource and safety feasibility rules may be insufficiently deterministic or complete for an approved plan revision.

Open questions
- None

### M004 — reusable-scientific-workstation-and-documentation-alignment

Goal: The local desktop supports reusable versioned campaign and apparatus records with complete durable replay and documentation that accurately describes the supported workstation contracts.
Depends on: [M001, M002, M003]
Surfaces: local helix-desktop Setup, Experiment, Plan, Run, Results, and Samples & Analysis workspaces; Agent scientific-record and result-ingress endpoints
Status: pending
Archive: null
Integrated: null

Scope: in
- Generalise the bounded demonstrator into durable, versioned automated, manual, collection, analysis, calculation, approval, and decision activities with explicit dependencies, lifecycle state, ownership, provenance, and campaign history.
- Operationalise independently versioned apparatus models, stable device instances, role bindings, typed capability metadata, safe approved visual editing, and live observed-status projections without collapsing design, verification, and observation.
- Expand sample lineage, test requests, external-result correlation, result revisions, raw artifacts, review, approval, source-specific mappings, and calculation definitions/implementations while preserving versioned replayable inputs, units, factors, outputs, and decisions.
- Complete the paired campaign-centred desktop experience across Setup, Experiment, Plan, Run, Results, and Samples & Analysis, reusing supporting operational panels without treating the active-experiment timeline as the campaign record.
- Provide durable replay of commands, observations, manual attestations, collections, result arrival, approvals, decisions, and plan revisions; maintain local-first recovery and Agent-only authority as scope broadens.
- Reconcile product and operational documentation with current code/runtime, supported configuration, deployment, recovery, integration boundaries, and historic claims.

Scope: out
- Full ELN, LIMS, chromatography-data-system, inventory, or regulatory e-signature parity; supported integrations remain explicitly bounded and versioned.
- A mandatory SiteServer, internet, LIMS, or external-analysis dependency for authorised local fumehood work.
- Resuming or replacing an apparatus editor that failed M002's gate without demonstrated equivalent-or-better operator quality, accessibility, commercial suitability, packaging/CI viability, and explicit approval.

Success criteria
1. Across the local helix-desktop Setup, Experiment, Plan, Run, Results, and Samples & Analysis workspaces, a scientist can create, revise, and inspect the versioned activities, dependencies, ownership, apparatus models, device roles/capabilities, samples, result revisions, calculations, approvals, decisions, and plan revisions of a supported campaign.
2. The Agent scientific-record and result-ingress endpoints preserve supported source-specific mappings, validation, provenance, revision, artifact, and approval boundaries across supported activity and result types.
3. In the local helix-desktop campaign history, a scientist can replay commands, observations, manual attestations, sample collection, analytical-result arrival, approvals, decisions, and plan revisions from durable records without representing records as proof of physical completion or device ownership.
4. Restart recovery of supported activities and external waits preserves local execution authority, lease/capability/safety revalidation, and explicit reconciliation of uncertain physical outcomes.
5. Product and operational documentation accurately describes the supported desktop, Agent, apparatus editor, campaign/replay, decision-support, mapping, artifact, deployment, configuration, and recovery contracts, with historic claims labelled or removed.

Risks
- Generalising activity and result types can blur version, review, provenance, and approval boundaries unless each contract remains explicit.
- A broad campaign history can become less usable than retained operational panels if the campaign-centred information architecture is not maintained.

Open questions
- None
