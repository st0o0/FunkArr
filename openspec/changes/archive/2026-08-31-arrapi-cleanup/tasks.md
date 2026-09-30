## 1. Newznab cleanup — drop JSON, flatten, inline

- [x] 1.1 Delete `CapsJsonProjection.cs` and `RssJsonProjection.cs`
- [x] 1.2 Remove `O` parameter from `IndexerRequest.cs`
- [x] 1.3 Rewrite `IndexerApiEndpoints.cs`: lambda-style with switch expression, inline XML serialization (private static `Serialize<T>`), inline NZB generation and base64 decode, remove all `Handle*`/`*Result` methods
- [x] 1.4 Delete `XmlHelper.cs` and `NzbGenerator.cs`

## 2. SABnzbd cleanup — IFormFile binding, flatten, inline

- [x] 2.1 Rewrite `DownloadApiEndpoints.cs`: add `IFormFile? nzbfile` as direct parameter on POST, add `.DisableAntiforgery()`, inline NZB parsing (private static `Deserialize<T>` + meta extraction), flatten GET handler to switch expression in lambda
- [x] 2.2 Delete `NzbParser.cs`

## 3. Tests

- [x] 3.1 Delete `JsonOutputTests.cs`
- [x] 3.2 Update `NzbGeneratorTests.cs` — test NZB generation through indexer endpoint or inline helper
- [x] 3.3 Update `NzbParserTests.cs` — test NZB parsing through download endpoint or inline helper
- [x] 3.4 Update `NzbRoundTripTests.cs` if API surface changed
- [x] 3.5 Verify all remaining tests pass (`dotnet run --project FunkArr.ArrApi.Tests`)

## 4. Finalize

- [x] 4.1 Run `dotnet format` on changed files
- [x] 4.2 Run `dotnet build FunkArr.slnx` — verify clean build
