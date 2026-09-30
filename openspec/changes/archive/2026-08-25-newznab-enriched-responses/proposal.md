## Why

Newznab search results produce indistinguishable filenames when Sonarr/Prowlarr doesn't pass season/episode parameters (RSS sync, browse, text search). Multiple results for the same show appear as "Tatort.GERMAN.720p.WEB.h264-FA" with no episode differentiation. The RuleSet matching engine already resolves episode metadata (season, episode number, episode name, air date) during TV search but this data is discarded before reaching the Newznab response. Additionally, the imperative XmlWriter-based XML generation is verbose and hard to extend — MediathekArr demonstrates a cleaner declarative model-based approach.

## What Changes

- Replace imperative `NewznabXmlBuilder` (XmlWriter) with declarative XML serialization models and a single `XmlSerializer.Serialize` call
- Thread `MatchedEpisodeInfo` from RuleSet matching through the search pipeline into Newznab responses
- Enrich release titles with episode names and air dates (standard: `S01E03.EpisodeName`, daily: `YYYY.MM.DD.EpisodeName`, fallback: `EpisodeName.YYYY.MM.DD`)
- Eliminate `NewznabResult` intermediate DTO — controller maps `SearchResult` directly to XML models
- Include resolved season/episode/tvdbid as Newznab attributes from match data (not just HTTP params)

## Capabilities

### New Capabilities
- `newznab-xml-models`: Declarative XML serialization models for Newznab RSS responses (RssFeed, RssChannel, RssItem, etc.) replacing imperative XmlWriter
- `release-title-builder`: Release title generation with standard/daily/movie/fallback variants including episode names and air dates

### Modified Capabilities
- `newznab-indexer`: Response now includes episode name in release title, resolved S/E from match data, air-date-based titles for daily shows
- `ruleset-matching-engine`: MatchedEpisodeInfo survives through the full pipeline (no longer discarded at SearchRequestActor)
- `tv-search-pipeline`: SearchResult enriched with optional Season, Episode, EpisodeName, AirDate, ShowName from RuleSet matching
- `text-search-pipeline`: Fallback release titles use raw Title + Timestamp from MediathekViewWeb for differentiation
- `movie-search-pipeline`: Movie results use year from match data when available

## Impact

- **API**: Newznab XML output changes cosmetically (same structure, richer content). Release titles become more descriptive. No breaking changes to API consumers.
- **Code**: Replaces `Indexer/NewznabXmlBuilder.cs` and `Shared/Models/NewznabResult.cs`. Touches `SearchResult`, `SearchMessages`, `ShowActor`, `SearchRequestActor`, `QualityExpander`, `NewznabController`.
- **Tests**: Contract `.verified.txt` files need regeneration. New unit tests for `ReleaseTitleBuilder` variants.
- **Dependencies**: None new. Uses built-in `System.Xml.Serialization`.
