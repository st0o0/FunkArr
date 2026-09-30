## Why

All 5 registered HttpClients (MediathekViewWeb, GitHub, TvdbClient, TmdbClient, SubtitlePreparer) have zero resilience — no retry, no circuit breaker, no timeout policies. Every external HTTP call can fail on transient network issues with no recovery, causing cascading failures through the actor system. The MediathekViewWeb API and GitHub API are particularly prone to transient errors and rate limiting.

## What Changes

- Add `Microsoft.Extensions.Http.Resilience` NuGet package (built on Polly 8) to the host project
- Configure a standard resilience pipeline on all 5 HttpClient registrations with retry, circuit breaker, and total request timeout
- Allow per-client configuration overrides for timeout and retry behavior where appropriate
- Keep TvdbClient's existing 401-reauth logic alongside the resilience pipeline (resilience handles transient failures; reauth handles auth expiry)

## Capabilities

### New Capabilities
- `httpclient-resilience`: Standard resilience pipeline configuration for all HttpClients — retry policy, circuit breaker, and timeout handling via Microsoft.Extensions.Http.Resilience

### Modified Capabilities
_None — this change adds resilience as infrastructure without changing the behavioral contracts of existing clients. The existing specs for mediathek-gateway, tvdb-client, tmdb-client, subtitle-preparer, and ruleset-updater remain unchanged._

## Impact

- **FunkArr (host)**: New package reference to `Microsoft.Extensions.Http.Resilience` in Directory.Packages.props
- **FunkArr/Configuration/MediathekSetupContainer.cs**: Add resilience handler to "MediathekViewWeb" client
- **FunkArr/Configuration/RuleSetSetupContainer.cs**: Add resilience handler to "GitHub" client
- **FunkArr/Configuration/MetadataSetupContainer.cs**: Add resilience handler to TvdbClient and TmdbClient
- **FunkArr.Download/DownloadServiceExtensions.cs**: Add resilience handler to SubtitlePreparer client
- **No API changes, no breaking changes**
