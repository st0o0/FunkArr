## Context

The matching engine (`RuleSetMatchingEngine`) evaluates Mediathek search results against RuleSet rules and emits traces. Today the trace model uses three disjoint C# records (`MatchedTrace`, `FilteredTrace`, `UnmatchedTrace`) inheriting from `MatchTrace`. The API contract mirrors this with three separate lists on `MatchRecord`. The frontend TypeScript types partially mirror the contracts but omit fields already serialized by the API (`itemTopic`, `itemDuration`, `itemChannel`). The Match Detail View renders a flat list per category with minimal information per item.

The `RecentMatchActor` holds the last N `MatchRecord`s in an in-memory ring buffer (not persisted). `ShowActor`/`MovieActor` aggregate stats from traces into `TopicStats` (event-sourced). Neither tier stores the detailed per-rule evaluation path.

## Goals / Non-Goals

**Goals:**
- Unified `ItemEvaluation` model with int-backed enum discrimination replacing three trace types
- Full rule evaluation pipeline per item: every rule tested, filter check results, regex capture details
- Match Detail View shows raw Mediathek item data and expandable evaluation pipeline
- All enums int-backed on wire (C# default + TypeScript numeric enums)

**Non-Goals:**
- Persisting SearchEvaluation to disk (remains in-memory ring buffer)
- Changing the TopicStats aggregation model (it only needs counters, not the full pipeline)
- Adding "fix suggestion" intelligence for unmatched items (future work)
- Changing the MatchTestPanel or RulesetEditor (they use a separate code path)

## Decisions

### Decision 1: Unified ItemEvaluation with flat list on SearchEvaluation

Replace `MatchRecord { Matched[], Filtered[], Unmatched[] }` with `SearchEvaluation { Items: ItemEvaluation[] }`.

Each `ItemEvaluation` carries:
- Raw item data: `ItemTitle`, `ItemTopic`, `ItemChannel`, `ItemDuration` (always present)
- `Outcome: EvaluationOutcome` enum (Matched=0, Filtered=1, Unmatched=2)
- Match result fields (nullable, present when Outcome=Matched): `Season`, `Episode`, `EpisodeName`, `Confidence`, `WinnerRuleIndex`
- Filter reason (nullable, present when Outcome=Filtered): `FilterReason`
- `RuleEvaluations: RuleEvaluation[]` (present when Outcome=Matched or Unmatched; empty when Outcome=Filtered since ContentFilter skips before rule evaluation)

**Why not keep three types with a shared base?** A unified type means the API contract is one array, the TypeScript type is one interface, and the UI can sort/filter/group items by any dimension without maintaining three parallel code paths. The nullable fields cost negligible space vs. the simplification gain.

### Decision 2: RuleEvaluation captures the full decision at each rule

```csharp
public sealed record RuleEvaluation
{
    public required int RuleIndex { get; init; }
    public required int Priority { get; init; }
    public required MatchingStrategy Strategy { get; init; }
    public required RuleOutcome Outcome { get; init; }  // Matched=0, FilterFailed=1, NoMatch=2
    public required IReadOnlyList<FilterCheck> FilterChecks { get; init; }
    public StrategyDetail? StrategyDetail { get; init; }  // null when Outcome=FilterFailed
}
```

`FilterCheck` records each individual filter evaluation:
```csharp
public sealed record FilterCheck
{
    public required string Field { get; init; }
    public required FilterOp Op { get; init; }
    public required string Value { get; init; }
    public required string Actual { get; init; }
    public required bool Passed { get; init; }
}
```

`StrategyDetail` records what the strategy attempted:
```csharp
public sealed record StrategyDetail
{
    public string? RegexPattern { get; init; }
    public string? RegexInput { get; init; }
    public bool? RegexMatched { get; init; }
    public string? CapturedValue { get; init; }
    public string? ConstructedTitle { get; init; }
    public string? TvdbMatch { get; init; }
}
```

**Why not a strategy-specific discriminated union?** The seven strategies produce overlapping subsets of these fields. A flat record with nullable fields is simpler than a sealed hierarchy with seven subtypes, and the UI needs the same expandable rendering for all of them. The field names are self-describing: `RegexPattern`+`RegexInput`+`CapturedValue` for regex-based strategies, `ConstructedTitle` for title-rule strategies, `TvdbMatch` for the TVDB lookup result.

### Decision 3: Collect evaluations in the engine's hot path

The `EvaluateRulesWithTraces` method currently breaks on first match. The change adds a `List<RuleEvaluation>` per item, populated for every rule (whether it matches or not). On match, the loop still breaks — but now the evaluations up to that point are attached to the `ItemEvaluation`.

Performance: for a typical search (200 items × 2-5 rules), this adds ~400-1000 small record allocations. These are short-lived (collected after the RecentMatchActor stores the SearchEvaluation). The MatchingEngine is not in a tight loop — it runs once per search request — so this is negligible.

For `EvaluateFilterGroup`, the current `FindFailingFilter` returns a single string. Replace with a method that returns `List<FilterCheck>` for all filters in the group (not just the first failure). This enables the UI to show green/red per filter.

For strategy methods (`MatchSeasonAndEpisode`, `MatchAbsoluteEpisode`, etc.), wrap the return in a tuple `(MatchedEpisodeInfo?, StrategyDetail)` so the detail is always captured regardless of match/no-match.

### Decision 4: Int-backed enums end-to-end

New enums:
```csharp
public enum EvaluationOutcome { Matched = 0, Filtered = 1, Unmatched = 2 }
public enum RuleOutcome { Matched = 0, FilterFailed = 1, NoMatch = 2 }
```

Existing `MatchingStrategy` and `FilterOp` are already int-backed by C# default. The API serializes them as integers. TypeScript mirrors with numeric enums:
```typescript
export enum EvaluationOutcome { Matched = 0, Filtered = 1, Unmatched = 2 }
export enum RuleOutcome { Matched = 0, FilterFailed = 1, NoMatch = 2 }
```

**Note:** The existing API contract uses NSwag-generated `MatchedTraceContractStrategy` as a string enum. The new contracts must be configured to serialize enums as integers. This is a breaking change in the API surface.

### Decision 5: SearchEvaluation replaces MatchRecord everywhere

- `RecentMatchActor`: stores `SearchEvaluation` in ring buffer, `GetRecent` returns them
- `ShowActor`/`MovieActor`: the `HandleMatch` method receives `SearchEvaluation`, derives stats from `items.Count(i => i.Outcome == ...)` instead of `matched.Count`/`filtered.Count`/`unmatched.Count`
- `MatchIntelligenceController`: maps `SearchEvaluation` to API contracts
- `ContractMappingExtensions`: new `ToContract()` for all new types

### Decision 6: UI — expandable rows with pipeline visualization

Default view: each item shows a compact row with title + raw data (channel, duration) + outcome badge. Clicking expands to show the full `RuleEvaluation[]` pipeline as a vertical stepper.

The summary bar still shows Matched/Filtered/Unmatched counts, computed from `items.filter(i => i.outcome === EvaluationOutcome.Matched).length` etc.

Items are grouped by outcome (Matched, Filtered, Unmatched) as before, but each group's items are expandable.

## Risks / Trade-offs

**[Larger payloads] → Accept:** SearchEvaluation records are 3-5x larger than MatchRecord. For a 200-item search with 5 rules, that's ~1000 RuleEvaluation objects. At ~200 bytes each, that's ~200KB per search evaluation. The RecentMatchActor holds 100 records, so worst case ~20MB in memory. Acceptable for a single-user Docker service.

**[Breaking API change] → Accept:** Version 0.x, no external consumers. The frontend is the only client. Clean break is fine.

**[NSwag contract generation] → Regenerate:** The OpenAPI spec must be updated with the new types, then NSwag regenerates `Contracts.g.cs`. The generated contracts must serialize enums as int, which may need an NSwag config change.

**[MatchTestPanel uses separate code path] → Skip for now:** The `MatchTestPanel` (used in RulesetEditor) calls `POST /api/v1/rulesets/{id}/test` which returns `TestResult { matched, filtered, unmatched }`. Migrating it to the new model is desirable but not required for this change — it can be done as follow-up.

## Open Questions

- Should the `RecentMatchActor` buffer size be increased from 100 to account for larger records, or decreased to keep memory stable? Probably keep at 100 and monitor.
- Should `TopicStats` also include per-rule strategy breakdown (how often each strategy type won)? Useful for RuleSet optimization but adds complexity. Defer to follow-up.
