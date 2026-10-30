---
last_mapped_commit: 25fb557bd430bea160370af41b8deb877170076b
last_mapped_at: 2026-10-29
---
# Coding Conventions

**Analysis Date:** 2026-10-29

## Naming Patterns

**Files:**
- Use PascalCase C# files whose name matches the primary type, such as `Helix.Orchestration.Core/WSL/WslCompiler.cs`, `Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`, and `tests/Helix.Orchestration.Core.Tests/WSL/WslCompilerTests.cs`.
- Keep TypeScript/React implementation files in PascalCase for components and pages (`helix-dashboard/src/pages/CampaignsPage.tsx`), and camelCase for hooks and libraries (`helix-dashboard/src/hooks/useTrendWebSocket.ts`, `helix-desktop/src/lib/workflowLibrary.ts`). Co-locate frontend tests with the target as `*.test.ts(x)`.

**Functions:**
- Use PascalCase for public C# methods and properties and camelCase for local variables, parameters, and private fields. Async C# methods end in `Async`, for example `TcpDeviceTransport.ConnectAsync` in `Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`.
- Use camelCase function names and `useX` names for React hooks, for example `useTrendWebSocket` in `helix-dashboard/src/hooks/useTrendWebSocket.ts`.

**Variables:**
- Use `_camelCase` for private C# instance fields, such as `_messageChannel` and `_logger` in `Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`.
- Use descriptive camelCase in TypeScript, such as `baseUrl`, `trendValues`, and `connected` in `helix-dashboard/src/App.tsx`.

**Types:**
- Use PascalCase for C# classes, interfaces, records, enums, and DTOs. Prefix C# interfaces with `I`, such as `IWslCompiler` and `IDeviceTransport`.
- Use PascalCase TypeScript component and type/interface names; keep props and hook return members camelCase.

## Code Style

**Formatting:**
- C# uses four-space indentation, file-scoped namespaces, and braces on new lines. Follow the layout in `Helix.Orchestration.Core/WSL/WslCompiler.cs`.
- Prefer modern C# collection expressions (`[]`), target-typed construction (`new()`), records/`with` expressions, pattern matching, and nullable reference types as configured in `tests/Helix.Orchestration.Core.Tests/Helix.Orchestration.Core.Tests.csproj`.
- TypeScript formatting differs between apps: dashboard source imports use double quotes and semicolons in `helix-dashboard/src/App.tsx`; its ESLint config uses no semicolons in `helix-dashboard/eslint.config.js`. Preserve the conventions of the edited subtree rather than reformatting unrelated files.

**Linting:**
- Run `npm run lint` in `helix-dashboard`; `helix-dashboard/eslint.config.js` applies ESLint recommended, TypeScript recommended, React Hooks, and Vite React Refresh rules to `*.ts` and `*.tsx`, excluding `dist`.
- Run `dotnet format --verify-no-changes` for C# formatting validation; `.github/workflows/ci.yml` enforces it as documented by `docs/CI.md`.
- CI builds C# with warnings as errors; avoid new compiler warnings and honor nullable annotations.

## Import Organization

**Order:**
1. C#: `System.*` namespaces, third-party/framework namespaces, project namespaces, then `Xunit` in tests; see `Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs` and `tests/Helix.Orchestration.SiteServer.Tests/Endpoints/ExperimentEndpointsTests.cs`.
2. TypeScript: framework/test-library imports, external packages, then relative project imports; see `helix-dashboard/src/pages/CampaignsPage.test.tsx`.
3. Keep a blank line between import groups and use relative imports inside frontend applications.

**Path Aliases:**
- Not detected in application imports. Use relative paths such as `./components/Layout` in `helix-dashboard/src/App.tsx`.

## Error Handling

**Patterns:**
- Guard constructor dependencies with `ArgumentNullException`, as in `TcpDeviceTransport` at `Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`.
- Validate domain input before work and return aggregate diagnostics for compiler-style validation; `WslCompiler.Compile` in `Helix.Orchestration.Core/WSL/WslCompiler.cs` accumulates `CompilationDiagnostic` values and returns a failed result for errors.
- For transport boundaries, catch specific operational exceptions, log contextual details, update state, and throw a domain exception that preserves the inner exception; use the `TransportFaultException` pattern in `TcpDeviceTransport.SendReceiveAsync`.
- Accept `CancellationToken` on I/O and long-running public async APIs, propagate it to framework calls, and do not swallow unexpected exceptions. Intentional cleanup exceptions are narrowly caught in `DisconnectAsync`.
- In React, represent failed remote state in component/hook state and render user-visible error UI; use the fetch-response checks exercised by `helix-dashboard/src/pages/CampaignsPage.test.tsx`.

## Logging

**Framework:** `Microsoft.Extensions.Logging` in C#; browser console logging is not a project pattern in frontend source.

**Patterns:**
- Inject `ILogger<T>` through constructors, keep it in a readonly `_logger` field, and use structured message templates with named values, as in `Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`.
- Use `LogInformation` for lifecycle/state transitions, `LogDebug`/`LogTrace` for transport detail, `LogWarning` for recoverable conditions, and `LogError(exception, ...)` before rethrowing boundary faults.

## Comments

**When to Comment:**
- Comment non-obvious domain rules, concurrency behavior, protocol framing, and security/threat mitigations. `Helix.Orchestration.Core/WSL/WslCompiler.cs` labels its validation/transformation phases and explains loop-limit threats.
- Use Arrange/Act/Assert comments only where a test setup or assertion needs clarification, as in `tests/Helix.Orchestration.Core.Tests/WSL/WslCompilerTests.cs`.

**JSDoc/TSDoc:**
- Use XML documentation for public C# types and members when behavior is externally meaningful; use `<summary>`, `<see>`, and `<inheritdoc/>` as in `Helix.Orchestration.Devices/Transports/Tcp/TcpDeviceTransport.cs`.
- JSDoc/TSDoc is not a prevalent frontend convention; use clear names and small components/hooks instead.

## Function Design

**Size:**
- Extract focused private helpers for validation, state transitions, and framing modes. `WslCompiler` separates validation methods and graph transforms; `TcpDeviceTransport` separates framing reads and `SetStatus`.
- Use expression-bodied members for simple forwarding/read-only members and block bodies for observable work.

**Parameters:**
- Prefer typed options, interfaces, and request/record types over unstructured arguments. Use optional `CancellationToken cancellationToken = default` for cancellable async public APIs.

**Return Values:**
- Return `Task`/`Task<T>` for asynchronous operations; return typed result/diagnostic objects for expected validation failures, and throw typed exceptions for unrecoverable transport failures.
- Use nullable return types deliberately for missing domain records, as in the repositories replaced by `tests/Helix.Orchestration.SiteServer.Tests/HelixWebApplicationFactory.cs`.

## Module Design

**Exports:**
- Keep C# types in feature namespaces matching their folders, e.g. `Helix.Orchestration.Core.WSL` in `Helix.Orchestration.Core/WSL/WslCompiler.cs`.
- Export React components as named exports in dashboard code (`helix-dashboard/src/App.tsx`) and use the existing default-export component convention in desktop entry files (`helix-desktop/src/App.tsx`). Preserve the local convention.

**Barrel Files:**
- Not detected. Import concrete files directly rather than adding barrels.

---

*Convention analysis: 2026-10-29*
