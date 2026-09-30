## Why

On a fresh Docker start, the complete and incomplete download directories don't exist. The health check fails until they're manually created. The Dockerfile creates `/app/data/temp` at build time but leaves `/shared/downloads/complete` and `/shared/downloads/incomplete` to be created by the user. This creates a broken first-run experience.

## What Changes

- Auto-create `complete` and `incomplete` download directories at application startup if they don't exist
- The creation happens in the `DataPaths` constructor or during the service container setup phase

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `data-paths`: Auto-create `Complete` and `Incomplete` directories during construction if they don't exist

## Impact

- **FunkArr.Core**: `DataPaths.cs` - add directory creation in constructor
- No Dockerfile changes needed (runtime creation is more flexible than build-time)
- Health check will pass on first run without manual intervention
