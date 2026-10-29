using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Helix.Orchestration.Agent.Security;

/// <summary>Validates short-lived opaque desktop sessions without retaining raw credentials.</summary>
public sealed class DesktopSessionService
{
    private readonly ConcurrentDictionary<string, DesktopSession> _sessions = new(StringComparer.Ordinal);

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

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private sealed record DesktopSession(string Reference, string ActorSid, string DisplayName, DateTimeOffset ExpiresAtUtc, bool Revoked);
}

public sealed record DesktopSessionIssue(string Token, string Reference, DateTimeOffset ExpiresAtUtc);
public sealed record VerifiedDesktopActor(string SessionReference, string Sid, string DisplayName);
