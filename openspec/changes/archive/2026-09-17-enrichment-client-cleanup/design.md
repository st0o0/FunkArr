## Context

TmdbClient and TvdbClient build query-parameter URLs via string interpolation. This works but is fragile — no URL encoding, easy to mis-concatenate `?` vs `&`. The fix is straightforward: use `QueryHelpers.AddQueryString` from `Microsoft.AspNetCore.WebUtilities`.

Current patterns:
- `TmdbClient`: `$"movie/{tmdbId}?api_key={ApiKey()}"`, `$"find/{imdbId}?api_key={ApiKey()}&external_source=imdb_id"`, `$"movie/{tmdbId}/alternative_titles?api_key={ApiKey()}"`
- `TvdbClient`: `$"series/{seriesId}/episodes/default?page={page}"`

## Goals / Non-Goals

**Goals:**
- Replace string-interpolated query parameters with `QueryHelpers.AddQueryString`
- Proper URL encoding for all parameter values
- Consistent URL construction pattern across both clients

**Non-Goals:**
- Changing auth flows (api_key stays as query param for TMDB, login+bearer stays for TVDB)
- DelegatingHandlers or architectural changes
- Extracting auth logic from TvdbClient
- Adopting third-party client libraries (TMDbLib, Tvdb.Sdk)
- Skyhook integration

## Decisions

### Use QueryHelpers.AddQueryString

Replace manual `?key=value&key2=value2` concatenation with `QueryHelpers.AddQueryString(path, name, value)`.

**TmdbClient** — 4 call sites:
- `FetchMovieAsync`: `QueryHelpers.AddQueryString($"movie/{tmdbId}", "api_key", ApiKey())`
- `FindByImdbIdAsync`: `QueryHelpers.AddQueryString($"find/{imdbId}", dict)` with `api_key` + `external_source`
- `FetchAlternativeTitlesAsync`: `QueryHelpers.AddQueryString($"movie/{tmdbId}/alternative_titles", "api_key", ApiKey())`

**TvdbClient** — 1 call site:
- `FetchEpisodesAsync`: `QueryHelpers.AddQueryString($"series/{seriesId}/episodes/default", "page", page.ToString(CultureInfo.InvariantCulture))`

### Add Microsoft.AspNetCore.WebUtilities via CPM

Add to `Directory.Packages.props` and reference in `FunkArr.Enrichment.csproj`. This is a lightweight library (no ASP.NET Core runtime dependency).

## Risks / Trade-offs

- **[Low] New package dependency** → `Microsoft.AspNetCore.WebUtilities` is a small, stable Microsoft package with no transitive bloat. Already used transitively by ASP.NET Core projects in the solution.
- **[None] Test impact** → Tests only verify `IsConfigured` guards via `new HttpClient()`. URL construction is not tested and the changes don't affect the test path.
