using Akka.Actor;
using FunkArr.Core;
using FunkArr.Messages;
using FunkArr.Messages.Mediathek;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Search;

namespace FunkArr.Search.Tests;

public sealed class VariantPolicyStateTests
{
    private const string _standardTitle = "Herz aus Eis";
    private const string _adTitle = "Herz aus Eis (Audiodeskription)";

    private static readonly IActorRef _noSender = ActorRefs.Nobody;

    [Fact]
    public void Tv_unscored_results_exclude_variant_by_default()
    {
        var state = TvState();
        state.Apply(new QueryMediathekCompleted([Item(_standardTitle), Item(_adTitle)], 2));

        var result = state.ToCompleted();

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, i => Assert.DoesNotContain(".AD.", i.Title));
    }

    [Fact]
    public void Tv_scored_results_exclude_matched_and_unmatched_variants()
    {
        var state = TvState();
        state.Apply(new QueryMediathekCompleted([Item(_standardTitle), Item(_adTitle), Item("Folge 2 (DGS)")], 3));
        state.ApplyRuleSet("tatort", "Tatort", null);
        state.Apply(new ScoreCompleted(Guid.Empty,
        [
            new ScoredItem(0, 0.9, true, new MetadataSpec("1", "1", null)),
            new ScoredItem(1, 0.9, true, new MetadataSpec("1", "1", null)),
            new ScoredItem(2, 0.0, false)
        ], []));

        var result = state.ToCompleted();

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, i => Assert.Multiple(
            () => Assert.DoesNotContain(".AD.", i.Title),
            () => Assert.DoesNotContain(".DGS.", i.Title)));
    }

    [Fact]
    public void Tv_allowed_variant_is_returned_with_tag()
    {
        var state = TvState(new VariantOptions { AudioDescription = VariantPolicy.Allow });
        state.Apply(new QueryMediathekCompleted([Item(_adTitle)], 1));

        var result = state.ToCompleted();

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, i => Assert.Contains(".AD.GERMAN.", i.Title));
    }

    [Fact]
    public void Tv_scoring_request_marks_excluded_candidates()
    {
        var state = TvState();
        state.Apply(new QueryMediathekCompleted([Item(_standardTitle), Item(_adTitle)], 2));
        state.ApplyRuleSet("tatort", "Tatort", null);

        Assert.True(state.TryGetScoringRequest(out var request));
        Assert.NotNull(request);
        Assert.Equal(2, request.Candidates.Length);
        Assert.Multiple(
            () => Assert.Null(request.Candidates[0].ExcludedVariant),
            () => Assert.Equal(MediathekVariant.AudioDescription, request.Candidates[1].ExcludedVariant));
    }

    [Fact]
    public void Tv_scoring_request_does_not_mark_allowed_variants()
    {
        var state = TvState(new VariantOptions { AudioDescription = VariantPolicy.Allow });
        state.Apply(new QueryMediathekCompleted([Item(_adTitle)], 1));
        state.ApplyRuleSet("tatort", "Tatort", null);

        Assert.True(state.TryGetScoringRequest(out var request));
        Assert.NotNull(request);
        var candidate = Assert.Single(request.Candidates);
        Assert.Null(candidate.ExcludedVariant);
    }

    [Fact]
    public void Movie_unscored_results_exclude_variant_by_default()
    {
        var state = MovieState();
        state.Apply(new QueryMediathekCompleted([Item("Die Brücke", "Film"), Item("Die Brücke (OmU)", "Film")], 2));

        var result = state.ToCompleted();

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, i => Assert.Contains(".GERMAN.", i.Title));
    }

    [Fact]
    public void Movie_scoring_request_marks_excluded_candidates()
    {
        var state = MovieState();
        state.Apply(new QueryMediathekCompleted([Item("Die Brücke (OV)", "Film")], 1));
        state.Apply("film", "Die Brücke", null);

        Assert.True(state.TryGetScoringRequest(out var request));
        Assert.NotNull(request);
        var candidate = Assert.Single(request.Candidates);
        Assert.Equal(MediathekVariant.OriginalVersion, candidate.ExcludedVariant);
    }

    private static TvEvaluationState TvState(VariantOptions? variants = null)
    {
        var id = Guid.NewGuid();
        var state = TvEvaluationState.From(id.ToString(), variants);
        state.Init(new EvaluateSeries(id, SearchSource.Sonarr, []), _noSender);
        return state;
    }

    private static MovieEvaluationState MovieState(VariantOptions? variants = null)
    {
        var id = Guid.NewGuid();
        var state = MovieEvaluationState.From(id.ToString(), variants);
        state.Init(new EvaluateMovies(id, SearchSource.Radarr, []), _noSender);
        return state;
    }

    private static MediathekItem Item(string title, string topic = "Tatort") =>
        new("ARD", topic, title, null, 1719244800, 5400, 0, null, "https://v.mp4", null, null, null);
}
