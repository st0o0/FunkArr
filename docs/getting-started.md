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
