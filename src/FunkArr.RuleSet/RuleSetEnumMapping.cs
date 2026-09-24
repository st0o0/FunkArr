using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Scoring;

namespace FunkArr.RuleSet;

public static class RuleSetEnumMapping
{
    public static bool TryParseStrategy(string? value, out IdentificationStrategy strategy)
    {
        strategy = default;
        if (value is null)
        {
            return false;
        }

        (strategy, var valid) = value switch
        {
            "seasonAndEpisodeNumber" => (IdentificationStrategy.SeasonAndEpisodeNumber, true),
            "byAbsoluteEpisodeNumber" => (IdentificationStrategy.AbsoluteEpisodeNumber, true),
            "itemTitleExact" => (IdentificationStrategy.TitleExact, true),
            "itemTitleIncludes" => (IdentificationStrategy.TitleIncludes, true),
            "itemTitleEqualsAirdate" => (IdentificationStrategy.AirdateExtraction, true),
            _ => (default, false),
        };

        return valid;
    }

    public static string ToDiskValue(this IdentificationStrategy strategy) => strategy switch
    {
        IdentificationStrategy.SeasonAndEpisodeNumber => "seasonAndEpisodeNumber",
        IdentificationStrategy.AbsoluteEpisodeNumber => "byAbsoluteEpisodeNumber",
        IdentificationStrategy.TitleExact => "itemTitleExact",
        IdentificationStrategy.TitleIncludes => "itemTitleIncludes",
        IdentificationStrategy.AirdateExtraction => "itemTitleEqualsAirdate",
        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null),
    };

    public static string ToDiskValue(this FilterField field) => field switch
    {
        FilterField.Title => "title",
        FilterField.Topic => "topic",
        FilterField.Channel => "channel",
        FilterField.Description => "description",
        FilterField.Duration => "duration",
        FilterField.Timestamp => "timestamp",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
    };

    public static bool TryParseFilterField(string? value, out FilterField field)
    {
        field = default;
        if (value is null) return false;

        (field, var valid) = value switch
        {
            "title" => (FilterField.Title, true),
            "topic" => (FilterField.Topic, true),
            "channel" => (FilterField.Channel, true),
            "description" => (FilterField.Description, true),
            "duration" => (FilterField.Duration, true),
            "timestamp" => (FilterField.Timestamp, true),
            _ => (default, false),
        };

        return valid;
    }

    public static string ToDiskValue(this FilterOp op) => op switch
    {
        FilterOp.Eq => "eq",
        FilterOp.Contains => "contains",
        FilterOp.NotContains => "notContains",
        FilterOp.GreaterThan => "greaterThan",
        FilterOp.LessThan => "lessThan",
        FilterOp.Regex => "regex",
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, null),
    };

    public static bool TryParseFilterOp(string? value, out FilterOp op)
    {
        op = default;
        if (value is null) return false;

        (op, var valid) = value switch
        {
            "eq" => (FilterOp.Eq, true),
            "contains" => (FilterOp.Contains, true),
            "notContains" => (FilterOp.NotContains, true),
            "greaterThan" => (FilterOp.GreaterThan, true),
            "lessThan" => (FilterOp.LessThan, true),
            "regex" => (FilterOp.Regex, true),
            _ => (default, false),
        };

        return valid;
    }

    public static string ToDiskValue(this TitlePartType type) => type switch
    {
        TitlePartType.Static => "static",
        TitlePartType.Regex => "regex",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static bool TryParseTitlePartType(string? value, out TitlePartType type)
    {
        type = default;
        if (value is null) return false;

        (type, var valid) = value switch
        {
            "static" => (TitlePartType.Static, true),
            "regex" => (TitlePartType.Regex, true),
            _ => (default, false),
        };

        return valid;
    }

    public static string ToDiskValue(this EnrichmentMethod method) => method switch
    {
        EnrichmentMethod.Title => "title",
        EnrichmentMethod.Airdate => "airdate",
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
    };

    public static string ToDiskValue(this RuntimeMode mode) => mode switch
    {
        RuntimeMode.Tiebreaker => "tiebreaker",
        RuntimeMode.Filter => "filter",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    public static string ToDiskValue(this MediaType type) => type switch
    {
        MediaType.Show => "show",
        MediaType.Movie => "movie",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static bool TryParseMediaType(string? value, out MediaType type)
    {
        type = default;
        if (value is null) return false;

        (type, var valid) = value switch
        {
            "show" => (MediaType.Show, true),
            "movie" => (MediaType.Movie, true),
            _ => (default, false),
        };

        return valid;
    }
}
