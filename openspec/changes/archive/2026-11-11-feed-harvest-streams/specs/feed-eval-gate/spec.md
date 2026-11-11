# Feed Eval Gate (delta)

Gate logic moves from private static methods on FeedCrawlPageActor to
instance methods on pipeline records.

## Evaluation

`ItemGroup.EvaluateAsync(IActorRef eval, TimeSpan timeout)`:
- Routes to TvEval or MovieEval based on MediaType
- Builds EvaluateSeries or EvaluateMovies message from group data
- Returns `Task<ScoredGroup>` (the Ask result mapped)

Evaluation parallelism via `SelectAsyncUnordered(4)` in the stream.
Groups are independent — order does not matter.

## Gating

`ScoredGroup.Gate(double minConfidence)`:
- Matches scored results back to source MediathekItems via URL
- Rejects: LowConfidence, MissingEpisode, MissingId, UnknownSource,
  MissingTimestamp
- Builds FeedEntry records for accepted items (StableId via SHA256)
- Returns `GatedResult(accepted[], seenCount, unmatchedCount, rejectedByReason)`

## RejectionReason enum — unchanged

## Types

```
ItemGroup(string RuleSetId, MediaType MediaType, ExternalIds Ids,
          MediathekItem[] Items)
  method: EvaluateAsync(eval, timeout) → Task<ScoredGroup>

ScoredGroup(ItemGroup Source, SearchResultItem[] Results)
  method: Gate(minConfidence) → GatedResult

GatedResult(FeedEntry[] Accepted, int SeenCount, int UnmatchedCount,
            IReadOnlyDictionary<RejectionReason, int> RejectedByReason)
```
