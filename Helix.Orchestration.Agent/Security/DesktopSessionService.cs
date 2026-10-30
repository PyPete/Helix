using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Helix.Orchestration.Core.Execution;

namespace Helix.Orchestration.Agent.Security;

/// <summary>Validates desktop sessions and durably preserves denied-request evidence while SQL is unavailable.</summary>
public sealed class DesktopSessionService
{
    private const long MaximumSpoolBytes = 64L * 1024 * 1024;
    private const int MaximumRecordBytes = 8 * 1024;
    private const int MaximumInFlightRecords = 32;
    private const int ShutdownMarkerReservationBytes = MaximumRecordBytes;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, DesktopSession> _sessions = new(StringComparer.Ordinal);
    private readonly object _spoolLock = new();
    private readonly string _spoolPath;
    private readonly string _ackPath;
    private readonly string _archivePath;
    private readonly string _agentGeneration;
    private long _nextOrdinal;
    private int _inFlightRecords;
    private long _reservedBytes;
    private AuditSpoolState _spoolState;

    public DesktopSessionService(string? spoolDirectory = null, string? agentGeneration = null)
    {
        var directory = spoolDirectory ?? Path.Combine(AppContext.BaseDirectory, "security-evidence");
        _spoolPath = Path.Combine(directory, "denied-attempts.ndjson");
        _ackPath = Path.Combine(directory, "denied-attempts.ack");
        _archivePath = Path.Combine(directory, "denied-attempts.archive.ndjson");
        _agentGeneration = agentGeneration ?? $"agent-{Environment.ProcessId}";

        try
        {
            Directory.CreateDirectory(directory);
            _nextOrdinal = ReadRecords().DefaultIfEmpty().Max(record => record?.SourceOrdinal ?? 0);
            _spoolState = AuditSpoolState.Healthy;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _spoolState = AuditSpoolState.Faulted;
        }
    }

    /// <summary>True only when new command work can be safely admitted.</summary>
    public bool IsAdmissionOpen => _spoolState == AuditSpoolState.Healthy &&
        Volatile.Read(ref _inFlightRecords) < MaximumInFlightRecords &&
        Volatile.Read(ref _reservedBytes) + MaximumRecordBytes <= MaximumSpoolBytes - ShutdownMarkerReservationBytes;

    /// <summary>SQL outage still permits redacted denial evidence to consume previously reserved local capacity.</summary>
    public bool CanRecordDeniedAttempts => _spoolState is AuditSpoolState.Healthy or AuditSpoolState.SqlUnavailable &&
        Volatile.Read(ref _inFlightRecords) < MaximumInFlightRecords &&
        Volatile.Read(ref _reservedBytes) + MaximumRecordBytes <= MaximumSpoolBytes - ShutdownMarkerReservationBytes;

    public AuditSpoolState SpoolState => _spoolState;

    public DesktopSessionIssue Issue(string verifiedActorSid, string displayName, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verifiedActorSid);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime));

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var reference = Guid.NewGuid().ToString("N");
        var expiresAt = DateTimeOffset.UtcNow.Add(lifetime);
        _sessions[Hash(token)] = new DesktopSession(reference, verifiedActorSid, displayName, expiresAt, false);
        return new DesktopSessionIssue(token, reference, expiresAt);
    }

    public bool TryValidate(string? token, out VerifiedDesktopActor? actor)
    {
        actor = null;
        if (string.IsNullOrWhiteSpace(token) || !_sessions.TryGetValue(Hash(token), out var session))
            return false;
        if (session.Revoked || session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            return false;

        actor = new VerifiedDesktopActor(session.Reference, session.ActorSid, session.DisplayName);
        return true;
    }

    public bool Revoke(string reference)
    {
        foreach (var entry in _sessions)
        {
            if (entry.Value.Reference != reference)
                continue;
            _sessions[entry.Key] = entry.Value with { Revoked = true };
            return true;
        }
        return false;
    }

    /// <summary>
    /// Reserves and synchronously flushes a redacted denial before its HTTP response is sent.
    /// A false result means the caller must fail closed without claiming that the request was audited.
    /// </summary>
    public bool TryRecordDeniedAttempt(string method, string route, string reason, VerifiedDesktopActor? verifiedActor = null)
    {
        if (!TryReserve(out var reservation))
            return false;

        try
        {
            var record = new DeniedAttemptRecord(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                Interlocked.Increment(ref _nextOrdinal),
                _agentGeneration,
                Bound(method, 16),
                Bound(route.Split('?', 2)[0], 256),
                Bound(reason, 256),
                verifiedActor?.Sid,
                verifiedActor?.DisplayName,
                verifiedActor?.SessionReference);
            var json = JsonSerializer.Serialize(record, JsonOptions);
            var bytes = Encoding.UTF8.GetByteCount(json) + 1;
            if (bytes > MaximumRecordBytes)
            {
                _spoolState = AuditSpoolState.Faulted;
                return false;
            }

            lock (_spoolLock)
            {
                using var stream = new FileStream(_spoolPath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
                using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
                writer.WriteLine(json);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _spoolState = AuditSpoolState.Faulted;
            return false;
        }
        finally
        {
            reservation.Dispose();
        }
    }

    /// <summary>Replays unacknowledged records by their stable attempt identifier and only reclaims space after durable acknowledgement.</summary>
    public async Task<SpoolDrainResult> DrainAsync(ICommandLedger ledger, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        if (_spoolState == AuditSpoolState.Faulted)
            return new SpoolDrainResult(0, 0, false);

        var acknowledgements = ReadAcknowledgements();
        var records = ReadRecords();
        var committed = 0;
        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hash = HashRecord(record);
            if (acknowledgements.TryGetValue(record.AttemptId, out var acknowledgedHash))
            {
                if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(acknowledgedHash), Convert.FromHexString(hash)))
                {
                    _spoolState = AuditSpoolState.Faulted;
                    return new SpoolDrainResult(committed, records.Count, false);
                }
                continue;
            }

            try
            {
                var intent = new CommandIntent(record.AttemptId, record.AttemptId, record.AttemptId, "agent-security",
                    "security.denied-attempt", JsonSerializer.Serialize(record, JsonOptions), record.VerifiedActorSid ?? "unverified",
                    record.VerifiedActorDisplayName ?? "unverified", record.SessionReference ?? "none", "security-spool-v1", record.OccurredAtUtc);
                var checks = new[]
                {
                    new CommandCheckEvidence("desktop-session", CommandCheckStatus.Failed, "DeniedRequest", record.Reason,
                        record.OccurredAtUtc, $"{record.AgentGeneration}:{record.SourceOrdinal}")
                };
                await ledger.AppendAdmissionAsync(intent, checks, CommandAdmissionState.Rejected, cancellationToken);
                AppendAcknowledgement(record.AttemptId, hash);
                acknowledgements.Add(record.AttemptId, hash);
                committed++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _spoolState = AuditSpoolState.SqlUnavailable;
                return new SpoolDrainResult(committed, records.Count, false);
            }
        }

        ArchiveAcknowledged(records, acknowledgements);
        _spoolState = AuditSpoolState.Healthy;
        return new SpoolDrainResult(committed, records.Count, IsAdmissionOpen);
    }

    private bool TryReserve(out AuditSpoolReservation reservation)
    {
        reservation = default;
        if (!CanRecordDeniedAttempts)
            return false;

        lock (_spoolLock)
        {
            var fileLength = File.Exists(_spoolPath) ? new FileInfo(_spoolPath).Length : 0;
            if (_inFlightRecords >= MaximumInFlightRecords ||
                fileLength + _reservedBytes + MaximumRecordBytes > MaximumSpoolBytes - ShutdownMarkerReservationBytes)
            {
                _spoolState = AuditSpoolState.Saturated;
                return false;
            }
            _inFlightRecords++;
            _reservedBytes += MaximumRecordBytes;
            reservation = new AuditSpoolReservation(this);
            return true;
        }
    }

    private void ReleaseReservation()
    {
        lock (_spoolLock)
        {
            _inFlightRecords--;
            _reservedBytes -= MaximumRecordBytes;
            if (_spoolState == AuditSpoolState.Saturated && IsAdmissionOpen)
                _spoolState = AuditSpoolState.Healthy;
        }
    }

    private List<DeniedAttemptRecord> ReadRecords()
    {
        if (!File.Exists(_spoolPath))
            return [];
        return File.ReadLines(_spoolPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<DeniedAttemptRecord>(line, JsonOptions) ?? throw new JsonException("Invalid denied-attempt record."))
            .ToList();
    }

    private Dictionary<Guid, string> ReadAcknowledgements()
    {
        if (!File.Exists(_ackPath))
            return [];
        return File.ReadLines(_ackPath)
            .Select(line => line.Split('|', 2))
            .ToDictionary(parts => Guid.Parse(parts[0]), parts => parts[1]);
    }

    private void AppendAcknowledgement(Guid attemptId, string hash)
    {
        lock (_spoolLock)
        {
            AppendTextDurably(_ackPath, $"{attemptId:D}|{hash}{Environment.NewLine}");
        }
    }

    private void ArchiveAcknowledged(IReadOnlyList<DeniedAttemptRecord> records, IReadOnlyDictionary<Guid, string> acknowledgements)
    {
        lock (_spoolLock)
        {
            var acknowledged = records.Where(record => acknowledgements.ContainsKey(record.AttemptId)).ToArray();
            if (acknowledged.Length == 0)
                return;
            AppendTextDurably(_archivePath, string.Concat(acknowledged.Select(record => JsonSerializer.Serialize(record, JsonOptions) + Environment.NewLine)));
            var temporarySpoolPath = _spoolPath + ".recovering";
            WriteTextDurably(temporarySpoolPath, string.Concat(records.Where(record => !acknowledgements.ContainsKey(record.AttemptId))
                .Select(record => JsonSerializer.Serialize(record, JsonOptions) + Environment.NewLine)));
            File.Move(temporarySpoolPath, _spoolPath, overwrite: true);
            WriteTextDurably(_ackPath, string.Empty);
        }
    }

    private static void AppendTextDurably(string path, string content)
    {
        using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
    }

    private static void WriteTextDurably(string path, string content)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string HashRecord(DeniedAttemptRecord record) => Hash(JsonSerializer.Serialize(record, JsonOptions));
    private static string Bound(string value, int maximumLength) => string.IsNullOrWhiteSpace(value) ? "unknown" : value[..Math.Min(value.Length, maximumLength)];

    private readonly struct AuditSpoolReservation(DesktopSessionService owner) : IDisposable
    {
        public void Dispose() => owner?.ReleaseReservation();
    }

    private sealed record DesktopSession(string Reference, string ActorSid, string DisplayName, DateTimeOffset ExpiresAtUtc, bool Revoked);
}

public enum AuditSpoolState { Healthy, SqlUnavailable, Saturated, Faulted }
public sealed record SpoolDrainResult(int Committed, int Pending, bool AdmissionOpen);
public sealed record DeniedAttemptRecord(Guid AttemptId, DateTimeOffset OccurredAtUtc, long SourceOrdinal, string AgentGeneration, string Method, string Route, string Reason, string? VerifiedActorSid, string? VerifiedActorDisplayName, string? SessionReference);
public sealed record DesktopSessionIssue(string Token, string Reference, DateTimeOffset ExpiresAtUtc);
public sealed record VerifiedDesktopActor(string SessionReference, string Sid, string DisplayName);
