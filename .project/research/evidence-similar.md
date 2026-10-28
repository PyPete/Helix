# Evidence — similar

Dimension: similar
Questions assigned: Which existing desktop components can be retained while making the apparatus and scientific campaign the organising user experience?

## Finding: The desktop already has a local operator shell with connection and status surfaces

- **Claim**: `HelixShell` composes Agent health and WebSocket event/trend streams with ribbon, activity bar, workspace, connection settings, and a status bar that displays connection/health and Agent URL.
- **Source**: `helix-desktop/src/shell/HelixShell.tsx:5-13,21-29,124-167` (opened directly); `.project/research/evidence-codebase.md:9-11` identifies this as the Tauri/React desktop; `.project/CHARTER.md:7,11,15` sets local desktop and Agent constraints.
- **Confidence**: high
- **Why it matters here**: These are existing local navigation, Agent-connection, event-stream, and status components relevant to the workstation; their presence does not establish that the current navigation should remain the organising information architecture.

## Finding: The dock workspace supports reusable panels and persisted layouts

- **Claim**: `WorkspaceLayout` uses `rc-dock`, defines a three-column default layout, registers panel factories, can open device-control and chart tabs, and exposes save/load layout operations; `HelixShell` loads saved layout names and invokes those operations.
- **Source**: `helix-desktop/src/shell/WorkspaceLayout.tsx:4,40-46,55-108,131-152,154-242,270-298,300-343,380-390`; `helix-desktop/src/shell/HelixShell.tsx:44-52,74-99` (all opened directly).
- **Confidence**: high
- **Why it matters here**: The existing panel and layout substrate can host apparatus- and campaign-related work alongside run-time tools, but component reuse alone cannot establish whether free-form docking suits the intended scientist workflow.

## Finding: Existing workspace modules cover device inspection, trends, workflow authoring, execution, safety, and logs

- **Claim**: The default dock registers a device explorer, trend explorer, workflow builder, execution panel, output log, safety panel, chart panel, and experiment timeline; device and chart panels can also be opened dynamically.
- **Source**: `helix-desktop/src/shell/WorkspaceLayout.tsx:9-20,55-108,138-152,154-242,278-298` (opened directly); `.project/research/evidence-codebase.md:9-11` confirms the separate local desktop and its Agent integration.
- **Confidence**: high
- **Why it matters here**: These are concrete candidates for contextual Setup/Run capabilities in paired UI/core slices; this inspection does not assess their polish, accessibility, tests, or fit as the primary campaign experience.

## Finding: The current experiment timeline is an active-experiment event list, not the charter's full campaign history

- **Claim**: `ExperimentTimeline` filters runspace and experiment event arrays to one `activeExperimentId`, merges and sorts them, and renders experiment/phase/manual-action events; it has no rendering path for samples, offline-result waits, result revisions, approvals, calculations, or decisions.
- **Source**: `helix-desktop/src/shell/panels/ExperimentTimeline.tsx:5-8,16-55,57-135`; `helix-desktop/src/shell/WorkspaceLayout.tsx:230-242` passes those props; `.project/CHARTER.md:12-17,41-46` describes the broader campaign requirements (all opened directly).
- **Confidence**: high
- **Why it matters here**: The timeline is a possible event-display starting point, but its current scope is not evidence that it can represent the durable, cross-activity scientific campaign required by the charter.

## Finding: DeviceExplorer marks every listed adapter healthy without checking returned health data

- **Claim**: `DeviceExplorer` polls `/devices` and maps each returned adapter to `healthy: true` and `message: 'registered'`; the shown response type contains only `adapterId` and `capabilities`.
- **Source**: `helix-desktop/src/shell/panels/DeviceExplorer.tsx:9-39` (opened directly); `.project/CHARTER.md:16,24,41` distinguishes apparatus design from verified physical/live state.
- **Confidence**: high
- **Why it matters here**: This explorer may be reusable for device discovery, but its present health mapping must not be treated as an observed health projection or as proof that a configured apparatus is physically connected.

## Finding: Comparator lesson — Opentrons documents distinct setup, active-run, recovery, and completion stages

- **Claim**: Opentrons' OT-2 instructions move from protocol selection and setup review to run preview with pause/cancel and then completion or error handling; Flex touchscreen documentation separately describes run progress, error recovery, and completion/summary.
- **Source**: Opentrons, “Running Protocols,” https://docs.opentrons.com/ot-2/opentrons-app/protocol-run/ (opened live); Opentrons, “Protocol Run,” https://docs.opentrons.com/flex/touchscreen/protocol-run/ (opened live).
- **Confidence**: high
- **Why it matters here**: This comparable-product material illustrates how operator context can distinguish preparation, execution, intervention, and outcome; it is not evidence that Helix should copy these screens or that they implement a scientific campaign.

## Finding: Comparator lesson — Opentrons explicitly separates configured fixtures from physical verification

- **Claim**: Flex deck configuration maps fixtures to deck locations and detects protocol/configuration conflicts, while its status definition for “Configured” explicitly instructs the operator to verify that the fixture is physically attached before running.
- **Source**: Opentrons, “Deck Configuration,” https://docs.opentrons.com/flex/touchscreen/deck-config/ (opened live); `.project/CHARTER.md:24` states Helix's corresponding constraint.
- **Confidence**: high
- **Why it matters here**: This is a relevant comparator lesson for keeping apparatus design/configuration distinct from physical verification, safety, and live observations; a diagram cannot serve as physical proof under the charter.

## Finding: Comparator lesson — Flex's touchscreen dashboard separates recent protocols from robot-wide controls

- **Claim**: The Flex touchscreen dashboard shows up to eight recently run protocols as cards with readiness/setup-conflict colors and exposes robot-wide actions separately from navigation to protocols, instruments, and settings.
- **Source**: Opentrons, “Dashboard,” https://docs.opentrons.com/flex/touchscreen/dashboard/ (opened live).
- **Confidence**: high
- **Why it matters here**: This comparable-product pattern separates return-to-work discovery and apparatus-wide controls from a selected run's immediate actions; it concerns Opentrons' robot touchscreen and does not change the charter's decision to park Helix's web dashboard.

## Finding: The opened comparator material does not establish a durable adaptive scientific campaign model

- **Claim**: The opened Opentrons run, setup, deck-configuration, and dashboard pages describe protocols, apparatus setup, and individual runs, but do not document the charter's linked campaign loop of durable offline-analysis waits, result-revision approval, calculations, and evidence-driven next-experiment decisions.
- **Source**: Opentrons “Running Protocols,” https://docs.opentrons.com/ot-2/opentrons-app/protocol-run/; “Deck Configuration,” https://docs.opentrons.com/flex/touchscreen/deck-config/; “Dashboard,” https://docs.opentrons.com/flex/touchscreen/dashboard/; `.project/CHARTER.md:12-17,42-46` (all opened/read).
- **Confidence**: medium
- **Why it matters here**: The comparator provides bounded operator-flow and apparatus-state lessons, not validation of a campaign information architecture for Helix's scientific feedback loop.

## Assigned questions — answers

- Which existing desktop components can be retained while making the apparatus and scientific campaign the organising user experience? → Direct inspection identifies the local `HelixShell` (Agent connection/health, event streams, ribbon/activity/status surfaces) and `rc-dock` workspace/layout substrate, plus device/trend/chart, workflow/execution, safety/log, and experiment-timeline modules as existing components relevant to the apparatus and run experience. Their maturity and campaign suitability are not established here: the timeline displays events for one active experiment rather than the charter's broader sample/result/approval/decision history, and `DeviceExplorer` assigns healthy status without returned health evidence. These codebase observations are separate from Opentrons comparator lessons about staged run contexts and distinguishing configured from physically verified apparatus. Sources: Helix paths above; Opentrons sources above; `.project/CHARTER.md:11-17,24,32-35,41-46`.

## Dead ends

- Web search requests for Opentrons official search results returned HTTP 403; official documentation pages were located through the documentation index and opened directly instead.
- `https://docs.opentrons.com/ot-2/first-run.html` and `https://docs.opentrons.com/software/protocol-designer/about/` returned HTTP 403; they were not used as evidence.
