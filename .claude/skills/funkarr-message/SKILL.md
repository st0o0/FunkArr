---
description: "Create commands, queries, and response types following FunkArr message conventions"
---

# FunkArr Message Conventions

All messages live in `FunkArr.Messages/<Domain>/`. Each command or query owns its full response
hierarchy in a single file. No domain-wide response interfaces.

## Command — VerbNoun

A command triggers an action. Name: `VerbNoun` (imperative). File: `VerbNoun.cs`.

```csharp
namespace FunkArr.Messages.<Domain>;

public sealed record <VerbNoun>(
    <fields>);

public abstract record <VerbNoun>Response;

public sealed record <VerbNoun>Completed(
    <result fields>) : <VerbNoun>Response;

public sealed record <VerbNoun>Failed(
    Exception Cause) : <VerbNoun>Response;
```

Real example — `SearchSeries.cs`:

```csharp
public sealed record SearchSeries(
    Guid SearchId,
    SearchSource Source,
    string? Query,
    int? Season,
    int? Episode,
    int? TvdbId,
    string? ImdbId,
    int? Limit,
    int? Offset) : SearchRequest(SearchId, Source, Query, Limit, Offset);

public abstract record SearchSeriesResponse;

public sealed record SearchSeriesCompleted(
    Guid SearchId,
    SearchResultItem[] Items,
    int Total) : SearchSeriesResponse;

public sealed record SearchSeriesFailed(
    Guid SearchId,
    Exception Cause) : SearchSeriesResponse;
```

**Notes:**
- `Failed` always has `Exception Cause` (may include additional context like `Guid RequestId`)
- `Completed` carries the result payload — name matches the command, not a generic "Result"
- Commands may extend a shared base (e.g. `SearchRequest`) but responses never do across domains
- Commands may implement marker interfaces like `IWithDownloadId`, `IWithRuleSetId`

## Query — QueryNoun

A query retrieves data. Name: `QueryNoun` (prefix `Query`). File: `QueryNoun.cs`.
Response drops the `Query` prefix: `NounResponse` / `NounResult` / `NounFailed`.

```csharp
namespace FunkArr.Messages.<Domain>;

public sealed record Query<Noun>(<fields>);

public abstract record <Noun>Response;

public sealed record <Noun>Result(
    <result fields>) : <Noun>Response;

public sealed record <Noun>Failed(
    Exception Cause) : <Noun>Response;
```

Real example — `QueryRuleSetDetail.cs`:

```csharp
public sealed record QueryRuleSetDetail(string RuleSetId);

public abstract record RuleSetDetailResponse;

public sealed record RuleSetDetailResult(
    string RuleSetId,
    RuleSetDetailResult.RuleSetIdentity Identity,
    RuleSetDetailResult.RuleSetSource Source,
    float DefaultConfidence,
    RuleSetDetailRule[] Rules,
    EnrichmentConfig Enrichment) : RuleSetDetailResponse
{
    public sealed record RuleSetIdentity(...);
    public sealed record RuleSetSource(...);
}

public sealed record RuleSetDetailFailed(Exception Cause) : RuleSetDetailResponse;
```

**Notes:**
- Complex results may nest helper records inside the `Result` record
- Supporting records (like `RuleSetDetailRule`) can live in the same file outside the hierarchy
- Parameterless queries are valid: `public sealed record QueryRuleSetSummaries;`

### Simple query variant

Some queries return a plain result record without an abstract base (no failure path needed):

```csharp
public sealed record QueryQueue(int Start = 0, int Limit = 0, MediaType? Category = null);
// Response is QueueResult in a separate file
```

Use this only when the query cannot fail (direct state reads). Prefer the full
`abstract record` + `Result` / `Failed` pattern for anything involving actor Ask.

## Config — fire-and-forget

Config messages push configuration into an actor. Name: `Noun` (no verb prefix). No response.

```csharp
public sealed record MatchingConfig(
    string RuleSetId,
    float DefaultConfidence,
    MatchingRule[] Rules);
```

Also used for simple commands that need no acknowledgement:
- `RemoveMatchingConfig(string RuleSetId)`
- `DeregisterRuleSet(string RuleSetId)`
- `CancelDownload(Guid DownloadId) : IWithDownloadId`

## Events — past tense

Persistence events live in `FunkArr.Persistence/Events/<Domain>/`. Name: past-tense verb.

```csharp
namespace FunkArr.Persistence.Events.<Domain>;

public sealed record DownloadEnqueued(Guid DownloadId);

public sealed record ScoringRecorded(
    Guid RequestId,
    SearchSource Source,
    string Query,
    DateTimeOffset Timestamp,
    int CandidateCount,
    int MatchedCount,
    ItemTrace[] ItemTraces);
```

**Rules:**
- No `Event` or `Dto` suffix
- Events are extend-only (never remove fields, only add)
- Events may reference types from `FunkArr.Messages` but never from domain projects

## Checklist

- [ ] File in `FunkArr.Messages/<Domain>/`, one file per command/query
- [ ] All records `sealed` (enforced by architecture tests)
- [ ] `Failed` variant includes `Exception Cause`
- [ ] No cross-domain response base types
- [ ] Events in `FunkArr.Persistence/Events/<Domain>/`, past-tense name
