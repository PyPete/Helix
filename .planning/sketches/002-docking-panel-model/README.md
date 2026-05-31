---
sketch: "002"
name: docking-panel-model
question: "How should dockable panels be arranged and interacted with?"
winner: null
tags: [layout, docking, panels, ide]
---

# Sketch 002: Docking Panel Model

## Design Question
How should the IDE-style docking workspace feel? Three fundamentally different panel models to compare.

## How to View
Open `.planning/sketches/002-docking-panel-model/index.html` in a browser.

## Variants
- **A: VS Split Panels** — Activity bar + fixed left/right sidebars with draggable splitters. Document tabs in the center. Closest to VS Code.
- **B: JetBrains Tab Groups** — Left/right dock groups each with their own tab strip. Multiple panels share a slot via tabs. Closest to IntelliJ/Rider.
- **C: Floating Panels** — All panels float freely over the canvas. Draggable, resizable, dock-zone drop targets shown during drag. Closest to Photoshop/Figma.

## What to Look For
- Does the split-panel model (A) feel too rigid for a lab UI with many active panels?
- Do tabbed dock groups (B) feel more flexible for power users who want Device Explorer + Safety in the same slot?
- Do floating panels (C) feel too chaotic for a data-dense scientific tool, or liberating?
- Which gives the workflow canvas (center) the most usable real estate?
