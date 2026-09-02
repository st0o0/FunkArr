using System.Text.Json;
using FunkArr.Messages.Mediathek;

namespace FunkArr.Search;

internal sealed class MediathekQueryBuilder
{
    private readonly List<QueryEntry> _queries = [];
    private string _sortBy = "timestamp";
    private string _sortOrder = "desc";
    private bool _future;
    private int _offset;
    private int _size = 15;
    private int? _durationMin;
    private int? _durationMax;

    public static MediathekQueryBuilder Create() => new();

    public MediathekQueryBuilder WithQuery(string[] fields, string query)
    {
        _queries.Add(new QueryEntry(fields, query));
        return this;
    }

    public MediathekQueryBuilder SortBy(string field, string order)
    {
        _sortBy = field;
        _sortOrder = order;
        return this;
    }

    public MediathekQueryBuilder WithDurationRange(int? min = null, int? max = null)
    {
        _durationMin = min;
        _durationMax = max;
        return this;
    }

    public MediathekQueryBuilder IncludeFuture(bool future)
    {
        _future = future;
        return this;
    }

    public MediathekQueryBuilder WithPagination(int offset, int size)
    {
        _offset = offset;
        _size = size;
        return this;
    }

    public static MediathekQueryBuilder FromMessage(QueryMediathek query)
    {
        var builder = new MediathekQueryBuilder
        {
            _sortBy = query.SortBy ?? "timestamp",
            _sortOrder = query.SortOrder ?? "desc",
            _future = query.Future,
            _offset = query.Offset,
            _size = query.Size,
            _durationMin = query.DurationMin,
            _durationMax = query.DurationMax,
        };

        foreach (var field in query.Fields)
        {
            builder._queries.Add(new QueryEntry(field.Fields, field.Query));
        }

        return builder;
    }

    public string Build()
    {
        var request = new Dictionary<string, object?>
        {
            ["queries"] = _queries.Select(q => new Dictionary<string, object>
            {
                ["fields"] = q.Fields,
                ["query"] = q.Query,
            }).ToArray(),
            ["sortBy"] = _sortBy,
            ["sortOrder"] = _sortOrder,
            ["future"] = _future,
            ["offset"] = _offset,
            ["size"] = _size,
            ["duration_min"] = _durationMin,
            ["duration_max"] = _durationMax,
        };

        return JsonSerializer.Serialize(request, _jsonOptions);
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
    };

    private sealed record QueryEntry(string[] Fields, string Query);
}
