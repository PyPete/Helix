using Helix.Orchestration.Agent.Security;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Integration;

public sealed class AgentAuthenticationTests
{
    [Fact]
    public void IssuedSession_ValidatesAsVerifiedActor()
    {
        var service = new DesktopSessionService();
        var issue = service.Issue("S-1-5-21-test", "Scientist", TimeSpan.FromMinutes(1));

        var valid = service.TryValidate(issue.Token, out var actor);

        Assert.True(valid);
        Assert.NotNull(actor);
        Assert.Equal("S-1-5-21-test", actor.Sid);
        Assert.Equal(issue.Reference, actor.SessionReference);
    }

    [Fact]
    public void RevokedSession_CannotAuthenticate()
    {
        var service = new DesktopSessionService();
        var issue = service.Issue("S-1-5-21-test", "Scientist", TimeSpan.FromMinutes(1));
        Assert.True(service.Revoke(issue.Reference));

        Assert.False(service.TryValidate(issue.Token, out _));
    }

    [Fact]
    public void UnknownSession_CannotAuthenticate()
    {
        var service = new DesktopSessionService();

        Assert.False(service.TryValidate("not-a-session", out _));
    }
}
