# Fehlerbehebung

## FFmpeg nicht gefunden

**Symptom:** Der Setup-Health-Check zeigt eine Warnung bei "FFmpeg". Downloads schlagen fehl.

**Lösung:** FFmpeg muss im `PATH` verfügbar sein. Im offiziellen Docker-Image ist es bereits enthalten. Wenn du FunkArr ohne Docker betreibst, installiere FFmpeg und stelle sicher, dass `ffmpeg -version` in der Shell funktioniert.

## FFmpeg-Fehler beim Download

**Symptom:** Downloads starten, schlagen aber während des Remux fehl. In der Historie steht eine Fehlermeldung mit "ffmpeg".

**Mögliche Ursachen:**

- Die Quell-URL ist abgelaufen. Mediathek-Stream-URLs sind zeitlich begrenzt gültig. Starte den Download erneut.
- Der HLS-Stream ist unvollständig oder vom Sender entfernt worden.
- Nicht genügend Speicherplatz im Incomplete-Verzeichnis.

**Debug:** Setze das Log-Level auf `Debug`, um die vollständigen FFmpeg-Kommandos und -Ausgaben zu sehen:

```
Serilog__MinimumLevel__Default=Debug
```

## MediathekViewWeb API-Timeouts

**Symptom:** Suchanfragen von Sonarr/Radarr geben keine Ergebnisse zurück oder dauern sehr lange.

**Lösung:**

1. Prüfe die Erreichbarkeit: Öffne `https://mediathekviewweb.de/` im Browser.
2. Prüfe den Health-Check: Gehe auf die Setup-Seite in der Web-Oberfläche oder rufe `/api/system/setup` auf.
3. Die MediathekViewWeb-API hat gelegentlich Lastspitzen. Warte einige Minuten und versuche es erneut.

## Regelwerk-Synchronisation fehlgeschlagen

**Symptom:** Community-Regelwerke werden nicht aktualisiert. Die Version auf dem Dashboard ändert sich nicht.

**Lösung:**

1. Prüfe, ob `FunkArr__RuleSet__RefreshEnabled` auf `true` steht (Standard).
2. Prüfe, ob das konfigurierte Repository (`FunkArr__RuleSet__Repository`) erreichbar ist.
3. Prüfe, ob der Container Internetzugang hat (GitHub-API muss erreichbar sein).
4. Wenn du eine bestimmte Version gepinnt hast (`FunkArr__RuleSet__Version`), wird keine neuere Version heruntergeladen. Setze auf `latest` zurück.

## Sonarr/Radarr findet keine Ergebnisse

**Symptom:** Suchen in Sonarr oder Radarr geben keine Treffer zurück, obwohl der Inhalt in der Mediathek vorhanden ist.

**Lösung:**

1. **Regelwerk vorhanden?** Prüfe in der Web-Oberfläche unter "Regelwerke", ob ein Regelwerk für die gesuchte Sendung existiert. Ohne Regelwerk kann FunkArr den Mediathek-Titel nicht der Sonarr/Radarr-Serie zuordnen.
2. **Korrekte IDs?** Das Regelwerk muss die richtige TVDB-ID (Serien) oder TMDB-ID (Filme) enthalten, damit Sonarr/Radarr den Match erkennt.
3. **API-Schlüssel?** Prüfe, ob der API-Schlüssel in Sonarr/Radarr mit dem FunkArr-Schlüssel übereinstimmt.
4. **Indexer-Test?** Führe in Sonarr/Radarr einen Test des Indexers durch. Schlägt er fehl, stimmt die URL oder der Schlüssel nicht.
5. **Interaktive Suche:** Nutze die interaktive Suche in Sonarr/Radarr, um zu sehen, welche Ergebnisse FunkArr liefert.

## Downloads bleiben hängen

**Symptom:** Downloads stehen dauerhaft auf "In Bearbeitung" ohne Fortschritt.

**Lösung:**

1. Prüfe, ob die Download-Pipeline pausiert ist (oben rechts auf der Aktivitäts-Seite).
2. Prüfe, ob genügend Speicherplatz im Incomplete-Verzeichnis vorhanden ist.
3. Prüfe die Netzwerkverbindung des Containers.
4. Starte den Container neu. Unvollständige Downloads werden beim Neustart aufgeräumt.
5. Erhöhe `FunkArr__Download__ConcurrentDownloads`, wenn der Server viel Last hat und Downloads in der Warteschlange warten.

## Untertitel-Konvertierung (TTML nach SRT)

**Symptom:** Untertitel fehlen in der fertigen MKV-Datei, obwohl die Mediathek Untertitel anbietet.

**Lösung:**

- FunkArr konvertiert TTML-Untertitel (das Format vieler Mediatheken) automatisch nach SRT und bettet sie als deutschsprachige Spur in die MKV-Datei ein.
- Wenn Untertitel fehlen, bietet die Quelle möglicherweise keine an. Nicht alle Mediathek-Einträge haben Untertitel.
- Aktiviere Debug-Logging, um zu sehen, ob Untertitel gefunden und verarbeitet werden.

## PostgreSQL-Verbindungsprobleme

**Symptom:** FunkArr startet nicht oder zeigt Datenbankfehler, wenn PostgreSQL konfiguriert ist.

**Lösung:**

1. Prüfe, ob alle Postgres-Variablen gesetzt sind: `FunkArr__Postgres__Host`, `User`, `Password`, `Database`.
2. Prüfe, ob die PostgreSQL-Instanz erreichbar ist und der Benutzer Zugriffsrechte hat.
3. Bei Docker Compose: Verwende `depends_on` mit Health-Check, damit FunkArr erst startet, wenn PostgreSQL bereit ist:

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

Eine vollständige PostgreSQL-Docker-Compose-Konfiguration findest du in `docker-compose.postgres.yml` im Repository.

4. Ohne `FunkArr__Postgres__Host` fällt FunkArr automatisch auf SQLite zurück.

## Container startet nicht

**Symptom:** Der Docker-Container startet und beendet sich sofort wieder.

**Lösung:**

1. Prüfe die Container-Logs: `docker compose logs funkarr`
2. Häufige Ursachen:
   - Fehlende oder falsche Umgebungsvariablen
   - Volume-Mount-Pfad existiert nicht auf dem Host
   - Port-Konflikt (ein anderer Dienst belegt den konfigurierten Port)
   - Nicht genügend Speicher oder CPU

## Debug-Logging aktivieren

Für detaillierte Fehlersuche setze das globale Log-Level auf `Debug`:

```
Serilog__MinimumLevel__Default=Debug
```

Einzelne Bereiche können gezielt lauter geschaltet werden:

| Variable | Was es zeigt |
|----------|-------------|
| `Serilog__MinimumLevel__Default=Debug` | Alle Bereiche |
| `Serilog__MinimumLevel__Override__Akka=Information` | Actor-System-Meldungen |
| `Serilog__MinimumLevel__Override__Microsoft.AspNetCore=Debug` | HTTP-Request-Details |

Gültige Level: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`.

::: warning
Debug-Logging erzeugt viel Ausgabe. Setze es nur temporär zur Fehlersuche ein und stelle es danach wieder auf `Information` zurück.
:::

## Health-Check-Endpunkte

Für automatische Überwachung stellt FunkArr drei Endpunkte bereit:

| Endpunkt | Zweck |
|----------|-------|
| `/healthz` | Vollständiger Health-Check (Datenbank, Actor-System) - gibt 200 oder 503 zurück |
| `/alive` | Einfacher Liveness-Probe - gibt immer 200 zurück |
| `/api/system/setup` | Detaillierte Setup-Validierung mit einzelnen Prüfungsergebnissen |

Verwende `/healthz` für Docker-Health-Probes:

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:6969/healthz"]
  interval: 30s
  timeout: 5s
  retries: 3
```
