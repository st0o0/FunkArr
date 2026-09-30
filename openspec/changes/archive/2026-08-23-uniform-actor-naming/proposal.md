# Proposal: Uniform Actor Naming

## Problem

The current actor naming uses multiple suffixes (`*Coordinator`, `*Worker`, `*Tracker`, `*Pipeline`) to encode lifecycle role. This creates:

- **Ambiguity**: `DownloadCoordinator` is both a coordinator (spawns children) and a sharded entity — neither suffix fully describes it.
- **Gaps**: `*Pipeline` actors don't fit the three-tier scheme at all.
- **Growing complexity**: Every new actor with a mixed role requires inventing or stretching a tier.

The reference project (njord) uses a uniform `*Actor` suffix with folder structure carrying the semantic grouping — simple, consistent, and no edge cases.

## Solution

Rename all 20 actors to use a uniform `*Actor` suffix. Dissolve the `Search/Pipelines/` folder (move files to `Search/`). Keep all other folder structure intact.

## Scope

- Pure mechanical rename: class name, file name, all references, namespace adjustment where folder changes.
- **No behavioral change.** No architecture change.
- **Persistence IDs remain unchanged** — only the class/file names change, the `PersistenceId` strings in event-sourced actors stay as-is to preserve journals.

## Actors NOT renamed

- `SeriesResolver`, `MovieResolver` — domain-meaningful names that aren't part of the old convention.
- `MatchingPipeline` — static utility class, not an actor.

## Rename Map

### Search/

| Current | New |
|---------|-----|
| `BrowseCoordinator` | `BrowseActor` |
| `MediathekGatewayWorker` | `MediathekGatewayActor` |
| `SearchPipelineBase` | `SearchActorBase` |
| `TextSearchPipeline` | `TextSearchActor` |
| `TvSearchPipeline` | `TvSearchActor` |
| `MovieSearchPipeline` | `MovieSearchActor` |

### RuleSet/

| Current | New |
|---------|-----|
| `RuleSetCoordinator` | `RuleSetActor` |
| `RuleSetGeneratorWorker` | `RuleSetGeneratorActor` |
| `RefreshWorker` | `RefreshActor` |
| `MatchQualityWorker` | `MatchQualityActor` |

### DownloadClient/Queue/

| Current | New |
|---------|-----|
| `QueueCoordinator` | `QueueActor` |

### DownloadClient/Pipeline/

| Current | New |
|---------|-----|
| `DownloadCoordinator` | `DownloadActor` |
| `HlsDownloadWorker` | `HlsDownloadActor` |
| `Mp4DownloadWorker` | `Mp4DownloadActor` |
| `RemuxWorker` | `RemuxActor` |
| `SubtitleDownloadWorker` | `SubtitleDownloadActor` |
| `SubtitleConvertWorker` | `SubtitleConvertActor` |
| `SubtitleExtractWorker` | `SubtitleExtractActor` |

### DownloadClient/Tracker/

| Current | New |
|---------|-----|
| `DownloadRequestTracker` | `DownloadRequestActor` |

## Folder Changes

- `Search/Pipelines/` dissolved — files move to `Search/`
- All other folders stay as-is

## Convention Update

CLAUDE.md naming convention changes from:

> `*Coordinator` (singletons/parents), `*Worker` (children), `*Tracker` (shard entities)

To:

> All actors use `*Actor` suffix. Folder structure carries semantic grouping.
