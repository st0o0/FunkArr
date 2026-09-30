## Context

FunkArr makes external HTTP calls to 5 services: MediathekViewWeb (search), GitHub (ruleset updates), TVDB (episode metadata), TMDB (movie metadata), and subtitle providers. All 5 HttpClients are registered via `IHttpClientFactory` but have zero resilience configuration. Transient network failures, rate limiting (GitHub), or service outages cause immediate failure with no retry.

## Goals / Non-Goals

**Goals:**
- Every HttpClient has a standard resilience pipeline (retry + circuit breaker + timeout)
- Per-client overrides where behavior differs (e.g., SubtitlePreparer can fail fast, GitHub needs rate-limit awareness)
- Resilience is configured at registration time in setup containers — no changes needed in call sites
- TvdbClient's existing 401 reauth logic coexists with the resilience pipeline

**Non-Goals:**
- Changing existing HTTP call patterns or response handling
- Adding health checks or monitoring dashboards
- Implementing custom Polly strategies beyond the standard pipeline
- Caching HTTP responses (separate concern)

## Decisions

### Decision 1: Use `AddStandardResilienceHandler()` as the baseline

`Microsoft.Extensions.Http.Resilience` provides `AddStandardResilienceHandler()` which configures a layered pipeline: total request timeout → retry → circuit breaker → attempt timeout. This is the recommended .NET approach and covers the common case with sensible defaults (3 retries with exponential backoff, 10s attempt timeout, 30s total timeout).

**Why not custom Polly pipelines:** The standard handler covers 90% of our needs. Custom pipelines add maintenance burden and diverge from the ecosystem pattern.

### Decision 2: Per-client timeout overrides via `Configure<HttpStandardResilienceOptions>`

Each client can override defaults through the fluent API:

```csharp
services.AddHttpClient("MediathekViewWeb", ...)
    .AddStandardResilienceHandler(options =>
    {
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
    });
```

Client-specific tuning:
- **MediathekViewWeb**: Longer timeout (search can be slow), standard retry
- **GitHub**: Standard timeout, standard retry (handles rate limiting via 429 status)
- **TvdbClient/TmdbClient**: Standard defaults work well for metadata lookups
- **SubtitlePreparer**: Shorter timeout (subtitles are optional, fail fast)

### Decision 3: TvdbClient 401 reauth coexists with resilience

The TvdbClient already retries once on 401 by re-authenticating. The resilience pipeline retries on transient HTTP errors (5xx, 408, 429). These don't conflict:
- 401 is not a transient error — the resilience pipeline won't retry it
- The TvdbClient's manual reauth handles the auth-specific case
- If the reauth itself fails with a transient error, the resilience pipeline covers that

No changes needed to the TvdbClient reauth logic.

### Decision 4: Package added to Directory.Packages.props only

`Microsoft.Extensions.Http.Resilience` is added to central package management. Only the host project (`FunkArr`) and `FunkArr.Download` need the package reference since they're where HttpClient registrations live.

## Risks / Trade-offs

- **[Retry amplification]** Retries on POST requests (MediathekViewWeb search) are safe because the API is idempotent (same query = same results). GitHub API GETs are naturally idempotent.
- **[Circuit breaker state]** The circuit breaker is per-client, not per-endpoint. If MediathekViewWeb has one bad endpoint, the circuit opens for all queries. Acceptable since each named client targets a single service.
- **[SubtitlePreparer in FunkArr.Download]** This is the only client registered outside the host project. It needs its own package reference to `Microsoft.Extensions.Http.Resilience`, or the resilience handler must be added in the host's DI setup. Since `DownloadServiceExtensions` already registers the client, adding the package to `FunkArr.Download` is cleaner.
