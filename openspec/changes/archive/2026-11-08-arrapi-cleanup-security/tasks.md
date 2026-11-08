## 1. NewznabSearchService decomposition

- [x] 1.1 Extract `NewznabAttributeBuilder` — move `BuildAttributes` logic into `Newznab/Services/NewznabAttributeBuilder.cs`, register as Scoped in `ServiceCollectionExtensions`
- [x] 1.2 Extract `RssItemBuilder` — move `ToItem`, `CreateRss`, `ToFeedRss` into `Newznab/Services/RssItemBuilder.cs` with `NewznabAttributeBuilder` dependency, register as Scoped
- [x] 1.3 Update `NewznabSearchService` to inject and delegate to `RssItemBuilder`, remove extracted methods
- [x] 1.4 Update existing `NewznabSearchService` tests — adjust DI setup for new classes, verify all assertions still pass

## 2. API key log redaction

- [x] 2.1 Implement `IHttpLoggingInterceptor` that redacts `apikey` query parameter value to `[REDACTED]`
- [x] 2.2 Register the interceptor in `CoreSetupContainer`, remove `[HttpLogging(HttpLoggingFields.All)]` from both controllers
- [x] 2.3 Add test verifying apikey is redacted in logged query string

## 3. ForwardedHeaders

- [x] 3.1 Register `ForwardedHeadersMiddleware` in host pipeline with `XForwardedFor | XForwardedProto`, clear `KnownProxies`/`KnownNetworks`

## 4. Timing-safe API key comparison

- [x] 4.1 Replace `string.Equals(..., StringComparison.Ordinal)` in `ApiKeyActionFilter` with `CryptographicOperations.FixedTimeEquals` on UTF-8 bytes
- [x] 4.2 Add test verifying valid/invalid/missing key behavior unchanged

## 5. Caps tv-search params

- [x] 5.1 Update `Caps.cs` tv-search `SupportedParams` from `"q,season,ep,tvdbid"` to `"q,season,ep,tvdbid,imdbid,tmdbid"`
- [x] 5.2 Update caps test assertion to expect the new params

## 6. Verify

- [x] 6.1 Build passes, format clean, all existing tests pass
