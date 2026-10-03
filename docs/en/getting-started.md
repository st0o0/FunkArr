# Getting Started

## Docker Compose

```yaml
# docker-compose.yml
services:
  funkarr:
    image: ghcr.io/st0o0/funkarr:latest
    restart: unless-stopped
    ports:
      - "8080:6969"
    volumes:
      - funkarr-data:/app/data
      - /path/to/media:/media
    environment:
      - PUID=1000
      - PGID=1000
      - FunkArr__ApiKey=your-api-key-here
      - FunkArr__Download__Path=/media/downloads
      - FunkArr__Download__Categories__0__Name=tv
      - FunkArr__Download__Categories__0__Dir=tv
      - FunkArr__Download__Categories__1__Name=movies
      - FunkArr__Download__Categories__1__Dir=movies

volumes:
  funkarr-data:
```

```bash
docker compose up -d
```

The web UI is available at `http://localhost:8080`.

::: tip Notes
- `PUID` / `PGID` (default: `1654`) set the user FunkArr runs as. This user needs write access to the download path.
- If `FunkArr__ApiKey` is not set, the default `funkarr-default-api-key` applies. Set your own value in production.
- SQLite (`/app/data/funkarr.db`) is the default database. PostgreSQL is optional, see [Configuration](configuration).
- Sonarr/Radarr must mount the download path (here `/media`) at the same path so they can import completed downloads.
- Inside the Docker network FunkArr always listens on port `6969`; `8080` is only the published host port mapping.
:::

## Setup in Prowlarr / Sonarr / Radarr

### Indexer (Prowlarr or Sonarr/Radarr)

1. Add a new indexer of type **Newznab**
2. URL: `http://funkarr:6969/index/api`
3. API Key: the value you set for `FunkArr__ApiKey`
4. Test and save

### Download Client (Sonarr / Radarr)

1. Add a new download client of type **SABnzbd**
2. Host: `funkarr`, Port: `6969`
3. URL Base: `/download/api`
4. API Key: the value you set for `FunkArr__ApiKey`
5. Test and save
