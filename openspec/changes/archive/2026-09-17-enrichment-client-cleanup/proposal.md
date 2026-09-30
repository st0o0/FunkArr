## Why

TmdbClient and TvdbClient build URLs via string interpolation (`$"movie/{id}?api_key={key}"`), which is fragile and skips URL encoding. This is a code quality cleanup to use structured URL building with `QueryHelpers.AddQueryString`.

## What Changes

- Replace all manual query-parameter string interpolation in `TmdbClient` with `QueryHelpers.AddQueryString`
- Replace page-parameter string interpolation in `TvdbClient` with `QueryHelpers.AddQueryString`
- Add `Microsoft.AspNetCore.WebUtilities` package to `FunkArr.Enrichment`
- Auth flows remain unchanged (TMDB api_key as query param, TVDB login + bearer token)

## Capabilities

### New Capabilities

None — this is a pure refactor with no behavioral changes.

### Modified Capabilities

None — the metadata-cache spec describes caching behavior, not HTTP client internals. URL building is an implementation detail that does not affect spec-level requirements.

## Impact

- **Code**: `TmdbClient.cs` (4 URL constructions), `TvdbClient.cs` (1 URL construction)
- **Dependencies**: New package `Microsoft.AspNetCore.WebUtilities` in `FunkArr.Enrichment.csproj` + `Directory.Packages.props`
- **Tests**: No changes — existing tests only verify `IsConfigured` guards, not URL construction
- **APIs**: No external API changes
