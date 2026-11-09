# Tasks

## movie-enrichment-prefix

- [x] Add `StripSuffix` method to `MovieEnricher` that removes ` - ...` suffix and trailing ` (...)` from candidate titles
- [x] In `ResolveCandidate`: after Levenshtein loop, add normalized prefix comparison using `StripSuffix` result against each TMDB title; assign confidence 0.95 if exact match after normalization
- [x] Pick max of Levenshtein score and prefix score as `bestSimilarity`
- [x] Add test: Mediathek title with ` - Spielfilm, Country Year` suffix matches TMDB title
- [x] Add test: Mediathek title with ` (Genre)` suffix matches TMDB title
- [x] Add test: title without suffix still works via Levenshtein (unchanged behavior)
- [x] Add test: suffix-only title (e.g. "Spielfilm") does NOT match unrelated TMDB title
- [x] Run `dotnet build src/FunkArr.slnx` and `dotnet run --project src/FunkArr.Enrichment.Tests`
