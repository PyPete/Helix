# Manual Control UI

## Design Decisions

### Card Grid (Sketch 003 — Variant B)
- **One card per capability.** Cards are the unit of interaction — not forms, not toolbar buttons.
- **Quick Execute for no-parameter capabilities:** `Start` and `Stop` show an Execute button directly on the card face without expanding. Reduces interaction for the most common actions.
- **Expand for parameterised capabilities:** `SetFlowRate`, `SetTemperature`, `Collect` show a `▾` chevron; clicking the card header toggles the parameter inputs open.
- **Cards default open** for parameterised capabilities — the scientist lands with inputs visible, not hidden.
- **Card grid layout:** `grid-template-columns: repeat(auto-fill, minmax(210px, 1fr))` — adapts to panel width.
- **Destructive capabilities** (EmergencyStop) use a **danger card style**: `border-color: #5a2020`, dark red header background, danger-coloured title. Visually distinct at a glance.
- **Confirm dialog for destructive actions:** Modal with the capability name + "recorded in event log" message. Cancel/Confirm buttons. Never inline confirmation.

### Execution Feedback
- **Result badge:** inline `rbadge` span next to the Execute button. Shows `✓ Success` (green) or `✗ Error` (red) for 4 seconds then fades. No toast for capability results — feedback stays in context.
- **Loading state:** button text changes to "..." and pointer-events disabled during 500–600ms fake latency. Real implementation: POST to `/devices/{id}/execute`.
- **Execute button:** `btn.primary.sm` (`background: var(--color-primary-dim)`, `border: var(--color-primary)`) for normal capabilities. `btn.danger.sm` for destructive ones.

### Parameter Inputs
- **Numeric inputs** use `type="number"` with `min`/`max`/`step` from `ParameterSchema`.
- **Range sliders** (`type="range"`) mirror numeric inputs — moving slider updates the number input. One-directional: slider → input only (avoids infinite loops).
- **Select dropdowns** for enum parameters (Pump A/B, Reactor R1/R2).
- **Unit label** rendered as dim text to the right of the input (mL/min, °C).
- **Field label:** `text-transform: uppercase`, `letter-spacing: 0.05em`, `font-size: var(--text-xs)`, `color: var(--color-text-muted)`.

### Manual Action Log
- Rendered below the capability panel (Variant C pattern worth borrowing).
- Monospace font, one line per action: `[HH:MM:SS] ✓ vapourtec.SetFlowRate {"pump":"A","flowRate":1.5}`
- Success lines: `var(--color-success)`. Failure lines: `var(--color-danger)`.
- This log feeds directly into the Event Protocol — manual actions recorded identically to automated workflow steps.

## CSS Patterns

```css
/* Card grid */
.card-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(210px, 1fr));
  gap: 10px;
}

/* Capability card */
.cap-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  overflow: hidden;
  transition: border-color 0.15s;
}
.cap-card:hover { border-color: var(--color-border-strong); }
.cap-card.expanded { border-color: var(--color-primary); }

/* Destructive card */
.cap-card.destructive {
  border-color: #5a2020;
}
.cap-card.destructive .cc-header {
  background: #2a1a1a;
}

/* Result badge */
.rbadge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 3px 9px;
  border-radius: var(--radius-full);
  font-size: var(--text-xs);
  font-weight: 600;
  opacity: 0;
  transition: opacity 0.2s;
}
.rbadge.ok  { background: #1a3d2a; color: var(--color-success); opacity: 1; }
.rbadge.err { background: #3d1a1a; color: var(--color-danger);  opacity: 1; }
```

## HTML Structures

```html
<!-- Capability card (parameterised, expandable) -->
<div class="cap-card expanded">
  <div class="cc-header" onclick="toggleCard(this)">
    <div class="cc-icon">💧</div>
    <span class="cc-title">SetFlowRate</span>
    <span class="cc-chevron">▾</span>
  </div>
  <div class="cc-body open">
    <div class="field-row">
      <div class="field">
        <label>Pump</label>
        <select><option>A</option><option>B</option></select>
      </div>
      <div class="field">
        <label>mL/min</label>
        <input type="number" value="1.0" min="0.1" max="10" step="0.1">
      </div>
    </div>
    <input type="range" min="0.1" max="10" step="0.1" value="1.0">
    <div class="btn-row">
      <button class="btn primary sm" onclick="execCapability(...)">Execute</button>
      <span class="rbadge"></span>
    </div>
  </div>
</div>

<!-- Device context header (always above capability panel) -->
<div class="dev-header">
  <div class="dev-icon">⚗</div>
  <div>
    <div class="dev-name">VapourtecR4</div>
    <div class="dev-meta">vapourtec.* · COM3</div>
  </div>
  <div><span class="online-dot"></span>Online</div>
</div>
```

## What to Avoid
- **Variant A (stacked accordion):** Too verbose for no-parameter capabilities. `Start` should be one button, not a section with a header + expand + body + button.
- **Variant C (inline toolbar):** Hides the full capability list. Scientists need to see what is available, not hunt through a toolbar. Better for expert users with 3+ months context; not the right default.
- **No confirm dialog for destructive actions.** Inline "are you sure?" text is easy to miss. The modal forces intent.
- **Toasts for capability results.** Keep feedback in context next to the button — toast overlays distract during live experiments.

## Origin
Synthesized from sketch: 003-manual-control-dialog (Winner: B)
Source files: `sources/003-manual-control-dialog/`
