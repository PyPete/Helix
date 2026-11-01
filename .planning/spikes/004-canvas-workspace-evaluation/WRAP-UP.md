---
spike: "004"
status: WRAPPED
wrapped_at: "2026-11-01"
production_code_changed: false
---

# Spike 004 Wrap-up — Canvas Workspace Evaluation

## Verified findings carried forward

- **Navigation:** the recorded randomized held-reconciliation observation found cards clearer because navigation was clearer. Adopt cards as the starting navigation surface; open a relationship graph on demand for dependency and parallelism inspection. This is bounded evidence, not a general usability verdict; production validation must compare the cards-first variant with the hybrid.
- **Renderer evidence:** all graph findings recorded before the renderer repair are invalid. The defect mixed SVG connector coordinates with absolutely positioned cards, producing floating lines and leaving only Reconcile reliably visible. Only a re-review of the repaired single-stage renderer (`data-renderer-check="pass"`, five stable nodes, six named edges) may be used as graph evidence.
- **Draft integrity:** changing contextual selection with a pending draft must require Save, Discard, or Keep editing. A draft must never be silently retained, reassigned, autosaved, retargeted, approved, dispatched, or applied to a reviewed run.
- **Truthful status:** keep a persistent reconciliation strip that states the held/uncertain state and routes to safety/evidence detail. It provides no direct Pause, Hold, or Abort action while reconciliation is pending.
- **Typed authoring:** the capability palette exposes Agent-declared logical capabilities and typed controls only. The inspector makes bindings, units, defaults, provenance, quality/review state, and resources inspectable. It must not expose driver calls, transports, or raw device commands.
- **Control flow:** If/Else has explicit True and False ports and an evidence placeholder. Parallel work is represented with explicit Fork and Join plus visible pending Agent resource/lease diagnostics; it is never implied by visual adjacency.
- **Lifecycle and authority:** the proposed lifecycle is Draft ? Agent preflight ? scientist approval ? observed execution. UI submission is a proposal request only; the local Helix Agent remains the only component that can admit or execute a physical command after deterministic validation.

## Clean-room constraint (binding)

A production Designer is an independently designed, **MIT-only clean-room** implementation. It may use the observed conceptual patterns, but must not copy or adapt IvoryOS source, shared UI, styles, exact text/layout, assets, API shapes, names/logos, or branding. Production planning must record independent provenance for terminology, visuals, layout, APIs, and assets.

## Production phase recommendation

**Title:** Proposal-Only Scientific Workflow Designer

**Scope:** Deliver an independently designed cards-first workflow authoring workspace with an on-demand relationship/parallelism graph; typed Agent-declared capability palette and inspector; resolve-first local drafts; explicit If/Else and Fork/Join semantics; a truthful status-and-route reconciliation strip; and the Draft ? Agent preflight ? approval ? observed-execution presentation lifecycle. Integrate only proposal/preflight and observed-state contracts—never UI-to-driver execution. Include comparative UX validation of cards-first versus the repaired hybrid graph and renderer acceptance checks before treating graph usability results as evidence.

**Non-goals:** production device commands, UI authority, reuse of IvoryOS implementation/design material, and changes to Phase 2 implementation.

## Closure record

This spike is wrapped as planning/design evidence. The prior graph-renderer evidence is invalidated; the cards-navigation observation is retained with its single-observation limitation. Any implementation remains approval-gated behind the dedicated production phase above.