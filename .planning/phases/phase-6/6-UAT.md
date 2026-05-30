---
status: complete
phase: phase-6
source: 06-01-SUMMARY.md, 06-04-SUMMARY.md, 06-05-SUMMARY.md, 06-06-SUMMARY.md, 06-07-SUMMARY.md, 06-08-SUMMARY.md, 06-09-SUMMARY.md, 06-10-SUMMARY.md
started: 2026-05-30T21:00:00Z
updated: 2026-05-30T21:47:00Z
---

## Current Test

number: 1
name: Agent REST API â€” Compile a workflow
expected: |
  POST http://localhost:5080/workflows/compile with a valid WslDocument JSON body
  returns HTTP 200 with a RunspaceDefinition body.
  Sending an invalid document (missing required fields) returns HTTP 422 with a
  diagnostics array describing the error.
awaiting: user response

## Tests

### 1. Agent REST API â€” Compile a workflow
expected: POST /workflows/compile with valid WslDocument returns 200 + RunspaceDefinition. Invalid document returns 422 + diagnostics array.
result: [pending]

### 2. Agent REST API â€” Start, pause, resume, abort a runspace
expected: |
  POST /runspaces with RunspaceDefinition body returns { runspaceId }.
  POST /runspaces/{id}/pause returns 200. POST /runspaces/{id}/resume returns 200.
  POST /runspaces/{id}/abort returns 200. GET /runspaces/{id} returns current status.
  An unknown ID returns 404.
result: [pending]

### 3. Agent REST API â€” List devices
expected: |
  GET /devices returns an array of objects like [{ adapterId, capabilities }].
  Each registered device adapter appears in the list.
result: [pending]

### 4. WorkflowBuilder â€” Canvas renders
expected: |
  Navigating to the Workflow Builder panel in the app shows a canvas area
  (min 600px height). No JavaScript console errors on load.
result: [pending]

### 5. WorkflowBuilder â€” Add nodes via context menu
expected: |
  Right-clicking on the canvas opens a context menu listing node types:
  Step, Condition, Branch, Loop, Parallel, Delay, WaitFor.
  Clicking a type adds a node to the canvas.
result: [pending]

### 6. WorkflowBuilder â€” Capability picker binds to Step node
expected: |
  The capability picker dropdown is populated with names from GET /devices.
  Selecting a capability name from the dropdown updates the selected Step node's
  capability input field (visible on the node or in its properties).
result: [pending]

### 7. WorkflowBuilder â€” Export WSL JSON
expected: |
  Clicking the Export / Run button on the WorkflowBuilder produces a WslDocument
  JSON object that reflects the nodes and connections on the canvas.
result: [pending]

### 8. ExecutionPanel â€” Start an experiment
expected: |
  With a valid workflow definition loaded, clicking Start calls
  POST /workflows/compile then POST /runspaces.
  If compile returns 422, the error message is shown and Start is blocked.
  On success, the runspace ID appears and the step event list updates.
result: [pending]

### 9. ExecutionPanel â€” Pause, resume, abort controls
expected: |
  After starting a runspace, Pause becomes active. Clicking Pause calls
  POST /runspaces/{id}/pause. Resume becomes active after pause.
  Clicking Abort calls POST /runspaces/{id}/abort and the panel resets.
result: [pending]

### 10. Live scalar trend chart updates
expected: |
  While a runspace is active, the Temperature chart (ScalarTrendChart) receives
  TrendValue frames from the WebSocket and visibly updates â€” new data points
  appear on the line chart without a page refresh.
result: [pending]

### 11. Spectral chart shows UV/MIR data
expected: |
  When the Agent emits TrendValue frames with trendName starting "UV:" or "MIR:"
  (e.g. "UV:350", "MIR:1600"), the SpectralChart renders two overlaid lines â€”
  one for UV absorbance vs wavelength, one for MIR â€” instead of blank axes.
result: [pending]

### 12. Safety panel shows interlock rules
expected: |
  While a runspace is active, the SafetyPanel shows at least one interlock rule
  with its description, severity badge, and safe/triggered state.
  If the Agent includes a currentValue in the SafetyEvent frame, it is displayed
  next to the rule (e.g. "23.50").
result: [pending]

### 13. Workflow save and load (Tauri runtime)
expected: |
  In the running Tauri desktop app, type a workflow name in the save field and
  click Save. Close and reopen the app (or just click Loadâ€¦).
  The saved workflow name appears in the dropdown. Selecting it loads the workflow
  back into the WorkflowBuilder canvas.
result: [pending]

### 14. WebSocket event stream during execution
expected: |
  After starting a runspace via the ExecutionPanel, the step event list receives
  at least a runspace-started frame within 10 seconds.
  Trend values appear in the chart area; safety events populate the SafetyPanel.
result: [pending]

## Summary

total: 14
passed: 4
issues: 2
pending: 0
skipped: 7
blocked: 1

## Gaps














