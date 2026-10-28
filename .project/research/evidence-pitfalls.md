# Evidence — pitfalls

Dimension: pitfalls
Questions assigned: Does the preferred apparatus-editor approach meet realistic performance, accessibility, packaging, CI, and commercial-licensing requirements for the local desktop?

## Finding: The settled codebase evidence does not identify a preferred apparatus editor

- **Claim**: The supplied settled map establishes a Tauri/React local desktop and mentions `@xyflow/react` as a desktop dependency, but it does not identify an apparatus editor or establish that the existing React Flow workflow canvas is the preferred apparatus-editor approach.
- **Source**: `.project/research/evidence-codebase.md:9-13,16-21`; `helix-desktop/package.json:13-22`; `.project/CHARTER.md:16-17,28` (opened locally).
- **Confidence**: high
- **Why it matters here**: Evidence about the workflow builder cannot by itself prove that a distinct scientific apparatus editor meets the charter’s quality, accessibility, or commercial veto.

## Finding: React Flow is a plausible in-tree canvas candidate, but there is no realistic apparatus performance evidence

- **Claim**: The desktop already uses React Flow 12 (`@xyflow/react`) for a workflow canvas, but neither the supplied map nor the inspected source establishes performance for a representative apparatus graph, device-state update rate, or laboratory workstation.
- **Source**: `helix-desktop/package.json:17`; `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx:296-354`; xyflow, [Performance](https://reactflow.dev/learn/advanced-use/performance) (opened live; advises memoizing node/edge components and callbacks, avoiding broad node/edge subscriptions, and simplifying complex styles for large diagrams).
- **Confidence**: high
- **Why it matters here**: The editor is a candidate with documented optimization techniques, not a demonstrated fit for live apparatus status and realistic graph sizes; the codebase map reports no performance benchmark or build/test result.

## Finding: React Flow has keyboard and screen-reader support, but its availability is not an accessibility audit

- **Claim**: React Flow documents keyboard focus, selection, movement, ARIA descriptions and live announcements as built-in features, while Helix's workflow-node styles encode execution state with border color and opacity and the inspected component/test evidence does not establish assistive-technology conformance.
- **Source**: xyflow, [Accessibility](https://reactflow.dev/learn/advanced-use/accessibility) (opened live); `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx:296-354`; `helix-desktop/src/components/WorkflowBuilder/nodes/HelixNodes.tsx:39-60`; `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.test.tsx:6-60` (opened locally).
- **Confidence**: medium
- **Why it matters here**: Library affordances help, but custom apparatus nodes, live-state meaning, focus order, contrast and screen-reader announcements must work for the actual local operator workflow to pass the charter veto.

## Finding: The current canvas unit tests replace React Flow, so they do not validate its real interactions

- **Claim**: The desktop CI runs Vitest, but `WorkflowBuilder.test.tsx` replaces the entire `@xyflow/react` module with a test double; the checked-in test therefore does not exercise React Flow's rendered canvas, measurements, keyboard navigation, screen-reader output, or real pointer behavior.
- **Source**: `.github/workflows/ci.yml:118-139`; `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.test.tsx:6-60`; xyflow, [Testing](https://reactflow.dev/learn/advanced-use/testing) (opened live; recommends Cypress or Playwright for React Flow applications and describes DOM-measurement needs).
- **Confidence**: high
- **Why it matters here**: Existing CI coverage is not a substitute for browser-level verification of the interactive editor or its accessibility and rendering behavior.

## Finding: Tauri packaging and three-platform build coverage exist, but release signing and target-machine behavior remain separate checks

- **Claim**: The repository configures Tauri bundles and CI builds Linux AppImage/DEB, macOS DMG/app, and Windows NSIS/MSI artifacts; the inspected CI section uploads artifacts without showing signing or notarization steps, and it does not establish behavior on users’ deployed WebView/runtime versions.
- **Source**: `helix-desktop/src-tauri/tauri.conf.json:6-21`; `.github/workflows/ci.yml:181-273`; Tauri, [Distribute](https://v2.tauri.app/distribute/) and [Prerequisites](https://v2.tauri.app/start/prerequisites/) (opened live; identifies platform bundles/signing and platform system dependencies, including Linux WebKitGTK and Windows WebView2).
- **Confidence**: high
- **Why it matters here**: React Flow is a web dependency inside the existing native shell, so desktop packaging is structurally plausible, but installability, signatures, native WebView differences and offline runtime behavior still need release-level validation.

## Finding: React Flow’s core MIT license permits commercial use, but hiding its attribution is a paid Pro condition

- **Claim**: xyflow states React Flow is MIT-licensed and can be used commercially without a subscription, but its official attribution page says subscribing to React Flow Pro permits removing attribution; Helix’s current `WorkflowBuilder` sets `proOptions={{ hideAttribution: true }}` and the repository evidence supplied here does not establish a Pro subscription or license decision.
- **Source**: xyflow, [MIT license](https://raw.githubusercontent.com/xyflow/xyflow/main/LICENSE), [Remove Attribution](https://reactflow.dev/remove-attribution), and [React Flow Pro](https://reactflow.dev/pro) (opened live); `helix-desktop/src/components/WorkflowBuilder/WorkflowBuilder.tsx:311-313` (opened locally).
- **Confidence**: high
- **Why it matters here**: Commercial use of the library itself appears compatible, but the current hidden-attribution configuration raises a distinct commercial authorization/cost question that must be resolved before treating this approach as commercially suitable.

## Finding: The codebase map records no editor-specific failure, but also no proof of readiness

- **Claim**: The supplied brownfield map records two independently maintained frontends, the Tauri desktop and checked-in CI jobs, while explicitly noting that the scan was static and did not run builds or tests; it does not record apparatus-editor performance, accessibility, package-install or commercial-license validation.
- **Source**: `.project/research/evidence-codebase.md:5,9-13,16-21,48-50` (opened locally).
- **Confidence**: high
- **Why it matters here**: These are unresolved validation points rather than established defects; do not treat the absence of a recorded failure as evidence that the editor passes the charter’s quality gate.

## Assigned questions — answers

- Does the preferred apparatus-editor approach meet realistic performance, accessibility, packaging, CI, and commercial-licensing requirements for the local desktop? → **Not established by the supplied settled codebase evidence.** It does not identify a preferred apparatus-specific editor. The identifiable in-tree candidate is React Flow 12 in the Tauri desktop's *workflow* builder: (1) **performance** is plausible but unbenchmarked for representative apparatus graphs/live updates; (2) **accessibility** has documented library keyboard/screen-reader features, but Helix's node-state styling and custom-node/assistive-technology conformance are not established; (3) **packaging** is structurally supported by the current Tauri shell and bundles, subject to platform/runtime and signing validation; (4) **CI** defines bundles across three OSes and runs desktop unit tests, but the canvas test mocks React Flow and no real-canvas browser/accessibility test is evidenced; (5) **commercial licensing** of the MIT core permits commercial use, while the existing `hideAttribution` option requires React Flow Pro per xyflow's official attribution page and no such authorization is evidenced. These sources are not enough to affirm that an apparatus-editor approach passes the charter veto; if the actual editor fails quality, accessibility, or commercial suitability, the charter says pause for explicit decision rather than substitute a materially lower-quality editor.

## Dead ends

- `https://xyflow.com/license` — the current live fetch failed with a transport error (a prior attempt recorded HTTP 404); it did not provide usable license terms, which were instead checked against xyflow’s official React Flow Pro, remove-attribution and raw GitHub MIT-license sources.
- `https://github.com/xyflow/xyflow/blob/main/packages/react/LICENSE` — returned HTTP 404; the repository-root `LICENSE` page opened successfully.
- `https://v2.tauri.app/distribute/sign/` — returned HTTP 404; the live Tauri distribute overview links to platform-specific signing guides and was used only for the documented general signing requirement.
- Web search was not needed: official xyflow and Tauri documentation pages were directly opened with live web reading. No browser/performance or screen-reader test of the Helix desktop was run.
