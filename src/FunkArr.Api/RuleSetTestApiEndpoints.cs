using System.Text.Json;
using System.Text.Json.Serialization;
using Akka.Actor;
using Akka.Hosting;
using FunkArr.Core;
using FunkArr.Messages.Scoring;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ApiModels = FunkArr.Api.Models;

namespace FunkArr.Api;

public static class RuleSetTestApiEndpoints
{
    private static readonly TimeSpan _testTimeout = TimeSpan.FromSeconds(15);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static WebApplication MapRuleSetTestApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/rulesets");

        group.MapPost("/test", async (JsonElement body, IActorRegistry registry) =>
        {
            var request = ParseTestRequest(body);
            if (request is null)
            {
                return Results.BadRequest(new { error = "Invalid request body" });
            }

            var (config, candidates) = request.Value;

            var manager = await registry.GetAsync<IMatchMagicManager>();
            try
            {
                var result = await manager.Ask<IScoringResponse>(
                    new TestScoreItems(Guid.NewGuid(), config, candidates), _testTimeout);

                return result switch
                {
                    TestScoreCompleted completed => Results.Ok(new
                    {
                        itemTraces = completed.ItemTraces
                            .Select(RuleSetApiEndpoints.ToItemTraceModel).ToArray(),
                    }),
                    _ => Results.Problem(statusCode: 504, title: "Gateway Timeout"),
                };
            }
            catch (Exception)
            {
                return Results.Problem(statusCode: 504, title: "Gateway Timeout");
            }
        })
        .Produces<object>()
        .ProducesProblem(400)
        .ProducesProblem(504);

        return app;
    }

    private static (MatchingConfig Config, ScoreCandidate[] Candidates)? ParseTestRequest(JsonElement body)
    {
        if (!body.TryGetProperty("config", out var configEl) ||
            !body.TryGetProperty("candidates", out var candidatesEl))
        {
            return null;
        }

        var defaultConfidence = configEl.TryGetProperty("defaultConfidence", out var confEl)
            ? (float)confEl.GetDouble()
            : 0f;

        var rules = configEl.TryGetProperty("rules", out var rulesEl)
            ? ParseRules(rulesEl)
            : [];

        var config = new MatchingConfig("test", defaultConfidence, rules);
        var candidates = ParseCandidates(candidatesEl);

        return (config, candidates);
    }

    private static MatchingRule[] ParseRules(JsonElement rulesEl)
    {
        var results = new List<MatchingRule>();

        foreach (var ruleEl in rulesEl.EnumerateArray())
        {
            var id = ruleEl.GetProperty("id").GetString() ?? "";
            var priority = ruleEl.TryGetProperty("priority", out var prioEl) ? prioEl.GetInt32() : 0;
            var confidence = ruleEl.TryGetProperty("confidence", out var confEl) && confEl.ValueKind != JsonValueKind.Null
                ? (float?)confEl.GetDouble()
                : null;
            var strategy = ruleEl.TryGetProperty("strategy", out var stratEl) ? stratEl.GetString() : null;

            var identification = ParseIdentification(strategy, ruleEl);
            if (identification is null)
            {
                continue;
            }

            var filters = ruleEl.TryGetProperty("filters", out var filtersEl) && filtersEl.ValueKind != JsonValueKind.Null
                ? ParseFilterSpec(filtersEl)
                : null;

            results.Add(new MatchingRule(id, priority, confidence, filters, identification));
        }

        return results.ToArray();
    }

    private static IdentificationSpec? ParseIdentification(string? strategy, JsonElement ruleEl)
    {
        var seasonRegex = ruleEl.TryGetProperty("seasonRegex", out var sEl) ? sEl.GetString() : null;
        var episodeRegex = ruleEl.TryGetProperty("episodeRegex", out var eEl) ? eEl.GetString() : null;
        var captureGroup = ruleEl.TryGetProperty("captureGroup", out var cgEl) && cgEl.ValueKind != JsonValueKind.Null
            ? (int?)cgEl.GetInt32()
            : null;

        return strategy switch
        {
            "seasonAndEpisodeNumber" => new IdentificationSpec(
                IdentificationStrategy.RegexCapture,
                SeasonPattern: seasonRegex,
                EpisodePattern: episodeRegex,
                CaptureGroup: captureGroup),

            "byAbsoluteEpisodeNumber" => new IdentificationSpec(
                IdentificationStrategy.RegexCapture,
                EpisodePattern: episodeRegex,
                CaptureGroup: captureGroup),

            "itemTitleExact" => new IdentificationSpec(
                IdentificationStrategy.TitleConstruction,
                MatchMode: TitleMatchMode.Exact,
                TitleParts: ParseTitleRules(ruleEl)),

            "itemTitleIncludes" => new IdentificationSpec(
                IdentificationStrategy.TitleConstruction,
                MatchMode: TitleMatchMode.Contains,
                TitleParts: ParseTitleRules(ruleEl)),

            "itemTitleEqualsAirdate" => new IdentificationSpec(
                IdentificationStrategy.AirdateExtraction),

            _ => null,
        };
    }

    private static TitlePart[]? ParseTitleRules(JsonElement ruleEl)
    {
        if (!ruleEl.TryGetProperty("titleRules", out var titleRulesEl) ||
            titleRulesEl.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var parts = new List<TitlePart>();

        foreach (var trEl in titleRulesEl.EnumerateArray())
        {
            var type = trEl.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
            var partType = type switch
            {
                "static" => TitlePartType.Static,
                "regex" => TitlePartType.Regex,
                _ => (TitlePartType?)null,
            };

            if (partType is null)
            {
                continue;
            }

            var field = trEl.TryGetProperty("field", out var fEl) ? ParseFilterField(fEl.GetString()) : null;
            var pattern = trEl.TryGetProperty("pattern", out var pEl) ? pEl.GetString() : null;
            var captureGroup = trEl.TryGetProperty("captureGroup", out var cgEl) && cgEl.ValueKind != JsonValueKind.Null
                ? (int?)cgEl.GetInt32()
                : null;
            var value = trEl.TryGetProperty("value", out var vEl) ? vEl.GetString() : null;

            parts.Add(new TitlePart(partType.Value, Value: value, Pattern: pattern, Field: field, CaptureGroup: captureGroup));
        }

        return parts.Count > 0 ? parts.ToArray() : null;
    }

    private static FilterSpec? ParseFilterSpec(JsonElement filtersEl)
    {
        var all = filtersEl.TryGetProperty("all", out var allEl) ? ParseFilterNodes(allEl) : null;
        var any = filtersEl.TryGetProperty("any", out var anyEl) ? ParseFilterNodes(anyEl) : null;
        var not = filtersEl.TryGetProperty("not", out var notEl) ? ParseFilterNodes(notEl) : null;

        if (all is null && any is null && not is null)
        {
            return null;
        }

        return new FilterSpec(all, any, not);
    }

    private static FilterNode[]? ParseFilterNodes(JsonElement arrayEl)
    {
        if (arrayEl.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var nodes = new List<FilterNode>();

        foreach (var el in arrayEl.EnumerateArray())
        {
            if (el.TryGetProperty("all", out _) || el.TryGetProperty("any", out _) || el.TryGetProperty("not", out _))
            {
                var nested = ParseFilterSpec(el);
                if (nested is not null)
                {
                    nodes.Add(new FilterNode.GroupNode(nested));
                }
            }
            else
            {
                var condition = ParseFilterCondition(el);
                if (condition is not null)
                {
                    nodes.Add(new FilterNode.ConditionNode(condition));
                }
            }
        }

        return nodes.Count > 0 ? nodes.ToArray() : null;
    }

    private static FilterCondition? ParseFilterCondition(JsonElement el)
    {
        if (!el.TryGetProperty("field", out var fieldEl) ||
            !el.TryGetProperty("op", out var opEl) ||
            !el.TryGetProperty("value", out var valueEl))
        {
            return null;
        }

        var field = ParseFilterField(fieldEl.GetString());
        var op = ParseFilterOp(opEl.GetString());

        if (field is null || op is null)
        {
            return null;
        }

        return new FilterCondition(field.Value, op.Value, valueEl.GetString() ?? "");
    }

    private static FilterField? ParseFilterField(string? value) => value switch
    {
        "title" => FilterField.Title,
        "topic" => FilterField.Topic,
        "channel" => FilterField.Channel,
        "description" => FilterField.Description,
        "duration" => FilterField.Duration,
        "timestamp" => FilterField.Timestamp,
        _ => null,
    };

    private static FilterOp? ParseFilterOp(string? value) => value switch
    {
        "eq" => FilterOp.Eq,
        "contains" => FilterOp.Contains,
        "notContains" => FilterOp.NotContains,
        "greaterThan" => FilterOp.GreaterThan,
        "lessThan" => FilterOp.LessThan,
        "regex" => FilterOp.Regex,
        _ => null,
    };

    private static ScoreCandidate[] ParseCandidates(JsonElement candidatesEl)
    {
        var results = new List<ScoreCandidate>();

        foreach (var el in candidatesEl.EnumerateArray())
        {
            var title = el.TryGetProperty("title", out var tEl) ? tEl.GetString() ?? "" : "";
            var topic = el.TryGetProperty("topic", out var toEl) ? toEl.GetString() ?? "" : "";
            var channel = el.TryGetProperty("channel", out var chEl) ? chEl.GetString() ?? "" : "";
            var duration = el.TryGetProperty("duration", out var dEl) ? dEl.GetInt32() : 0;
            var quality = el.TryGetProperty("quality", out var qEl) ? qEl.GetInt32() : 0;
            var description = el.TryGetProperty("description", out var deEl) && deEl.ValueKind != JsonValueKind.Null
                ? deEl.GetString()
                : null;
            var timestamp = el.TryGetProperty("timestamp", out var tsEl) ? tsEl.GetInt64() : 0;

            results.Add(new ScoreCandidate(title, topic, channel, duration, quality, description, timestamp));
        }

        return results.ToArray();
    }
}
