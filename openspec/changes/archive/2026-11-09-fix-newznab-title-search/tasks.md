# Tasks

## Part A: title→q mapping

- [x] In `NewznabSearchService.Search()`: map `req.Title` → `req.Q` when Q is empty, before `IsWithoutSearchCriteria`
- [x] Add test: tvsearch with `title` triggers MediathekViewWeb search (not feed fallback)
- [x] Add test: `q` takes precedence over `title` when both set
- [x] Add test: movie search with `title` triggers movie search path

## Part B: Feed title filter

- [x] In `NewznabSearchService.FeedOrValidation()` or surrounding code: filter feed results by `req.Q` (topic contains, case-insensitive) when Q is set
- [x] Add test: feed path with `q=Tatort` returns only Tatort-topic entries
- [x] Add test: feed path without `q` returns all entries (unchanged behavior)

## Part C: Season/episode airdate fallback

- [x] In `TvEvaluationState.Apply(EnrichEpisodesCompleted)`: when resolved season != requested season AND source AiredAt.Year == requested season, remap Identity.Season to AiredAt.Year
- [x] In `NewznabAttributeBuilder.BuildAttributes`: when Metadata.Season is null and item.AiredAt is available, emit season attr from AiredAt.Year
- [x] Add test: season remapping when TVDB season mismatches but AiredAt year matches
- [x] Add test: no remapping when TVDB season already matches
- [x] Add test: airdate fallback emits season attr when Metadata.Season is null
- [x] Add test: no fallback when both Metadata.Season and AiredAt are null

## Validation

- [x] Run `dotnet build src/FunkArr.slnx`
- [x] Run `dotnet run --project src/FunkArr.ArrApi.Tests`
- [x] Run `dotnet run --project src/FunkArr.Search.Tests`
