## Why

The ArrApi layer has a 328-line god service (`NewznabSearchService`) mixing search orchestration, RSS building, attribute assembly, and filtering in one class. Additionally, three security gaps (#17 API key in logs, #19 no ForwardedHeaders, #20 non-timing-safe key compare) make the adapter unsafe behind a reverse proxy—the standard deployment scenario.

## What Changes

- **Decompose `NewznabSearchService`** into focused classes: slim search orchestration, RSS item builder, Newznab attribute builder. The service keeps coordination; building logic moves out.
- **Redact API key from HTTP logs** — the `[HttpLogging]` attribute on both controllers logs query strings including `apikey`. Switch to field-level filtering that excludes the key.
- **Add ForwardedHeaders middleware** — so `Request.Scheme` and `Request.Host` are correct behind TLS-terminating reverse proxies. Support a configurable URL base path.
- **Timing-safe API key comparison** — replace `StringComparison.Ordinal` with `CryptographicOperations.FixedTimeEquals` to prevent timing side-channels.
- **Caps: advertise `imdbid`/`tmdbid` for tv-search** — already accepted at runtime but not declared in caps XML, so Prowlarr doesn't know it can send them.

## Capabilities

### New Capabilities
- `arrapi-security`: ForwardedHeaders middleware, timing-safe API key comparison, configurable URL base path
- `newznab-search-decomposition`: Extract RSS item building and Newznab attribute assembly from NewznabSearchService into dedicated classes

### Modified Capabilities
- `newznab-indexer-api`: Advertise `imdbid` and `tmdbid` in tv-search supportedParams in caps XML
- `arrapi-http-logging`: Redact `apikey` query parameter from HTTP log entries

## Impact

- **FunkArr.ArrApi**: NewznabSearchService split into 3 classes, ApiKeyActionFilter updated, new ForwardedHeaders config
- **FunkArr (Host)**: ForwardedHeaders middleware registration, URL base path configuration
- **Configuration**: New `FunkArr:ReverseProxy` (or similar) config section for known proxies/networks and base path
- **Existing tests**: NewznabSearchService tests need updating for the extracted classes
- **No wire-protocol changes**: RSS XML output, SABnzbd JSON, and all endpoint routes stay identical
