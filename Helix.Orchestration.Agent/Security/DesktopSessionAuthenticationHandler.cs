using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Helix.Orchestration.Agent.Security;

/// <summary>Fail-closed HTTP session gate; only the native broker may supply its opaque bearer token.</summary>
public sealed class DesktopSessionAuthenticationHandler(RequestDelegate next, ILogger<DesktopSessionAuthenticationHandler> logger)
{
    public const string ActorItemKey = "helix.verifiedDesktopActor";

    public async Task InvokeAsync(HttpContext context, DesktopSessionService sessions)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!authorization.StartsWith(prefix, StringComparison.Ordinal) ||
            !sessions.TryValidate(authorization[prefix.Length..], out var actor))
        {
            logger.LogWarning("Denied desktop session request {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        context.Items[ActorItemKey] = actor;
        await next(context);
    }
}
