using System.Text.Json;
using System.Text.Json.Serialization;

namespace FunkArr.MatchMagic;

[JsonConverter(typeof(FilterGroupJsonConverter))]
public sealed record FilterGroup(
    IReadOnlyList<FilterNode>? All = null,
    IReadOnlyList<FilterNode>? Any = null,
    IReadOnlyList<FilterNode>? Not = null)
{
    public bool Evaluate(MediaItem item)
    {
        if (All is null && Any is null && Not is null)
        {
            return true;
        }

        if (All is { Count: > 0 } && !All.All(n => n.Evaluate(item)))
        {
            return false;
        }

        if (Any is { Count: > 0 } && !Any.Any(n => n.Evaluate(item)))
        {
            return false;
        }

        if (Not is { Count: > 0 } && Not.Any(n => n.Evaluate(item)))
        {
            return false;
        }

        return true;
    }
}

[JsonConverter(typeof(FilterNodeJsonConverter))]
public abstract record FilterNode
{
    public abstract bool Evaluate(MediaItem item);

    public sealed record Leaf(Filter Filter) : FilterNode
    {
        public override bool Evaluate(MediaItem item) => Filter.Evaluate(item);
    }

    public sealed record Group(FilterGroup FilterGroup) : FilterNode
    {
        public override bool Evaluate(MediaItem item) => FilterGroup.Evaluate(item);
    }
}

internal sealed class FilterNodeJsonConverter : JsonConverter<FilterNode>
{
    public override FilterNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject for FilterNode");
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.TryGetProperty("all", out _) || root.TryGetProperty("any", out _) || root.TryGetProperty("not", out _))
        {
            var group = root.Deserialize<FilterGroup>(options)!;
            return new FilterNode.Group(group);
        }

        var filter = root.Deserialize<Filter>(options)!;
        return new FilterNode.Leaf(filter);
    }

    public override void Write(Utf8JsonWriter writer, FilterNode value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case FilterNode.Leaf leaf:
                JsonSerializer.Serialize(writer, leaf.Filter, options);
                break;
            case FilterNode.Group group:
                JsonSerializer.Serialize(writer, group.FilterGroup, options);
                break;
        }
    }
}

internal sealed class FilterGroupJsonConverter : JsonConverter<FilterGroup>
{
    public override FilterGroup Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject for FilterGroup");
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var all = DeserializeNodes(root, "all", options);
        var any = DeserializeNodes(root, "any", options);
        var not = DeserializeNodes(root, "not", options);

        return new FilterGroup(all, any, not);
    }

    public override void Write(Utf8JsonWriter writer, FilterGroup value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.All is { Count: > 0 })
        {
            writer.WritePropertyName("all");
            JsonSerializer.Serialize(writer, value.All, options);
        }

        if (value.Any is { Count: > 0 })
        {
            writer.WritePropertyName("any");
            JsonSerializer.Serialize(writer, value.Any, options);
        }

        if (value.Not is { Count: > 0 })
        {
            writer.WritePropertyName("not");
            JsonSerializer.Serialize(writer, value.Not, options);
        }

        writer.WriteEndObject();
    }

    private static IReadOnlyList<FilterNode>? DeserializeNodes(
        JsonElement root, string property, JsonSerializerOptions options)
    {
        if (!root.TryGetProperty(property, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var nodes = new List<FilterNode>();
        foreach (var item in element.EnumerateArray())
        {
            var raw = item.GetRawText();
            var node = JsonSerializer.Deserialize<FilterNode>(raw, options)!;
            nodes.Add(node);
        }

        return nodes;
    }
}
