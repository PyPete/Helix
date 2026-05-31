# Spike Manifest

## Idea
Validate allotment (VS Code split-panel library) as the panel layout engine for the
Helix Desktop IDE shell. The target layout: left sidebar (Device Explorer, 220px) +
center (doc tabs + canvas + bottom output strip) + right sidebar (Trend Explorer, 200px),
all resizable with drag handles and collapsible via double-click. rc-dock (alpha) was
considered and dropped — allotment is production-ready and a better fit for the
VS-split-panel layout validated in sketch 002.

## Requirements

- Use `allotment` v1.20.5+ for all panel split/resize functionality
- `proportionalLayout={false}` on every `<Allotment>` instance — required for fixed-width sidebars
- Wrap allotment in a flex container with `min-height: 0` — required to prevent overflow in flex column layouts
- Import `allotment/dist/style.css` at the component level
- Sash colours override with CSS `!important` to match dark theme tokens
- Do NOT use rc-dock — alpha status (v4.0.0-alpha.2), 20x fewer weekly downloads, unnecessary complexity for the split-panel winner

## Spikes

| # | Name | Type | Validates | Verdict | Tags |
|---|------|------|-----------|---------|------|
| 001 | allotment-basic-layout | standard | allotment IDE shell layout works in React 18 + Vite 6 + TS strict | VALIDATED ✓ | allotment, layout, react, vite |
| 002a | allotment-tauri-webview | standard | allotment shell works in Tauri 2 webview without CSP or renderer issues | PENDING | allotment, tauri, webview |