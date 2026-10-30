# Phase 01 safety-boundary operations

## Supported deployment

Phase 01 supports **one scientist account on one Windows workstation**. Start the
published Agent interactively from that account; it listens only on
`127.0.0.1:5080`/`[::1]:5080`. Container Agent, Site Server control, Linux/systemd,
Windows LocalSystem services, and shared service accounts are unsupported. The
compose profile supplies only local PostgreSQL and MQTT dependencies.

Before launch, inspect `Get-NetTCPConnection -LocalPort 5080`,
`Get-Process -Id <pid> -FileVersionInfo`, and `Get-Item Env:ASPNETCORE_URLS`.
Also inspect `Kestrel__Endpoints__Http__Url`, `ASPNETCORE_URLS`, and `urls` machine,
user, and process overrides. A wildcard, LAN, or non-loopback override must make
startup fail. Inspect `Get-CimInstance Win32_Service -Filter "Name='HelixAgent'"`
and old binary registrations before altering them; this phase does not claim or
replace installed services. Secrets belong in the user-scoped secret store under
the connection-string names, never command lines. The protected authentication
spool and same-user pipe must be ACLed to the enrolled account.

## Enrollment and recovery

The scientist approves limits before dispatch. Session expiry, logout, revocation,
or Agent restart requires pairing again. A same-OS-user adversary remains outside
this boundary. During an in-flight abort, physical outcome can be uncertain: hold
the experiment for review/reconciliation; never retry a command merely because it
is uncertain. An abort remains terminal and independent holds remain held.

## Schema gate and upgrade

Use separate migration and runtime PostgreSQL identities. Set
`HELIX_MIGRATION_USER` and `HELIX_RUNTIME_USER`, then run:

```powershell
powershell -NoProfile -File scripts/verification/phase01-migrate.ps1 -Mode CheckAndApply
```

The gate records immutable migration IDs/checksums, refuses checksum drift, and
checks runtime `SELECT, INSERT` fact-table grants. It reports identifiers only.
Back up legacy provenance before upgrade; destructive migration or data deletion
is not authorized.

## Acceptance inventory

Run `phase01-smoke.ps1 -Scenario All` only against a disposable real PostgreSQL
database. Record effective listeners, executable/version, session account, schema
IDs, and unavailable installed-state checks separately. Native/toolchain/database
absence is a blocking failure, not a skipped acceptance.
