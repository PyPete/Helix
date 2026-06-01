using System.Data;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Helix.Orchestration.Devices.Abstractions;
using Helix.Orchestration.Devices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Devices.Drivers;

/// <summary>
/// Device adapter for the Syrris Atlas HD Reaction System.
/// Uses File I/O transport via IDirectoryInfo, IReadCsvFile, and IGetProcess.
/// The IDeviceTransport parameter satisfies the base class requirement but is never used for I/O.
/// </summary>
public sealed class AtlasReactionSystem : DeviceAdapterBase
{
    private readonly IDirectoryInfo _directoryInfo;
    private readonly IReadCsvFile _csvReader;
    private readonly IGetProcess _processChecker;

    private string? _fullAtlasLogFile;
    private string? _atlasMethodFile;
    private int _currentRow = -1;
    private string?[] _headers = [];

    public AtlasReactionSystem(
        IDirectoryInfo directoryInfo,
        IReadCsvFile csvReader,
        IGetProcess processChecker,
        IDeviceTransport transport,
        ILogger<AtlasReactionSystem> logger)
        : base(transport, logger)
    {
        _directoryInfo = directoryInfo;
        _csvReader = csvReader;
        _processChecker = processChecker;
    }

    protected override void RegisterCapabilities(CapabilityDispatchBuilder builder)
    {
        // Open: sets the data log directory path
        builder.Register(
            CapabilityDescriptor.CreateWithSchemas(
                "Open",
                "Open the Atlas data log directory for monitoring",
                parameterSchema: "{\"type\":\"object\",\"properties\":{\"dataLogPath\":{\"type\":\"string\"}},\"required\":[\"dataLogPath\"]}"),
            (parameters, _, _) =>
            {
                var dataLogPath = parameters.GetProperty("dataLogPath").GetString()
                    ?? string.Empty;

                var pathExists = _directoryInfo.CheckFolderExists(dataLogPath);
                if (!pathExists)
                    return Task.FromResult(CapabilityInvocationResult.Failed($"Path does not exist: {dataLogPath}"));

                _directoryInfo.SetPath(dataLogPath);
                _fullAtlasLogFile = dataLogPath;

                return Task.FromResult(CapabilityInvocationResult.Successful());
            });

        // IsDevice: checks if the Atlas desktop process is running
        builder.RegisterSimple(
            "IsDevice",
            "Check whether the Atlas desktop application is running",
            _ =>
            {
                var (atlasRunning, fullTitle) = _processChecker.FindProcessByName("Atlas");
                if (!atlasRunning)
                    return Task.FromResult(CapabilityInvocationResult.Failed("Atlas application is not running"));

                var parts = fullTitle.Split(['-'], 2);
                if (parts.Length >= 2)
                    _atlasMethodFile = parts[1].TrimStart();

                return Task.FromResult(CapabilityInvocationResult.Successful(
                    JsonSerializer.SerializeToElement(new { atlasRunning, methodFile = _atlasMethodFile })));
            });

        // Close: closes the monitoring session
        builder.RegisterSimple(
            "Close",
            "Close the Atlas data log monitoring session",
            _ =>
            {
                _fullAtlasLogFile = null;
                _headers = [];
                _currentRow = -1;
                return Task.FromResult(CapabilityInvocationResult.Successful());
            });

        // GetLatestData: reads the latest row from the CSV log file
        builder.RegisterSimple(
            "GetLatestData",
            "Read the latest data row from the Atlas CSV log file",
            _ =>
            {
                if (string.IsNullOrEmpty(_fullAtlasLogFile))
                    return Task.FromResult(CapabilityInvocationResult.Failed("No Atlas log file path set; call Open first"));

                try
                {
                    var dt = _csvReader.Read(_fullAtlasLogFile);
                    if (dt.Rows.Count == 0)
                        return Task.FromResult(CapabilityInvocationResult.Failed("CSV file is empty"));

                    var lastRow = dt.Rows.Count - 1;
                    var data = new Dictionary<string, double>();
                    var headers = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();

                    for (var j = 3; j < dt.Rows[lastRow].ItemArray.Length; j++)
                    {
                        if (j < headers.Length &&
                            dt.Rows[lastRow].ItemArray[j] is string s &&
                            double.TryParse(s, out var val))
                        {
                            data[headers[j]] = val;
                        }
                    }

                    return Task.FromResult(CapabilityInvocationResult.Successful(
                        JsonSerializer.SerializeToElement(data)));
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "AtlasReactionSystem: failed to read CSV file");
                    return Task.FromResult(CapabilityInvocationResult.Failed($"Failed to read CSV: {ex.Message}"));
                }
            });
    }

    public override async IAsyncEnumerable<TrendSample> StreamTrendsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // If no log file set, try to find the latest one
            if (string.IsNullOrEmpty(_fullAtlasLogFile))
            {
                TryGetFile();
            }

            if (!string.IsNullOrEmpty(_fullAtlasLogFile))
            {
                DataTable? dt = null;
                try
                {
                    dt = _csvReader.Read(_fullAtlasLogFile);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "AtlasReactionSystem: failed to read CSV in StreamTrendsAsync");
                }

                if (dt is not null && dt.Rows.Count > 0 && dt.Rows.Count - 1 > _currentRow)
                {
                    var lastRow = dt.Rows.Count - 1;
                    var timestamp = DateTimeOffset.UtcNow;

                    // Ensure headers are populated
                    if (_headers.Length == 0)
                    {
                        _headers = dt.Columns.Cast<DataColumn>()
                            .Select(c => c.ColumnName)
                            .ToArray();
                    }

                    for (var j = 3; j < dt.Rows[lastRow].ItemArray.Length; j++)
                    {
                        if (j < _headers.Length &&
                            dt.Rows[lastRow].ItemArray[j] is string s &&
                            double.TryParse(s, out var result))
                        {
                            var trendName = $"atlasreactionsystem.{_headers[j] ?? $"col{j}"}";
                            yield return new TrendSample
                            {
                                TrendName = trendName,
                                Value = result,
                                Timestamp = timestamp,
                                Unit = string.Empty
                            };
                        }
                    }

                    _currentRow = lastRow;
                }
            }

            var delayed = false;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                delayed = true;
            }
            if (delayed) yield break;
        }
    }

    private void TryGetFile()
    {
        try
        {
            var files = _directoryInfo.GetFiles()
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            if (files.Count == 0) return;

            var file = files[0];
            if (file.LastWriteTime.HasValue)
            {
                var dt1 = file.LastWriteTime.Value.Date;
                var dt2 = DateTime.Now.Date;
                if (dt1 != dt2) return;
            }

            var csvTable = _csvReader.Read(file.FullName);
            _headers = csvTable.Columns.Cast<DataColumn>()
                .Select(c => c.ColumnName)
                .ToArray();
            _fullAtlasLogFile = file.FullName;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "AtlasReactionSystem: TryGetFile failed");
        }
    }
}
