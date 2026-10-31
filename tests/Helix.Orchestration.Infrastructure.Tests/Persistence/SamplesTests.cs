using Helix.Orchestration.Core.Samples;
using Helix.Orchestration.Infrastructure.Persistence;
using Xunit;

namespace Helix.Orchestration.Infrastructure.Tests.Persistence;

public sealed class SamplesTests
{
    [Fact]
    public void Repository_is_scoped_sample_port_implementation()
    {
        Assert.True(typeof(ISampleRepository).IsAssignableFrom(typeof(PostgresSampleRepository)));
    }
}
