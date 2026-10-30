using Helix.Orchestration.Core.WSL;
using Helix.Orchestration.Infrastructure.Persistence;
using Helix.Orchestration.SiteServer.Agent;
using Helix.Orchestration.SiteServer.Campaigns;
using Moq;
using Xunit;

namespace Helix.Orchestration.SiteServer.Tests.Campaigns;

public class CampaignManagerTests
{
    [Fact]
    public async Task CreateAsync_WithParameterSets_CreatesDraftCampaignWithIterations()
    {
        var agentProxy = new Mock<IAgentProxy>();
        var repo = new Mock<ICampaignRepository>();
        repo.Setup(r => r.SaveAsync(It.IsAny<CampaignRecord>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var manager = new CampaignManager(agentProxy.Object, new SequentialStrategy(), repo.Object);

        var campaign = await manager.CreateAsync(
            "Campaign A",
            "desc",
            new List<Dictionary<string, object?>>
            {
                new() { ["temp"] = 40 },
                new() { ["temp"] = 50 }
            });

        Assert.Equal("Campaign A", campaign.Name);
        Assert.Equal(CampaignStatus.Draft, campaign.Status);
        Assert.Equal(2, campaign.Iterations.Count);
        Assert.All(campaign.Iterations, i => Assert.Equal(CampaignIterationStatus.Pending, i.Status));
    }

    [Fact]
    public async Task StartAsync_SubmitsAllIterations_AndCompletesCampaign()
    {
        var submissionCounter = 0;
        var agentProxy = new Mock<IAgentProxy>();
        agentProxy
            .Setup(a => a.SubmitExperimentAsync(It.IsAny<RunspaceDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"runspace-{++submissionCounter}");

        var repo = new Mock<ICampaignRepository>();
        repo.Setup(r => r.SaveAsync(It.IsAny<CampaignRecord>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var manager = new CampaignManager(agentProxy.Object, new SequentialStrategy(), repo.Object);

        var created = await manager.CreateAsync(
            "Campaign B",
            null,
            new List<Dictionary<string, object?>>
            {
                new() { ["flow"] = 1.2 },
                new() { ["flow"] = 1.5 }
            });

        var started = await manager.StartAsync(created.CampaignId);

        Assert.Equal(CampaignStatus.Completed, started.Status);
        Assert.All(started.Iterations, i => Assert.Equal(CampaignIterationStatus.Completed, i.Status));
        Assert.All(started.Iterations, i => Assert.False(string.IsNullOrWhiteSpace(i.RunspaceId)));
        agentProxy.Verify(a => a.SubmitExperimentAsync(It.IsAny<RunspaceDefinition>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task StartAsync_OnSubmissionFailure_MarksCampaignFailed()
    {
        var first = true;
        var agentProxy = new Mock<IAgentProxy>();
        agentProxy
            .Setup(a => a.SubmitExperimentAsync(It.IsAny<RunspaceDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                if (first)
                {
                    first = false;
                    return "runspace-1";
                }

                throw new InvalidOperationException("submit failed");
            });

        var repo = new Mock<ICampaignRepository>();
        repo.Setup(r => r.SaveAsync(It.IsAny<CampaignRecord>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var manager = new CampaignManager(agentProxy.Object, new SequentialStrategy(), repo.Object);

        var created = await manager.CreateAsync(
            "Campaign C",
            null,
            new List<Dictionary<string, object?>>
            {
                new() { ["p"] = 1 },
                new() { ["p"] = 2 }
            });

        var started = await manager.StartAsync(created.CampaignId);

        Assert.Equal(CampaignStatus.Failed, started.Status);
        Assert.Equal(CampaignIterationStatus.Completed, started.Iterations[0].Status);
        Assert.Equal(CampaignIterationStatus.Failed, started.Iterations[1].Status);
        Assert.Equal("submit failed", started.Iterations[1].Message);
    }

    [Fact]
    public async Task DeleteAsync_RemovesCampaign()
    {
        var agentProxy = new Mock<IAgentProxy>();
        var repo = new Mock<ICampaignRepository>();
        repo.Setup(r => r.SaveAsync(It.IsAny<CampaignRecord>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((CampaignRecord?)null);
        var manager = new CampaignManager(agentProxy.Object, new SequentialStrategy(), repo.Object);

        var created = await manager.CreateAsync("Campaign D", null, new List<Dictionary<string, object?>>());

        var deleted = await manager.DeleteAsync(created.CampaignId);
        var fetched = await manager.GetByIdAsync(created.CampaignId);

        Assert.True(deleted);
        Assert.Null(fetched);
    }
}
