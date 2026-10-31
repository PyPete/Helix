using System.Net;
using Helix.Orchestration.Agent.Tests.Scientific;
using Xunit;

namespace Helix.Orchestration.Agent.Tests.Integration;

[Collection(nameof(ScientificPostgresCollection))]
public sealed class ScientificSetupTests(ScientificPostgresFixture fixture)
{
    [Theory]
    [InlineData("procedures")]
    [InlineData("plans")]
    public async Task Authenticated_scientist_can_list_independent_scientific_kinds(string kind)
    {
        using var client = fixture.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/scientific-setup/{kind}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
