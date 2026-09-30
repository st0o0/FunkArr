## Context

The Docker container currently runs the .NET process with a hardcoded UID 1654 and uses `umask 000` to work around file permission issues on bind-mounted volumes. There is no mechanism for users to control which UID/GID the process runs as. The *arr ecosystem universally supports PUID/PGID environment variables — users deploying FunkArr alongside Sonarr/Radarr/Prowlarr expect this pattern.

Current entrypoint: `/bin/sh -c "umask 000 && exec dotnet FunkArr.dll"` — runs as whatever user the container defaults to with world-readable/writable files.

## Goals / Non-Goals

**Goals:**
- Users can set PUID/PGID to match their host user's UID/GID
- Container creates a proper `funkarr` user/group at startup and drops privileges
- Files written by FunkArr are owned by the specified UID/GID
- Backward compatible — existing deployments without PUID/PGID keep working (default 1654)
- Zero overhead — `su-exec` does a direct exec, no daemon

**Non-Goals:**
- Recursive chown of `/media` volumes (user responsibility, could take hours on large libraries)
- Supporting non-Alpine base images
- Running as non-root without an entrypoint (would need `user:` in compose but breaks user creation)

## Decisions

### 1. Use an entrypoint.sh script (not inline ENTRYPOINT)

**Rationale:** The user creation logic has edge cases (user/group already exists, PUID=0 means root) that become unreadable when inlined in the Dockerfile ENTRYPOINT. A separate script is debuggable (`docker run --entrypoint sh`) and follows the ecosystem convention.

**Alternative considered:** Inline `ENTRYPOINT ["/bin/sh", "-c", "..."]` — rejected because multi-line shell in Dockerfile is fragile and hard to maintain.

**Alternative considered:** .NET-level setuid/setgid — rejected because .NET doesn't support this cleanly on Alpine, and the process must start as root to chown data dirs.

### 2. Use `su-exec` (not `gosu`)

**Rationale:** `su-exec` is in Alpine repos (`apk add su-exec`), ~10KB, identical interface to `gosu`. `gosu` requires downloading a binary from GitHub releases on Alpine (~1.8MB). Both do the same thing: exec as a different user without forking.

### 3. Use busybox `addgroup`/`adduser` (not `shadow` package)

**Rationale:** Alpine ships busybox `addgroup`/`adduser` — no extra package needed. The `shadow` package adds ~1MB for `useradd`/`groupmod` which we don't need. Busybox commands handle the happy path; the `2>/dev/null || true` pattern handles "already exists" gracefully.

### 4. Default PUID/PGID to 1654

**Rationale:** The current Dockerfile hardcodes UID 1654. Existing users have volumes with files owned by 1654. Changing the default would break existing deployments.

### 5. Chown only `/app/data`, never `/media`

**Rationale:** `/app/data` contains the SQLite database, rulesets, and temp files — small directory, fast to chown recursively. `/media` is the user's media library — potentially terabytes. Sonarr/Radarr follow the same pattern: they only chown their config dir, not media mounts.

### 6. PUID=0 or PGID=0 skips user-switch

**Rationale:** If a user explicitly wants to run as root, honor that. Trying to create a user with UID 0 would collide with the root user entry. This matches linuxserver.io behavior.

## Risks / Trade-offs

- **[Risk] Existing volumes owned by 1654 with new PUID/PGID** → The entrypoint does `chown -R` on `/app/data` at every startup, so ownership transitions automatically. First start with new PUID/PGID may take a moment longer if `/app/data` has many files.
- **[Risk] Download dir not writable** → If the user's download path is under `/media` and not writable by the PUID/PGID user, downloads will fail. Mitigated by documentation: user must ensure PUID/PGID has write access to the download path, same as Sonarr/Radarr.
- **[Trade-off] Container starts as root** → Required to create users and chown dirs. Privileges are dropped before the application starts. This is the standard Docker pattern for PUID/PGID support.
