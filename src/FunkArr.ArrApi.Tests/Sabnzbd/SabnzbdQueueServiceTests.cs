using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.ArrApi.Sabnzbd;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SabnzbdModels = FunkArr.ArrApi.Sabnzbd.Models;

namespace FunkArr.ArrApi.Tests.Sabnzbd;

public sealed class SabnzbdQueueServiceTests : IAsyncDisposable
{
    private readonly ActorSystem _system = ActorSystem.Create("test");
    private readonly XunitAssertions _assertions = new();
    private readonly string _tempDir;
    private readonly DataPaths _dataPaths;

    public SabnzbdQueueServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"funkarr-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _dataPaths = new DataPaths(
            Options.Create(new FunkArrOptions { DataPath = _tempDir }),
            Options.Create(new DownloadOptions()));
        _dataPaths.EnsureDirectories();
    }

    private (SabnzbdQueueService Service, TestProbe ManagerProbe, TestProbe HistoryProbe) CreateService(
        DownloadOptions? downloadOptions = null)
    {
        var registry = ActorRegistry.For(_system);
        var managerProbe = new TestProbe(_system, _assertions);
        var historyProbe = new TestProbe(_system, _assertions);
        registry.Register<IDownloadManager>(managerProbe);
        registry.Register<IDownloadHistoryManager>(historyProbe);

        var service = new SabnzbdQueueService(
            registry,
            Options.Create(new ArrApiOptions()),
            Options.Create(downloadOptions ?? new DownloadOptions()),
            _dataPaths,
            NullLogger<SabnzbdQueueService>.Instance);

        return (service, managerProbe, historyProbe);
    }

    [Fact]
    public async Task GetQueue_ReturnsQueueResponse()
    {
        var (service, managerProbe, _) = CreateService();
        var queueResult = new QueueResult([], 0, 0, false, false, null);

        var task = service.GetQueue(0, 10, null, CancellationToken.None);
        managerProbe.ExpectMsg<QueryQueue>();
        managerProbe.Reply(queueResult);
        var result = await task;

        var ok = Assert.IsType<SabnzbdResult.Ok>(result);
        Assert.IsType<SabnzbdModels.QueueResponse>(ok.Data);
    }

    [Fact]
    public async Task GetQueue_NonQueueResult_ReturnsError()
    {
        var (service, managerProbe, _) = CreateService();

        var task = service.GetQueue(0, 10, null, CancellationToken.None);
        managerProbe.ExpectMsg<QueryQueue>();
        managerProbe.Reply(new QueueFailed(new Exception("fail")));
        var result = await task;

        var error = Assert.IsType<SabnzbdResult.Error>(result);
        Assert.Equal(502, error.StatusCode);
    }

    [Fact]
    public async Task GetQueue_WithItems_BuildsSlots()
    {
        var (service, managerProbe, _) = CreateService();
        var items = new[]
        {
            new QueueItem(
                Guid.NewGuid(), "Test Show", DownloadStatus.Processing,
                "ARD", false, 1_000_000, new DownloadProgress(500_000, 5_000_000, 1.0),
                60, MediaType.Show, DownloadPriority.Normal, DownloadPhase.VideoDownload, 1)
        };
        var queueResult = new QueueResult(items, 1, 1, false, false, null);

        var task = service.GetQueue(0, 10, null, CancellationToken.None);
        managerProbe.ExpectMsg<QueryQueue>();
        managerProbe.Reply(queueResult);
        var result = await task;

        var ok = Assert.IsType<SabnzbdResult.Ok>(result);
        var response = Assert.IsType<SabnzbdModels.QueueResponse>(ok.Data);
        var slot = Assert.Single(response.Queue.Slots);
        Assert.Equal("Test Show", slot.Filename);
    }

    [Fact]
    public async Task GetHistory_ReturnsHistoryResponse()
    {
        var (service, _, historyProbe) = CreateService();
        var historyResult = new HistoryResult([], 0);

        var task = service.GetHistory(0, 10, null, CancellationToken.None);
        historyProbe.ExpectMsg<QueryHistory>();
        historyProbe.Reply(historyResult);
        var result = await task;

        var ok = Assert.IsType<SabnzbdResult.Ok>(result);
        Assert.IsType<SabnzbdModels.HistoryResponse>(ok.Data);
    }

    [Fact]
    public async Task GetFullStatus_ReturnsFullStatusResponse()
    {
        var (service, managerProbe, _) = CreateService();
        var queueResult = new QueueResult([], 0, 0, false, false, null);

        var task = service.GetFullStatus(CancellationToken.None);
        managerProbe.ExpectMsg<QueryQueue>();
        managerProbe.Reply(queueResult);
        var result = await task;

        var ok = Assert.IsType<SabnzbdResult.Ok>(result);
        Assert.IsType<SabnzbdModels.FullStatusResponse>(ok.Data);
    }

    [Fact]
    public async Task GetFullStatus_QueueFailed_ReturnsError()
    {
        var (service, managerProbe, _) = CreateService();

        var task = service.GetFullStatus(CancellationToken.None);
        managerProbe.ExpectMsg<QueryQueue>();
        managerProbe.Reply(new QueueFailed(new Exception("fail")));
        var result = await task;

        var error = Assert.IsType<SabnzbdResult.Error>(result);
        Assert.Equal(502, error.StatusCode);
    }

    [Fact]
    public async Task PauseQueue_Success_ReturnsOk()
    {
        var (service, managerProbe, _) = CreateService();

        var task = service.PauseQueue(CancellationToken.None);
        managerProbe.ExpectMsg<PauseDownloads>();
        managerProbe.Reply(new PauseDownloadsResult(true));
        var result = await task;

        Assert.IsType<SabnzbdResult.Ok>(result);
    }

    [Fact]
    public async Task PauseQueue_Failure_ReturnsError()
    {
        var (service, managerProbe, _) = CreateService();

        var task = service.PauseQueue(CancellationToken.None);
        managerProbe.ExpectMsg<PauseDownloads>();
        managerProbe.Reply(new PauseDownloadsResult(false));
        var result = await task;

        var error = Assert.IsType<SabnzbdResult.Error>(result);
        Assert.Equal("Pause failed", error.Message);
    }

    [Fact]
    public async Task ResumeQueue_Success_ReturnsOk()
    {
        var (service, managerProbe, _) = CreateService();

        var task = service.ResumeQueue(CancellationToken.None);
        managerProbe.ExpectMsg<ResumeDownloads>();
        managerProbe.Reply(new ResumeDownloadsResult(true));
        var result = await task;

        Assert.IsType<SabnzbdResult.Ok>(result);
    }

    [Fact]
    public async Task ResumeQueue_Failure_ReturnsError()
    {
        var (service, managerProbe, _) = CreateService();

        var task = service.ResumeQueue(CancellationToken.None);
        managerProbe.ExpectMsg<ResumeDownloads>();
        managerProbe.Reply(new ResumeDownloadsResult(false));
        var result = await task;

        var error = Assert.IsType<SabnzbdResult.Error>(result);
        Assert.Equal("Resume failed", error.Message);
    }

    [Fact]
    public void GetConfig_ReturnsConfigWithCategories()
    {
        var downloadOptions = new DownloadOptions
        {
            Categories = [new DownloadCategory { Name = "tv", Dir = "shows" }]
        };
        var (service, _, _) = CreateService(downloadOptions);

        var result = service.GetConfig();

        Assert.IsType<SabnzbdResult.Ok>(result);
    }

    [Fact]
    public void GetConfig_EmptyCategories_ReturnsOk()
    {
        var (service, _, _) = CreateService();

        var result = service.GetConfig();

        Assert.IsType<SabnzbdResult.Ok>(result);
    }

    public async ValueTask DisposeAsync()
    {
        await _system.Terminate();

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
