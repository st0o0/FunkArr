## 1. Scoring.Tests (heaviest - ~81 `[0]` + ~6 `!.`)

- [x] 1.1 Clean up ScoringActorTests.cs: replace `result.Results[0]` with `Assert.Single`, replace `!.` with `Assert.NotNull`, add count guards for multi-element access
- [x] 1.2 Clean up ScoringEngineTests.cs: replace `scored[0]`/`traces[0]` with `Assert.Single`, replace `FilterTrace!.`/`Identification!.` with `Assert.NotNull`
- [x] 1.3 Clean up ScoringManagerTests.cs: replace `[0]` indexing with `Assert.Single` or count guards
- [x] 1.4 Run Scoring.Tests, verify all pass

## 2. Search.Tests (~44 `[0]`)

- [x] 2.1 Clean up TvSearchWorkerStateTests.cs and TvSearchWorkerTests.cs: replace `[0]` with `Assert.Single`, replace `Match!.` with `Assert.NotNull`
- [x] 2.2 Clean up MovieSearchWorkerStateTests.cs and MovieSearchWorkerTests.cs: same patterns
- [x] 2.3 Clean up SearchManagerTests.cs, MediathekApiModelTests.cs, MediathekQueryBuilderTests.cs
- [x] 2.4 Run Search.Tests, verify all pass

## 3. Download.Tests (~48 `[0]` + ~2 `!.`)

- [x] 3.1 Clean up DownloadManagerStateTests.cs and DownloadManagerRetryStateTests.cs
- [x] 3.2 Clean up DownloadWorkerStateTests.cs and DownloadWorkerRetryStateTests.cs: replace `Media!.` with `Assert.NotNull`
- [x] 3.3 Clean up DownloadHistoryManagerStateTests.cs
- [x] 3.4 Clean up subtitle format tests (WebVttFormatTests, TtmlFormatTests, SrtFormatTests) and FfmpegRunnerTests
- [x] 3.5 Run Download.Tests, verify all pass

## 4. Enrichment.Tests (~36 `[0]`)

- [x] 4.1 Clean up EpisodeEnricherTests.cs: replace `[0]` with `Assert.Single` or count guards
- [x] 4.2 Clean up EnrichmentManagerTests.cs, MovieEnricherTests.cs, TmdbClientTests.cs, TvdbClientTests.cs
- [x] 4.3 Run Enrichment.Tests, verify all pass

## 5. RuleSet.Tests (~21 `[0]` + ~4 `!.`)

- [x] 5.1 Clean up RuleSetMergerTests.cs: replace `result!.` with `Assert.NotNull`, replace `Rules!.`/`Aliases!.` with `Assert.NotNull`
- [x] 5.2 Clean up MatchingToDiskExtensionsTests.cs and DiskToMatchingExtensionsTests.cs: replace `!.` with `Assert.NotNull`
- [x] 5.3 Clean up remaining test files (RuleSetStoreTests, RuleSetResolverTests, RuleSetWorkerTests, RuleSetManagerTests, RuleSetValidatorTests, RuleSetResolverStateSnapshotTests, RuleSetEnumMappingTests)
- [x] 5.4 Run RuleSet.Tests, verify all pass

## 6. History.Tests (~11 `[0]` + ~5 `!.`)

- [x] 6.1 Clean up HistoryStateTests.cs: replace `MatchRate!.Value`/`EnrichmentRate!.Value` with `Assert.NotNull`, replace `[0]` with `Assert.Single`
- [x] 6.2 Clean up StatsCollectorTests.cs and StatsCollectorStateTests.cs
- [x] 6.3 Run History.Tests, verify all pass

## 7. ArrApi.Tests (~19 `[0]`, skip XLinq `!.`)

- [x] 7.1 Clean up non-XML test files: NzbServiceTests.cs, SearchResultCacheTests.cs, SearchResultMappingTests.cs, ApiKeyActionFilterTests.cs, ApiKeyEndpointFilterTests.cs
- [x] 7.2 Clean up NzbGetTests.cs, NzbRoundTripTests.cs, SearchParameterTests.cs (skip XLinq `!.Value` in NewznabXmlTests/NzbGeneratorTests)
- [x] 7.3 Run ArrApi.Tests, verify all pass

## 8. Api.Tests + Persistence.Tests (light)

- [x] 8.1 Clean up Api.Tests: SafeUrlAttributeTests.cs (`result!.ErrorMessage!`), DownloadApiIntegrationTests.cs (`result!.Success`)
- [x] 8.2 Clean up Persistence.Tests: any `[0]` indexing in verify tests
- [x] 8.3 Run Api.Tests and Persistence.Tests, verify all pass

## 9. Final verification

- [x] 9.1 Run full solution build: `dotnet build src/FunkArr.slnx`
- [x] 9.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 9.3 Grep for remaining `!\.` in test files to confirm none were missed (excluding XLinq patterns)
