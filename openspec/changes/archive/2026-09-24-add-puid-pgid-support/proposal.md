## Why

The Docker container runs with a hardcoded UID 1654 and uses `umask 000` as a workaround for file permission issues. Users cannot control which UID/GID the FunkArr process runs as, causing permission mismatches on bind-mounted volumes. Every *arr-ecosystem tool (Sonarr, Radarr, Prowlarr) supports PUID/PGID — users expect it.

## What Changes

- Add `PUID` and `PGID` environment variables to control the runtime user identity
- Create an entrypoint script that creates user/group, sets ownership, and drops privileges via `su-exec`
- Default PUID/PGID to 1654 for backward compatibility with existing deployments
- Remove the `umask 000` workaround
- Add `su-exec` to the Alpine runtime image packages

## Capabilities

### New Capabilities
- `container-permissions`: PUID/PGID-based user identity and file ownership in the Docker container

### Modified Capabilities

_(none — no existing spec-level requirements change)_

## Impact

- **Dockerfile**: New runtime dependency (`su-exec`), new entrypoint script, removal of hardcoded chown and umask
- **entrypoint.sh**: New file at repo root
- **docker-compose.example.yml**: Document PUID/PGID environment variables
- **docker-compose.dev.yml**: Add PUID/PGID for dev consistency
- **Existing deployments**: No breaking change — default UID/GID remains 1654
