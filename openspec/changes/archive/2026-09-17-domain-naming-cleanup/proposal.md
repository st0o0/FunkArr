## Why

Project names, actor names, and namespaces are out of sync with the domain vocabulary the code actually uses. "MatchMagic" is a brand name — every message, event, and engine type says "Scoring". "MetadataResolver" sounds like match resolution (MatchMagic's job), when it actually enriches Mediathek items with TMDB/TVDB metadata. This caused real confusion during development and needs a clean alignment pass.

## What Changes

- **BREAKING** Rename `FunkArr.MatchMagic` project → `FunkArr.Scoring`, align all actor/state type names to Scoring vocabulary
- **BREAKING** Rename `FunkArr.MetadataResolver` project → `FunkArr.Enrichment`, align all actor/type names to Enrichment vocabulary
- **BREAKING** Rename `FunkArr.MatchMagic.Tests` → `FunkArr.Scoring.Tests`
- **BREAKING** Rename `FunkArr.MetadataResolver.Tests` → `FunkArr.Enrichment.Tests`
- Delete empty `FunkArr.MetadataMatching.Tests` project (leftover shell with assembly infos from three different names)
- Rename actor keys, Akka registration names, and config sections to match
- Move `Messages.MetadataResolver` namespace → `Messages.Enrichment`
- Rename persistence event namespace `Events.MatchHistory` → `Events.ScoringHistory`
- `Messages.Scoring` namespace stays unchanged — already correct

## Capabilities

### New Capabilities

_None — this is a pure rename/alignment, no new behavior._

### Modified Capabilities

- `match-scoring`: Namespace and type renames (MatchMagicManager → ScoringManager, etc.)
- `matchmagic-evaluation`: Namespace and type renames, project rename
- `matchmagic-data-model`: Namespace rename to FunkArr.Scoring
- `scoring-engine`: Namespace rename only (types already named correctly)
- `scoring-trace-model`: Namespace rename only
- `scoring-trace-persistence`: Namespace rename only
- `match-history-persistence`: Namespace and type renames (MatchHistory → ScoringHistory)
- `match-history-queries`: Namespace and type renames
- `matching-config`: Namespace rename to FunkArr.Scoring
- `parallel-scoring`: Namespace rename only
- `metadata-cache`: Namespace and type renames (MetadataResolver → Enrichment)
- `metadata-cache-status`: Namespace rename
- `episode-resolution`: Namespace and type renames (EpisodeResolver → EpisodeEnricher)
- `episode-resolution-messages`: Namespace move (Messages.MetadataResolver → Messages.Enrichment)
- `movie-resolution`: Namespace and type renames (MovieResolver → MovieEnricher)
- `movie-resolution-messages`: Namespace move (Messages.MetadataResolver → Messages.Enrichment)

## Impact

- All projects referencing `FunkArr.MatchMagic` or `FunkArr.MetadataResolver` need updated `<ProjectReference>` entries
- Solution file (`FunkArr.slnx`) needs updated project paths
- `Directory.Packages.props` unaffected (no package changes)
- Akka persistence journal: existing persisted data uses actor paths with old names — shard region names change from `match-history` to `scoring-history`
- Docker compose and config files referencing `FunkArr:MatchHistory` section need updating
- Architecture tests may assert on namespace/naming patterns
- OpenSpec specs referencing old names need delta updates
- CLAUDE.md solution structure section needs updating
