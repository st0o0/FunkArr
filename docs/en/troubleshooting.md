# Troubleshooting

## FFmpeg Not Found

**Symptom:** The setup health check shows a warning for "FFmpeg". Downloads fail.

**Solution:** FFmpeg must be available on `PATH`. The official Docker image includes it. If you run FunkArr without Docker, install FFmpeg and verify that `ffmpeg -version` works in your shell.

## FFmpeg Errors During Download

**Symptom:** Downloads start but fail during remux. The history shows an error message containing "ffmpeg".

**Possible causes:**

- The source URL has expired. Mediathek stream URLs are only valid for a limited time. Retry the download.
- The HLS stream is incomplete or has been removed by the broadcaster.
- Not enough disk space in the incomplete directory.

**Debug:** Set the log level to `Debug` to see full FFmpeg commands and output:

```
Serilog__MinimumLevel__Default=Debug
```

## MediathekViewWeb API Timeouts

**Symptom:** Search requests from Sonarr/Radarr return no results or take a very long time.

**Solution:**

1. Check reachability: Open `https://mediathekviewweb.de/` in a browser.
2. Check the health check: Go to the Setup page in the web UI or call `/api/system/setup`.
3. The MediathekViewWeb API occasionally experiences load spikes. Wait a few minutes and try again.

## Ruleset Sync Failed

**Symptom:** Community rulesets are not updating. The version on the dashboard does not change.

**Solution:**

1. Check that `FunkArr__RuleSet__RefreshEnabled` is `true` (default).
2. Check that the configured repository (`FunkArr__RuleSet__Repository`) is reachable.
3. Check that the container has internet access (the GitHub API must be reachable).
4. If you pinned a specific version (`FunkArr__RuleSet__Version`), no newer version will be downloaded. Set it back to `latest`.

## Sonarr/Radarr Returns No Results

**Symptom:** Searches in Sonarr or Radarr return no hits even though the content exists in the Mediathek.

**Solution:**

1. **Ruleset exists?** Check in the web UI under "Rulesets" whether a ruleset exists for the show you are looking for. Without a ruleset, FunkArr cannot map the Mediathek title to the Sonarr/Radarr series.
2. **Correct IDs?** The ruleset must contain the correct TVDB ID (series) or TMDB ID (movies) so Sonarr/Radarr can recognize the match.
3. **API key?** Verify the API key in Sonarr/Radarr matches the FunkArr key.
4. **Indexer test?** Run an indexer test in Sonarr/Radarr. If it fails, the URL or the key is wrong.
5. **Interactive search:** Use interactive search in Sonarr/Radarr to see what results FunkArr returns.

## Downloads Stuck

**Symptom:** Downloads stay at "Processing" permanently without progress.

**Solution:**

1. Check whether the download pipeline is paused (top-right on the Activity page).
2. Check whether there is enough disk space in the incomplete directory.
3. Check the container's network connection.
4. Restart the container. Incomplete downloads are cleaned up on restart.
5. Increase `FunkArr__Download__ConcurrentDownloads` if the server is under heavy load and downloads are waiting in the queue.

## Subtitle Conversion (TTML to SRT)

**Symptom:** Subtitles are missing from the finished MKV file even though the Mediathek offers subtitles.

**Solution:**

- FunkArr automatically converts TTML subtitles (the format used by many Mediatheken) to SRT and embeds them as a German-language track in the MKV file.
- If subtitles are missing, the source may not offer any. Not all Mediathek entries have subtitles.
- Enable debug logging to see whether subtitles are found and processed.

## PostgreSQL Connection Issues

**Symptom:** FunkArr does not start or shows database errors when PostgreSQL is configured.

**Solution:**

1. Check that all Postgres variables are set: `FunkArr__Postgres__Host`, `User`, `Password`, `Database`.
2. Check that the PostgreSQL instance is reachable and the user has access.
3. With Docker Compose: Use `depends_on` with a health check so FunkArr only starts when PostgreSQL is ready:

```yaml
services:
  funkarr:
    depends_on:
      db:
        condition: service_healthy

  db:
    image: postgres:17-alpine
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U funkarr"]
      interval: 5s
      timeout: 3s
      retries: 5
```

A complete PostgreSQL Docker Compose configuration is available in `docker-compose.postgres.yml` in the repository.

4. Without `FunkArr__Postgres__Host`, FunkArr falls back to SQLite automatically.

## Container Fails to Start

**Symptom:** The Docker container starts and exits immediately.

**Solution:**

1. Check the container logs: `docker compose logs funkarr`
2. Common causes:
   - Missing or incorrect environment variables
   - Volume mount path does not exist on the host
   - Port conflict (another service is using the configured port)
   - Not enough memory or CPU

## Enable Debug Logging

For detailed troubleshooting, set the global log level to `Debug`:

```
Serilog__MinimumLevel__Default=Debug
```

Individual areas can be made more verbose separately:

| Variable | What it shows |
|----------|--------------|
| `Serilog__MinimumLevel__Default=Debug` | All areas |
| `Serilog__MinimumLevel__Override__Akka=Information` | Actor system messages |
| `Serilog__MinimumLevel__Override__Microsoft.AspNetCore=Debug` | HTTP request details |

Valid levels: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`.

::: warning
Debug logging produces a lot of output. Use it only temporarily for troubleshooting and reset it to `Information` afterwards.
:::

## Health Check Endpoints

For automated monitoring, FunkArr provides three endpoints:

| Endpoint | Purpose |
|----------|---------|
| `/healthz` | Full health check (database, actor system) - returns 200 or 503 |
| `/alive` | Simple liveness probe - always returns 200 |
| `/api/system/setup` | Detailed setup validation with individual check results |

Use `/healthz` for Docker health probes:

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:6969/healthz"]
  interval: 30s
  timeout: 5s
  retries: 3
```
