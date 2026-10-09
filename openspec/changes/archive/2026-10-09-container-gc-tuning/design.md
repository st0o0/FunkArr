## Context

FunkArr's Dockerfile sets no GC-related environment variables. The .NET runtime detects ASP.NET hosting and defaults to Server GC, which allocates one GC heap per logical CPU core. On typical NAS hardware (4 cores), this means 4 independent heaps with their own SOH/LOH/POH segments — reserving ~400-600MB RSS even when the application holds ~50MB of live objects.

The existing Dockerfile already sets `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` as the only runtime knob. The two new ENV vars follow the same pattern — runtime configuration, no code changes.

## Goals / Non-Goals

**Goals:**

- Reduce idle memory footprint by ~60% (from ~300-600MB to ~120-200MB)
- Keep all functionality identical — zero behavioral changes
- Configuration via ENV vars only, reversible by removing them

**Non-Goals:**

- Docker resource limits (`deploy.resources.limits.memory`) — future phase
- GC heap hard limits (`DOTNET_GCHeapHardLimit`) — future phase
- ThreadPool tuning — future phase
- Akka dispatcher thread pool sizing — future phase
- IL trimming or ReadyToRun publishing — future phase
- Native AOT (incompatible with Akka.NET's reflection usage)

## Decisions

### Use Workstation GC instead of Server GC

**Choice:** `DOTNET_gcServer=0`

**Rationale:** Server GC is designed for high-throughput, multi-core API servers handling thousands of concurrent requests. FunkArr handles Sonarr/Radarr polling (low-frequency), occasional UI requests, and periodic search/download bursts. Workstation GC uses a single heap and concurrent collection on a shared thread — far better suited for this workload.

**Alternative considered:** Keep Server GC but set `DOTNET_GCHeapCount=1` to force a single heap. This combines the worst of both — Server GC's thread allocation overhead with a single heap. Workstation GC is the correct abstraction for this workload class.

### Set GCConserveMemory to maximum

**Choice:** `DOTNET_GCConserveMemory=9`

**Rationale:** Scale 0-9 where 9 means the GC tries hardest to return memory to the OS. On shared NAS hardware, returning unused memory promptly matters more than avoiding GC pauses. FunkArr's workload is bursty — a search burst may temporarily allocate, but the GC should reclaim that memory quickly rather than holding it speculatively.

**Alternative considered:** A moderate value like 5. However, FunkArr's burst duration is short (seconds, not minutes) and throughput requirements during bursts are low (processing search results, not serving high-RPS). Maximum conservation is appropriate.

### Place ENV vars in Dockerfile, not docker-compose

**Choice:** Set both variables as `ENV` directives in the Dockerfile, grouped with the existing `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` setting.

**Rationale:** These are sensible defaults for any FunkArr deployment, not environment-specific overrides. Users who want Server GC can override via `docker-compose.yml` environment section or `-e` flags. The Dockerfile establishes the optimized baseline.

## Risks / Trade-offs

- **Slightly longer GC pauses during peak load** → Negligible for FunkArr. Workstation GC concurrent mode still collects without stopping all threads. The bursty-then-idle workload profile means pauses happen during low-activity periods.
- **FFmpeg child processes unaffected** → FFmpeg runs as OS child processes outside the .NET managed heap. Their memory (~50-80MB per download) is not controlled by these settings. This is correct — FFmpeg memory is kernel-managed.
- **Users running on high-core-count servers may see less throughput** → Unlikely scenario for a Mediathek download tool. Can override with `DOTNET_gcServer=1` in compose if needed.
