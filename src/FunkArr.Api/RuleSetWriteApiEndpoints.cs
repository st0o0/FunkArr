using System.Text.Json;
using System.Text.RegularExpressions;
using FunkArr.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FunkArr.Api;

public static partial class RuleSetWriteApiEndpoints
{
    private static readonly Regex _ruleSetIdPattern = MyRegex();

    public static WebApplication MapRuleSetWriteApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/rulesets");

        group.MapPost("/", HandleCreate);
        group.MapPut("/{id}", HandleUpdate);
        group.MapDelete("/{id}", HandleDelete);
        group.MapGet("/{id}/raw", HandleGetRaw);

        return app;
    }

    private static IResult HandleCreate(JsonElement body, IDataFiles dataFiles, DataPaths dataPaths)
    {
        if (!body.TryGetProperty("ruleSetId", out var idEl) || idEl.ValueKind != JsonValueKind.String)
        {
            return Results.BadRequest(new { error = "ruleSetId is required" });
        }

        var ruleSetId = idEl.GetString()!;
        if (!_ruleSetIdPattern.IsMatch(ruleSetId))
        {
            return Results.BadRequest(new { error = "ruleSetId must be kebab-case (lowercase letters, numbers, hyphens)" });
        }

        if (!body.TryGetProperty("topic", out var topicEl) || topicEl.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(topicEl.GetString()))
        {
            return Results.BadRequest(new { error = "topic is required" });
        }

        var localPath = Path.Join(dataPaths.LocalRuleSets, $"{ruleSetId}.json");
        if (dataFiles.Exists(localPath))
        {
            return Results.Conflict(new { error = $"Local ruleset '{ruleSetId}' already exists" });
        }

        var json = body.GetRawText();
        dataFiles.CreateDirectory(dataPaths.LocalRuleSets);
        dataFiles.WriteAtomic(localPath, json);

        return Results.Created($"/api/rulesets/{ruleSetId}", new { ruleSetId });
    }

    private static IResult HandleUpdate(string id, JsonElement body, IDataFiles dataFiles, DataPaths dataPaths)
    {
        var localPath = Path.Join(dataPaths.LocalRuleSets, $"{id}.json");
        var communityPath = Path.Join(dataPaths.CommunityRuleSets, $"{id}.json");

        if (!dataFiles.Exists(localPath) && !dataFiles.Exists(communityPath))
        {
            return Results.NotFound();
        }

        var json = body.GetRawText();
        dataFiles.CreateDirectory(dataPaths.LocalRuleSets);
        dataFiles.WriteAtomic(localPath, json);

        return Results.Ok();
    }

    private static IResult HandleGetRaw(string id, IDataFiles dataFiles, DataPaths dataPaths)
    {
        var localPath = Path.Join(dataPaths.LocalRuleSets, $"{id}.json");
        var communityPath = Path.Join(dataPaths.CommunityRuleSets, $"{id}.json");

        if (dataFiles.Exists(localPath))
        {
            var json = dataFiles.ReadText(localPath);
            return Results.Content(json, "application/json");
        }

        if (dataFiles.Exists(communityPath))
        {
            var json = dataFiles.ReadText(communityPath);
            return Results.Content(json, "application/json");
        }

        return Results.NotFound();
    }

    private static IResult HandleDelete(string id, IDataFiles dataFiles, DataPaths dataPaths)
    {
        var localPath = Path.Join(dataPaths.LocalRuleSets, $"{id}.json");

        if (!dataFiles.Exists(localPath))
        {
            return Results.NotFound();
        }

        dataFiles.Remove(localPath);

        return Results.Ok();
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
