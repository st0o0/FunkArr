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
- **PUID/PGID**: Sets the user/group ID inside the container. Default is `1654`. Set `PUID=1000` and `PGID=1000` to avoid permission issues.
- **API Key**: Without `FunkArr__ApiKey`, the default key `funkarr-default-api-key` is used. Set your own for production use.
- **Database**: SQLite is the default. For PostgreSQL see [Configuration](configuration).
- **Paths**: Sonarr/Radarr need the download path mounted at the same path to import completed downloads.
- **Ports**: Inside the Docker network, FunkArr runs on port `6969`. Port `8080` is only the host mapping.
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
