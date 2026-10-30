# Spike Conventions

Patterns explicitly chosen by the user or repeated across 001/002 are recorded here; no cross-spike usability validation is claimed.

## Stack

Standalone vanilla HTML/CSS/JavaScript ES modules. Node 20+ built-ins for static hosting and `node:test`. No install, bundler, framework, external fonts, remote assets or new dependencies.

## Structure

`NNN-descriptive-name/` holds `index.html`, `style.css`, `app.mjs`, pure `model.mjs`, `model.test.mjs`, and a README with GSD frontmatter. Comparisons may share one numbered folder and identify a/b variants in state and MANIFEST. Root `index.html` is the reusable launcher. `server.mjs` serves only this directory on `127.0.0.1:4317`; `PORT` or a numeric CLI argument overrides the port. Future launcher links are enabled only once their index files exist.

## Patterns

Explicit user preference from 002: graph preferred, cards remain first-class interchangeable views with unchanged canonical identities/semantics and retained saved/unsaved fields. Do not infer a usability winner from preference. In 003, keep per-entity pending fields across selection and layout changes; only explicit Save/Discard changes their status. Its isolated stylesheet is named `styles.css` as dispatched.

One model per comparison; switching presentation does not reset apparatus, draft, task or run state. Freeze admitted run revisions; edits invalidate draft review, not the active run. Actions are requests until a separate simulated observation is confirmed. Uncertain lifecycle blocks start/resume/retry, even after reconnecting. Fixture scenario changes are explicit, not recovery commands.

Use native labeled controls, keyboard focus, text plus state symbols, reduced-motion support, and responsive layouts with safety always available. Render observer notes as text, not HTML. Export logs locally as JSON; no telemetry or hardware networking.

## Tools & Libraries

Across 001/002, keep one canonical model and a permanent shared editor while comparing presentation. Preserve unsaved buffers on toggles, review only saved revisions, and export before reload. For 002's explicitly requested fixture replacement, confirm dirty draft/buffer loss and preserve run/log state on cancellation. Reuse the restrained 001 visual stylesheet without importing product code.

Only Node built-ins (`http`, `fs`, `path`, `node:test`, `assert`). Tests target the spike model and localhost server boundary. Do not install reference repositories. Source links pin inspected revisions. Automated semantics and browser checks are distinct from human usability evidence.
