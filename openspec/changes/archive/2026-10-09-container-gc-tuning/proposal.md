## Why

FunkArr runs as a Docker container on resource-constrained NAS/home server hardware where dozens of containers share limited RAM. The .NET runtime defaults to Server GC for ASP.NET hosts, which allocates one GC heap per CPU core — optimized for high-throughput API servers. FunkArr is a low-traffic media management tool that is idle >90% of runtime. Server GC on a 4-core host reserves ~400-600MB RSS even when the app only holds ~50MB of live objects. Switching to Workstation GC with conservative memory settings brings idle RSS down to ~120-200MB with no code changes.

## What Changes

- Add `DOTNET_gcServer=0` environment variable in Dockerfile to switch from Server GC to Workstation GC (single heap instead of one per core)
- Add `DOTNET_GCConserveMemory=9` environment variable in Dockerfile to enable maximum memory conservation (GC returns memory to the OS more aggressively)

## Capabilities

### New Capabilities

- `gc-tuning`: Container GC runtime configuration for reduced memory footprint on resource-constrained hardware

### Modified Capabilities

## Impact

- **Dockerfile**: Two `ENV` lines added alongside existing `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` setting
- **Runtime behavior**: Lower idle memory (~60% reduction), slightly longer GC pauses during peak load (search bursts, concurrent downloads) — negligible for FunkArr's bursty workload profile
- **No code changes**: Pure runtime configuration, no csproj or C# modifications
- **No API changes**: Zero impact on Newznab/SABnzbd compatibility or internal API
