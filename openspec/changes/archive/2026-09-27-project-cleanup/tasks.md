## 1. Remuxer Redesign

- [x] 1.1 Add `RemuxOptions` internal sealed record to Remuxer.cs with `VideoUrl`, `SubtitlePath`, `OutputPath`, `ProxyUrl`, `SubtitleLanguage`, computed `IsHls`
- [x] 1.2 Move SubtitlePreparer logic into Remuxer (subtitle download, format detection, SRT conversion), take IHttpClientFactory via constructor
- [x] 1.3 Move FfmpegRunner logic into Remuxer (RunAsync process execution, telemetry, error handling)
- [x] 1.4 Rewrite BuildArguments as single flow: SelectStream for mapping, CopyChannel for codecs, conditional BSF for HLS, conditional subtitle args
- [x] 1.5 Move ParseProgressLine, ClassifyFailure, ExtractError, ParseSpeed as internal static methods
- [x] 1.6 Delete FfmpegRunner.cs, IFfmpegRunner.cs, SubtitlePreparer.cs, ISubtitlePreparer.cs
- [x] 1.7 Update DownloadServiceExtensions.cs to register only IRemuxer (remove IFfmpegRunner, ISubtitlePreparer registrations)

## 2. Remuxer Tests

- [x] 2.1 Add BuildArgumentsTests: direct MP4 without subtitle (verify -map, -c:v copy, -c:a copy, no -bsf:a)
- [x] 2.2 Add BuildArgumentsTests: HLS without subtitle (verify -map, -c:v copy, -c:a copy, -bsf:a aac_adtstoasc=no_validation=1)
- [x] 2.3 Add BuildArgumentsTests: HLS with subtitle (verify subtitle args + BSF)
- [x] 2.4 Add BuildArgumentsTests: proxy option (verify -http_proxy before -i)
- [x] 2.5 Add RemuxOptions tests: IsHls derivation from .m3u8 vs .mp4
- [x] 2.6 Update existing Download.Tests that mock IFfmpegRunner or ISubtitlePreparer

## 3. SetupContainer Redesign

- [x] 3.1 Create CoreSetupContainer with FunkArrOptions, PostgresOptions, DataPaths, IDataFiles, IFileSystem, RoutingOptions, IRouteResolver, JSON config, directory health checks
- [x] 3.2 Rename MediathekSetupContainer to SearchSetupContainer, add Search actors (SearchManager, MediathekViewWebManager, TvSearchWorker, MovieSearchWorker) and MediathekViewWeb health check
- [x] 3.3 Update DownloadSetupContainer: keep DownloadOptions, Remuxer, /api/downloads endpoints. Add Download actors (DownloadManager, DownloadScheduler, DownloadHistoryManager, DownloadWorker) and FFmpeg health check
- [x] 3.4 Create ScoringSetupContainer with ScoringOptions, ScoringHistoryOptions, Scoring actors (ScoringManager), History actors (StatsCollector, HistoryWorker)
- [x] 3.5 Update RuleSetSetupContainer: keep RuleSetUpdaterOptions, IRuleSetValidator, RuleSetStore, GitHub HttpClient, /api/rulesets endpoints. Add RuleSet actors (RuleSetResolver, RuleSetManager, RuleSetUpdater, RuleSetWorker). Remove ScoringOptions/ScoringHistoryOptions
- [x] 3.6 Rename MetadataSetupContainer to EnrichmentSetupContainer, add Enrichment actors (EnrichmentManager, TvdbEnrichmentActor, TmdbEnrichmentActor)
- [x] 3.7 Create ArrApiSetupContainer with Newznab/SABnzbd services, ApiKeyFilter, ArrApiClient, controller mapping
- [x] 3.8 Slim AkkaSetupContainer to actor system, persistence, remoting, clustering only (remove all actor registrations)
- [x] 3.9 Delete ServiceSetupContainer (verify all registrations moved)
- [x] 3.10 Update Program.cs AppBuilder chain with new container order

## 4. Verify

- [x] 4.1 Run `dotnet build src/FunkArr.slnx` and fix compilation errors
- [x] 4.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes` and fix formatting
- [x] 4.3 Run all test projects and verify green
