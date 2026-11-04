using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models.Mediathek;
using FunkArr.Api.Models.Shared;
using FunkArr.Core;
using FunkArr.Messages.Mediathek;

namespace FunkArr.IntegrationTests.Api;

[Collection("Mediathek")]
public sealed class MediathekSearchTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task Search_WithResults_ReturnsTypedResponse()
    {
        var task = _fixture.Client.GetAsync("/api/mediathek/search?q=Tatort", TestContext.Current.CancellationToken);

        var probe = _fixture.GetProbe<IMediathekManager>();
        probe.ExpectMsg<QueryMediathek>(cancellationToken: TestContext.Current.CancellationToken);
        probe.Reply(TestData.MediathekSearchResult(count: 2));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MediathekSearchResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalResults);
        Assert.Equal(2, result.Items.Length);

        var first = result.Items[0];
        Assert.Contains("Tatort", first.Title);
        Assert.Equal("ARD", first.Channel);
        Assert.True(first.Duration > 0);
    }

    [Fact]
    public async Task Search_WithoutParams_Returns400()
    {
        var response = await _fixture.Client.GetAsync("/api/mediathek/search", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Contains("required", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_ByChannel_ReturnsResults()
    {
        var task = _fixture.Client.GetAsync("/api/mediathek/search?channel=ZDF", TestContext.Current.CancellationToken);

        var probe = _fixture.GetProbe<IMediathekManager>();
        var msg = probe.ExpectMsg<QueryMediathek>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(msg.Fields, f => f.Fields.Contains("channel"));
        probe.Reply(TestData.MediathekSearchResult(count: 1));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MediathekSearchResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalResults);
    }

    [Fact]
    public async Task Search_ByTopic_ReturnsResults()
    {
        var task = _fixture.Client.GetAsync("/api/mediathek/search?topic=Tatort", TestContext.Current.CancellationToken);

        var probe = _fixture.GetProbe<IMediathekManager>();
        var msg = probe.ExpectMsg<QueryMediathek>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(msg.Fields, f => f.Fields.Contains("topic"));
        probe.Reply(TestData.MediathekSearchResult(count: 1));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MediathekSearchResponse>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }
}
