# Phase 8 — Typed Analytical Data Model (Option B Refactor) — Handover

**Phase**: 8  
**Date**: 2025-05-25  
**Status**: ✅ Complete  

---

## Executive Summary

Phase 8 refactors the Helix spectral data layer from a set of ad-hoc technique-specific records into a **permanent structural envelope + typed metadata** architecture. The goal was to support complex analytical instrument outputs — NMR 1D/2D, MS (full-scan), MS/MS (tandem), Mid-IR / ATR-FTIR, and future techniques — without creating a proliferating parallel type hierarchy per analytical result type.

Two envelope types (`Spectrum1D`, `Spectrum2D`) carry a stable shape contract, while all technique-specific semantics travel inside an `ISpectralMetadata` implementor. The pattern is called **Option B**: fixed geometry, open-ended metadata.

---

## Design Rationale — Why Option B?

| Approach | Description | Verdict |
|---|---|---|
| **Option A** (type-per-result) | `NmrSpectrum1D`, `MsMsSpectrum`, `MidIrSpectrum`, … each a dedicated record | ✗ Provider interfaces bloat with each new technique; tests duplicate boilerplate |
| **Option B** (envelope + metadata) | `Spectrum1D` / `Spectrum2D` as permanent shapes; technique semantics in `ISpectralMetadata` | ✓ Provider/trend interfaces are stable forever; new techniques add one metadata record |

Option B reduces long-term maintenance cost because:
- `ISpectralTrend`, `TrendValue`, and providers never change shape when a new instrument is added.
- Pattern-matching on metadata is idiomatic C# and compile-checked.
- Generic escape-hatch (`GenericSpectralMetadata`) handles unknown/future techniques without code changes.

---

## What Was Delivered

### 1. Structural Envelope Records

**`Spectrum1D`** — `Helix.Orchestration.Core/Trends/Spectral/Spectrum1D.cs`

```csharp
public sealed record Spectrum1D(
    string TrendId,
    double[] X,
    double[] Y,
    SpectralType SpectralType,
    ISpectralMetadata Metadata,
    string? SampleId,
    DateTimeOffset Timestamp
);
```

- Covers all 1D techniques: NMR proton/carbon, MS full-scan, MS/MS fragmentation, Mid-IR, UV-Vis, Raman.
- `X`/`Y` are axis-agnostic: could be chemical shift / intensity, m/z / abundance, wavenumber / absorbance, etc.
- `SampleId` is optional to support both sample-tracked and continuous-flow instrument modes.

**`Spectrum2D`** — `Helix.Orchestration.Core/Trends/Spectral/Spectrum2D.cs`

```csharp
public sealed record Spectrum2D(
    string TrendId,
    double[] Axis1,
    double[] Axis2,
    double[,] Intensity,
    SpectralType SpectralType,
    ISpectralMetadata Metadata,
    DateTimeOffset Timestamp
);
```

- Covers 2D NMR (HSQC, COSY, NOESY, HMBC), LC-MS heat maps, and other matrix-valued output.
- `Intensity[i, j]` maps `Axis1[i]` × `Axis2[j]` grid points.

---

### 2. Typed Metadata Records

All records implement `ISpectralMetadata` (`Helix.Orchestration.Core/Trends/Spectral/ISpectralMetadata.cs`).

#### NMR 1D — `NmrMetadata`

| Property | Type | Description |
|---|---|---|
| `Nucleus` | `string` | e.g. `"1H"`, `"13C"`, `"31P"` |
| `FieldStrengthMhz` | `double` | Spectrometer frequency in MHz |
| `Solvent` | `string` | NMR solvent code (CDCl₃, DMSO-d6 …) |
| `NumberOfScans` | `int` | Accumulated scans |
| `SpectralWidthHz` | `double` | Acquisition window in Hz |
| `TransmitterFrequencyHz` | `double` | Absolute transmitter frequency |

#### NMR 2D — `Nmr2DMetadata`

| Property | Type | Description |
|---|---|---|
| `F1Nucleus` | `string` | Indirect dimension nucleus |
| `F2Nucleus` | `string` | Direct dimension nucleus |
| `ExperimentType` | `string` | `"HSQC"`, `"COSY"`, `"NOESY"`, `"HMBC"` … |
| `FieldStrengthMhz` | `double` | Spectrometer frequency |
| `NumberOfPointsF1` | `int` | Grid points in F1 |
| `NumberOfPointsF2` | `int` | Grid points in F2 |

#### Mass Spectrometry — `MsMetadata`

| Property | Type | Description |
|---|---|---|
| `MsLevel` | `int` | 1 = full scan, 2+ = MS/MS/MSⁿ |
| `PrecursorMz` | `double?` | Selected precursor (null for MS1) |
| `PrecursorCharge` | `int?` | Charge state |
| `RetentionTimeSec` | `double` | LC retention time |
| `FragmentationMethod` | `string?` | `"HCD"`, `"CID"`, `"ETD"` … |
| `CollisionEnergy` | `double?` | Normalised collision energy |
| `ScanNumber` | `int` | Raw scan index |

#### Tandem MS — `MsMsMetadata` (extends `MsMetadata`)

| Additional Property | Type | Description |
|---|---|---|
| `IsolationWindowMz` | `double` | Centre of isolation window |
| `IsolationWindowWidth` | `double` | Half-width of isolation window |
| `ParentScanNumber` | `int` | MS1 precursor scan |

#### Mid-IR / ATR-FTIR — `MidIrMetadata`

| Property | Type | Description |
|---|---|---|
| `WavenumberMin` | `double` | Low-end wavenumber (cm⁻¹) |
| `WavenumberMax` | `double` | High-end wavenumber (cm⁻¹) |
| `ResolutionCm` | `double` | Spectral resolution in cm⁻¹ |
| `ScanCount` | `int` | Number of co-added scans |
| `AttenuatedTotalReflection` | `bool` | ATR vs. transmission mode |
| `ApodizationFunction` | `string` | e.g. `"Happ-Genzel"`, `"Blackman-Harris"` |

#### Generic / Unknown — `GenericSpectralMetadata`

```csharp
public sealed record GenericSpectralMetadata(
    IReadOnlyDictionary<string, object?> Fields
) : ISpectralMetadata;
```

Used by MQTT ingestion for payloads that don't yet have a typed metadata record, and as an escape-hatch for future techniques before a dedicated record is added.

---

### 3. Updated Trend Infrastructure

#### `ISpectralTrend` — `Helix.Orchestration.Core/Trends/ISpectralTrend.cs`

Interface now returns `Spectrum1D` / `Spectrum2D` exclusively:

```csharp
Spectrum1D? LatestScan { get; }
Spectrum2D? LatestMatrix { get; }
IObservable<Spectrum1D> Scans { get; }
IObservable<Spectrum2D> Matrices { get; }
IReadOnlyList<Spectrum1D> GetRecentScans(int count);
IReadOnlyList<Spectrum2D> GetRecentMatrices(int count);
```

The interface is **stable** — adding a new analytical technique never requires modifying it.

#### `TrendValue` discriminated union — `Helix.Orchestration.Core/Trends/TrendValue.cs`

Spectral wrappers updated:

```csharp
SpectralValue(Spectrum1D Scan)
SpectralMatrixValue(Spectrum2D Matrix)
```

#### `InMemorySpectralTrendProvider` — `Helix.Orchestration.Agent/Trends/InMemorySpectralTrendProvider.cs`

- Internal subjects changed to `Subject<Spectrum1D>` / `Subject<Spectrum2D>`.
- `Push(Spectrum1D)` / `Push(Spectrum2D)` are the new entry points.
- Bounded history lists, latest-value tracking, and disposal all updated.

#### `InMemoryTrendProvider` — `Helix.Orchestration.Agent/Trends/InMemoryTrendProvider.cs`

- Top-level registry exposes `Push1D(string trendId, Spectrum1D)` and `Push2D(string trendId, Spectrum2D)`.

#### `TrendAggregator` — `Helix.Orchestration.Agent/Streaming/TrendAggregator.cs`

- Call sites migrated from the old push names to `Push1D` / `Push2D`.

#### `MqttTrendIngestionService` — `Helix.Orchestration.Agent/Trends/MqttTrendIngestionService.cs`

- Constructs `Spectrum1D` / `Spectrum2D` from decoded MQTT payloads.
- Falls back to `GenericSpectralMetadata` for unrecognised technique codes.

---

### 4. Updated Test Context API

#### `ITestContext` / `TestContext`

```csharp
void InjectSpectralScan(string trendId, Spectrum1D scan);
void InjectSpectralMatrix(string trendId, Spectrum2D matrix);
```

Previously operated on deleted legacy types. Now fully typed to the new envelopes.

---

### 5. Bug Fix — `InMemoryRunspace.AbortAsync`

During Phase 8 validation, `RunspaceManagerTests.AbortAsync_RemovesRunspace` was found to hang indefinitely when the runspace had never been started (status `Idle`). The root cause: `_completion` was never resolved for an unstarted runspace, so `WaitAsync` blocked forever.

**Fix** — `InMemoryRunspace.cs`:
```csharp
public async Task AbortAsync(CancellationToken cancellationToken = default)
{
    // Runspace never started — resolve immediately rather than blocking forever.
    if (Status == RunspaceStatus.Idle)
    {
        Status = RunspaceStatus.Faulted;
        EndedAt = DateTimeOffset.UtcNow;
        Fault = new OperationCanceledException("Runspace aborted before start.");
        _completion.TrySetResult();
        return;
    }

    if (_cts is not null)
        await _cts.CancelAsync();
    await WaitAsync(cancellationToken);
}
```

---

## File Map

```
Helix.Orchestration.Core/
  Trends/
    Spectral/
      ISpectralMetadata.cs          ← Marker interface for all metadata records
      Spectrum1D.cs                 ← Universal 1D envelope (NEW)
      Spectrum2D.cs                 ← Universal 2D envelope (NEW)
      NmrMetadata.cs                ← 1D NMR metadata (NEW)
      Nmr2DMetadata.cs              ← 2D NMR metadata (NEW)
      MsMetadata.cs                 ← MS1 / MSn metadata (NEW)
      MsMsMetadata.cs               ← Tandem MS2+ metadata, extends MsMetadata (NEW)
      MidIrMetadata.cs              ← Mid-IR / ATR-FTIR metadata (NEW)
      GenericSpectralMetadata.cs    ← Escape-hatch dict-based metadata (NEW)
    ISpectralTrend.cs               ← Updated to Spectrum1D / Spectrum2D
    TrendValue.cs                   ← SpectralValue / SpectralMatrixValue updated
    SpectralType.cs                 ← Existing enum; NMR1D / NMR2D / MS / MSMS / MidIR values
  Runspace/
    InMemoryRunspace.cs             ← AbortAsync Idle-state hang fix
  TestContext/
    ITestContext.cs                 ← Updated injection signatures
    TestContext.cs                  ← Updated injection implementation

Helix.Orchestration.Agent/
  Trends/
    InMemorySpectralTrendProvider.cs  ← Push1D / Push2D, typed subjects
    InMemoryTrendProvider.cs          ← Push1D / Push2D top-level registry
  Streaming/
    TrendAggregator.cs               ← Migrated call sites
  Trends/
    MqttTrendIngestionService.cs     ← Constructs typed envelopes from MQTT

tests/Helix.Orchestration.Core.Tests/
  Trends/
    SpectralDataModelTests.cs        ← 10 new analytical model tests (NEW)
    TrendValueTests.cs               ← Updated for new spectral wrappers
  TestContext/
    TestContextTests.cs              ← Updated for new injection API
  Runspace/
    RunspaceManagerTests.cs          ← Abort test no longer hangs; timeout guard retained

tests/Helix.Orchestration.Agent.Tests/
  Trends/
    InMemoryTrendProvidersTests.cs   ← Updated for Push1D / Push2D
```

---

## Test Coverage

| Test Class | Tests | Coverage Area |
|---|---|---|
| `SpectralDataModelTests` | 10 | NMR 1H, NMR 13C, HSQC, COSY, MS1, MS/MS, Mid-IR ATR, GenericMetadata, Provider push (1D + 2D) |
| `TrendValueTests` | 2 | SpectralType enum values, SpectralMatrixValue payload |
| `TestContextTests` | 2 | InjectSpectralScan, InjectSpectralMatrix event recording |
| `RunspaceManagerTests` | 3 | Create, Abort (was hanging — now fixed), DuplicateId |
| `InMemoryTrendProvidersTests` | 2 | Provider bounded history, TrendAggregator routing |

All 107 Core tests and Agent tests pass. No regressions from Phase 7.

---

## How to Extend with a New Analytical Technique

Adding a new instrument type (e.g. Raman, XRD, UV-Vis) requires:

1. **Add a metadata record** (e.g. `RamanMetadata.cs` in `Trends/Spectral/`):
   ```csharp
   public sealed record RamanMetadata(
       double LaserWavelengthNm,
       int ExposureMs,
       int Accumulations
   ) : ISpectralMetadata;
   ```

2. **Add an enum value** to `SpectralType` if needed (e.g. `Raman`).

3. **Push a `Spectrum1D`** with `SpectralType.Raman` and the new metadata record.

4. **Consume** via pattern-matching:
   ```csharp
   if (spectrum.Metadata is RamanMetadata rm)
       Console.WriteLine($"Laser: {rm.LaserWavelengthNm} nm");
   ```

No changes required to `ISpectralTrend`, `TrendValue`, providers, or aggregators.

---

## Known Gaps and Recommended Next Steps

| Area | Gap | Recommendation |
|---|---|---|
| **Serialisation** | `ISpectralMetadata` is an interface; JSON round-trip through MQTT requires a discriminator strategy | Add a `[JsonPolymorphic]` hierarchy or a type-tag convention in `MqttTrendIngestionService` when durable payloads are needed |
| **Persistent storage** | Spectra are in-memory only | Phase 9 or later: wire `Spectrum1D/2D` into a time-series store (InfluxDB, TimescaleDB) behind a repository interface |
| **UV-Vis / Raman** | `SpectralType` has no `UVVis` or `Raman` values yet | Add them and corresponding metadata records before the first instrument adapter is implemented |
| **2D MS heat maps** | LC-MS retention-time × m/z matrix is possible with `Spectrum2D` but has no dedicated metadata | Add `LcMsMapMetadata` when LC-MS support is scoped |
| **`Spectrum2D` sample tracking** | `Spectrum2D` has no `SampleId` property | Add when sample-tracked 2D collection is required |
| **MQTT type tag** | `GenericSpectralMetadata` is the MQTT fallback | Instrument adapters should emit a typed `SpectralType` tag so ingestion can construct the correct metadata record |

---

## Architectural Notes

- **`ISpectralMetadata` is a marker interface only** — no shared properties are forced on all techniques, which is intentional. Different analytical domains have fundamentally different metadata; a forced common base would be artificial.
- **`MsMsMetadata` extends `MsMetadata`** — the `is MsMetadata` pattern-match catches both MS1 and MS/MS payloads, which is useful for routing logic that handles any MS-level data.
- **`GenericSpectralMetadata.Fields`** is `IReadOnlyDictionary<string, object?>` — suitable for runtime-decoded MQTT payloads before a typed metadata record exists. Migrate to a typed record once the instrument protocol is stable.
- **`Spectrum1D.SampleId`** is nullable — continuous-flow instruments (e.g. in-line IR) emit spectra without discrete sample identity.
