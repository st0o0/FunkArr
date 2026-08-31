using System.Text.Json.Serialization;

namespace FunkArr.MatchMagic;

[JsonConverter(typeof(JsonStringEnumConverter<FilterOp>))]
public enum FilterOp
{
    [JsonStringEnumMemberName("greaterThan")]
    GreaterThan,

    [JsonStringEnumMemberName("lessThan")]
    LessThan,

    [JsonStringEnumMemberName("eq")]
    Eq,

    [JsonStringEnumMemberName("contains")]
    Contains,

    [JsonStringEnumMemberName("notContains")]
    NotContains,

    [JsonStringEnumMemberName("regex")]
    Regex,
}
