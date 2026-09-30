## Context

FunkArr uses Serilog for structured logging (configured in `LoggingSetupContainer`) and Akka.Logger.Serilog for actor logging. The infrastructure is solid, but the actual usage is sparse: actors average ~3 log lines, API endpoint code has no `ILogger` injected, and ~18 `catch (Exception)` blocks silently swallow errors returning generic 504s. Two PipeTo handlers in Search workers discard exceptions entirely.

## Goals / Non-Goals

**Goals:**
- Every caught exception is logged with structured context before returning an error response
- API endpoint methods receive `ILogger` via DI for consistent logging
- PipeTo failure handlers always forward the exception into typed failure messages
- Actors log key state transitions (message received at Debug, failures at Warning/Error)
- Fix MatchMagic bugs surfaced during audit (dead field, object return type)

**Non-Goals:**
- Changing the Serilog infrastructure/sinks/template (already well-specified)
- Adding distributed tracing or correlation IDs (separate concern)
- Comprehensive happy-path logging for every message (incremental, not this change)
- Performance logging or metrics

## Decisions

### Decision 1: ILogger injection via endpoint method parameters

ASP.NET Minimal API endpoints can receive `ILogger<T>` as a parameter in the lambda or static method. We'll use this pattern rather than wrapping endpoints in classes.

**Pattern:**
```csharp
static async Task<IResult> HandleSomething(
    [FromServices] IActorRef actor,
    [FromServices] ILogger<SomeEndpoints> logger)
{
    try { ... }
    catch (Exception ex)
    {
        logger.LogError(ex, "Operation failed for {Context}", context);
        return ApiResults.GatewayTimeout();
    }
}
```

**Why not endpoint filter:** An exception-logging filter would centralize the catch, but we'd lose the structured context (which search query, which download ID, which ruleset). Per-catch logging with context is more valuable for debugging.

### Decision 2: Structured log templates with domain context

Each log statement SHALL include the relevant domain identifier as a structured property:
- Search: `{SearchId}`
- Download: `{DownloadId}`
- RuleSet: `{RuleSetId}`
- Scoring: `{RequestId}`, `{RuleSetId}`

This enables filtering in log aggregation without changing the Serilog sink configuration.

### Decision 3: Fix SearchFailed to carry Exception? Cause

All other `*Failed` messages (`MediathekQueryFailed`, `ScoringFailed`, `MovieResolutionFailed`, `EpisodeResolutionFailed`) carry both `Reason` (string) and `Cause` (Exception?). `SearchFailed` is the outlier — it only has `Reason`. Adding `Cause` aligns the pattern.

### Decision 4: Fix MatchMagic bugs inline

- **Dead `field` in PersistenceId**: The `MatchHistoryWorker` has a `PersistenceId` using the C# 14 `field` keyword but it gets overwritten — fix to use proper property initialization.
- **`object` return in QueryDetail**: Replace with a discriminated type or use the existing typed message pattern.

### Decision 5: Actor logging level guidelines

- `Debug`: Message received, state transition completed, cache hit/miss
- `Information`: Actor started, recovery completed, significant lifecycle events
- `Warning`: Transient failures (timeout, HTTP error), degraded operation
- `Error`: Unrecoverable failures, exception in message handling

Actors already use Akka's `ILoggingAdapter` (`_log`) — we keep that, no need to switch to `ILogger` in actors.

## Risks / Trade-offs

- **[Verbosity]** More logging means more output. Mitigated by using Debug level for high-frequency events and keeping Warning/Error for real problems.
- **[SearchFailed breaking change]** Adding `Cause` parameter changes the record constructor. Since we're 0.x, breaking changes are acceptable and all callers are internal.
- **[Scope creep]** We could add logging everywhere, but this change focuses on the blind spots (catch blocks, PipeTo handlers, key transitions). Comprehensive logging is incremental.
