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
- **PUID/PGID**: Setzt die Benutzer-/Gruppen-ID im Container. Standard ist `1654`. Setze `PUID=1000` und `PGID=1000` um Berechtigungsprobleme zu vermeiden.
- **API-Schlüssel**: Ohne `FunkArr__ApiKey` wird der Standard-Schlüssel `funkarr-default-api-key` verwendet. Setze deinen eigenen für produktive Nutzung.
- **Datenbank**: SQLite ist Standard. Für PostgreSQL siehe [Konfiguration](configuration).
- **Pfade**: Sonarr/Radarr benötigen den Download-Pfad am selben Mount-Punkt um abgeschlossene Downloads importieren zu können.
- **Ports**: Im Docker-Netzwerk läuft FunkArr auf Port `6969`. Port `8080` ist nur das Host-Mapping.
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
