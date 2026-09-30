## 1. Entrypoint Script

- [x] 1.1 Create `entrypoint.sh` at repo root: read PUID/PGID env vars (default 1654), skip user-switch when 0, create group/user via busybox addgroup/adduser, chown -R /app/data, exec su-exec funkarr dotnet FunkArr.dll
- [x] 1.2 Ensure entrypoint.sh has LF line endings and is executable (chmod +x in Dockerfile)

## 2. Dockerfile Changes

- [x] 2.1 Add `su-exec` to the `apk add --no-cache` line in the runtime stage
- [x] 2.2 Remove the hardcoded `chown 1654:1654` from the RUN mkdir line (entrypoint handles this now)
- [x] 2.3 COPY entrypoint.sh and chmod +x it
- [x] 2.4 Replace `ENTRYPOINT ["/bin/sh", "-c", "umask 000 && exec dotnet FunkArr.dll"]` with `ENTRYPOINT ["/entrypoint.sh"]`

## 3. Docker Compose Updates

- [x] 3.1 Add commented PUID/PGID environment variables to `docker-compose.example.yml` with documentation
- [x] 3.2 Add PUID/PGID environment variables to `docker-compose.dev.yml`

## 4. Verification

- [x] 4.1 Build and start with `docker compose -f docker-compose.dev.yml up -d --build`, verify process runs as expected UID/GID
