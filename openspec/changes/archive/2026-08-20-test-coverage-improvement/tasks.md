## 1. Remove Integration Tests

- [x] 1.1 Delete `src/FunkArr.Tests/Integration/EndpointTests.cs`
- [x] 1.2 Delete `src/FunkArr.Tests/Integration/WebUiEndpointTests.cs`
- [x] 1.3 Delete `src/FunkArr.Tests/Integration/SetupValidationEndpointTests.cs`
- [x] 1.4 Remove the `Integration` directory if empty

## 2. Rename Misnamed Test File

- [x] 2.1 Rename `src/FunkArr.Tests/DownloadClient/DownloadQueueActorTests.cs` to `DownloadEventsTests.cs`

## 3. Add PathMappingHelper Tests

- [x] 3.1 Create `src/FunkArr.Tests/Api/PathMappingHelperTests.cs` with tests for `ParsePathMapping` (valid mapping, null/empty, invalid format) and `MapPath` (matching prefix, non-matching prefix, null mapping, empty path)

## 4. Add MediathekClient Tests

- [x] 4.1 Create `src/FunkArr.Tests/Search/MediathekClientTests.cs` using FakeHttpMessageHandler pattern — test `QueryAsync` success with deserialized response, HTTP error returning null, malformed JSON returning null

## 5. Add TvdbClient Tests

- [x] 5.1 Create `src/FunkArr.Tests/Search/TvdbClientTests.cs` using FakeHttpMessageHandler pattern — test `GetShowAsync` success/error/null and `GetEpisodesAsync` success/error/null

## 6. Add SearchChildHelpers Tests

- [x] 6.1 Create `src/FunkArr.Tests/Search/SearchChildHelpersTests.cs` — test `BuildGenericPipelineRecord` output fields and `SearchMediathekAsync` success/exception/blank-query paths

## 7. Add RuleSetFileWriter Tests

- [x] 7.1 Create `src/FunkArr.Tests/RuleSet/RuleSetFileWriterTests.cs` with temp directory — test that `Write` creates a correctly-named JSON file that round-trips back to the original RuleSetFile

## 8. Verify

- [x] 8.1 Run full test suite and confirm all tests pass
- [x] 8.2 Run `dotnet format` on test project
