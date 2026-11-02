---
phase: "03"
slug: "clean-room-scientific-workflow-designer"
status: draft
nyquist_compliant: false
wave_0_complete: false
created: "2026-11-02"
---

# Phase 3 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution. Unsupported evidence is a required fail-closed outcome, never a substituted source of scientific truth.

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Vitest 4.1.7 with React Testing Library and jsdom |
| **Config file** | `helix-desktop/vitest.config.ts` |
| **Quick run command** | `npm --prefix helix-desktop test -- --run` |
| **Full suite command** | `npm --prefix helix-desktop test -- --run` |
| **Build command** | `npm --prefix helix-desktop run build` |
| **Estimated runtime** | under 60 seconds for the focused/full desktop suite |

## Sampling Rate

- **After every task commit:** `npm --prefix helix-desktop test -- --run`
- **After every plan wave:** `npm --prefix helix-desktop test -- --run`
- **Before the installed-Tauri checkpoint:** `npm --prefix helix-desktop run build && npm --prefix helix-desktop test -- --run`
- **Max feedback latency:** 60 seconds

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 03-01-01 | 01 | 1 | UX-01 | T-03-01 | Native health is threaded only through HelixShell→WorkspaceLayout; board and legacy comparator are reachable. | integration component | `npm --prefix helix-desktop test -- --run` | ✅ existing tests extended | ⬜ pending |
| 03-01-02 | 01 | 1 | UX-01 | T-03-02 | Health-only, missing, malformed, unhealthy, and unsupported evidence cannot grant authority. | unit/component | `npm --prefix helix-desktop test -- --run` | ❌ created by task | ⬜ pending |
| 03-01-03 | 01 | 1 | UX-01 | T-03-02 | Persistent reconciliation status exposes evidence/retry navigation only and remains read-only. | component | `npm --prefix helix-desktop test -- --run` | ❌ created by task | ⬜ pending |
| 03-02-01 | 02 | 2 | UX-01 | T-03-03 | Local selected-run/lifecycle, stream state, operation context, and command history retain their source labels and never become reconciliation authority. | unit/integration | `npm --prefix helix-desktop test -- --run` | ❌ created by task | ⬜ pending |
| 03-02-02 | 02 | 2 | UX-01 | T-03-04 | Six views pass through supported immutable evidence and render unavailable/read-only for absent results, records, diagnostics, or conflicts. | component | `npm --prefix helix-desktop test -- --run` | ❌ created by task | ⬜ pending |
| 03-03-01 | 03 | 3 | UX-01 | T-03-05 | Palette/inspector show only Agent-declared logical fields and unavailable metadata stays non-editable. | component | `npm --prefix helix-desktop test -- --run` | ❌ created by task | ⬜ pending |
| 03-03-02 | 03 | 3 | UX-01 | T-03-06 | If/Else and Fork/Join preserve explicit proposal structure without simulating admission, lease, or resource checks. | component | `npm --prefix helix-desktop test -- --run` | ❌ created by task | ⬜ pending |
| 03-04-01 | 04 | 4 | UX-01 | T-03-07 | Dirty context changes require acknowledged Save, Discard, or Keep editing and retain original target/operation identity. | unit/component | `npm --prefix helix-desktop test -- --run` | ❌ created by task | ⬜ pending |
| 03-04-02 | 04 | 4 | UX-01 | T-03-08 | Keyboard/screen-reader paths receive the same read-only gate, names, focus, and status announcements. | accessibility component | `npm --prefix helix-desktop test -- --run` | ❌ created by task | ⬜ pending |
| 03-05-01 | 05 | 5 | UX-01 | T-03-09 | Provenance/UAT records require six actual evidence rows and preserve a pending promotion disposition. | planning-record structural | `node .planning/phases/03-clean-room-scientific-workflow-designer/03-validate-evidence.cjs` | ❌ created by task | ⬜ pending |
| 03-05-02 | 05 | 5 | UX-01 | T-03-10 | Installed-Tauri comparison and explicit scientist decision are not replaced by automated or synthetic evidence. | manual checkpoint + build | `npm --prefix helix-desktop run build` | ❌ human evidence required | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

## Fail-Closed Source Contract

| Source | Supported use | Explicitly unsupported/absent condition | Required validation |
|--------|---------------|------------------------------------------|---------------------|
| `useAgentHealth` | Native-broker `healthy`, `error`, `lastChecked` threaded by HelixShell. | Health is only an eligibility observation; it is not a reconciled lifecycle. | Test pending, exact healthy, unhealthy, and error; healthy without complete evidence remains read-only. |
| HelixShell local selected run/lifecycle | Label as local/event-derived presentation only. | No Agent receipt and no contradiction comparator. | Test null/unmapped and present local values do not enable edits or claim Agent reconciliation. |
| `useAgentWebSocket` | Committed event records and connection/loading/error/partial indicators. | Disconnected, partial, errored, empty, or unmatched stream cannot prove safety. | Test each stream condition renders unavailable/read-only. |
| `useScientificOperation` | Board-originated original-operation reconciliation only if one exists. | No shell-wide or last-operation record; session storage is not evidence. | Test absence/pending/uncertain has no outcome and preserves original operation ID. |
| `useCommandEvidence` | Known-ID cursor-backed committed history and evidence navigation. | No known ID, loading, unavailable, or empty history cannot produce a lifecycle or approval. | Test immutable ordered history and all missing/partial branches remain read-only. |
| Scientific six-view/capability/diagnostic/result inputs | Exact current native records only. | No shared board record, result record, diagnostics, or metadata beyond current descriptors. | Test explicit unavailable presentation; no synthetic IDs, revisions, samples, results, or execution facts. |

## Wave 0 Requirements

Existing Vitest/Testing Library infrastructure covers all phase requirements. Each task creates or extends the listed test in the same task before implementation; no task declares a missing test dependency.

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Installed-Tauri six-view comparison | UX-01 | Native window, real authenticated Agent records, and comparator interaction are outside jsdom. | Run `npm --prefix helix-desktop run tauri -- dev`; compare setup, experiment, plan, run, results, and samples/analysis; record actual IDs/revisions/status/outcomes/evidence locations. |
| Keyboard-only and screen-reader evidence | UX-01 | Assistive technology and installed focus behavior require human observation. | Complete every six-view comparison using keyboard only and a screen reader; record announcements, focus restoration, and defects in `03-INSTALLED-TAURI-UAT.md`. |
| Promotion disposition | UX-01 | Only a scientist can approve or reject complete real evidence. | After all six rows are complete, record the explicit scientist decision in `03-PROMOTION-DECISION.md`; primary selection stays unchanged in this phase. |

## Validation Sign-Off

- [x] All planned tasks have an `<automated>` verify; the human checkpoint also has a build gate.
- [x] Sampling continuity: every implementation task has an automated command.
- [x] Wave 0: existing framework is present and each task creates/extends its own test.
- [x] No watch-mode flags.
- [x] Feedback latency target is under 60 seconds.
- [ ] `nyquist_compliant: true` is set by `/gsd-validate-phase` only after execution evidence is assessed.

**Approval:** pending — installed-Tauri UAT and explicit scientist decision remain future human evidence.
