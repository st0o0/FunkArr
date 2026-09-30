## 1. MatchMethod enum and IEnrichmentResult interface

- [x] 1.1 Create `MatchMethod` enum in `FunkArr.Messages/MetadataResolver/MatchMethod.cs` with values: RegexExtracted, TitleMatch, AirdateMatch, YearMatch
- [x] 1.2 Create `IEnrichmentResult` interface in `FunkArr.Messages/MetadataResolver/IEnrichmentResult.cs` with Index, Confidence, Method properties

## 2. Rename episode enrichment messages

- [x] 2.1 Rename `ResolveEpisodes` to `EnrichEpisodes`, remove `ResolutionConfig` parameter — file: `FunkArr.Messages/MetadataResolver/ResolveEpisodes.cs`
- [x] 2.2 Rename `ResolvedEpisode` to `EnrichedEpisode`, replace `Strategy: string` with `Method: MatchMethod`, implement `IEnrichmentResult` — file: `FunkArr.Messages/MetadataResolver/ResolvedEpisode.cs`
- [x] 2.3 Rename `IEpisodeResolutionResponse` to `IEpisodeEnrichmentResponse`, `EpisodesResolved` to `EpisodesEnriched`, `EpisodeResolutionFailed` to `EpisodeEnrichmentFailed` — file: `FunkArr.Messages/MetadataResolver/IEpisodeResolutionResponse.cs`
- [x] 2.4 Delete `ResolutionConfig.cs` from `FunkArr.Messages/MetadataResolver/`

## 3. Rename movie enrichment messages

- [x] 3.1 Rename `ResolveMovie` to `EnrichMovies` (plural), file: `FunkArr.Messages/MetadataResolver/ResolveMovie.cs`
- [x] 3.2 Rename `MovieResolved` to `EnrichedMovie`, replace `Strategy: string` with `Method: MatchMethod`, implement `IEnrichmentResult` — file: `FunkArr.Messages/MetadataResolver/MovieResolved.cs`
- [x] 3.3 Rename `IMovieResolutionResponse` to `IMovieEnrichmentResponse`, `MoviesResolved` to `MoviesEnriched`, `MovieResolutionFailed` to `MovieEnrichmentFailed` — file: `FunkArr.Messages/MetadataResolver/IMovieResolutionResponse.cs`

## 4. Rename SearchResultItem fields

- [x] 4.1 Rename `ResolutionConfidence` to `MatchConfidence` and `ResolutionStrategy` (string?) to `MatchMethod` (MatchMethod?) on `SearchResultItem` — file: `FunkArr.Messages/Search/SearchResultItem.cs`

## 5. Update MetadataResolver domain

- [x] 5.1 Delete `ResolutionStrategy.cs` from `FunkArr.MetadataResolver/`
- [x] 5.2 Update `EpisodeResolver` — remove `ResolutionConfig` parameter, use threshold=0.7f and airdateTolerance=7 as internal constants, return `MatchMethod` enum values instead of strings
- [x] 5.3 Update `MovieResolver` — return `MatchMethod` enum values instead of strings, rename output type to `EnrichedMovie`
- [x] 5.4 Update `TvdbResolverActor` — adapt to `EnrichEpisodes`/`EpisodesEnriched`/`EpisodeEnrichmentFailed` message names, remove `ResolutionConfig`/strategy-none check

## 6. Update SearchWorkers

- [x] 6.1 Update `TvSearchWorker` — use `EnrichEpisodes` (no config), handle `EpisodesEnriched`/`EpisodeEnrichmentFailed`, use `EnrichedEpisode` in `BuildScoredResult`, map `MatchConfidence`/`MatchMethod` on `SearchResultItem`
- [x] 6.2 Update `MovieSearchWorker` — use `EnrichMovies`, handle `MoviesEnriched`/`MovieEnrichmentFailed`, use `EnrichedMovie` in `BuildScoredResult`, map `MatchConfidence`/`MatchMethod` on `SearchResultItem`

## 7. Update tests

- [x] 7.1 Update `TvSearchWorkerTests` — adapt assertions to new type names (`EnrichedEpisode`, `EpisodesEnriched`, `MatchMethod` enum values, `MatchConfidence`/`MatchMethod` fields)
- [x] 7.2 Update `MovieSearchWorkerTests` — adapt assertions to new type names (`EnrichedMovie`, `MoviesEnriched`, `MatchMethod` enum values, `MatchConfidence`/`MatchMethod` fields)
- [x] 7.3 Update `MetadataResolver.Tests` if resolution tests exist — adapt to new type names and enum values

## 8. Rename files to match new type names

- [x] 8.1 Rename message files: `ResolveEpisodes.cs` -> `EnrichEpisodes.cs`, `ResolvedEpisode.cs` -> `EnrichedEpisode.cs`, `IEpisodeResolutionResponse.cs` -> `IEpisodeEnrichmentResponse.cs`, `ResolveMovie.cs` -> `EnrichMovies.cs`, `MovieResolved.cs` -> `EnrichedMovie.cs`, `IMovieResolutionResponse.cs` -> `IMovieEnrichmentResponse.cs`

## 9. Build and format

- [x] 9.1 Run `dotnet build src/FunkArr.slnx` — verify clean compile
- [x] 9.2 Run `dotnet format src/FunkArr.slnx` — fix formatting
- [x] 9.3 Run all affected test projects — verify green
