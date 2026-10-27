# Charter — Helix Vision Mark II

Review panel: off

## Vision

Helix becomes a local-first, fumehood-first scientific workstation that coordinates connected instruments, manual laboratory work, samples, offline analytical testing, and adaptive scientific campaigns. Scientists use the local desktop to model their apparatus, design and run work, capture samples and results, and make auditable next-step decisions without making a central server, an AI system, or an external analytical system the authority for physical execution. The program’s distinctive value is a trustworthy, reproducible feedback loop between physical laboratory work and scientific decision-making.

## Full scope: in

- Local desktop workstation: redesign the primary operator experience through paired vertical slices across Setup, Experiment, Plan, Run, Results, and Samples & Analysis.
- Scientific activity and campaign domain: durable, versioned automated, manual, collection, analysis, calculation, approval, and decision activities with explicit dependencies, lifecycle state, ownership, and provenance.
- Sample and analytical-result domain: stable sample identity, collection attestation, lineage, test requests, result revisions, raw-artifact provenance, review/approval, and durable external-result correlation.
- Trustworthy scientific feedback loop: a simulated two-pump reaction can produce a confirmed sample, wait durably for an offline HPLC result, calculate a defined metric, record a decision, and propose a validated next experiment after required approval.
- Local-first execution and recovery: the .NET Agent remains the durable execution, safety, capability, lease, and recovery authority while the desktop is closed or the optional SiteServer is unavailable.
- Apparatus and device foundations: stable device instances, role bindings, typed capability metadata, an independently versioned apparatus model, and a safe visual apparatus editor with live status projections.
- Scientific decision support: versioned calculations, explainable rule/optimiser/AI proposals, deterministic feasibility and safety validation, and human approval gates where policy requires them.
- Documentation alignment: reconcile product documentation with the current code/runtime unless a deliberately approved program milestone changes the code contract.

## Full scope: out (vetoes)

- Full ELN, LIMS, chromatography-data-system, inventory, or regulatory e-signature feature parity — Helix integrates with external systems rather than replacing them.
- Direct or unrestricted AI-to-device control — the Agent remains the only authority that can execute physical commands.
- Treating an apparatus diagram as proof of physical connectivity, safety, or observed device state — design, physical verification, and live observations remain distinct.
- Mandatory SiteServer, internet, LIMS, or external-analysis availability for authorised local fumehood work — the workstation remains local-first.
- Automatic control, scheduling, or completion claims for offline analytical instruments without external evidence.
- Web-dashboard feature development — the dashboard is parked for future work while the local desktop is the primary surface.
- Substituting a materially lower-quality apparatus-editor experience if the chosen editor approach fails quality, accessibility, or commercial suitability — pause the affected direction for an explicit decision.

## Constraints

- Build on the existing .NET Agent, PostgreSQL/Cleipnir durable-execution baseline, device capability/lease/safety path, and local desktop; avoid speculative rewrites.
- UI redesign and core scientific functionality are delivered together in paired, usable vertical slices.
- Device commands execute only through the Agent’s capability, lease, validation, safety, and audit path; no browser, LLM, or remote driver bypasses this boundary.
- Definitions, executions, methods, mappings, result revisions, calculations, decisions, and approvals must be versioned and traceable where they affect scientific conclusions or physical work.
- No additional fixed deadline, budget, compliance target, operating-system mandate, or external-integration commitment is set at program definition.
- Documentation conflict ruling: current runtime/code is authoritative by default; update documentation unless a later approved milestone intentionally changes code.

## Program success criteria

1. In the local desktop, a scientist can configure a simulated two-pump, mixer, reactor, and sampling-point apparatus; bind two distinct pump instances; and see the apparatus and relevant live state in the primary operator experience.
2. The scientist can run a simulated reaction, receive a manual sample-collection task, confirm collection, and obtain a stable, scannable sample identifier linked to the campaign and source run.
3. The scientist can create an HPLC request, close the UI or restart the Agent while the result is pending, then return to a recovered campaign without duplicate physical actions or a held device lease.
4. The scientist can import a structured analytical result, inspect its provenance and review state, approve the exact result revision, and see a defined calculation and decision trace.
5. Helix can propose next conditions from approved evidence, visibly validate the proposal against apparatus/resource/safety constraints, require configured approval, and execute the approved next simulated experiment only through the Agent.
6. The local desktop can replay the campaign’s commands, observations, manual attestations, sample collection, analytical-result arrival, approvals, decisions, and plan revisions from durable records.
7. Product and operational documentation describes the supported current contracts, configuration, and deployment behavior, with historic claims clearly labelled or removed.

## Open questions

- [RESEARCH] Which existing Agent durable-workflow primitives can safely implement activity execution, external-result waits, idempotency, and restart recovery without creating a second orchestration engine?
- [RESEARCH] Does the preferred apparatus-editor approach meet realistic performance, accessibility, packaging, CI, and commercial-licensing requirements for the local desktop?
- [RESEARCH] What sample-label scope, result-review policy, calculation validation, local artifact storage, and CSV mapping requirements are demonstrated by representative laboratory workflows and exports?
- [RESEARCH] Which existing desktop components can be retained while making the apparatus and scientific campaign the organising user experience?
- [NEEDS-USER] If the preferred apparatus editor fails its quality, accessibility, or commercial decision gate, what criteria justify resuming or changing the paused direction?

## Corrections

- 2026-10-27: "Yes multi milestone."
- 2026-10-27: "I am keen the UI redesign, is baked in early as we go. But core funcitonality should also be focussed on."
- 2026-10-27: "Local desktop is still primary surface. Web dashboard, is parked for future."
- 2026-10-27: "update doc choices is important."
- 2026-10-27: "Yes use Mark II as the charter"
- 2026-10-27: "Use Mark II demo"
- 2026-10-27: "Paired vertical slices"
- 2026-10-27: "Pause until polished UI"
- 2026-10-27: "No additional constraints"
- 2026-10-27: "Off"
- 2026-10-27: "Fix docs by default"
