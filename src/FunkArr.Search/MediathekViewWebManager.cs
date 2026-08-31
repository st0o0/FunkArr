using System.Text.Json;
using Akka.Actor;
using FunkArr.Messages.Mediathek;

namespace FunkArr.Search;

public sealed class MediathekViewWebManager : ReceiveActor, IWithUnboundedStash
{
    private sealed record State(int InFlight);

    private sealed record HttpCompleted(MediathekQueryCompleted Result, IActorRef ReplyTo);

    private sealed record HttpFailed(string Reason, IActorRef ReplyTo);

    private readonly HttpClient _httpClient;
    private readonly int _maxConcurrent;
    private State _state = new(InFlight: 0);

    public IStash Stash { get; set; } = null!;

    public MediathekViewWebManager(IHttpClientFactory httpClientFactory, int maxConcurrent = 3)
    {
        _httpClient = httpClientFactory.CreateClient("MediathekViewWeb");
        _maxConcurrent = maxConcurrent;

        Receive<MediathekQuery>(HandleQuery);
        Receive<HttpCompleted>(HandleHttpCompleted);
        Receive<HttpFailed>(HandleHttpFailed);
    }

    private void HandleQuery(MediathekQuery query)
    {
        if (_state.InFlight >= _maxConcurrent)
        {
            Stash.Stash();
            return;
        }

        _state = _state with { InFlight = _state.InFlight + 1 };
        var sender = Sender;
        ExecuteQuery(query, sender);
    }

    private void ExecuteQuery(MediathekQuery query, IActorRef replyTo)
    {
        var json = MediathekQueryBuilder.FromMessage(query).Build();
        var self = Self;

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
                        Size: r.Size,
                        UrlVideoLow: r.UrlVideoLow,
                        UrlVideo: r.UrlVideo,
                        UrlVideoHd: r.UrlVideoHd,
                        UrlSubtitle: r.UrlSubtitle,
                        UrlWebsite: r.UrlWebsite))
                    .ToArray();

                var total = apiResponse?.Result?.QueryInfo?.TotalResults ?? items.Length;

                return new HttpCompleted(new MediathekQueryCompleted(items, total), replyTo) as object;
            }
            catch (Exception ex)
            {
                return new HttpFailed(ex.Message, replyTo);
            }
        }).PipeTo(self);
    }

    private void HandleHttpCompleted(HttpCompleted msg)
    {
        msg.ReplyTo.Tell(msg.Result);
        SlotFreed();
    }

    private void HandleHttpFailed(HttpFailed msg)
    {
        msg.ReplyTo.Tell(new MediathekQueryFailed(msg.Reason));
        SlotFreed();
    }

    private void SlotFreed()
    {
        _state = _state with { InFlight = _state.InFlight - 1 };
        Stash.Unstash();
    }

    private static readonly JsonSerializerOptions _apiJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    internal sealed record MediathekApiResponse(MediathekApiResult? Result, string? Err);

    internal sealed record MediathekApiResult(MediathekApiItem[]? Results, MediathekApiQueryInfo? QueryInfo);

    internal sealed record MediathekApiQueryInfo(int TotalResults);

    internal sealed record MediathekApiItem(
        string? Channel,
        string? Topic,
        string? Title,
        string? Description,
        long Timestamp,
        int Duration,
        long Size,
        string? UrlVideo,
        string? UrlVideoLow,
        string? UrlVideoHd,
        string? UrlSubtitle,
        string? UrlWebsite);
}
