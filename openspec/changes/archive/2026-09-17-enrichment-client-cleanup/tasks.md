## 1. Package Setup

- [x] 1.1 Add `Microsoft.AspNetCore.WebUtilities` to `Directory.Packages.props`
- [x] 1.2 Add `PackageReference` for `Microsoft.AspNetCore.WebUtilities` to `FunkArr.Enrichment.csproj`

## 2. TmdbClient URL Cleanup

- [x] 2.1 Replace string interpolation in `FetchMovieAsync` with `QueryHelpers.AddQueryString`
- [x] 2.2 Replace string interpolation in `FindByImdbIdAsync` with `QueryHelpers.AddQueryString` (multi-param dictionary overload)
- [x] 2.3 Replace string interpolation in `FetchAlternativeTitlesAsync` with `QueryHelpers.AddQueryString`

## 3. TvdbClient URL Cleanup

- [x] 3.1 Replace string interpolation in `FetchEpisodesAsync` page parameter with `QueryHelpers.AddQueryString`

## 4. Verify

- [x] 4.1 Run `dotnet build src/FunkArr.slnx`
- [x] 4.2 Run `dotnet format src/FunkArr.slnx`
- [x] 4.3 Run enrichment tests (`dotnet run --project src/FunkArr.Enrichment.Tests`)
