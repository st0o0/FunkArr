using System.Text.Json.Serialization;

namespace FunkArr.Messages.Scoring;

[JsonConverter(typeof(JsonStringEnumConverter<FilterOp>))]
public enum FilterOp
{
    [JsonStringEnumMemberName("eq")] Eq,
    [JsonStringEnumMemberName("contains")] Contains,
    [JsonStringEnumMemberName("notContains")] NotContains,
    [JsonStringEnumMemberName("greaterThan")] GreaterThan,
    [JsonStringEnumMemberName("lessThan")] LessThan,
    [JsonStringEnumMemberName("regex")] Regex,
}
