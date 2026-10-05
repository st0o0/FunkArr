## Context

FunkArr has three hand-rolled backoff implementations:

1. **Search workers** (`TvSearchWorker`, `MovieSearchWorker`): Hardcoded `TimeSpan[]` arrays `[500ms, 1500ms]` indexed by attempt number. Used for MediathekViewWeb queue-full retries.
2. **DownloadWorker**: Manual `Math.Pow(2, attempt - 1) * base` with a `_maxBackoff` cap. Base delay configurable via `DownloadOptions.RetryBackoffBase`.
3. **AkkaSetupContainer**: Akka's built-in `BackoffSupervisor` for supervisor restarts (not a retry mechanism — out of scope).

Servus 0.35.0 ships `Servus.Resilience.Backoff` — a pure exponential delay calculator with `Delay(attempt)` and `DelayWithJitter(attempt)`. Already in the dependency graph, unused.

## Goals / Non-Goals

**Goals:**
- Replace hand-rolled backoff code with `Servus.Resilience.BackoffPolicy` in search workers and download worker
- Add jitter to all retry delays to prevent thundering herd
- Establish a single backoff pattern: `BackoffPolicy` + `DelayWithJitter`

**Non-Goals:**
- Changing retry counts, retry conditions, or retry semantics
- Replacing Akka's `BackoffSupervisor` (different mechanism, different purpose)
- Replacing Polly-based HTTP resilience in `RetryAfterDefaults` (server-driven `Retry-After` headers)
- Adding a shared backoff abstraction or base class — each actor creates its own policy

## Decisions

### D1: Always use `DelayWithJitter`, never `Delay`

**Choice**: `DelayWithJitter` everywhere.

**Rationale**: All retry targets are external (MediathekViewWeb API, download servers). Deterministic delays cause correlated retries when multiple actors retry simultaneously (RSS sync triggers parallel search workers). Jitter (±25% spread) breaks correlation. No FunkArr scenario benefits from deterministic retry timing.

**Alternative considered**: `Delay` for downloads (single worker, no herd). Rejected — no downside to jitter, and consistency across the codebase is worth more than a theoretical distinction.

### D2: Search workers — `static readonly BackoffPolicy`

**Choice**: Single shared `static readonly` policy per worker type.

```
Backoff.Create(TimeSpan.FromMilliseconds(500), maxDelay: TimeSpan.FromMilliseconds(1500))
```

**Rationale**: Initial delay (500ms) and max delay (1500ms) are constants, not configurable. The policy is stateless and thread-safe — sharing it across all instances of the worker is correct and efficient. `MaxRetries` stays as a separate constant controlling the retry count.

### D3: DownloadWorker — instance-level `BackoffPolicy` created in constructor

**Choice**: Create `BackoffPolicy` in the constructor from `IOptionsMonitor<DownloadOptions>.CurrentValue`.

```
_backoff = Backoff.Create(opts.RetryBackoffBase, maxDelay: TimeSpan.FromMinutes(5))
```

**Rationale**: `RetryBackoffBase` is configurable via options. Three alternatives considered:

| Option | Hot-reload? | Consistency | Complexity |
|--------|------------|-------------|------------|
| A: `static readonly` | No | N/A | Lowest |
| **B: Instance in ctor** | **Per-worker** | **Retry cycle stays consistent** | **Low** |
| C: Create per-retry | Yes, immediate | Delay can shift mid-cycle | Medium |

Option B wins: DownloadWorker is a sharded entity with bounded lifecycle. New workers pick up config changes; running workers keep a consistent delay progression through their retry cycle. This is better than the current behavior (Option C equivalent) where changing `RetryBackoffBase` mid-retry could cause non-monotonic delay jumps.

### D4: Attempt index mapping

**Choice**: Use zero-indexed attempts as Servus expects. Adjust attempt tracking if needed.

**Rationale**: Servus `BackoffPolicy.Delay(0)` returns `initialDelay`. Search workers already pass `_state.Attempt` starting at 0. DownloadWorker uses `attempt - 1` in its `Math.Pow` — verify the state's attempt semantics and adjust the call site, not the state.

## Risks / Trade-offs

- **[Jitter changes exact retry timing]** → Acceptable. Retry delays shift by ±25% around the same base values. No downstream code depends on exact retry timing. Logging already includes the actual delay value.
- **[DownloadWorker loses immediate config hot-reload]** → Acceptable trade-off for retry cycle consistency. New workers pick up changes. In practice, nobody changes `RetryBackoffBase` during an active download's retry cycle.
- **[Search worker max delay changes slightly]** → With `Backoff.Create(500ms, maxDelay: 1500ms)` and multiplier 2.0: attempt 0 = 500ms, attempt 1 = 1000ms (was 1500ms). Verify whether `maxDelay` caps attempt 1 at 1500ms or the formula gives 1000ms. If the latter, adjust `multiplier` to 3.0 to match `[500ms, 1500ms]` exactly, or accept the slight timing change.
