# Unified HttpClient Handling

## Problem

HttpClient configuration is split between two places: `ServiceSetupContainer` (registration) and the consuming classes (constructor). `TvdbClient` and `TmdbClient` set `BaseAddress` in their constructors and hold a single `HttpClient` instance forever, defeating `IHttpClientFactory`'s handler rotation (DNS changes, socket exhaustion prevention). `MediathekViewWebManager` and `RuleSetUpdater` do the same - call `CreateClient()` once and store the result.

## Approach

- Convert all 4 HTTP consumers to typed clients via `AddHttpClient<T>()`
- Move all BaseAddress and default header configuration into the registration
- The typed client pattern gives each class a fresh `HttpClient` per scope automatically - no manual `CreateClient()` calls, no long-lived instances
- `MediathekViewWebManager` is an actor (singleton lifetime in Akka) - it gets `IHttpClientFactory` injected and calls `CreateClient()` per request instead of holding one forever

## Scope

- `TvdbClient` - typed client
- `TmdbClient` - typed client
- `RuleSetUpdater` (actor) - `IHttpClientFactory`, create per-request
- `MediathekViewWebManager` (actor) - `IHttpClientFactory`, create per-request
- `ServiceSetupContainer` - consolidate all registration with config
- Test updates for `IHttpClientFactory` mocks

## Out of scope

- HTTP logging (separate change: logging-coverage)
- Download pipeline (uses FFmpeg directly, not HttpClient)
