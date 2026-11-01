# Spike 004 — Clean-Room Designer Decision

## Decision

Any future Helix Designer implementation must be a clean-room, MIT-only reimplementation inspired only by observed interaction behavior. Do not copy or adapt IvoryOS source, shared-ui code, styles, exact text or layout, assets, API shapes, name/logo, or branding.

## Allowed conceptual patterns

- Three-phase authoring.
- Palette, canvas, and typed inspector.
- Explicit control flow.
- Draft, preflight, and review stages.

## Helix safety boundary

Designer UI is proposal-only. The local Helix Agent remains the sole authority that may address drivers or execute physical commands; every command remains subject to deterministic validation.

## Future production-plan boundary

Before any Designer production plan, confirm: MIT-only clean-room provenance; independently designed terminology, visuals, layout, APIs, assets, and branding; proposal-only UI/Agent-only execution authority; and safety validation, review, and auditability requirements.
