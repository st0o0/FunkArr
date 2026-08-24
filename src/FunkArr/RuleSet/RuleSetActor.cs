using System.Text.Json;
using Akka.Actor;
using Akka.Event;
using FunkArr.Configuration;
using FunkArr.Search;
using FunkArr.Search.Resolvers;
using Microsoft.Extensions.Options;
using Servus.Akka;

namespace FunkArr.RuleSet;

public sealed class RuleSetActor : ReceiveActor, IWithTimers
{
    private readonly GitHubReleaseClient _gitHubReleaseClient;
    private readonly MediathekClient _mediathekClient;
    private readonly TvdbClient _tvdbClient;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    private readonly string _communityPath;
    private readonly string _generatedPath;
    private readonly string _localPath;

    public ITimerScheduler Timers { get; set; } = null!;

    private readonly Dictionary<string, RuleSetFile> _byTopic = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RuleSetFile> _byAlias = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, RuleSetFile> _byTvdbId = new();
    private readonly HashSet<int> _generationInProgress = [];

    public sealed record GetRulesForTopic(string Topic, int? TvdbId);
    public sealed record RulesResponse(IReadOnlyList<Rule> Rules);
    public sealed record RefreshCommunity;
    public sealed record GenerationComplete(RuleSetFile RuleSet);
    public sealed record GenerationFailed(int TvdbId);
    public sealed record ReloadLocal;

    public sealed record GetAllRulesets;
    public sealed record AllRulesetsResponse(IReadOnlyList<RuleSetSummary> Rulesets);
    public sealed record RuleSetSummary(string Topic, string Source, int RuleCount, MediaReference Media, IReadOnlyList<string> Aliases);

    public sealed record GetRuleSet(string Topic);
    public sealed record RuleSetResponse(RuleSetFile? RuleSet);

    public sealed record SaveLocalRuleSet(RuleSetFile RuleSet);
    public sealed record SaveLocalRuleSetResponse(bool Success);

    public sealed record DeleteLocalRuleSet(string Topic);
    public sealed record DeleteLocalRuleSetResponse(bool Found);

    public sealed record TestRules(string Topic, int? TvdbId, IReadOnlyList<Rule> Rules);
    public sealed record TestRulesResponse(
        IReadOnlyList<MatchedTrace> Matched,
        IReadOnlyList<FilteredTrace> Filtered,
        IReadOnlyList<UnmatchedTrace> Unmatched,
        int TotalItems);

    private IActorRef _RefreshActor = ActorRefs.Nobody;
    private IActorRef _MatchQualityActor = ActorRefs.Nobody;

    public RuleSetActor(
        GitHubReleaseClient gitHubReleaseClient,
        MediathekClient mediathekClient,
        TvdbClient tvdbClient,
        IOptions<RuleSetOptions> options)
    {
        _gitHubReleaseClient = gitHubReleaseClient;
        _mediathekClient = mediathekClient;
        _tvdbClient = tvdbClient;
        var options1 = options.Value;

        _communityPath = Path.Combine(options1.Path, "community");
        _generatedPath = Path.Combine(options1.Path, "generated");
        _localPath = Path.Combine(options1.Path, "local");

        Directory.CreateDirectory(_communityPath);
        Directory.CreateDirectory(_generatedPath);
        Directory.CreateDirectory(_localPath);

        LoadAllFromDisk();

        _RefreshActor = Context.ActorOf(Props.Create(() =>
            new RefreshActor(gitHubReleaseClient)), "refresh");
        _MatchQualityActor = Context.ActorOf(Props.Create(() =>
            new MatchQualityActor()), "match-quality");

        Timers.StartPeriodicTimer(
            "refresh-community",
            new RefreshCommunity(),
            TimeSpan.Zero,
            TimeSpan.FromMinutes(options1.RefreshIntervalMinutes));

        Receive<GetRulesForTopic>(HandleGetRulesForTopic);
        Receive<RefreshCommunity>(_ =>
            _RefreshActor.Tell(new RefreshActor.DoRefresh(_communityPath)));
        Receive<RefreshActor.RefreshComplete>(HandleRefreshComplete);
        Receive<GenerationComplete>(HandleGenerationComplete);
        Receive<GenerationFailed>(HandleGenerationFailed);
        Receive<ReloadLocal>(HandleReloadLocal);
        Receive<GetAllRulesets>(HandleGetAllRulesets);
        Receive<GetRuleSet>(HandleGetRuleSet);
        Receive<SaveLocalRuleSet>(HandleSaveLocalRuleSet);
        Receive<DeleteLocalRuleSet>(HandleDeleteLocalRuleSet);
        ReceiveAsync<TestRules>(HandleTestRulesAsync);

        Receive<MatchQualityActor.RecordMatchResult>(msg =>
            _MatchQualityActor.Forward(msg));
        Receive<MatchQualityActor.GetRecentMatches>(msg =>
            _MatchQualityActor.Forward(msg));
        Receive<MatchQualityActor.GetTopicStats>(msg =>
            _MatchQualityActor.Forward(msg));
        Receive<MatchQualityActor.GetAllTopicStats>(msg =>
            _MatchQualityActor.Forward(msg));
        Receive<MatchQualityActor.GetUnmatchedItems>(msg =>
            _MatchQualityActor.Forward(msg));
    }

    private void HandleRefreshComplete(RefreshActor.RefreshComplete msg)
    {
        if (msg.Updated)
        {
            LoadAllFromDisk();
            _log.Info("Refreshed community rulesets, {Count} topics loaded", _byTopic.Count);
        }
    }

    private void LoadAllFromDisk()
    {
        _byTopic.Clear();
        _byAlias.Clear();
        _byTvdbId.Clear();

        LoadLayer(_communityPath, isLocal: false);
        LoadLayer(_generatedPath, isLocal: false);
        LoadLayer(_localPath, isLocal: true);
    }

    private void LoadLayer(string directory, bool isLocal)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var ruleSet = JsonSerializer.Deserialize<RuleSetFile>(json, RuleSetJsonOptions.Default);
                if (ruleSet is null)
                {
                    _log.Warning("Failed to deserialize ruleset from {Path}", file);
                    continue;
                }

                if (isLocal && ruleSet.Overrides?.Mode == OverrideMode.Merge
                    && _byTopic.TryGetValue(ruleSet.Topic, out var baseRuleSet))
                {
                    ruleSet = ApplyMergeOverride(ruleSet, baseRuleSet);
                }

                _byTopic[ruleSet.Topic] = ruleSet;

                foreach (var alias in ruleSet.Aliases)
                {
                    if (_byAlias.TryGetValue(alias, out var existing) && existing.Topic != ruleSet.Topic)
                    {
                        _log.Warning("Alias '{Alias}' conflicts: '{ExistingTopic}' vs '{NewTopic}'", alias, existing.Topic, ruleSet.Topic);
                    }

                    _byAlias[alias] = ruleSet;
                }

                if (ruleSet.Media.TvdbId is { } tvdbId)
                {
                    _byTvdbId[tvdbId] = ruleSet;
                }
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Skipping malformed ruleset file {Path}", file);
            }
        }
    }

    private RuleSetFile ApplyMergeOverride(RuleSetFile local, RuleSetFile baseRuleSet)
    {
        var baseRules = baseRuleSet.Rules.ToList();

        // Remove by index (descending to preserve indices)
        foreach (var idx in local.Overrides!.Remove.OrderByDescending(i => i))
        {
            if (idx >= 0 && idx < baseRules.Count)
            {
                baseRules.RemoveAt(idx);
            }
        }

        // Add new rules
        baseRules.AddRange(local.Overrides.Add);

        return baseRuleSet with
        {
            Rules = baseRules.OrderBy(r => r.Priority).ToList(),
            Source = "local",
        };
    }

    private void HandleGetRulesForTopic(GetRulesForTopic request)
    {
        var rules = new List<Rule>();

        if (_byTopic.TryGetValue(request.Topic, out var topicMatch))
        {
            rules.AddRange(topicMatch.Rules);
        }
        else if (_byAlias.TryGetValue(request.Topic, out var aliasMatch))
        {
            rules.AddRange(aliasMatch.Rules);
        }
        else if (request.TvdbId is { } tvdbId && _byTvdbId.TryGetValue(tvdbId, out var tvdbMatch))
        {
            rules.AddRange(tvdbMatch.Rules);
        }
        else if (request.TvdbId is { } id && _generationInProgress.Add(id))
        {
            _log.Info(
                "No ruleset for topic '{Topic}' (tvdbId={TvdbId}), spawning generator",
                request.Topic,
                id);
            var generator = Context.ResolveChildActor<RuleSetGeneratorActor>(
                $"generator-{id}");
            generator.Tell(new RuleSetGeneratorActor.GenerateRuleSet(id, request.Topic));
        }

        Sender.Tell(new RulesResponse(rules));
    }

    private void HandleGenerationComplete(GenerationComplete msg)
    {
        var ruleSet = msg.RuleSet;

        if (ruleSet.Media.TvdbId is { } tvdbId)
        {
            _generationInProgress.Remove(tvdbId);
        }

        _byTopic[ruleSet.Topic] = ruleSet;

        if (ruleSet.Media.TvdbId is { } id)
        {
            _byTvdbId[id] = ruleSet;
        }

        RuleSetFileWriter.Write(_generatedPath, ruleSet);

        _log.Info("Generation complete for topic '{Topic}'", ruleSet.Topic);
    }

    private void HandleGenerationFailed(GenerationFailed msg)
    {
        _generationInProgress.Remove(msg.TvdbId);
        _log.Warning("Ruleset generation failed for tvdbId={TvdbId}", msg.TvdbId);
    }

    private void HandleReloadLocal(ReloadLocal _)
    {
        LoadAllFromDisk();
        _log.Info("Reloaded rulesets from disk, {Count} topics in index", _byTopic.Count);
    }

    private void HandleGetAllRulesets(GetAllRulesets _)
    {
        var summaries = _byTopic.Values
            .Select(rs => new RuleSetSummary(
                rs.Topic,
                rs.Source,
                rs.Rules.Count,
                rs.Media,
                rs.Aliases))
            .OrderBy(s => s.Topic, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Sender.Tell(new AllRulesetsResponse(summaries));
    }

    private void HandleGetRuleSet(GetRuleSet msg)
    {
        _byTopic.TryGetValue(msg.Topic, out var ruleSet);
        Sender.Tell(new RuleSetResponse(ruleSet));
    }

    private void HandleSaveLocalRuleSet(SaveLocalRuleSet msg)
    {
        RuleSetFileWriter.Write(_localPath, msg.RuleSet);
        LoadAllFromDisk();
        Sender.Tell(new SaveLocalRuleSetResponse(true));
    }

    private void HandleDeleteLocalRuleSet(DeleteLocalRuleSet msg)
    {
        var slug = TopicSlugGenerator.Generate(msg.Topic);
        var path = Path.Combine(_localPath, $"{slug}.json");
        var found = File.Exists(path);

        if (found)
        {
            File.Delete(path);
            LoadAllFromDisk();
        }

        Sender.Tell(new DeleteLocalRuleSetResponse(found));
    }

    private async Task HandleTestRulesAsync(TestRules msg)
    {
        var query = new MediathekQuery
        {
            Queries = [new MediathekQueryItem { Fields = ["topic", "title"], Query = msg.Topic }],
        };

        var mediathekResponse = await _mediathekClient.QueryAsync(query);
        var items = mediathekResponse?.Result?.Results ?? [];

        var episodes = Array.Empty<TvdbEpisodeInfo>();
        if (msg.TvdbId is { } tvdbId and > 0)
        {
            episodes = await _tvdbClient.GetEpisodesAsync(tvdbId, 1) ?? [];
        }

        var (_, traces) = RuleSetMatchingEngine.EvaluateRulesWithTraces(
            items, msg.Rules, episodes, msg.Topic);

        var matched = traces.OfType<MatchedTrace>().ToList();
        var filtered = traces.OfType<FilteredTrace>().ToList();
        var unmatched = traces.OfType<UnmatchedTrace>().ToList();

        Sender.Tell(new TestRulesResponse(matched, filtered, unmatched, items.Length));
    }
}
