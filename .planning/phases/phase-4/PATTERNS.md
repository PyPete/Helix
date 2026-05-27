# Phase 4 Patterns

**Mapped:** 2026-05-26
**Files analyzed:** 6 test files + 4 implementation files
**Analogs found:** 6 / 6

---

## New File to Closest Analog

| New / Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---------------------|------|-----------|----------------|---------------|
| ModbusTransportTests.cs (extend) | test | request-response fake | same file - existing 3 tests | exact |
| SerialTransportTests.cs (extend) | test | request-response fake | same file - existing 5 tests | exact |
| OpcTransportTests.cs (extend) | test | event-driven NSubstitute | same file - existing 4 tests | exact |
| TcpTransportTests.cs (extend) | test | request-response real loopback | same file - existing 3 tests | exact |
| E2eWorkflowExecutionTests.cs (new) | test | request-response + CRUD in-proc | WslExecutionAdapterTests.cs + DurableRunspaceTests.cs | role-match |
| PostgresRepositoriesTests.cs (modify) | test | CRUD Testcontainers | same file - remove Skip annotations | exact |

---

## Pattern Details

### ModbusTransportTests.cs - extend with WriteMultipleRegisters and ReadInputRegisters

**Analog:** tests/Helix.Orchestration.Devices.Tests/Transports/ModbusTransportTests.cs (same file)

**Constructor / factory pattern** (lines 65-72):
Copy CreateTransport(FakeModbusClient client, byte slaveId = 1) exactly.
Internal test constructor: ModbusDeviceTransport(options, NullLogger, FakeModbusClientFactory)

**FakeModbusClient fields already present (lines 80-150):**
  WriteException        -- set to new IOException to trigger fault on WriteMultipleRegisters
  LastWriteData (byte[]) -- captured from WriteMultipleRegisters call
  InputRegistersResult  -- returned by ReadInputRegisters
  ReadInputException    -- thrown by ReadInputRegisters when set

**Assertion pattern (from ReadHolding tests, lines 12-28):**
  Happy path:  Assert.Equal(expectedBytes, result);
               Assert.Equal((byte)slaveId, client.LastSlaveId);
               Assert.Equal((ushort)address, client.LastAddress);
  Error path:  var ex = await Assert.ThrowsAsync<TransportFaultException>(...);
               Assert.Equal(Modbus RTU, ex.TransportType);
               Assert.Equal(TransportStatus.Faulted, transport.Status);

**New [Fact] tests to add:**
  1. WriteMultipleRegistersAsync_WritesDataToClient
     call with [0xAA, 0xBB]; assert client.LastWriteData == [0xAA, 0xBB]; LastSlaveId and LastAddress match
  2. WriteMultipleRegistersAsync_WhenIOException_SetsFaulted
     client.WriteException = new IOException; assert ThrowsAsync<TransportFaultException>; TransportType == Modbus RTU; Faulted
  3. ReadInputRegistersAsync_ReturnsBytesFromClient
     client.InputRegistersResult = [0x01, 0x02]; assert result equals those bytes
  4. ReadInputRegistersAsync_WhenIOException_SetsFaulted
     client.ReadInputException = new IOException; assert Faulted

**Deviations needed:** None. FakeModbusClient has all required fields.

---

### SerialTransportTests.cs - extend with BaudRate parameter test and timeout edge case

**Analog:** tests/Helix.Orchestration.Devices.Tests/Transports/SerialTransportTests.cs (same file)

**Constructor / factory pattern** (lines 81-86):
CreateTransport(FakeSerialPortConnection port, string terminator)
Creates SerialDeviceTransport with SerialTransportOptions { PortName = COM1, CommandTerminator = terminator } + NullLogger

**BaudRate test deviation:**
CreateTransport does not pass BaudRate. For BaudRate test, construct options inline:
  new SerialTransportOptions { PortName = COM1, CommandTerminator = CRLF, BaudRate = 115200 }
FakeSerialPortConnectionFactory must expose a CapturedOptions property (store options in Create call).

**Timeout test:**
port.ReadDelay = TimeSpan.FromMilliseconds(600) - exceeds ReadTimeoutMs default of 500 ms.
Add no EnqueueRead calls. Worker loop CancellationToken times out, fault propagates.

**EventuallyAsync helper** (lines 88-97 - copy verbatim): 20 x 25ms polling; Assert.True on timeout.

**New [Fact] tests to add:**
  1. SendReceiveAsync_WithCustomBaudRate_PassesBaudRateToFactory
     Extend FakeSerialPortConnectionFactory with CapturedOptions property.
     Assert factory.CapturedOptions.BaudRate == 115200
  2. SendReceiveAsync_WhenNoDataArrivesWithinTimeout_SetsFaulted
     port.ReadDelay = TimeSpan.FromMilliseconds(600); no EnqueueRead
     Assert.ThrowsAnyAsync<Exception>; await EventuallyAsync(() => transport.Status == TransportStatus.Faulted)

**Deviations needed:** FakeSerialPortConnectionFactory needs CapturedOptions property (not in existing file).

---

### OpcTransportTests.cs - extend with DataReceived propagation and unsubscribe tests

**Analog:** tests/Helix.Orchestration.Devices.Tests/Transports/OpcTransportTests.cs (same file)

**Constructor / factory pattern** (lines 80-93):
Copy CreateTransport(IOpcSession session, int reconnectPeriodMs = 5000) exactly.
Returns OpcDeviceTransport via FakeOpcSessionFactory.

**NSubstitute callback capture pattern** (lines 50-62):
  Action<object?>? callback = null;
  session.SubscribeNodeAsync(nodeId, Arg.Do<Action<object?>>(x => callback = x),
      Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(subscription);

**DataReceived observable pattern** (from TcpTransportTests.cs lines 76-95):
  var nextPayload = transport.DataReceived.FirstAsync().ToTask();  // subscribe FIRST
  await transport.SendReceiveAsync(subscribeCommand);
  callback!(99.9);    // fire the captured callback
  var payload = await nextPayload;
  Assert.Contains(nodeId, Encoding.UTF8.GetString(payload));
  Assert.Contains(99.9, Encoding.UTF8.GetString(payload));

**IOpcSubscription (from IOpcSession.cs):** IDisposable + NodeId (string) + IsActive (bool).
Use Substitute.For<IOpcSubscription>().

**New [Fact] tests to add:**
  1. SendReceiveAsync_Subscribe_DataReceived_PropagatesCallbackPayload
     Subscribe to DataReceived.FirstAsync().ToTask() BEFORE invoking callback.
     invoke callback(99.9); await payload; assert JSON contains nodeId and 99.9
  2. SendReceiveAsync_Subscribe_Disconnect_DisposesSubscription
     Execute subscribe; await transport.DisconnectAsync()
     assert subscription.Received(1).Dispose()

**Deviations needed:** None. NSubstitute Received(1) already used in project.

---

### TcpTransportTests.cs - extend with LengthPrefix framing mode test

**Analog:** tests/Helix.Orchestration.Devices.Tests/Transports/TcpTransportTests.cs (same file)

**IMPORTANT - TcpFramingMode.FixedLength does NOT exist.**
Enum values: Delimiter, LengthPrefix, Raw. Phase 4 FixedLength goal maps to LengthPrefix
(4-byte LE int32 prefix before message body). Flag to planner: if true fixed-count-bytes mode
(N bytes, no prefix) is required, TcpFramingMode enum + TcpDeviceTransport read path must be added first.

**Real loopback server pattern** (lines 10-20 - copy exactly):
  TcpListener(IPAddress.Loopback, 0); listener.Start(); port from IPEndPoint;
  serverTask = Task.Run(async () => { ... }); await Task.Delay(50); connect transport; await serverTask;

**LengthPrefix server write pattern (inside serverTask):
  var body = Encoding.ASCII.GetBytes(RESPONSE);
  await stream.WriteAsync(BitConverter.GetBytes(body.Length));  // 4-byte LE int32 prefix
  await stream.WriteAsync(body);
  await stream.FlushAsync();
  await Task.Delay(200);  // hold connection open while client reads

**Transport options:** FramingMode = TcpFramingMode.LengthPrefix, ReceiveTimeoutMs = 5000

**New [Fact] test to add:**
  1. SendReceiveAsync_WithLengthPrefixFraming_ReturnsResponse
     server writes 4-byte length prefix + body; assert response equals body

**Deviations needed:** Use LengthPrefix not FixedLength. See IMPORTANT note above.

---

### E2eWorkflowExecutionTests.cs - new file

**Primary analog:** tests/Helix.Orchestration.Agent.Tests/Execution/WslExecutionAdapterTests.cs
**Secondary analog:** tests/Helix.Orchestration.Agent.Tests/Runspaces/DurableRunspaceTests.cs
**Namespace:** Helix.Orchestration.Agent.Tests

**Additional usings beyond WslExecutionAdapterTests baseline:**
  using Helix.Orchestration.Agent.Streaming;
  using Helix.Orchestration.Agent.Trends;
  using Helix.Orchestration.Core.Capabilities;
  using Helix.Orchestration.Core.Experiment;
  using Helix.Orchestration.Core.Trends;
  using Microsoft.Extensions.Logging.Abstractions;

**StubWorkflowExecutor** (WslExecutionAdapterTests.cs lines 44-59 - copy verbatim):
  Sealed inner class; LastWorkflowName; ExecuteAsync records name, returns Task.CompletedTask;
  GetStatusAsync returns WorkflowStatus.Completed; CancelAsync returns Task.CompletedTask.

**WslDocument construction** (adapt from WslExecutionAdapterTests.cs lines 17-28):
  Name = wf-e2e; Nodes = [WslNode(node-1, Pump, Capability, {deviceId = pump-e2e})]; Edges = []

**ExperimentContext transitions** (ExperimentContext.cs lines 48-65):
  Valid path: Created -> Running -> Completed
  After Completed: ctx.CompletedAt.HasValue == true

**CapabilityRegistry usage** (CapabilityRegistryTests.cs lines 13-27):
  new CapabilityRegistry(); Register(name, invoker); await InvokeAsync(name); Assert result.Success
  StubCapabilityInvoker: ICapabilityInvoker returning CapabilityInvocationResult.Succeeded(value)

**TrendAggregator / InMemoryTrendProvider** (TrendAggregatorScalarTests.cs lines 10-22):
  new InMemoryTrendProvider(); new TrendAggregator(provider, NullLogger<TrendAggregator>.Instance);
  await aggregator.ProcessAsync(flow.rate, new ScalarValue<double>(10.5));
  Assert provider.TryGetScalar(flow.rate, out v) and v == 10.5 and IsTrendAvailableAsync == true

**Four [Fact] tests (one per subsystem leg):

  1. FullWorkflow_WslDocument_ExecutesToCompletion
     Wire: InMemoryDeviceLeasingService + RunspaceManager + StubWorkflowExecutor + WslExecutionAdapter
     await adapter.ExecuteAsync(document, holder-e2e); await runspace.WaitAsync()
     Assert: runspace.Status == RunspaceStatus.Completed
     Assert: leasing.IsHeldBy(pump-e2e, holder-e2e) == true
     Assert: executor.LastWorkflowName == wf-e2e

  2. FullWorkflow_WithTrendAggregator_TrendIsAvailableAfterExecution
     ProcessAsync(flow.rate, new ScalarValue<double>(10.5))
     Assert: TryGetScalar == true; v == 10.5; IsTrendAvailableAsync == true

  3. FullWorkflow_WithCapabilityRegistry_CapabilityInvokable
     Register StubCapabilityInvoker; await InvokeAsync; Assert result.Success

  4. FullWorkflow_ExperimentContext_TransitionsToCompleted
     new ExperimentContext -> Running -> Completed
     Assert: Lifecycle == ExperimentLifecycle.Completed; CompletedAt.HasValue

**Deviations from WslExecutionAdapterTests:** Each Fact tests one subsystem leg independently.
No god-test. Matches project pattern of focused per-method assertions.

---

### PostgresRepositoriesTests.cs - remove [Fact(Skip)] annotations

**Analog:** tests/Helix.Orchestration.Infrastructure.Tests/Persistence/PostgresRepositoriesTests.cs (same file)

**Guard already present:**
PostgresFixture.InitializeAsync catches all exceptions; sets IsAvailable = false when Docker unavailable.
Each test body (lines 62-63): if (_fixture.ConnectionFactory == null or !_fixture.IsAvailable) return;
This is the runtime soft-skip: test returns immediately (passes) without Docker.

**Required change - both test methods:**
  Before: [Fact(Skip = Requires Docker. Run with Docker available.)]
  After:  [Fact]
  - Line 57: PostgresExperimentRepository_SaveAndGetByRunspaceId_Works
  - Line 86: PostgresCampaignRepository_SaveAndGetById_Works

**Deviations needed:** None.

---

## Shared Patterns

### Fake factory injection
Source: ModbusTransportTests.cs (FakeModbusClientFactory, lines 75-82)
        SerialTransportTests.cs (FakeSerialPortConnectionFactory, lines 100-111)
Apply to: All transport test extensions using the internal test constructor
Pattern: sealed inner class implementing factory interface; constructor takes pre-built fake; Create() returns it.

### NSubstitute session mock (OPC only)
Source: OpcTransportTests.cs lines 8-15
Apply to: OpcTransportTests.cs new tests
Pattern: Substitute.For<IOpcSession>() + session.IsConnected.Returns(true) before every test;
         Arg.Do<T> to capture callbacks; subscription.Received(1).Dispose() for verify-dispose.

### Real loopback server (TCP only)
Source: TcpTransportTests.cs lines 10-20
Apply to: TcpTransportTests.cs LengthPrefix test
Pattern: TcpListener(IPAddress.Loopback, 0); listener.Start(); port from IPEndPoint;
         server Task.Run; await Task.Delay(50) before connect; await serverTask at end.

### EventuallyAsync polling helper
Source: SerialTransportTests.cs lines 88-97
Apply to: Serial timeout test asserting Faulted status
Pattern: 20 x 25ms polling; Assert.True(predicate(), message) on timeout.

### StubWorkflowExecutor
Source: WslExecutionAdapterTests.cs lines 44-59 and DurableRunspaceTests.cs lines 36-49 (identical shape)
Apply to: E2eWorkflowExecutionTests.cs
Pattern: sealed inner class; LastWorkflowName/LastWorkflowId; GetStatusAsync returns WorkflowStatus.Completed.

### NullLogger<T>.Instance
Source: all test files in the project
Apply to: all new tests constructing implementation classes
Pattern: always NullLogger<TImpl>.Instance - never a real logger in unit tests.

---

## No Analog Found

None. All six target files have a direct (exact) or role-match analog in the codebase.

---

## Metadata

**Analog search scope:** tests/, Helix.Orchestration.Devices/, Helix.Orchestration.Agent/, Helix.Orchestration.Core/
**Files scanned:** 12 source files read in full
**Pattern extraction date:** 2026-05-26

