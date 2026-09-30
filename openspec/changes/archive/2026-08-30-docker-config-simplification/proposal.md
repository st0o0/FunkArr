## Why

The Kestrel port configuration in Program.cs (`FunkArr:Http:Port` with default 8080)
contradicts the Dockerfiles which expose port 6969. In a Docker-first application,
the internal port is fixed — users control external mapping via `ports:` in
docker-compose. The configurable port adds complexity and a mismatch bug.

## What Changes

- Remove the Kestrel configuration block from Program.cs (`FunkArr:Http:Port` check,
  `ConfigureKestrel` call)
- Add `ENV ASPNETCORE_URLS=http://+:6969` to both Dockerfiles so ASP.NET binds
  the correct port automatically
- Add `appsettings.Development.json` URL override for local development (`http://localhost:5000`)

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `application-bootstrap`: Remove Kestrel HTTP configuration requirement, port is
  no longer application-configured

## Impact

- `src/FunkArr/Program.cs` — Kestrel block removed (5 lines)
- `Dockerfile` — add ENV line
- `Dockerfile.dev` — add ENV line
- `src/FunkArr/appsettings.Development.json` — add Urls override for local dev
