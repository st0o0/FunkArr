# Local development

FunkArr runs in the dev stack, not with `dotnet run` on the host project: the container brings FFmpeg, the data paths and the *arr apps FunkArr talks to.

```bash
docker compose -f docker-compose.dev.yml up -d --build
```

| Service | URL |
|---|---|
| FunkArr | `http://localhost:6969` |
| Sonarr | `http://localhost:8989` |
| Radarr | `http://localhost:7878` |
| Prowlarr | `http://localhost:9696` |

FunkArr is built from your working tree. Run the same command again after backend changes. FunkArr uses the API key `funkarr-dev-api-key-01`, logs at `Debug` level, and downloads to a volume that Sonarr and Radarr also mount.

## Metadata keys

Enrichment needs TVDB and TMDB keys. Pass them in from your shell or a `.env` file next to the compose file:

```bash
TVDB_API_KEY=… TMDB_API_KEY=… docker compose -f docker-compose.dev.yml up -d --build
```

## Geo-restricted channels

The `proxy` profile adds a Gluetun container. ORF channels are routed through it; everything else goes direct. It needs WireGuard credentials in the environment:

```bash
docker compose -f docker-compose.dev.yml --profile proxy up -d --build
```

## Day to day

```bash
docker compose -f docker-compose.dev.yml logs -f funkarr     # follow the logs
docker compose -f docker-compose.dev.yml down                # stop
docker compose -f docker-compose.dev.yml down -v             # stop and wipe all data
```

For UI work, run the Vite dev server on top of the stack, see [Web UI](./web-ui). Configuration options are described in [Configuration](/en/configuration); the dev stack sets them as `FunkArr__…` environment variables.
