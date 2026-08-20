using Akka.Actor;
using FunkArr.RuleSet;
using FunkArr.Shared.Models;

namespace FunkArr.Search;

internal sealed record ExecuteTvSearch(
    string CacheKey,
    SearchActor.TvSearchRequest Request,
    string SearchTerm,
    string? ShowName,
    IReadOnlyList<Rule> Rules,
    IActorRef ReplyTo);

internal sealed record ExecuteMovieSearch(
    string CacheKey,
    SearchActor.MovieSearchRequest Request,
    string SearchTerm,
    IActorRef ReplyTo);

internal sealed record ExecuteTextSearch(
    string CacheKey,
    SearchActor.TextSearchRequest Request,
    IActorRef ReplyTo);

internal sealed record SearchCompleted(
    string CacheKey,
    IReadOnlyList<SearchResult> Results,
    MatchRecord? MatchRecord,
    IActorRef ReplyTo);
