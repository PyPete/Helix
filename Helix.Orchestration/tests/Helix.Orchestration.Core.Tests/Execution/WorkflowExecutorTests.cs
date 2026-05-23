using Cleipnir.ResilientFunctions;
using Cleipnir.ResilientFunctions.Storage;
using Helix.Orchestration.Core.Capabilities;
using Helix.Orchestration.Core.Execution;
using Helix.Orchestration.Core.Safety;
using Helix.Orchestration.Core.State;
using Helix.Orchestration.Core.Trends;
using Helix.Orchestration.Core.WorkflowEngine;
using Xunit;

namespace Helix.Orchestration.Core.Tests.Execution;

public class WorkflowExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_RunsSimpleWorkflow_Successfully()
    {
        // Arrange
        var functionsRegistry = new FunctionsRegistry(new InMemoryFunctionStore());
        var registry = new WorkflowRegistry();
        var stateStore = new WorkflowStateStore();

        // Register a simple test workflow
        var testWorkflow = new TestWorkflow();
        registry.Register("TestWorkflow", testWorkflow);

        // Create context factory
        WorkflowContext ContextFactory(string workflowId) => new WorkflowContext
        {
            WorkflowId = workflowId,
            Capabilities = new CapabilityRegistry(),
            Trends = new TestTrendProvider(),
            Safety = new SafetyEngine(),
            State = stateStore,
            Experiment = null
        };

        var executor = new WorkflowExecutor(functionsRegistry, registry, ContextFactory);

        // Act
        await executor.ExecuteAsync("TestWorkflow", "test-instance-1");

        // Assert
        Assert.True(testWorkflow.WasExecuted);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsCompletedStatus_AfterWorkflowFinishes()
    {
        // Arrange
        var functionsRegistry = new FunctionsRegistry(new InMemoryFunctionStore());
        var registry = new WorkflowRegistry();
        var stateStore = new WorkflowStateStore();

        var testWorkflow = new TestWorkflow();
        registry.Register("TestWorkflow", testWorkflow);

        WorkflowContext ContextFactory(string workflowId) => new WorkflowContext
        {
            WorkflowId = workflowId,
            Capabilities = new CapabilityRegistry(),
            Trends = new TestTrendProvider(),
            Safety = new SafetyEngine(),
            State = stateStore,
            Experiment = null
        };

        var executor = new WorkflowExecutor(functionsRegistry, registry, ContextFactory);

        // Act
        await executor.ExecuteAsync("TestWorkflow", "test-instance-2");

        var status = await executor.GetStatusAsync("test-instance-2");

        Assert.Equal(WorkflowStatus.Completed, status);
        Assert.True(testWorkflow.WasExecuted);
    }

    // Simple test workflow implementation
    private class TestWorkflow : IWorkflow
    {
        public bool WasExecuted { get; private set; }

        public Task ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken = default)
        {
            WasExecuted = true;
            return Task.CompletedTask;
        }
    }

    // Test trend provider that returns empty trends
    private class TestTrendProvider : ITrendProvider
    {
        public TrendWindow<T> GetTrend<T>(string trendName) where T : struct, IComparable<T>
            => new(trendName, 10);

        public IScalarTrend<T> GetScalarTrend<T>(string trendName) where T : struct, IComparable<T>
            => new EmptyScalarTrend<T>(trendName);

        public ISpectralTrend GetSpectralTrend(string trendName)
            => new EmptySpectralTrend(trendName);

        public IDiodeArrayTrend GetDiodeArrayTrend(string trendName)
            => new EmptyDiodeArrayTrend(trendName);

        public IImageTrend GetImageTrend(string trendName)
            => new EmptyImageTrend(trendName);

        public IVideoTrend GetVideoTrend(string trendName)
            => new EmptyVideoTrend(trendName);

        public IDerivedTrend GetDerivedTrend(string trendName)
            => new EmptyDerivedTrend(trendName);

        public Task<bool> IsTrendAvailableAsync(string trendName)
            => Task.FromResult(false);

        public IEnumerable<string> GetAllTrendNames()
            => [];

        public IEnumerable<ITrend> GetAllTrends()
            => [];

        public IEnumerable<ITrend> GetTrendsByKind(TrendKind kind)
            => [];

        public IEnumerable<ITrend> GetTrendsByExperiment(string experimentId)
            => [];

        public void RegisterDerivedTrend(DerivedTrendDefinition definition)
        {
        }
    }

    private abstract class EmptyTrendBase : ITrend
    {
        protected EmptyTrendBase(string trendName, TrendKind kind)
        {
            TrendName = trendName;
            TrendKind = kind;
        }

        public string TrendId => TrendName;
        public string TrendName { get; }
        public TrendKind TrendKind { get; }
        public string? Units => null;
        public string? ExperimentId => null;
        public IReadOnlyList<string> Tags => [];
        public bool IsOnline => false;
        public DateTimeOffset LastUpdatedAt => DateTimeOffset.UtcNow;
        public IObservable<TrendValue> Values => EmptyObservable<TrendValue>.Instance;
    }

    private sealed class EmptyScalarTrend<T>(string trendName) : EmptyTrendBase(trendName, TrendKind.Scalar), IScalarTrend<T>
        where T : struct, IComparable<T>
    {
        public TrendWindow<T> Window { get; } = new(trendName, 10);
        public T? Latest => Window.Latest();
    }

    private sealed class EmptySpectralTrend(string trendName) : EmptyTrendBase(trendName, TrendKind.Spectral), ISpectralTrend
    {
        public SpectralScan? LatestScan => null;
        public SpectralMatrix? LatestMatrix => null;
        public IObservable<SpectralScan> Scans => EmptyObservable<SpectralScan>.Instance;
        public IObservable<SpectralMatrix> Matrices => EmptyObservable<SpectralMatrix>.Instance;
        public IReadOnlyList<SpectralScan> GetRecentScans(int count) => [];
        public IReadOnlyList<SpectralMatrix> GetRecentMatrices(int count) => [];
    }

    private sealed class EmptyDiodeArrayTrend(string trendName) : EmptyTrendBase(trendName, TrendKind.DiodeArray), IDiodeArrayTrend
    {
        public IObservable<DiodeArraySlice> Slices => EmptyObservable<DiodeArraySlice>.Instance;
        public IReadOnlyList<DiodeArraySlice> GetSnapshot() => [];
        public IScalarTrend<double> ExtractChromatogram(double wavelengthNm) => new EmptyScalarTrend<double>("chromatogram");
        public SpectralScan? GetSpectrumAt(DateTimeOffset timestamp) => null;
    }

    private sealed class EmptyImageTrend(string trendName) : EmptyTrendBase(trendName, TrendKind.Image), IImageTrend
    {
        public IObservable<ImageFrame> Frames => EmptyObservable<ImageFrame>.Instance;
        public ImageFrame? LatestFrame => null;
    }

    private sealed class EmptyVideoTrend(string trendName) : EmptyTrendBase(trendName, TrendKind.Video), IVideoTrend
    {
        public IObservable<VideoSegment> Segments => EmptyObservable<VideoSegment>.Instance;
        public VideoSegment? LatestSegment => null;
    }

    private sealed class EmptyDerivedTrend(string trendName) : EmptyTrendBase(trendName, TrendKind.Derived), IDerivedTrend
    {
        public DerivedTrendDefinition Definition { get; } = new(
            TrendId: trendName,
            TrendName: trendName,
            DerivationKind: DerivationKind.CSharpDelegate,
            Expression: null,
            SourceTrendIds: [],
            Delegate: null
        );

        public IReadOnlyList<string> SourceTrendIds => Definition.SourceTrendIds;
    }

    private sealed class EmptyObservable<T> : IObservable<T>
    {
        public static EmptyObservable<T> Instance { get; } = new();

        public IDisposable Subscribe(IObserver<T> observer)
            => EmptyDisposable.Instance;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
