---
phase: "7"
date: "2026-06-01"
---

# Phase 7 Discussion Log

## Areas Discussed

### 1. Fumehood List & Status Source

**Question:** Where does the dashboard get the fumehood list and status?
**Options presented:** New /api/fumehoods endpoint / Derive from campaigns / Placeholder simulated data
**Selected:** Placeholder / simulated data for Phase 7

**Follow-up:** What does a simulated fumehood card show?
**Selected:** Name + status badge + active experiment name (if available) + last-seen timestamp

---

### 2. Navigation & Page Structure

**Question:** How should the dashboard handle navigation?
**Options presented:** React Router with URL routes / Single-page tabs / Collapsible sidebar nav
**Selected:** React Router with URL-based routes

**Follow-up:** Chrome style vs. helix-desktop?
**Selected:** Same dark IDE palette, top navbar instead of Ribbon

---

### 3. Chart Library

**Question:** Which chart library for the dashboard?
**Options presented:** recharts (reuse helix-desktop) / lightweight alternative / You decide
**User input (free text):** SciChart.js

**Follow-up:** Licence handling for SciChart.js?
**Options presented:** Community licence / Placeholder (recharts first) / Full paid licence wiring now
**Selected:** Full SciChart.js with paid licence wiring now

---

### 4. Live Trend Connection

**Question:** How should the dashboard receive live trend data?
**Options presented:** ASP.NET Core WebSocket endpoint / SignalR Hub / REST polling only
**User asked:** Which is language-independent?
**Clarified:** Raw WebSocket is most language-independent — no .NET client library needed in browser.
**Selected:** Raw WebSocket endpoint on SiteServer

**Follow-up:** Message format?
**Selected:** Same JSON frame format as Agent WebSocket (TrendValue / RunspaceEvent)

---

## Deferred Ideas

- Real /api/fumehoods endpoint backed by live Agent health aggregation
- SciChart.js licence key management at deployment
