using System.Text.Json;
using Akka.Actor;
using Akka.IO;
using FunkArr.Messages.Mediathek;

namespace FunkArr.Search;

public sealed class MediathekViewWebManager : ReceiveActor, IWithUnboundedStash
{
    private sealed record HttpCompleted(MediathekQueryCompleted Result);

    private sealed record HttpFailed(string Reason);

    private readonly HttpClient _httpClient;
    private readonly int _maxConcurrent;
    private MediathekViewWebManagerState _state = MediathekViewWebManagerState.Empty;

    public IStash Stash { get; set; } = null!;

    public MediathekViewWebManager(IHttpClientFactory httpClientFactory, int maxConcurrent = 3)
    {
        _httpClient = httpClientFactory.CreateClient("MediathekViewWeb");
        _maxConcurrent = maxConcurrent;

        Receive<QueryMediathek>(HandleQuery);
        Receive<HttpCompleted>(HandleHttpCompleted);
        Receive<HttpFailed>(HandleHttpFailed);
    }

    private void HandleQuery(QueryMediathek query)
    {
        if (!_state.HasCapacity(_maxConcurrent))
        {
            Stash.Stash();
            return;
        }

        _state = _state.Increment();
        ExecuteQuery(query);
    }

    private void ExecuteQuery(QueryMediathek query)
    {
        var json = MediathekQueryBuilder.FromMessage(query).Build();
        var self = Self;
        var sender = Sender;

        Task.Run(async () =>
        {
            try
            {
                using var content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
                using var response = await _httpClient.PostAsync("", content);
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<MediathekApiResponse>(body, _apiJsonOptions);

                var items = (apiResponse?.Result?.Results ?? [])
                    .Select(r => new MediathekItem(
                        Channel: r.Channel ?? "",
                        Topic: r.Topic ?? "",
                        Title: r.Title ?? "",
                        Description: r.Description,
                        Timestamp: r.Timestamp,
                        Duration: r.Duration,
                        Size: r.Size ?? EstimateSize(r.Duration, r.UrlVideoHd, r.UrlVideo, r.UrlVideoLow),
                        UrlVideoLow: r.UrlVideoLow,
                        UrlVideo: r.UrlVideo,
                        UrlVideoHd: r.UrlVideoHd,
                        UrlSubtitle: r.UrlSubtitle,
                        UrlWebsite: r.UrlWebsite))
                    .ToArray();

                var total = apiResponse?.Result?.QueryInfo?.TotalResults ?? items.Length;

                return new HttpCompleted(new MediathekQueryCompleted(items, total)) as object;
            }
            catch (Exception ex)
            {
                return new HttpFailed(ex.Message);
            }
        }).PipeTo(self, sender);
    }

    private void HandleHttpCompleted(HttpCompleted msg)
    {
        Sender.Tell(msg.Result);
        SlotFreed();
    }

    private void HandleHttpFailed(HttpFailed msg)
    {
        Sender.Tell(new MediathekQueryFailed(msg.Reason));
        SlotFreed();
    }

    private void SlotFreed()
    {
        _state = _state.Decrement();
        Stash.Unstash();
    }

    internal static long EstimateSize(int duration, string? urlHd, string? urlVideo, string? urlLow) =>
        duration * (urlHd is not null ? 312_500L : urlVideo is not null ? 187_500L : urlLow is not null ? 100_000L : 0L);

    private static readonly JsonSerializerOptions _apiJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}
