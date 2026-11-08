## Context

`NewznabSearchService` (328 lines) mixes four concerns: search orchestration (cache + actor Ask), command building (IndexerRequest → SearchCommand), RSS construction (SearchResultItem → Rss/Item POCOs), and Newznab attribute assembly. Adding new attributes (language, subs, usenetdate from the backlog) will make this worse.

Security: API key appears in HTTP logs via `[HttpLogging(HttpLoggingFields.All)]` on both controllers and `RequestQuery` in the global `AddHttpLogging` config. `BaseUrl` is `Request.Scheme://Request.Host` — wrong behind TLS-terminating proxies. Key comparison is `string.Equals(..., StringComparison.Ordinal)`.

## Goals / Non-Goals

**Goals:**
- Split `NewznabSearchService` so each class has one job
- Redact API key from all HTTP log paths
- Correct scheme/host behind reverse proxy via ForwardedHeaders
- Timing-safe API key comparison
- Caps: declare `imdbid,tmdbid` for tv-search

**Non-Goals:**
- SABnzbd service refactoring (boilerplate is acceptable)
- POST form-field API key or `ma_username`/`ma_password` fallback (no Arr client sends these)
- `UsePathBase` / configurable URL base path (separate concern, deferred)
- New Newznab attributes (language, subs, usenetdate) — this change makes them easy to add later

## Decisions

### 1. NewznabSearchService decomposition

Extract two classes, keep the service as coordinator:

| Class | Responsibility |
|-------|---------------|
| `NewznabSearchService` (slimmed) | Cache lookup, actor Ask, filtering, pagination, delegates to builders |
| `RssItemBuilder` | `ToItem(SearchResultItem, baseUrl, apiKey)` → `Item`, `CreateRss(items, offset, total)` → `Rss` |
| `NewznabAttributeBuilder` | `BuildAttributes(SearchResultItem, category)` → `List<NewznabAttribute>` |

**Why not more classes?** Command building (`BuildCommand`/`BuildTvSearch`/etc.) is tightly coupled to the service's search dispatch — separating it adds a class for 40 lines of pure mapping with no reuse. Category helpers (`ParseInt`, `CapLimit`, `BuildKey`) are small private utilities that stay on the service.

**Why not a single `RssBuilder`?** Attribute assembly is the part that grows (backlog: language, subs, usenetdate). Keeping it separate means new attributes don't touch RSS structure code.

Both new classes are `sealed` with no interface — they're internal implementation details of the ArrApi adapter, injected via DI as Scoped (same lifetime as the service). `RssItemBuilder` takes `NewznabAttributeBuilder` as a dependency.

### 2. API key log redaction

Remove `[HttpLogging(HttpLoggingFields.All)]` from both controllers. In the global `AddHttpLogging` config in `CoreSetupContainer`, keep `RequestQuery` but add a request header/query filter that redacts the `apikey` parameter. ASP.NET's `HttpLoggingOptions` supports `RequestHeaders`/`ResponseHeaders` filtering but not query parameter filtering directly.

**Approach:** Switch from `RequestQuery` to a custom `IHttpLoggingInterceptor` that rewrites the query string, replacing the `apikey` value with `[REDACTED]`. This is the official .NET 8+ extensibility point.

**Alternative considered:** Remove query string logging entirely — rejected because the other query params (`t`, `q`, `tvdbid`, `mode`) are valuable for debugging.

### 3. ForwardedHeaders

Register `ForwardedHeadersMiddleware` early in the pipeline (before routing/auth). Use `ForwardedHeadersOptions` with `ForwardedHeaders.XForwardedFor | XForwardedProto`. Default to allowing all proxied headers without restricting `KnownProxies`/`KnownNetworks` — FunkArr runs in Docker behind user-controlled reverse proxies, and the typical setup is a single hop (Traefik/nginx → FunkArr).

Lives in host `Program.cs` / `CoreSetupContainer`, not in ArrApi — it's infrastructure.

**Why not restrict KnownProxies?** Docker network addresses vary per deployment. The restrictive default (`127.0.0.1` only) breaks every Docker setup. Clearing `KnownProxies` and `KnownNetworks` makes it work out of the box. This is standard for Docker-deployed .NET apps.

### 4. Timing-safe API key comparison

Replace `string.Equals(apiKey, expectedApiKey, StringComparison.Ordinal)` with `CryptographicOperations.FixedTimeEquals` on the UTF-8 bytes. This prevents timing side-channels without changing the filter's structure.

**Length check:** `FixedTimeEquals` already handles different-length inputs in constant time (returns false), so no pre-check needed.

### 5. Caps tv-search params

Change the `SupportedParams` default from `"q,season,ep,tvdbid"` to `"q,season,ep,tvdbid,imdbid,tmdbid"` in `Caps.cs`. One-line change.

## Risks / Trade-offs

- **[ForwardedHeaders trust-all]** → Allows header spoofing if FunkArr is exposed directly to the internet without a proxy. Mitigation: FunkArr is designed for LAN/Docker deployment behind a reverse proxy; document in README.
- **[IHttpLoggingInterceptor availability]** → Requires .NET 8+. Mitigation: project is already .NET 10.
- **[Service decomposition breaks tests]** → Existing `NewznabSearchService` tests will need updating. Mitigation: the tests verify search behavior, not internal class structure — most assertions stay valid, just the setup changes.
