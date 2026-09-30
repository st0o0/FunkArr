## Context

FunkArr is a Docker-first application. The internal port is fixed at 6969 (matching
the *arr ecosystem convention). Users map it externally via docker-compose `ports:`.
The current Program.cs has a configurable port defaulting to 8080, which contradicts
the Dockerfiles exposing 6969.

## Goals / Non-Goals

**Goals:**
- Single source of truth for the port: Dockerfile ENV
- Program.cs has zero port/Kestrel logic
- Local development works out of the box via appsettings.Development.json

**Non-Goals:**
- No changes to PersistencePath (default already works with Docker volume)
- No changes to other options

## Decisions

### Port controlled via ASPNETCORE_URLS, not application code

ASP.NET respects `ASPNETCORE_URLS` natively. Setting it as `ENV` in the Dockerfile
means the port is fixed at build time. No application code needed.

**Why:** Docker best practice. The application shouldn't know or care about its port.
Port mapping is an infrastructure concern.

### Local development uses appsettings.Development.json Urls override

For `dotnet run` outside Docker, `appsettings.Development.json` sets
`"Urls": "http://localhost:5000"`. This is the standard ASP.NET mechanism —
no custom Kestrel code needed.

**Why:** Keeps Program.cs clean. The Development environment is auto-detected
by ASP.NET when running with `dotnet run`.

## Risks / Trade-offs

- **Non-Docker users** must set `ASPNETCORE_URLS` or use the Development profile.
  This is acceptable — the project is Docker-first and `dotnet run` defaults to
  the Development environment which has the Urls override.
