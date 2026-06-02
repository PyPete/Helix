---
id: experiment-setup
title: "Experiment Setup Guide"
sidebar_position: 4
---

# Experiment Setup Guide

## The Campaign → Experiment → Runspace Lifecycle

Helix organises execution in a three-level hierarchy:

- **Campaign** — groups related experiments (e.g. a parameter sweep). Campaigns are created and managed via the SiteServer REST API or the React dashboard. A campaign holds the workflow definition and controls how many iterations to run.
- **Experiment** — represents one execution run within a campaign. It carries a RunspaceDefinition (compiled WSL workflow) and records the outcome (Completed, Faulted, or Aborted).
- **Runspace** — the live execution engine on the Agent. The Agent creates a DurableRunspace when `POST /runspaces` is called; it tracks step-level progress and survives Agent process restart via the Cleipnir PostgreSQL durability store.

```mermaid
flowchart LR
    A[Create Campaign: POST /api/campaigns] --> B[Start Campaign: POST /api/campaigns/{id}/start]
    B --> C[SiteServer submits Experiment to Agent: POST /runspaces]
    C --> D[Agent runs DurableRunspace]
    D --> E[Step-level status via WebSocket RunspaceEvents]
    E --> F{Runspace outcome}
    F -- Completed --> G[Experiment result recorded]
    F -- Faulted --> H[Fault recorded; campaign may retry or abort]
    G --> I[Next iteration or campaign complete]
```

## Creating a Campaign

**Via the React dashboard:** Navigate to Site Overview → Campaigns → **New Campaign**. Fill in:

- **Name** — a descriptive label for the campaign (e.g. "Temperature sweep 60–90°C").
- **Description** — optional notes about the campaign purpose.
- **Workflow definition** — upload a saved WSL JSON file or select a workflow from the library.
- **Iteration count** — how many times to run the workflow.

**Via the REST API:**

```http
POST /api/campaigns
Content-Type: application/json

{
  "name": "Temperature sweep 60–90°C",
  "workflowDefinitionJson": "{ ... }"
}
```

## Starting a Campaign

**Via the React dashboard:** Open the campaign and click **Start**. The SiteServer begins submitting experiments to the configured Agent. Monitor per-iteration status on the campaign page.

**Via the REST API:**

```http
POST /api/campaigns/{id}/start
```

The SiteServer submits the first experiment immediately and queues subsequent iterations.

## Monitoring Execution

Real-time monitoring is available from two places:

- **Desktop app Execution Panel** — shows each step's status (Pending → Running → Completed / Faulted) via the Agent WebSocket stream. Updates are pushed as the runspace progresses through nodes.
- **React dashboard campaign page** — shows live trends via the `/api/trends/ws` WebSocket endpoint, which bridges MQTT messages from the Agent. The Analytical Viewer displays scalar charts and UV/MIR spectral overlays.

The Execution Panel updates as each step transitions. A faulted step displays the exception message and stack trace (if available) inline.

## Viewing Experiment History

Navigate to the **Experiment History** page in the React dashboard. Filter by fumehood, device, or date range. Each experiment record shows:

- Outcome (Completed, Faulted, or Aborted)
- Step count and total duration
- Result summaries for any capability that returned values

Trend data is browsable in the **Analytical Viewer** (scalar charts, UV/MIR spectral overlay).

## Aborting an Experiment

**Via the REST API:**

```http
DELETE /api/experiments/{id}
```

**Via the desktop app:** Click the **Abort** button in the Execution Panel. The Agent calls `DurableRunspace.AbortAsync`, which requests graceful shutdown of the current step. If a transport fault occurs during abort, the runspace is force-terminated and recorded as Aborted.
