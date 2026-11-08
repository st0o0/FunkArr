## 1. Remove year validation from MovieEnricher

- [x] 1.1 Remove `ValidateYear` method and its call from `ResolveCandidate` in `src/FunkArr.Enrichment/Tmdb/MovieEnricher.cs`
- [x] 1.2 Remove the `YearMatch` fallback path (lines 74-85) from `ResolveCandidate` — only the title-similarity path remains
- [x] 1.3 Remove the `yearTolerance` parameter from `ResolveCandidate` since it's no longer used
- [x] 1.4 Update `MovieEnricher` tests: remove year-validation tests, add test for re-broadcast movie with large year gap succeeding on title match alone

## 2. Fix title stutter in SceneRelease.ForMovie

- [x] 2.1 In `SceneRelease.ForMovie`, after resolving display, set `EpisodeTitle` to empty string when it equals `MediaName` (ordinal, case-insensitive)
- [x] 2.2 Add test `FormatTitle_MovieTitleDeduplicatedWhenEpisodeTitleMatchesMediaName` verifying no stutter output
- [x] 2.3 Verify existing test `FormatTitle_MovieWithYear` still passes (title differs from media name)

## 3. Validate

- [x] 3.1 Run `dotnet build src/FunkArr.slnx` — zero warnings
- [x] 3.2 Run enrichment tests: `dotnet run --project src/FunkArr.Enrichment.Tests`
- [x] 3.3 Run search tests: `dotnet run --project src/FunkArr.Search.Tests`
- [x] 3.4 Run `dotnet format src/FunkArr.slnx --verify-no-changes` — pre-existing line-ending issue in NewznabSearchTests.cs (not from this change)
