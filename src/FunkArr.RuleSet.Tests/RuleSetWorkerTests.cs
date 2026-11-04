using System.IO.Abstractions;
using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.RuleSet;
using FunkArr.Messages.Scoring;
using FunkArr.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.RuleSet.Tests;

public sealed class RuleSetWorkerTests : TestKit
{
    private readonly string _tempDir;
    private readonly TestDataFiles _dataFiles;
    private readonly RuleSetStore _store;

    private const string _sampleJson = """
                                       {"topic":"Test Show","aliases":[],"media":{"name":"Test","type":"show","tvdbId":12345},"confidence":0.9,"rules":[{"id":"test-rule","priority":0,"strategy":"itemTitleIncludes"}]}
                                       """;

    public RuleSetWorkerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"funkarr-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_tempDir, "rulesets", "community"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "rulesets", "local"));
        var dataPaths = new DataPaths(
            Options.Create(new FunkArrOptions { DataPath = _tempDir }),
            Options.Create(new DownloadOptions()));
        dataPaths.EnsureDirectories();
        _dataFiles = new TestDataFiles(new DataFiles(new FileSystem(), NullLogger<DataFiles>.Instance));
        _store = new RuleSetStore(_dataFiles, dataPaths);
    }

    private IActorRef CreateWorkerWithProbes(out TestProbe scoringProbe, out TestProbe resolverProbe,
        out TestProbe managerProbe)
    {
        scoringProbe = CreateTestProbe();
        resolverProbe = CreateTestProbe();
        managerProbe = CreateTestProbe();

        var registry = ActorRegistry.For(Sys);
        registry.Register<IScoringManager>(scoringProbe);
        registry.Register<IRuleSetResolver>(resolverProbe);
        registry.Register<IRuleSetManager>(managerProbe);

        return Sys.ActorOf(Props.Create(() =>
            new RuleSetWorker("test-show", _store, new RuleSetValidator())), "test-show");
    }

    [Fact]
    public void Load_tells_downstream_and_manager()
    {
        var worker = CreateWorkerWithProbes(out var scoringProbe, out var resolverProbe, out var managerProbe);

        var communityPath = Path.Combine(_tempDir, "rulesets", "community", "test-show.json");
        _dataFiles.WriteText(communityPath, _sampleJson);

        worker.Tell(new RuleSetWorker.LoadRuleSet("test-show", communityPath, null));

        scoringProbe.ExpectMsg<MatchingConfig>(cancellationToken: TestContext.Current.CancellationToken);
        resolverProbe.ExpectMsg<RegisterRuleSet>(cancellationToken: TestContext.Current.CancellationToken);
        var ready = managerProbe.ExpectMsg<WorkerReady>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("test-show", ready.RuleSetId);
        Assert.Equal(1, ready.RuleCount);
        Assert.Equal("community", ready.SourceType);
    }

    [Fact]
    public void QueryDetail_responds_from_memory()
    {
        var worker = CreateWorkerWithProbes(out var scoringProbe, out var resolverProbe, out var managerProbe);

        var communityPath = Path.Combine(_tempDir, "rulesets", "community", "test-show.json");
        _dataFiles.WriteText(communityPath, _sampleJson);
        worker.Tell(new RuleSetWorker.LoadRuleSet("test-show", communityPath, null));

        scoringProbe.ExpectMsg<MatchingConfig>(cancellationToken: TestContext.Current.CancellationToken);
        resolverProbe.ExpectMsg<RegisterRuleSet>(cancellationToken: TestContext.Current.CancellationToken);
        managerProbe.ExpectMsg<WorkerReady>(cancellationToken: TestContext.Current.CancellationToken);

        worker.Tell(new QueryRuleSetDetail("test-show"), TestActor);
        var detail = ExpectMsg<RuleSetDetailResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("test-show", detail.RuleSetId);
        Assert.Equal("Test Show", detail.Identity.Topic);
    }

    [Fact]
    public void CreateLocal_writes_and_responds()
    {
        var worker = CreateWorkerWithProbes(out _, out _, out _);

        var body = new RuleSetBody("New Show", null,
            new RuleSetMediaInput("New", MediaType.Show),
            0.8f,
            [new RuleSetRuleInput(Id: "new-rule", Strategy: IdentificationStrategy.TitleIncludes)],
            null, null, null);

        worker.Tell(new CreateLocalRuleSet("test-show", body), TestActor);
        var result = ExpectMsg<CreateLocalRuleSetCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("test-show", result.RuleSetId);
        Assert.True(_store.ExistsLocal("test-show"));
    }

    [Fact]
    public void CreateLocal_duplicate_responds_already_exists()
    {
        var worker = CreateWorkerWithProbes(out _, out _, out _);
        _dataFiles.WriteText(Path.Combine(_tempDir, "rulesets", "local", "test-show.json"), "{}");

        var body = new RuleSetBody("Dup", null, new RuleSetMediaInput("D", MediaType.Show), null,
            [new RuleSetRuleInput(Id: "dup-rule", Strategy: IdentificationStrategy.TitleIncludes)],
            null, null, null);

        worker.Tell(new CreateLocalRuleSet("test-show", body), TestActor);
        var result = ExpectMsg<CreateLocalRuleSetFailed>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(CreateLocalRuleSetFailureReason.AlreadyExists, result.Reason);
    }

    [Fact]
    public void DeleteLocal_removes_and_responds()
    {
        var worker = CreateWorkerWithProbes(out var scoringProbe, out var resolverProbe, out var managerProbe);

        _dataFiles.WriteText(Path.Combine(_tempDir, "rulesets", "local", "test-show.json"), _sampleJson);

        worker.Tell(new DeleteLocalRuleSet("test-show"), TestActor);
        ExpectMsg<DeleteLocalRuleSetCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(_store.ExistsLocal("test-show"));

        scoringProbe.ExpectMsg<RemoveMatchingConfig>(cancellationToken: TestContext.Current.CancellationToken);
        resolverProbe.ExpectMsg<DeregisterRuleSet>(cancellationToken: TestContext.Current.CancellationToken);
        managerProbe.ExpectMsg<WorkerRemoved>(cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void DeleteLocal_nonexistent_responds_not_found()
    {
        var worker = CreateWorkerWithProbes(out _, out _, out _);

        worker.Tell(new DeleteLocalRuleSet("test-show"), TestActor);
        var result = ExpectMsg<DeleteLocalRuleSetFailed>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(DeleteLocalRuleSetFailureReason.NotFound, result.Reason);
    }

    [Fact]
    public void Export_without_local_responds_not_found()
    {
        var worker = CreateWorkerWithProbes(out _, out _, out _);

        _dataFiles.WriteText(Path.Combine(_tempDir, "rulesets", "community", "test-show.json"), _sampleJson);
        worker.Tell(new RuleSetWorker.LoadRuleSet("test-show", null, null));

        worker.Tell(new ExportRuleSet("test-show"), TestActor);
        var result = ExpectMsg<ExportRuleSetFailed>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(ExportRuleSetFailureReason.NoLocalOverlay, result.Reason);
    }

    [Fact]
    public void Export_with_local_responds_json()
    {
        var worker = CreateWorkerWithProbes(out var scoringProbe, out var resolverProbe, out var managerProbe);

        _dataFiles.WriteText(Path.Combine(_tempDir, "rulesets", "local", "test-show.json"), _sampleJson);
        worker.Tell(new RuleSetWorker.LoadRuleSet("test-show", null, null));

        scoringProbe.ExpectMsg<MatchingConfig>(cancellationToken: TestContext.Current.CancellationToken);
        resolverProbe.ExpectMsg<RegisterRuleSet>(cancellationToken: TestContext.Current.CancellationToken);
        managerProbe.ExpectMsg<WorkerReady>(cancellationToken: TestContext.Current.CancellationToken);

        worker.Tell(new ExportRuleSet("test-show"), TestActor);
        var result = ExpectMsg<ExportRuleSetCompleted>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("Test Show", result.Json);
    }

    protected override void AfterAll()
    {
        base.AfterAll();
        try
        {
            Directory.Delete(_tempDir, true);
        }
        catch
        {
            // noop
        }
    }
}
