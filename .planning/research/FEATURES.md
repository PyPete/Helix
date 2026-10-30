# Feature Landscape

**Domain:** Local-first autonomous laboratory workstation for one fumehood scientist
**Project:** Helix Vision Mark II
**Researched:** 2026-10-29
**Confidence:** MEDIUM — recommendations synthesize primary standards and current lab-informatics/autonomy sources; the exact fumehood hardware and procedures still need phase-specific validation.

## Product Position and Scope Boundary

Helix is not a general ELN, cloud LIMS, or unattended robot controller. It is the scientist's local control-and-evidence workstation: bind the physical setup, define an executable workflow, track samples and results, and run only campaign-approved loops under an explicit human safety envelope. The durable unit of work is a **campaign**: versioned workflow + apparatus binding + input samples + allowed action set/limits + approval + evidence record.

For v1, optimize for one accountable scientist working at one hood. Prefer fast, local, inspectable records over multi-user administration, enterprise integrations, or broad instrument support. The system must make it easy to stop and review—not merely easy to automate.

## Table Stakes

Features users expect for a credible scientific-workstation v1. Missing items undermine reproducibility, traceability, or safe use.

| Feature | Why Expected | Complexity | Notes / v1 Scope |
|---|---|---:|---|
| Local workspace and durable identifiers | Experimental records need a stable, searchable identity and must remain useful without a network connection. FAIR principles call for identifiers and rich metadata. | Med | Create opaque IDs for apparatus, workflow revision, sample, result import, run, campaign, and event. Keep all source artifacts locally with explicit export. Do not promise global DOI/PID registration. |
| Apparatus registry and setup binding | Results need to say which physical setup, configuration, and capabilities produced them. | Med | Register apparatus components, key settings, calibration/inspection references, and supported commands. Bind an immutable setup snapshot to each campaign/run; changes require a new binding or an explicit deviation. |
| Structured, versioned workflows | ELN/LIMS practice expects reusable methods, required metadata, and a clear record of the method actually used. | Med | Author stepwise workflows with parameters, input/output requirements, hold points, and permitted automation hooks. Version on material change; runs point to a revision, never a mutable “latest.” |
| Samples, aliquots, and lineage | Scientists must trace a result back through parent sample, preparation, consumption, and derived material. | High | Model a directed lineage graph: sample creation, split/aliquot, transform, consume, discard, and link to runs/results. Require quantities/units where relevant; prevent cycles and negative inventory. |
| Campaign lifecycle and durable execution state | Long experiments cannot be a transient chat/session. They need resumable state and a visible lifecycle. | High | Draft → approved → running/paused → awaiting-review → completed/aborted. Persist planned work, completed steps, retries, holds, and reasoned state transitions. Never auto-resume after process restart or an abnormal halt. |
| Campaign approval envelope and emergency controls | Physical actions have real consequences. The approval must constrain execution before it starts, not just provide a post-hoc log. | High | At approval show exact workflow revision, apparatus binding, samples, allowed commands, parameter bounds, rate/count/time limits, and expiry. Provide always-visible pause/abort; default to deny/stop on expiry, lost binding, invalid result, or unknown device state. |
| CSV ingestion with validation and scientist review | CSV is a common practical result interchange, but columns, units, and provenance are often ambiguous. | Med | Preserve original bytes and checksum; select a versioned import profile; preview parsing, mappings, unit conversions, errors, and row counts; quarantine invalid rows. Only a review/accept action creates accepted observations. |
| Result review, annotations, and disposition | Raw acquisition is not equivalent to a trusted result. A scientist needs to mark anomalies and decide use. | Med | Support accepted/rejected/needs-review disposition, reason, reviewer, timestamp, source import, and links to samples/runs. Keep raw data unchanged; corrections are new reviewed records. |
| Append-only provenance and export | Audit trails, timestamps, version history, and contextual links are core ELN expectations; FAIR explicitly names detailed provenance. | High | Emit append-only domain events for edits, approvals, command proposals/executions, imports, review, overrides, and lifecycle changes. Export a self-contained campaign evidence bundle (records, raw files, hashes, workflow/binding snapshots, event log). |
| Search and evidence navigation | A local record store is only useful if a scientist can retrieve a past campaign, sample, import, or anomaly. | Low | Search/filter by identifiers, status, workflow, apparatus, dates, tags, and sample lineage. Prioritize campaign timeline and “why is this result here?” navigation over dashboards. |

## Differentiators

Capabilities that make Helix safer and more useful than a notebook plus scripts, without expanding it into a universal lab platform.

| Feature | Value Proposition | Complexity | Notes |
|---|---|---:|---|
| Evidence-first apparatus binding | Turns “which setup did I use?” into a reproducible, reviewable artifact rather than free text. | Med | Show a diff between current apparatus state and the approved binding before a run. A device adapter exposes only declared capabilities; SiLA 2’s typed Feature/Command/Property model is a useful design reference, not a required v1 protocol. |
| Executable campaign contract | Makes bounded autonomy concrete and inspectable: what may happen, how often, with what parameters, and when it ends. | High | Compile workflow permissions, limits, stop conditions, and approval into a deterministic validator outside any AI planner. The planner may propose; it cannot expand its authority. |
| Review-first result promotion | Prevents a malformed CSV or an anomalous measurement from silently steering the next loop. | Med | Import → validate → preview → scientist disposition → accepted observation. Only accepted observations are eligible campaign inputs. |
| Provenance replay view | Lets the scientist reconstruct any conclusion from a campaign timeline: inputs, method, setup, raw CSV, review, commands, and decisions. | High | Build an evidence timeline and lineage links from the same append-only events used for export; avoid separate, drifting audit-log logic. |
| Safe bounded closed loop | Supports genuine iteration while retaining scientist authority. | High | Permit only a narrow loop: propose next allowed run from accepted campaign observations, validate contract, execute through adapter, ingest/review results, repeat until a bounded stop condition. Require a new approval for any changed workflow, apparatus, limits, or new action class. |
| Portable local evidence bundle | Makes local-first credible: a completed campaign can be independently inspected, archived, or handed to a collaborator without server dependence. | Med | Include manifest/version, canonical record exports, raw attachments, checksums, snapshots, and event log. Treat this as archival/export, not a cryptographically regulated signature system. |

## Anti-Features / Explicit Deferrals

These are attractive expansion paths, but building them in v1 weakens the safety boundary or delays the single-hood workflow.

| Anti-Feature | Why Avoid | What to Do Instead |
|---|---|---|
| Open-ended AI agent with direct apparatus access | “Autonomous” without independent capability and limit enforcement creates excessive physical authority and poor auditability. | AI/planner can propose only campaign-valid actions; a deterministic policy gate and adapter enforce allow-list, parameter bounds, counts, rate, and expiry. |
| Fully unattended, self-modifying optimization | Exploration can change conditions, chemistry, or safety assumptions faster than the scientist can assess them. | Bounded loops with explicit termination, scientist-reviewed results, pause/abort, and re-approval for any contract change. |
| Generic remote/cloud operation | Conflicts with local-first use at a fumehood and introduces outages, credential/telemetry risks, and ambiguous remote responsibility. | Make v1 single-machine and offline-capable; export bundles manually. Revisit synchronization only after evidence semantics are stable. |
| Multi-user LIMS/ELN administration | Roles, tenancy, permissions, collaboration, e-signatures, and regulated validation are a separate product. | Assume one accountable operator; capture operator identity locally and design clean export/import boundaries. |
| 21 CFR Part 11/GxP compliance claim | Formal compliance is process- and deployment-specific, not obtained by adding an audit table. | Build provenance-friendly primitives, but state clearly that v1 is not a validated regulated system. |
| Universal device control or a full SiLA/Allotrope implementation | Standards implementation and long-tail drivers would dominate v1 and still not cover bespoke hood hardware. | Provide a narrow adapter contract per supported apparatus. Preserve raw data plus contextual metadata; add standards adapters only for demonstrated hardware/data needs. |
| Automatic data cleaning or silent CSV repair | Conceals scientific decisions and can corrupt campaign inputs. | Validate, display proposed transformations, retain source bytes, and require explicit review/disposition. |
| Rich dashboards, collaboration feeds, and generalized notebook authoring | They distract from the evidence/control loop and create high UI surface without improving safe operation. | Deliver timeline, search, and concise status/exception views; link or export to external ELNs for narrative work. |

## Feature Dependencies

```text
Stable local IDs + append-only provenance
  → workflow revision snapshots + apparatus binding snapshots
  → campaign approval envelope
  → bounded execution policy gate + durable campaign state
  → command execution evidence + pause/abort handling

Samples/lineage + workflow input/output declarations
  → CSV import profile + raw artifact retention
  → validation/review/disposition
  → accepted observations
  → bounded campaign loop proposals

All records + raw artifacts + event log
  → campaign evidence timeline
  → portable evidence-bundle export
```

## MVP Recommendation

Prioritize in this order:

1. **Evidence foundation:** local IDs; apparatus and workflow revision snapshots; append-only events; samples and lineage; searchable campaign timeline.
2. **Human-run campaign:** durable campaign state, explicit approval envelope, manual step execution, pause/abort, and reviewable command/run records.
3. **CSV evidence loop:** raw attachment/checksum, versioned mapping/validation, preview, explicit result disposition, and accepted observations linked to samples/runs.
4. **One bounded autonomous loop:** proposal generation is constrained by a deterministic campaign contract, has a narrow apparatus adapter, and stops safely on every uncertainty path.
5. **Portable evidence bundle:** prove the record survives outside the workstation before extending automation breadth.

Defer: multi-user/cloud synchronization, formal e-signatures/compliance claims, generalized LIMS inventory, arbitrary instrument drivers, semantic-standard implementation, and open-ended autonomous experiment design. These require separate product, security, and validation work.

## Scope Guidance for Roadmap

- Treat **approval and policy enforcement** as product features, not UI affordances. The system must be able to reject a command even if generated by a future planner, script, or UI bug.
- The first autonomous phase must be demonstrably narrower than the manual phase: one apparatus adapter, a finite command allow-list, explicit numeric limits, a bounded iteration count/time window, and a simulator/dry-run path.
- Make CSV acceptance a provenance boundary. Store raw bytes, parser/import-profile version, mapping, validation report, and reviewer disposition; never overwrite a source result.
- Design records as immutable snapshots plus appended events. Do not depend on mutable “current state” alone for campaign evidence.
- At each later phase, validate the chosen fumehood’s physical hazards, interlocks, emergency-stop behavior, and operating procedure with the scientist/site safety process; software controls do not replace them.

## Sources

Primary / high-authority sources:

- Wilkinson et al., *The FAIR Guiding Principles for scientific data management and stewardship* (Scientific Data, 2016): persistent identifiers, rich metadata, and detailed provenance. https://www.nature.com/articles/sdata201618 — **MEDIUM** (Brave retrieval, cross-checked)
- Allotrope Data Format v1.5.3 overview: contextual metadata, analytical observations, workflow context, packaging, and long-term access. https://docs.allotrope.org/Allotrope%20Data%20Format.html — **MEDIUM** (Brave retrieval, cross-checked)
- SiLA specification documentation: feature-based device capabilities, typed commands/properties, and interoperability framing. https://sila2.gitlab.io/sila_base and https://sila-standard.com/standards — **MEDIUM** (Brave retrieval, cross-checked)
- ADePT framework, *Communications Chemistry*: staged autonomous laboratory operation and continuing importance of human-in-the-loop supervision. https://www.nature.com/articles/s42004-026-01932-9 — **MEDIUM** (Brave retrieval; phase-specific safety validation still required)

Supporting practice sources:

- Harvard Medical School, Electronic Lab Notebooks: searchable records, timestamping/provenance, inventory/sample and equipment links. https://datamanagement.hms.harvard.edu/collect-analyze/electronic-lab-notebooks — **MEDIUM**
- CASRAI, ELN template guide: metadata, versioning, and the distinction between audit trails and a simple “last edited” timestamp. https://casrai.org/guides/electronic-lab-notebook-template — **MEDIUM**
- SciSure, ELN/LIMS workflow practice: structured workflows, links among experiments, samples, inventory, and equipment. https://www.scisure.com/blog/i-have-an-eln-now-what — **MEDIUM**

## Research Gaps to Validate During Planning

- Exact apparatus interface(s), hardware interlocks, safe states, and whether a simulator can faithfully exercise the first loop.
- Scientific domain’s required sample identifiers, units, transformations, uncertainty/replicate rules, and CSV schemas.
- Whether campaign evidence needs any institutional retention, encryption, backup, or regulated-record controls beyond the local-first v1 boundary.
- Whether autonomous proposals use rules, statistical optimization, or an LLM; all require the same external policy gate, but their evaluation and validation plans differ.
