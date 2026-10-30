using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;
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
        var task = _fixture.Client.GetAsync("/api/mediathek/search?q=Tatort");

        var probe = _fixture.GetProbe<IMediathekManager>();
        probe.ExpectMsg<QueryMediathek>();
        probe.Reply(TestData.MediathekSearchResult(count: 2));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MediathekSearchResponse>();
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
        var response = await _fixture.Client.GetAsync("/api/mediathek/search");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(result);
        Assert.Contains("required", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Search_ByChannel_ReturnsResults()
    {
        var task = _fixture.Client.GetAsync("/api/mediathek/search?channel=ZDF");

        var probe = _fixture.GetProbe<IMediathekManager>();
        var msg = probe.ExpectMsg<QueryMediathek>();
        Assert.Contains(msg.Fields, f => f.Fields.Contains("channel"));
        probe.Reply(TestData.MediathekSearchResult(count: 1));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MediathekSearchResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalResults);
    }

    [Fact]
    public async Task Search_ByTopic_ReturnsResults()
    {
        var task = _fixture.Client.GetAsync("/api/mediathek/search?topic=Tatort");

        var probe = _fixture.GetProbe<IMediathekManager>();
        var msg = probe.ExpectMsg<QueryMediathek>();
        Assert.Contains(msg.Fields, f => f.Fields.Contains("topic"));
        probe.Reply(TestData.MediathekSearchResult(count: 1));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<MediathekSearchResponse>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }
}
