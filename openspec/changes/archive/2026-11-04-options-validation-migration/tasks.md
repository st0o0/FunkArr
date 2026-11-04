## 1. Terp bump and Feed gauge fix

- [x] 1.1 Bump Terp from 0.2.0 to 0.2.1 in `src/Directory.Packages.props` (Terp.Akka stays 0.2.0)
- [x] 1.2 Convert `Feed/Telemetry.cs` from `ObservableGauge` + backing field + `SetEntries()` to `Gauge<int> Entries` with `.Record()`, update all callers (`FeedStoreManager`)
- [x] 1.3 Build, run Feed tests, format check

## 2. Options registration migration

- [x] 2.1 `CoreSetupContainer`: convert `FunkArrOptions`, `PostgresOptions`, `RoutingOptions` to `AddOptionsWithValidateOnStart`, remove separate `AddSingleton<IValidateOptions<RoutingOptions>>`
- [x] 2.2 `DownloadSetupContainer`: convert `DownloadOptions` to `AddOptionsWithValidateOnStart<DownloadOptions, DownloadOptionsValidator>`, remove separate `AddSingleton<IValidateOptions<DownloadOptions>>`
- [x] 2.3 `ScoringSetupContainer`: convert `ScoringOptions`, `VariantOptions`, `ScoringHistoryOptions` to `AddOptionsWithValidateOnStart`
- [x] 2.4 `RuleSetSetupContainer`: convert `RuleSetUpdaterOptions` to `AddOptionsWithValidateOnStart`
- [x] 2.5 `EnrichmentSetupContainer`: convert `TvdbOptions`, `TmdbOptions` to `AddOptionsWithValidateOnStart`, change `BindConfiguration()` to `Bind(context.Configuration.GetSection(...))`, rename `SetupContext _` to `context`
- [x] 2.6 `ArrApi/ServiceCollectionExtensions.cs`: convert `ArrApiOptions` to `AddOptionsWithValidateOnStart`
- [x] 2.7 `FeedSetupContainer`: convert to `AddOptionsWithValidateOnStart<FeedOptions>` (without validator yet, remove inline `.Validate()` calls in next task group)
- [x] 2.8 Build, run all test projects, format check

## 3. FeedOptionsValidator

- [x] 3.1 Create `FunkArr.Core/FeedOptionsValidator.cs` using `ValidationFailures(FeedOptions.SectionName)` with all 8 rules from the inline lambdas
- [x] 3.2 Update `FeedSetupContainer` to `AddOptionsWithValidateOnStart<FeedOptions, FeedOptionsValidator>`
- [x] 3.3 Create `FunkArr.Feed.Tests/FeedOptionsValidatorTests.cs` covering: all-valid, each individual rule failure, multiple simultaneous failures
- [x] 3.4 Build, run Feed tests, format check

## 4. Existing validators to ValidationFailures

- [x] 4.1 Convert `DownloadOptionsValidator` from `List<string>` to `ValidationFailures()` (no section prefix), keep message text identical
- [x] 4.2 Convert `RoutingOptionsValidator` from `List<string>` to `ValidationFailures()` (no section prefix), keep message text identical
- [x] 4.3 Build, run Download tests (includes `DownloadOptionsValidatorTests` and `RoutingOptionsValidatorTests`), format check

## 5. Cleanup

- [x] 5.1 Delete `Migration.md` from repo root
- [x] 5.2 Final build, full test run, format check
