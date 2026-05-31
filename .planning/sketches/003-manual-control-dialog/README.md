---
sketch: "003"
name: manual-control-dialog
question: "How should auto-generated capability controls be presented?"
winner: null
tags: [manual-control, capabilities, forms, ux]
---

# Sketch 003: Manual Control Dialog

## Design Question
CapabilityDescriptor drives auto-generated UI. How should device capabilities be laid out for a scientist doing manual control?

## How to View
Open `.planning/sketches/003-manual-control-dialog/index.html` in a browser.
Uses VapourtecR4 capabilities as real data: Start, Stop, SetFlowRate, SetTemperature, Collect, EmergencyStop, etc.

## Variants
- **A: Stacked Form** — Accordion sections, one per capability. Expand to reveal params + Execute button. Familiar, thorough, works for many params.
- **B: Card Grid** — Each capability is a card. Simple ones show Quick Execute inline; complex ones expand. Good for scanning all capabilities at once.
- **C: Inline Toolbar** — Capabilities as toolbar buttons. Clicking one reveals a detail panel below. Compact, IDE-like. Fast for power users who know their capabilities.

## What to Look For
- Does A feel too verbose for capabilities with no parameters (Start, Stop)?
- Does B scan well when there are 10+ capabilities?
- Does C (toolbar) feel natural, or does it hide too much from new users?
- All variants fake-execute with 600ms delay + success/failure badge — does the feedback feel right?
- Destructive actions (EmergencyStop) use a confirm dialog — does this friction feel appropriate?
