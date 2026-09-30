## Why

The application logs plain text to console, which forces Alloy/Promtail to use fragile regex parsing to extract level, timestamp, and structured properties. Multi-line exceptions break line-based log scrapers. Switching to JSON output in production makes logs natively parseable and preserves all structured properties Serilog already captures internally.

Additionally, `HttpPort` in `FunkArrOptions` is infrastructure config that belongs to Kestrel, not to the application domain. Hardcoding the internal container port to `6969` and letting Docker handle port mapping simplifies configuration.

## What Changes

- Add `Serilog.Formatting.Compact` NuGet for CLEF (Compact Log Event Format) JSON output
- Add `LogFormat` option (`json` | `text`, default `text`) to `FunkArrOptions` to toggle between human-readable and JSON console output
- In production/Docker, set `FunkArr__LogFormat=json` so Alloy can use a simple `json` stage
- Enrich logs with application version for traceability across deployments
- **BREAKING**: Remove `HttpPort` from `FunkArrOptions`, hardcode `ASPNETCORE_URLS=http://+:6969` in `Program.cs`, update `docker-compose.example.yml` to map external port to internal `6969`
- Update `appsettings.json` and `docker-compose.example.yml` to reflect new defaults

## Capabilities

### New Capabilities
- `structured-logging`: Environment-driven log format switching (JSON for production, plain text for development) with CLEF output compatible with Grafana Alloy/Promtail/Loki pipeline

### Modified Capabilities
- `project-infrastructure`: Remove `HttpPort` from options, hardcode internal port to 6969, update configuration and health endpoint documentation

## Impact

- **Code**: `FunkArrOptions.cs`, `FunkArrOptionsValidator.cs`, `Program.cs`, `FunkArrServiceSetup.cs`
- **Config**: `appsettings.json`, `appsettings.Development.json`, `docker-compose.example.yml`, `Dockerfile`
- **Dependencies**: Add `Serilog.Formatting.Compact` to `Directory.Packages.props`
- **Breaking**: Users currently setting `FunkArr__HttpPort` must switch to Docker port mapping or `ASPNETCORE_URLS`
