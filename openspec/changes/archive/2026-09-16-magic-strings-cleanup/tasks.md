## 1. Shared constants in FunkArr.Core

- [x] 1.1 Create `FunkArrHeaders.cs` in FunkArr.Core with X-FunkArr-* header name constants
- [x] 1.2 Create `HttpClientNames.cs` in FunkArr.Core with named HttpClient key constants ("MediathekViewWeb", "GitHub")
- [x] 1.3 Create `MediaTypes.cs` in FunkArr.Core with content type constants (application/xml, application/x-nzb, text/plain, text/event-stream, application/json)

## 2. Replace germanMonths with CultureInfo

- [x] 2.1 Replace `germanMonths` array and manual month lookup in `MatchMagicActor.ExtractGermanDate` with `CultureInfo("de-DE")` + `DateTime.TryParseExact` using format `"d. MMMM yyyy"`
- [x] 2.2 Verify existing MatchMagic tests still pass after germanMonths replacement

## 3. Domain-specific constants

- [x] 3.1 Create `ResolutionStrategy.cs` in FunkArr.MetadataResolver with strategy name constants (TitleMatch, YearMatch, FuzzyTitleMatch, AirdateMatch, RegexExtracted, None)
- [x] 3.2 Create `SabnzbdConstants.cs` in FunkArr.ArrApi with version string constant ("4.3.3")

## 4. Replace magic strings with constants — Host and setup

- [x] 4.1 Replace HttpClient name literals in `MediathekSetupContainer.cs` and `RuleSetSetupContainer.cs` with `HttpClientNames` constants
- [x] 4.2 Replace hardcoded "Accept" and "User-Agent" header names in setup containers (use `System.Net.Http.Headers.HeaderNames` or constants)

## 5. Replace magic strings with constants — ArrApi

- [x] 5.1 Replace X-FunkArr-* header literals in `NewznabApiEndpoints.cs` (NzbGetResult) with `FunkArrHeaders` constants
- [x] 5.2 Replace X-FunkArr-* header literals in `SabnzbdApiEndpoints.cs` with `FunkArrHeaders` constants
- [x] 5.3 Replace SABnzbd version literal in `SabnzbdApiEndpoints.cs` with `SabnzbdConstants.Version`
- [x] 5.4 Replace content type literals in ArrApi endpoint files with `MediaTypes` constants

## 6. Replace magic strings with constants — Api and domains

- [x] 6.1 Replace content type literals in `RuleSetApiEndpoints.cs`, `DownloadsApiEndpoints.cs`, `MediathekApiEndpoints.cs` with `MediaTypes` constants
- [x] 6.2 Replace content type literal in `MediathekViewWebManager.cs` with `MediaTypes` constant
- [x] 6.3 Replace strategy string literals in `MovieResolver.cs`, `EpisodeResolver.cs`, `TvdbResolverActor.cs` with `ResolutionStrategy` constants
- [x] 6.4 Replace `"none"` strategy check in `TvdbResolverActor` and `EpisodeResolver` with `ResolutionStrategy.None`

## 7. Verify and format

- [x] 7.1 Run `dotnet build src/FunkArr.slnx` and fix any compilation errors
- [x] 7.2 Run `dotnet format src/FunkArr.slnx` to apply code style
- [x] 7.3 Run test projects to verify no regressions
