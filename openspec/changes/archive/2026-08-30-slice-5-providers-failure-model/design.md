## Context

After slices 0–4, FunkArr has clean Core/Messages/Actor layering, a shared MediaRuleSetActor base, and proper rule-set merge. However, the provider layer has no consistent shape: `TvdbV4Client`, `TmdbClient`, `MediathekClient`, `GitHubReleaseClient` are scattered across folders, three of them are injected by multiple consumers (bypassing rate limiting), all throw on failure (swallowed to null by gateway actors), and no HTTP resilience exists. Five `*Service` types blur the actor/provider/Core boundary.

## Goals / Non-Goals

**Goals:**
- One rule for "provider or actor?" — enforced by naming convention (`*Provider` suffix) and folder structure (`Providers/{name}/`)
- Providers never throw past their boundary; failures are typed values
- Gateway actors explicitly decide failure semantics
- Standard retry + circuit breaker on all HTTP clients
- `*Service` suffix eliminated

**Non-Goals:**
- Changing gateway actor persistence or caching strategies
- Adding new providers (Axis 2 seam prepared but not exercised)
- Modifying Newznab/SABnzbd adapter surfaces
- Adding IContentSource interface (future slice)

## Decisions

### D1: IQueryResult as open interface hierarchy, not closed union

**Choice:** `interface IQueryResult` with `QuerySuccess<T>` and `QueryFailure` as records. Open hierarchy.

**Why over DU/OneOf:** The hierarchy is extensible — a future provider could return a `QueryPartial<T>` without modifying the base. The cost is that every consumer `switch` needs a `default` arm and the compiler cannot prove exhaustiveness. Accepted deliberately per design doc §8.

**Location:** `FunkArr.Core/Results/` — no Akka dependency, usable from any layer.

### D2: FailureReason as enum, not type hierarchy

**Choice:** `enum FailureReason { NotFound, Unauthorized, RateLimited, Timeout, Cancelled, Transport, Malformed }`.

**Why:** The reason is metadata on a failure value, not a dispatch target. Pattern matching on an enum is clearer than `is RateLimitedFailure`. The `Detail` string carries provider-specific context.

### D3: Provider/Gateway co-location in Providers/ folders

**Choice:** Each provider and its owning gateway actor live in `Providers/{Name}/`. The folder makes the "exactly one actor owns this provider" rule visible in the file tree.

**Why over flat structure:** Flat `Search/Resolvers/` hides ownership. A folder-per-provider makes the pairing trivial to verify and matches the extension seam (new source = new folder).

### D4: Rename clients to *Provider suffix

**Choice:** `TvdbV4Client` → `TvdbProvider`, `TmdbClient` → `TmdbProvider`, etc.

**Why:** The `*Provider` suffix is the category marker from the design doc §3. The `*Client` suffix gives no architectural signal. The rename is mechanical but enforces the naming convention uniformly.

### D5: AddStandardResilienceHandler for HTTP resilience

**Choice:** `Microsoft.Extensions.Http.Resilience` with `AddStandardResilienceHandler()` on all typed HTTP clients.

**Why over Polly v8 direct:** `AddStandardResilienceHandler()` wraps Polly v8 with sensible defaults (retry with exponential backoff, circuit breaker, timeout) and is the Microsoft-recommended approach for .NET 8+. One line per client registration. Specific configuration overrides per provider where needed (e.g., longer timeout for FFmpeg/HLS).

### D6: Service → Provider/Core/Actor conversion

**Choice:**
- `FfmpegService`/`IFfmpegService` → `FfmpegProvider`/`IFfmpegProvider` (pure rename + move)
- `FileService`/`IFileService` → `FileSystemProvider`/`IFileSystemProvider` (pure rename + move)
- `RuleSetGenerationService` → disbanded: Core `RuleSetGenerator` already has the logic, generation preview becomes a direct call in the controller or a new message on MediaRuleSetActor
- `SetupValidationService` → `SetupValidator` (Core utility, no actor needed — it's pure validation)
- `ApiKeyValidationService` → `ApiKeyValidator` (Core utility)

**Why not actor for validation:** Validation is stateless and synchronous. Making it an actor adds mailbox overhead for no benefit. Core utility is the right category.

### D7: Gateway failure handling strategy

**Choice:** Each gateway actor's failure handling is explicit and documented per provider:
- **TvdbGateway:** On `QueryFailure` → serve stale cache if available, else propagate failure
- **TmdbGateway:** Same as Tvdb
- **MediathekGateway:** On `QueryFailure` → propagate (no cache), log with FailureReason
- **GitHubProvider:** On failure → retain existing community files, log warning

**Why not uniform:** Different providers have different cache/staleness semantics. A uniform policy would either be too aggressive (always fail) or too lax (always serve stale).

## Risks / Trade-offs

- **Rename churn** → Large diff with many file moves. Mitigated by doing renames in a dedicated task before behavior changes, so each step is verifiable.
- **Open hierarchy lacks exhaustiveness checking** → Accepted per design doc §8. Every consumer `switch` must have `default`.
- **Resilience handler defaults may not suit all providers** → Can be tuned per client registration. Defaults are conservative (3 retries, 30s timeout).
- **Breaking DI registrations** → All `AddHttpClient<OldName>()` calls must be updated. Compile error if missed.
