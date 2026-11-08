using System.Text;
using System.Text.Json;
using Akka.Actor;
using Akka.Hosting;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using FunkArr.ArrApi.Sabnzbd;
using FunkArr.ArrApi.Sabnzbd.Requests;
using FunkArr.ArrApi.Sabnzbd.Services;
using FunkArr.Core.Actors;
using FunkArr.Core.Configuration;
using FunkArr.Core.Data;
using FunkArr.Messages;
using FunkArr.Messages.Download;
using FunkArr.Messages.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunkArr.ArrApi.Tests.Sabnzbd;

public sealed class SabnzbdWireTests : IAsyncDisposable
{
    private static readonly JsonSerializerOptions _mvcJson = new(JsonSerializerDefaults.Web);
    private static readonly Guid _firstId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _secondId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly ActorSystem _system = ActorSystem.Create("test");
    private readonly XunitAssertions _assertions = new();
    private readonly string _tempDir;
    private readonly DataPaths _dataPaths;

    public SabnzbdWireTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"funkarr-wire-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _dataPaths = new DataPaths(
            Options.Create(new FunkArrOptions { DataPath = _tempDir }),
            Options.Create(new DownloadOptions()));
        _dataPaths.EnsureDirectories();
    }

    private string CompleteDir => _dataPaths.Complete.Replace('\\', '/');

    private (SabnzbdController Controller, TestProbe Manager, TestProbe History) CreateController(
        DownloadOptions? downloadOptions = null)
    {
        var registry = ActorRegistry.For(_system);
        var manager = new TestProbe(_system, _assertions);
        var history = new TestProbe(_system, _assertions);
        registry.Register<IDownloadManager>(manager);
        registry.Register<IDownloadHistoryManager>(history);

        var queue = new SabnzbdQueueService(
            registry,
            Options.Create(downloadOptions ?? new DownloadOptions()),
            _dataPaths,
            NullLogger<SabnzbdQueueService>.Instance);
        var downloads = new SabnzbdDownloadService(
            registry,
            NullLogger<SabnzbdDownloadService>.Instance);

        return (new SabnzbdController(queue, downloads), manager, history);
    }

    private static DownloadGetRequest Get(
        string? mode,
        string? name = null,
        string? value = null,
        string? value2 = null,
        int? start = null,
        int? limit = null,
        string? category = null) =>
        new(mode, name, value, value2, start, limit, null, null, category, null);

    private static void AssertWire(int expectedStatus, string expectedJson, IActionResult result)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.NotNull(objectResult.Value);
        var json = JsonSerializer.Serialize(objectResult.Value, objectResult.Value.GetType(), _mvcJson);

        Assert.Multiple(
            () => Assert.Equal(expectedStatus, objectResult.StatusCode ?? 200),
            () => Assert.Equal(expectedJson, json));
    }

    [Fact]
    public async Task Version_returns_version_json()
    {
        var (controller, _, _) = CreateController();

        var result = await controller.HandleGet(Get("version"), TestContext.Current.CancellationToken);

        AssertWire(200, "{\"version\":\"4.3.3\"}", result);
    }

    [Fact]
    public async Task GetConfig_returns_categories_and_misc_settings()
    {
        var (controller, _, _) = CreateController(downloadOptions: new DownloadOptions
        {
            Categories =
            [
                new DownloadCategory { Name = "sonarr", Dir = "tv" },
                new DownloadCategory { Name = "radarr", Dir = "" }
            ]
        });

        var result = await controller.HandleGet(Get("get_config"), TestContext.Current.CancellationToken);

        var completeDir = JsonSerializer.Serialize(CompleteDir);
        AssertWire(200,
            "{\"config\":{\"misc\":{\"complete_dir\":" + completeDir + ",\"enable_tv_sorting\":false," +
            "\"enable_movie_sorting\":false,\"enable_date_sorting\":false,\"pre_check\":false," +
            "\"history_retention\":\"all\",\"history_retention_option\":\"all\",\"history_retention_number\":0," +
            "\"tv_categories\":[\"sonarr\",\"radarr\"],\"movie_categories\":[],\"date_categories\":[]}," +
            "\"categories\":[{\"name\":\"sonarr\",\"order\":0,\"dir\":\"tv\",\"newzbin\":\"\",\"priority\":0}," +
            "{\"name\":\"radarr\",\"order\":1,\"dir\":\"radarr\",\"newzbin\":\"\",\"priority\":0}],\"sorters\":[]}}",
            result);
    }

    [Fact]
    public async Task GetConfig_without_categories_returns_empty_category_list()
    {
        var (controller, _, _) = CreateController();

        var result = await controller.HandleGet(Get("get_config"), TestContext.Current.CancellationToken);

        var completeDir = JsonSerializer.Serialize(CompleteDir);
        AssertWire(200,
            "{\"config\":{\"misc\":{\"complete_dir\":" + completeDir + ",\"enable_tv_sorting\":false," +
            "\"enable_movie_sorting\":false,\"enable_date_sorting\":false,\"pre_check\":false," +
            "\"history_retention\":\"all\",\"history_retention_option\":\"all\",\"history_retention_number\":0," +
            "\"tv_categories\":[],\"movie_categories\":[],\"date_categories\":[]}," +
            "\"categories\":[],\"sorters\":[]}}",
            result);
    }

    [Fact]
    public async Task FullStatus_returns_status_block()
    {
        var (controller, manager, _) = CreateController();

        var task = controller.HandleGet(Get("fullstatus"), TestContext.Current.CancellationToken);
        manager.ExpectMsg<QueryQueue>(cancellationToken: TestContext.Current.CancellationToken);
        manager.Reply(new QueueResult([], 0, 0, true, false, null));
        var result = await task;

        var completeDir = JsonSerializer.Serialize(CompleteDir);
        AssertWire(200,
            "{\"status\":{\"paused\":true,\"speedlimit\":\"\",\"diskspace1\":\"0\",\"diskspace2\":\"0\"," +
            "\"completedir\":" + completeDir + ",\"speed\":\"0\"}}",
            result);
    }

    [Fact]
    public async Task Queue_empty_returns_empty_slots()
    {
        var (controller, manager, _) = CreateController();

        var task = controller.HandleGet(Get("queue", start: 0, limit: 10), TestContext.Current.CancellationToken);
        manager.ExpectMsg<QueryQueue>(cancellationToken: TestContext.Current.CancellationToken);
        manager.Reply(new QueueResult([], 0, 0, false, false, null));
        var result = await task;

        AssertWire(200,
            "{\"queue\":{\"paused\":false,\"speedlimit\":\"\",\"noofslots_total\":0,\"diskspace1\":\"0\"," +
            "\"diskspace2\":\"0\",\"speed\":\"0\",\"slots\":[]}}",
            result);
    }

    [Fact]
    public async Task Queue_with_downloading_and_queued_items_returns_slots()
    {
        var (controller, manager, _) = CreateController();
        QueueItem[] items =
        [
            new(_firstId, "Tatort", DownloadStatus.Processing, "ARD", false,
                104_857_600, new DownloadProgress(52_428_800, 10_000_000, 2.0),
                3600, MediaType.Show, DownloadPriority.Normal, DownloadPhase.VideoDownload, 1),
            new(_secondId, "Film", DownloadStatus.Queued, "ZDF", true,
                10_485_760, new DownloadProgress(0, 0, 0),
                0, MediaType.Movie, DownloadPriority.High, DownloadPhase.VideoDownload, 1)
        ];

        var task = controller.HandleGet(Get("queue", start: 0, limit: 10), TestContext.Current.CancellationToken);
        manager.ExpectMsg<QueryQueue>(cancellationToken: TestContext.Current.CancellationToken);
        manager.Reply(new QueueResult(items, 2, 2, false, false, null));
        var result = await task;

        AssertWire(200,
            "{\"queue\":{\"paused\":false,\"speedlimit\":\"\",\"noofslots_total\":2,\"diskspace1\":\"0\"," +
            "\"diskspace2\":\"0\",\"speed\":\"0\",\"slots\":[" +
            "{\"nzo_id\":\"11111111-1111-1111-1111-111111111111\",\"status\":\"Downloading\",\"index\":0," +
            "\"timeleft\":\"00:29:55\",\"mb\":\"100.0\",\"filename\":\"Tatort\",\"cat\":\"tv\",\"mbleft\":\"50.0\"," +
            "\"percentage\":\"50\",\"priority\":\"Normal\",\"speed\":\"5242880\"}," +
            "{\"nzo_id\":\"22222222-2222-2222-2222-222222222222\",\"status\":\"Queued\",\"index\":1," +
            "\"timeleft\":\"00:00:00\",\"mb\":\"10.0\",\"filename\":\"Film\",\"cat\":\"movies\",\"mbleft\":\"10.0\"," +
            "\"percentage\":\"0\",\"priority\":\"High\",\"speed\":\"0\"}]}}",
            result);
    }

    [Fact]
    public async Task Queue_unrecognized_subcommand_returns_error()
    {
        var (controller, _, _) = CreateController();

        var result = await controller.HandleGet(Get("queue", name: "bogus"), TestContext.Current.CancellationToken);

        AssertWire(200, "{\"status\":false,\"error\":\"Invalid queue command\"}", result);
    }

    [Fact]
    public async Task Queue_failure_returns_502_error()
    {
        var (controller, manager, _) = CreateController();

        var task = controller.HandleGet(Get("queue"), TestContext.Current.CancellationToken);
        manager.ExpectMsg<QueryQueue>(cancellationToken: TestContext.Current.CancellationToken);
        manager.Reply(new QueueFailed(new InvalidOperationException("boom")));
        var result = await task;

        AssertWire(200, "{\"status\":false,\"error\":\"Queue query failed\"}", result);
    }

    [Fact]
    public async Task History_with_completed_and_failed_items_returns_slots()
    {
        var (controller, _, history) = CreateController();
        const string relativePath = "tv/Tatort/Tatort.mkv";
        HistoryItem[] items =
        [
            new(_firstId, new DownloadCompletion(
                "Tatort", MediaType.Show, 1_500_000_000, DownloadStatus.Completed,
                relativePath, null, 120, 1_700_000_000)),
            new(_secondId, new DownloadCompletion(
                "Film", MediaType.Movie, 0, DownloadStatus.Failed,
                null, "connection lost", 0, 1_700_000_100))
        ];

        var task = controller.HandleGet(Get("history", start: 0, limit: 10), TestContext.Current.CancellationToken);
        history.ExpectMsg<QueryHistory>(cancellationToken: TestContext.Current.CancellationToken);
        history.Reply(new HistoryResult(items, 2));
        var result = await task;

        var storage = JsonSerializer.Serialize(Path.GetDirectoryName(Path.Join(_dataPaths.Complete, relativePath)));
        AssertWire(200,
            "{\"history\":{\"noofslots\":2,\"slots\":[" +
            "{\"nzo_id\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"Tatort\",\"nzb_name\":\"Tatort.nzb\"," +
            "\"category\":\"tv\",\"bytes\":1500000000,\"download_time\":120,\"storage\":" + storage + "," +
            "\"status\":\"Completed\",\"fail_message\":\"\",\"completed_on\":1700000000}," +
            "{\"nzo_id\":\"22222222-2222-2222-2222-222222222222\",\"name\":\"Film\",\"nzb_name\":\"Film.nzb\"," +
            "\"category\":\"movies\",\"bytes\":0,\"download_time\":0,\"storage\":null," +
            "\"status\":\"Failed\",\"fail_message\":\"connection lost\",\"completed_on\":1700000100}]}}",
            result);
    }

    [Fact]
    public async Task Pause_returns_status_true()
    {
        var (controller, manager, _) = CreateController();

        var task = controller.HandleGet(Get("pause"), TestContext.Current.CancellationToken);
        manager.ExpectMsg<PauseDownloads>(cancellationToken: TestContext.Current.CancellationToken);
        manager.Reply(new PauseDownloadsResult(true));
        var result = await task;

        AssertWire(200, "{\"status\":true}", result);
    }

    [Fact]
    public async Task Resume_returns_status_true()
    {
        var (controller, manager, _) = CreateController();

        var task = controller.HandleGet(Get("resume"), TestContext.Current.CancellationToken);
        manager.ExpectMsg<ResumeDownloads>(cancellationToken: TestContext.Current.CancellationToken);
        manager.Reply(new ResumeDownloadsResult(true));
        var result = await task;

        AssertWire(200, "{\"status\":true}", result);
    }

    [Fact]
    public async Task Queue_delete_returns_status_true()
    {
        var (controller, manager, _) = CreateController();

        var task = controller.HandleGet(Get("queue", name: "delete", value: _firstId.ToString()), TestContext.Current.CancellationToken);
        manager.ExpectMsg<DeleteDownload>(cancellationToken: TestContext.Current.CancellationToken);
        manager.Reply(new DeleteDownloadResult(true, null));
        var result = await task;

        AssertWire(200, "{\"status\":true}", result);
    }

    [Fact]
    public async Task Queue_delete_with_invalid_id_returns_item_not_found()
    {
        var (controller, _, _) = CreateController();

        var result = await controller.HandleGet(Get("queue", name: "delete", value: "not-a-guid"), TestContext.Current.CancellationToken);

        AssertWire(200, "{\"status\":false,\"error\":\"Item not found\"}", result);
    }

    [Fact]
    public async Task History_delete_returns_status_true()
    {
        var (controller, _, history) = CreateController();

        var task = controller.HandleGet(Get("history", name: "delete", value: _firstId.ToString()), TestContext.Current.CancellationToken);
        history.ExpectMsg<RemoveHistoryEntry>(cancellationToken: TestContext.Current.CancellationToken);
        history.Reply(new DeleteDownloadResult(true, null));
        var result = await task;

        AssertWire(200, "{\"status\":true}", result);
    }

    [Fact]
    public async Task Retry_returns_status_true()
    {
        var (controller, manager, _) = CreateController();

        var task = controller.HandleGet(Get("retry", value: _firstId.ToString()), TestContext.Current.CancellationToken);
        manager.ExpectMsg<RetryDownload>(cancellationToken: TestContext.Current.CancellationToken);
        manager.Reply(new RetryDownloadResult(true, null));
        var result = await task;

        AssertWire(200, "{\"status\":true}", result);
    }

    [Fact]
    public async Task Retry_without_value_returns_missing_value_error()
    {
        var (controller, _, _) = CreateController();

        var result = await controller.HandleGet(Get("retry"), TestContext.Current.CancellationToken);

        AssertWire(200, "{\"status\":false,\"error\":\"Missing value parameter\"}", result);
    }

    [Fact]
    public async Task Queue_priority_returns_status_true()
    {
        var (controller, manager, _) = CreateController();

        var task = controller.HandleGet(Get("queue", name: "priority", value: _firstId.ToString(), value2: "1"), TestContext.Current.CancellationToken);
        var message = manager.ExpectMsg<SetDownloadPriority>(cancellationToken: TestContext.Current.CancellationToken);
        manager.Reply(new SetDownloadPriorityCompleted());
        var result = await task;

        Assert.Equal(DownloadPriority.High, message.Priority);
        AssertWire(200, "{\"status\":true}", result);
    }

    [Fact]
    public async Task Queue_priority_force_returns_status_true()
    {
        var (controller, manager, _) = CreateController();

        var task = controller.HandleGet(Get("queue", name: "priority", value: _firstId.ToString(), value2: "2"), TestContext.Current.CancellationToken);
        manager.ExpectMsg<ForceStartDownload>(cancellationToken: TestContext.Current.CancellationToken);
        manager.Reply(new ForceStartDownloadResult(true, null));
        var result = await task;

        AssertWire(200, "{\"status\":true}", result);
    }

    [Fact]
    public async Task Queue_switch_returns_status_true()
    {
        var (controller, manager, _) = CreateController();

        var task = controller.HandleGet(
            Get("queue", name: "switch", value: _firstId.ToString(), value2: _secondId.ToString()),
            TestContext.Current.CancellationToken);
        manager.ExpectMsg<SwapDownloads>(cancellationToken: TestContext.Current.CancellationToken);
        manager.Reply(new SwapDownloadsCompleted());
        var result = await task;

        AssertWire(200, "{\"status\":true}", result);
    }

    [Fact]
    public async Task Unknown_mode_returns_invalid_mode_error()
    {
        var (controller, _, _) = CreateController();

        var result = await controller.HandleGet(Get("bogus"), TestContext.Current.CancellationToken);

        AssertWire(200, "{\"status\":false,\"error\":\"Invalid mode\"}", result);
    }

    [Fact]
    public async Task Missing_mode_returns_invalid_mode_error()
    {
        var (controller, _, _) = CreateController();

        var result = await controller.HandleGet(Get(null), TestContext.Current.CancellationToken);

        AssertWire(200, "{\"status\":false,\"error\":\"Invalid mode\"}", result);
    }

    [Fact]
    public async Task Post_with_other_mode_returns_invalid_mode_error()
    {
        var (controller, _, _) = CreateController();

        var result = await controller.HandlePost(new DownloadPostRequest("version", null, null), null, TestContext.Current.CancellationToken);

        AssertWire(200, "{\"status\":false,\"error\":\"Invalid mode\"}", result);
    }

    [Fact]
    public async Task AddFile_returns_status_and_nzo_ids()
    {
        var (controller, manager, _) = CreateController();
        var nzb = NzbTestData.Generate("Tatort", "https://example.com/video.mp4", "https://example.com/sub.vtt", "ARD", 5400, 1_234_567_890, MediaType.Show);
        var file = new FormFile(new MemoryStream(nzb), 0, nzb.Length, "name", "tatort.nzb");

        var task = controller.HandlePost(new DownloadPostRequest("addfile", null, null), file, TestContext.Current.CancellationToken);
        var add = manager.ExpectMsg<AddDownload>(cancellationToken: TestContext.Current.CancellationToken);
        manager.Reply(new DownloadAdded(_firstId));
        var result = await task;

        Assert.Equal(
            new AddDownload(new DownloadMedia(
                "Tatort", "https://example.com/video.mp4", "https://example.com/sub.vtt",
                "ARD", 5400, 1_234_567_890, MediaType.Show)),
            add);
        AssertWire(200, "{\"status\":true,\"nzo_ids\":[\"11111111-1111-1111-1111-111111111111\"]}", result);
    }

    [Fact]
    public async Task AddFile_without_file_returns_error()
    {
        var (controller, _, _) = CreateController();

        var result = await controller.HandlePost(new DownloadPostRequest("addfile", null, null), null, TestContext.Current.CancellationToken);

        AssertWire(200, "{\"status\":false,\"error\":\"No NZB file uploaded\"}", result);
    }

    [Fact]
    public async Task AddFile_with_invalid_xml_returns_error()
    {
        var (controller, _, _) = CreateController();
        var bytes = Encoding.UTF8.GetBytes("this is not xml");
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "name", "bad.nzb");

        var result = await controller.HandlePost(new DownloadPostRequest("addfile", null, null), file, TestContext.Current.CancellationToken);

        AssertWire(200, "{\"status\":false,\"error\":\"Invalid NZB file: not valid XML\"}", result);
    }

    [Fact]
    public async Task Api_key_filter_error_body()
    {
        var filter = new SabnzbdApiKeyFilter(Options.Create(new FunkArrOptions { ApiKey = "expected" }));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?apikey=wrong");
        var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var context = new Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext(
            actionContext, [], new Dictionary<string, object?>(), new object());

        await filter.OnActionExecutionAsync(context, () =>
            Task.FromResult(new Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext(context, [], new object())));

        var json = Assert.IsType<JsonResult>(context.Result);
        Assert.NotNull(json.Value);
        Assert.Multiple(
            () => Assert.Null(json.StatusCode),
            () => Assert.Equal(
                "{\"status\":false,\"error\":\"API Key Required\"}",
                JsonSerializer.Serialize(json.Value, json.Value.GetType(), _mvcJson)));
    }

    public async ValueTask DisposeAsync()
    {
        await _system.Terminate();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
