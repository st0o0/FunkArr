# Lokale Entwicklung

FunkArr läuft im Dev-Stack, nicht mit `dotnet run` auf dem Host-Projekt: Der Container bringt FFmpeg, die Datenpfade und die *arr-Apps mit, mit denen FunkArr spricht.

```bash
docker compose -f docker-compose.dev.yml up -d --build
```

| Dienst | URL |
|---|---|
| FunkArr | `http://localhost:6969` |
| Sonarr | `http://localhost:8989` |
| Radarr | `http://localhost:7878` |
| Prowlarr | `http://localhost:9696` |

FunkArr wird aus deinem Arbeitsstand gebaut. Nach Backend-Änderungen startest du denselben Befehl noch einmal. FunkArr nutzt den API-Key `funkarr-dev-api-key-01`, loggt auf `Debug`-Level und lädt in ein Volume, das auch Sonarr und Radarr eingebunden haben.

## Metadaten-Keys

Enrichment braucht TVDB- und TMDB-Keys. Gib sie aus deiner Shell oder einer `.env`-Datei neben der Compose-Datei mit:

```bash
TVDB_API_KEY=… TMDB_API_KEY=… docker compose -f docker-compose.dev.yml up -d --build
```

## Geo-gesperrte Sender

Das Profil `proxy` fügt einen Gluetun-Container hinzu. ORF-Sender laufen darüber, alles andere direkt. Es braucht WireGuard-Zugangsdaten in der Umgebung:

```bash
docker compose -f docker-compose.dev.yml --profile proxy up -d --build
```

## Im Alltag

```bash
docker compose -f docker-compose.dev.yml logs -f funkarr     # Logs verfolgen
docker compose -f docker-compose.dev.yml down                # stoppen
docker compose -f docker-compose.dev.yml down -v             # stoppen und alle Daten löschen
```

Für Arbeit an der Oberfläche startest du den Vite-Dev-Server zusätzlich zum Stack, siehe [Web-Oberfläche](./web-ui). Die Konfigurationsoptionen stehen unter [Konfiguration](/configuration); der Dev-Stack setzt sie als `FunkArr__…`-Umgebungsvariablen.
