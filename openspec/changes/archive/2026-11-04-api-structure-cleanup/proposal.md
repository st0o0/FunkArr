# API Structure Cleanup

## Problem

The `FunkArr.Api` project root mixes endpoint files with infrastructure
concerns. Four non-endpoint `.cs` files sit alongside the six `*ApiEndpoints.cs`
files, making the root noisier than it needs to be. Additionally,
`SetupArrEndpoints.cs` breaks the `*ApiEndpoints.cs` naming convention.

## Scope

1. **Move infrastructure files** out of root into an `Infrastructure/`
   subdirectory:
   - `ApiResults.cs` — shared result helpers
   - `ArrSetupClient.cs` — HTTP client for arr service setup
   - `EndpointExceptionFilter.cs` — global exception filter
   - `RingBufferSink.cs` — logging ring buffer sink

2. **Rename `SetupArrEndpoints.cs`** → `SetupApiEndpoints.cs` to match the
   `*ApiEndpoints.cs` convention.

After this, the project root contains only:
- `AssemblyMarker.cs` (conventional root location)
- `*ApiEndpoints.cs` (6 files, all consistently named)

## Out of scope

- Ask timeout changes — explored and decided against (explicit timeouts are
  defensive and cost nothing)
- Model reorganization (already in subfolders)
- Any behavior changes
