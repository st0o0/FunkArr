using System.Text.Json;
using FunkArr.Messages.Mediathek;
using FunkArr.Search;
using Xunit;

namespace FunkArr.Search.Tests;

public sealed class MediathekQueryBuilderTests
{
    [Fact]
    public void Minimal_query_produces_valid_json()
    {
        var json = MediathekQueryBuilder.Create()
            .WithQuery(["title"], "tatort")
            .Build();

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(1, root.GetProperty("queries").GetArrayLength());
        Assert.Equal("tatort", root.GetProperty("queries")[0].GetProperty("query").GetString());
        Assert.Equal("timestamp", root.GetProperty("sortBy").GetString());
        Assert.Equal("desc", root.GetProperty("sortOrder").GetString());
        Assert.False(root.GetProperty("future").GetBoolean());
        Assert.Equal(0, root.GetProperty("offset").GetInt32());
        Assert.Equal(15, root.GetProperty("size").GetInt32());
    }

    [Fact]
    public void Full_query_includes_all_fields()
    {
        var json = MediathekQueryBuilder.Create()
            .WithQuery(["topic"], "Tatort")
            .WithQuery(["channel"], "ARD")
            .SortBy("duration", "asc")
            .WithDurationRange(min: 300, max: 7200)
            .IncludeFuture(true)
            .WithPagination(10, 50)
            .Build();

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(2, root.GetProperty("queries").GetArrayLength());
        Assert.Equal("duration", root.GetProperty("sortBy").GetString());
        Assert.Equal("asc", root.GetProperty("sortOrder").GetString());
        Assert.True(root.GetProperty("future").GetBoolean());
        Assert.Equal(10, root.GetProperty("offset").GetInt32());
        Assert.Equal(50, root.GetProperty("size").GetInt32());
        Assert.Equal(300, root.GetProperty("duration_min").GetInt32());
        Assert.Equal(7200, root.GetProperty("duration_max").GetInt32());
    }

    [Fact]
    public void Multiple_fields_in_single_query()
    {
        var json = MediathekQueryBuilder.Create()
            .WithQuery(["title", "topic"], "das boot")
            .Build();

        var doc = JsonDocument.Parse(json);
        var fields = doc.RootElement.GetProperty("queries")[0].GetProperty("fields");

        Assert.Equal(2, fields.GetArrayLength());
        Assert.Equal("title", fields[0].GetString());
        Assert.Equal("topic", fields[1].GetString());
    }

    [Fact]
    public void Duration_min_only()
    {
        var json = MediathekQueryBuilder.Create()
            .WithQuery(["title"], "test")
            .WithDurationRange(min: 3600)
            .Build();

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(3600, root.GetProperty("duration_min").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("duration_max").ValueKind);
    }

    [Fact]
    public void FromMessage_roundtrips()
    {
        var query = new MediathekQuery(
            Fields: [new MediathekQueryField(["topic"], "Tatort"), new MediathekQueryField(["channel"], "ARD")],
            SortBy: "timestamp",
            SortOrder: "desc",
            Future: false,
            Offset: 5,
            Size: 25,
            DurationMin: 300,
            DurationMax: null);

        var json = MediathekQueryBuilder.FromMessage(query).Build();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(2, root.GetProperty("queries").GetArrayLength());
        Assert.Equal(5, root.GetProperty("offset").GetInt32());
        Assert.Equal(25, root.GetProperty("size").GetInt32());
        Assert.Equal(300, root.GetProperty("duration_min").GetInt32());
    }
}
