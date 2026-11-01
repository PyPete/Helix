# Deferred Items

- **02-17 Acceptance:** `DurableRunspaceTests.StartAsync_DelegatesToWorkflowExecutor_AndCompletes` and `PauseAsync_CancelsCurrentExecution_AndSetsPausedStatus` expect legacy execution that `DurableRunspace` intentionally disables for safety. Their owning safety/runspace work must update or retire the stale expectations before aggregate Acceptance can pass.
