## Context

FunkArr uses Akka.NET Cluster Sharding with 5 shard regions. Each region has its own `HashCodeMessageExtractor` subclass that extracts an entity key from incoming messages. The logic is trivial (read one string property) but duplicated across 5 classes. The two largest namespace folders (`Search/` 23 files, `DownloadClient/` 22 files) have become flat dumps mixing actors, workers, services, clients, and models.

Current extractors:
- `DownloadCoordinatorMessageExtractor` → `IWithNzoId.NzoId`
- `DownloadRequestTrackerMessageExtractor` (inline) → `IWithNzoId.NzoId`
- `TvSearchPipelineMessageExtractor` → `TvdbId.ToString()`
- `MovieSearchPipelineMessageExtractor` → `EntityKey` (raw string param)
- `TextSearchPipelineMessageExtractor` → `Query`

## Goals / Non-Goals

**Goals:**
- One interface (`IShardedMessage`) and one extractor (`ShardedMessageExtractor`) for all shard regions
- Entity key derivation lives on the message type itself, not in external extractors
- `MovieSearchPipeline.Search` uses typed fields (`ImdbId`, `Query`) with a computed `EntityKey`
- `Search/` and `DownloadClient/` folders restructured into logical subfolders with matching namespaces
- Clean, navigable codebase with ~5-8 files per subfolder

**Non-Goals:**
- Changing shard region count or sharding strategy
- Modifying persistence (events, snapshots, DTOs)
- Restructuring `Configuration/`, `RuleSet/`, `Api/`, or other folders
- Introducing a base class for messages (interface-only approach)

## Decisions

### 1. Interface with computed property over marker + external extraction

```csharp
public interface IShardedMessage
{
    string EntityKey { get; }
}
```

Each message computes its own key. The extractor just casts and reads.

**Why not Func-based extractor?** Keeps key logic co-located with the message definition. Discoverable via IDE "go to implementation". Compile-time enforcement that every sharded message has a key.

### 2. IWithNzoId extends IShardedMessage via default interface method

```csharp
public interface IWithNzoId : IShardedMessage
{
    string NzoId { get; }
    string IShardedMessage.EntityKey => NzoId;
}
```

**Why:** Zero changes needed on the ~10 existing `IWithNzoId` implementors. They already have `NzoId` and automatically satisfy `IShardedMessage`.

### 3. MovieSearchPipeline.Search drops EntityKey parameter

```csharp
// Before: Search(string EntityKey, string? ImdbId, string? Query)
// After:
public sealed record Search(string? ImdbId, string? Query) : IShardedMessage
{
    public string EntityKey => ImdbId ?? $"q:{Query}";
}
```

**Why:** The controller currently builds `entityKey = imdbId ?? $"q:{q}"` then passes it as a constructor arg. This is logic leaking into the controller. The message should own its identity derivation.

### 4. ShardedMessageExtractor takes maxShards as constructor arg

```csharp
public sealed class ShardedMessageExtractor(int maxNumberOfShards)
    : HashCodeMessageExtractor(maxNumberOfShards)
{
    public override string? EntityId(object message) => message switch
    {
        IShardedMessage m => m.EntityKey,
        _ => null,
    };
}
```

Different shard regions can still use different shard counts (Download: 10, Search: 20) by passing the value at registration.

### 5. Subfolder structure with namespace alignment

Subfolders get their own namespace (`FunkArr.Search.Pipelines`, `FunkArr.DownloadClient.Pipeline`, etc.). This matches .NET convention where folder = namespace segment.

**Search/ restructuring:**
| Subfolder | Namespace | Contents |
|-----------|-----------|----------|
| `Pipelines/` | `FunkArr.Search.Pipelines` | TvSearchPipeline, MovieSearchPipeline, TextSearchPipeline, SearchPipelineBase |
| `Resolvers/` | `FunkArr.Search.Resolvers` | SeriesResolver, MovieResolver, TvdbClient, TmdbClient |
| `Matching/` | `FunkArr.Search.Matching` | MatchingPipeline, MatchContext, DateMatcher |
| `Quality/` | `FunkArr.Search.Quality` | QualityProbeService, QualityExpander, HlsManifestParser, Mp4AtomParser, UrlPatternAnalyzer |
| root | `FunkArr.Search` | BrowseCoordinator, SearchCoordinatorMessages, MediathekClient, MediathekGatewayWorker |

**DownloadClient/ restructuring:**
| Subfolder | Namespace | Contents |
|-----------|-----------|----------|
| `Pipeline/` | `FunkArr.DownloadClient.Pipeline` | DownloadCoordinator, Events, Messages, Job, Outcome, Progress, all workers (Hls, Mp4, Remux, Subtitle*) |
| `Queue/` | `FunkArr.DownloadClient.Queue` | QueueCoordinator, QueueCoordinatorEvents |
| `Tracker/` | `FunkArr.DownloadClient.Tracker` | DownloadRequestTracker, Events, SourceType, FailureKind |
| `Ffmpeg/` | `FunkArr.DownloadClient.Ffmpeg` | FfmpegService, IFfmpegService, FfmpegProgressParser |
| root | `FunkArr.DownloadClient` | IWithNzoId |

### 6. IShardedMessage lives in Shared/

`FunkArr.Shared` is the natural home — it's the cross-cutting namespace already used for shared interfaces and models.

## Risks / Trade-offs

- **Large diff from namespace moves** → Mitigate by doing restructuring in a separate commit from interface changes. Easier to review.
- **Broken using statements across tests and API layer** → IDE refactoring handles this; verify with `dotnet build` after each step.
- **MovieSearchPipeline.Search breaking change** → Only one caller (NewznabController). Version is 0.x so breaking changes are acceptable.
- **Default interface method requires C# 8+** → Already on .NET 10, no concern.
