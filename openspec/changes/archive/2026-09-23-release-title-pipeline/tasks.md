## 1. ReleaseDisplay Record and EnrichedItem

- [x] 1.1 Create `ReleaseDisplay` record (MediaName, EpisodeTitle) in `FunkArr.Search/ReleaseDisplay.cs`
- [x] 1.2 Add nullable `ReleaseDisplay? Display` field to `EnrichedItem` record. Update all construction sites to pass `Display: null` for backward compatibility.

## 2. Worker State Display Population

- [x] 2.1 Update `TvSearchWorkerState.Apply(ScoreCompleted)` — build `ReleaseDisplay` for each scored item using `_mediaName ?? Source.Topic` as MediaName and `constructedTitle ?? ""` as EpisodeTitle. Remove `_constructedTitles` dictionary (no longer needed as transit).
- [x] 2.2 Update `TvSearchWorkerState.Apply(EnrichEpisodesCompleted)` — when enrichment confidence ≥ 0.9 and EpisodeName is not null, override `Display.EpisodeTitle` with the TVDB episode name.
- [x] 2.3 Update `TvSearchWorkerState.ToSearchCompleted()` — for items with null Display (unscored), build fallback Display using `CleanTitle(Source.Title, Source.Topic)` as EpisodeTitle and `Source.Topic` as MediaName.
- [x] 2.4 Apply the same pattern to `MovieSearchWorkerState` — Display populated in Apply(ScoreCompleted), overridden in Apply(EnrichMoviesCompleted), fallback in ToSearchCompleted().

## 3. ReleaseVariant and Identifier Building

- [x] 3.1 Move identifier formatting into `ReleaseVariant.Expand` — build the S##E## / date / year string from `Identity` and `Source.AiredAt` before calling the builder. Use `ReleaseTitleBuilder.FormatSeasonEpisode` for S##E## formatting.
- [x] 3.2 Update `ReleaseVariant.Expand` to pass `Display.MediaName`, identifier, `Display.EpisodeTitle`, and quality to the builder. Remove the `mediaName` parameter from `Expand` (it comes from Display now). Remove `ResolveMediaName` helper.

## 4. ReleaseTitleBuilder Rewrite

- [x] 4.1 Replace `Build` method with `Format(string mediaName, string? identifier, string episodeTitle, int quality)`. Remove `StripTopicFromTitle`, `StripSeasonEpisodePattern`, `AppendTvIdentifier`, `AppendMovieIdentifier`. Keep `Sanitize`, `CollapseDots`, `MapQuality`, `FormatSeasonEpisode`, `PadNumber` as public utilities.
- [x] 4.2 Add `CleanTitle` static method (extracted from old `StripTopicFromTitle` logic) for the fallback path. Move to `ReleaseVariant` or keep on builder as a utility.
- [x] 4.3 Remove `MetadataSpec.ConstructedTitle` — kept on MetadataSpec as transit field from scoring engine, but no longer used by ReleaseTitleBuilder field from `FunkArr.Messages/Scoring/MetadataSpec.cs`. Update all callers in ScoringEngine that set ConstructedTitle — the constructed title is now set directly on Display in the worker state.

## 5. Tests

- [x] 5.1 Rewrite `ReleaseTitleBuilderTests` for the new `Format` API — all existing scenario expectations preserved with new method signature. Remove StripTopicFromTitle tests (logic moved).
- [x] 5.2 Update `TvSearchWorkerStateTests` — existing tests pass with Display default param — verify Display is populated after each Apply phase, verify EpisodeTitle override at high/low enrichment confidence, verify fallback for unscored items.
- [x] 5.3 Update `MovieSearchWorkerStateTests` — existing tests pass with Display default param — same display population verification.
- [x] 5.4 Run all test projects — 713 tests, 0 failed to verify no regressions.
