# Erste Schritte

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

Die Web-Oberfläche ist unter `http://localhost:8080` erreichbar.

::: tip Hinweise
- `PUID` / `PGID` (Standard: `1654`) legen fest, unter welchem Benutzer FunkArr läuft. Dieser Benutzer braucht Schreibzugriff auf den Download-Pfad.
- Ohne `FunkArr__ApiKey` gilt der Standardwert `funkarr-default-api-key`. Setze für den produktiven Betrieb einen eigenen Wert.
- Als Datenbank wird standardmäßig SQLite (`/app/data/funkarr.db`) verwendet. PostgreSQL ist optional, siehe [Konfiguration](configuration).
- Sonarr/Radarr müssen den Download-Pfad (hier `/media`) unter demselben Pfad eingebunden haben, damit sie fertige Downloads importieren können.
- Innerhalb des Docker-Netzwerks läuft FunkArr immer auf Port `6969`; `8080` ist nur das veröffentlichte Host-Port-Mapping.
:::

## Einrichtung in Prowlarr / Sonarr / Radarr

### Indexer (Prowlarr oder Sonarr/Radarr)

1. Neuen Indexer vom Typ **Newznab** hinzufügen
2. URL: `http://funkarr:6969/index/api`
3. API Key: der Wert, den du für `FunkArr__ApiKey` gesetzt hast
4. Testen und speichern

### Download Client (Sonarr / Radarr)

1. Neuen Download Client vom Typ **SABnzbd** hinzufügen
2. Host: `funkarr`, Port: `6969`
3. URL Base: `/download/api`
4. API Key: der Wert, den du für `FunkArr__ApiKey` gesetzt hast
5. Testen und speichern
