# Fehlerbehebung

## FFmpeg nicht gefunden

**Symptom:** Der Setup-Health-Check zeigt eine Warnung bei "FFmpeg". Downloads schlagen fehl.

**Loesung:** FFmpeg muss im `PATH` verfuegbar sein. Im offiziellen Docker-Image ist es bereits enthalten. Wenn du FunkArr ohne Docker betreibst, installiere FFmpeg und stelle sicher, dass `ffmpeg -version` in der Shell funktioniert.

## FFmpeg-Fehler beim Download

**Symptom:** Downloads starten, schlagen aber waehrend des Remux fehl. In der Historie steht eine Fehlermeldung mit "ffmpeg".

**Moegliche Ursachen:**

- Die Quell-URL ist abgelaufen. Mediathek-Stream-URLs sind zeitlich begrenzt gueltig. Starte den Download erneut.
- Der HLS-Stream ist unvollstaendig oder vom Sender entfernt worden.
- Nicht genuegend Speicherplatz im Incomplete-Verzeichnis.

**Debug:** Setze das Log-Level auf `Debug`, um die vollstaendigen FFmpeg-Kommandos und -Ausgaben zu sehen:

```
Serilog__MinimumLevel__Default=Debug
```

## MediathekViewWeb API-Timeouts

**Symptom:** Suchanfragen von Sonarr/Radarr geben keine Ergebnisse zurueck oder dauern sehr lange.

**Loesung:**

1. Pruefe die Erreichbarkeit: Oeffne `https://mediathekviewweb.de/` im Browser.
2. Pruefe den Health-Check: Gehe auf die Setup-Seite in der Web-Oberflaeche oder rufe `/api/system/setup` auf.
3. Die MediathekViewWeb-API hat gelegentlich Lastspitzen. Warte einige Minuten und versuche es erneut.

## Regelwerk-Synchronisation fehlgeschlagen

**Symptom:** Community-Regelwerke werden nicht aktualisiert. Die Version auf dem Dashboard aendert sich nicht.

**Loesung:**

1. Pruefe, ob `FunkArr__RuleSet__RefreshEnabled` auf `true` steht (Standard).
2. Pruefe, ob das konfigurierte Repository (`FunkArr__RuleSet__Repository`) erreichbar ist.
3. Pruefe, ob der Container Internetzugang hat (GitHub-API muss erreichbar sein).
4. Wenn du eine bestimmte Version gepinnt hast (`FunkArr__RuleSet__Version`), wird keine neuere Version heruntergeladen. Setze auf `latest` zurueck.

## Sonarr/Radarr findet keine Ergebnisse

**Symptom:** Suchen in Sonarr oder Radarr geben keine Treffer zurueck, obwohl der Inhalt in der Mediathek vorhanden ist.

**Loesung:**

1. **Regelwerk vorhanden?** Pruefe in der Web-Oberflaeche unter "Regelwerke", ob ein Regelwerk fuer die gesuchte Sendung existiert. Ohne Regelwerk kann FunkArr den Mediathek-Titel nicht der Sonarr/Radarr-Serie zuordnen.
2. **Korrekte IDs?** Das Regelwerk muss die richtige TVDB-ID (Serien) oder TMDB-ID (Filme) enthalten, damit Sonarr/Radarr den Match erkennt.
3. **API-Schluessel?** Pruefe, ob der API-Schluessel in Sonarr/Radarr mit dem FunkArr-Schluessel uebereinstimmt.
4. **Indexer-Test?** Fuehre in Sonarr/Radarr einen Test des Indexers durch. Schlaegt er fehl, stimmt die URL oder der Schluessel nicht.
5. **Interaktive Suche:** Nutze die interaktive Suche in Sonarr/Radarr, um zu sehen, welche Ergebnisse FunkArr liefert.

## Downloads bleiben haengen

**Symptom:** Downloads stehen dauerhaft auf "In Bearbeitung" ohne Fortschritt.

**Loesung:**

1. Pruefe, ob die Download-Pipeline pausiert ist (oben rechts auf der Aktivitaets-Seite).
2. Pruefe, ob genuegend Speicherplatz im Incomplete-Verzeichnis vorhanden ist.
3. Pruefe die Netzwerkverbindung des Containers.
4. Starte den Container neu. Unvollstaendige Downloads werden beim Neustart aufgeraeumt.
5. Erhoehe `FunkArr__Download__ConcurrentDownloads`, wenn der Server viel Last hat und Downloads in der Warteschlange warten.

## Untertitel-Konvertierung (TTML nach SRT)

**Symptom:** Untertitel fehlen in der fertigen MKV-Datei, obwohl die Mediathek Untertitel anbietet.

**Loesung:**

- FunkArr konvertiert TTML-Untertitel (das Format vieler Mediatheken) automatisch nach SRT und bettet sie als deutschsprachige Spur in die MKV-Datei ein.
- Wenn Untertitel fehlen, bietet die Quelle moeglicherweise keine an. Nicht alle Mediathek-Eintraege haben Untertitel.
- Aktiviere Debug-Logging, um zu sehen, ob Untertitel gefunden und verarbeitet werden.

## PostgreSQL-Verbindungsprobleme

**Symptom:** FunkArr startet nicht oder zeigt Datenbankfehler, wenn PostgreSQL konfiguriert ist.

**Loesung:**

1. Pruefe, ob alle Postgres-Variablen gesetzt sind: `FunkArr__Postgres__Host`, `User`, `Password`, `Database`.
2. Pruefe, ob die PostgreSQL-Instanz erreichbar ist und der Benutzer Zugriffsrechte hat.
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

Eine vollstaendige PostgreSQL-Docker-Compose-Konfiguration findest du in `docker-compose.postgres.yml` im Repository.

4. Ohne `FunkArr__Postgres__Host` faellt FunkArr automatisch auf SQLite zurueck.

## Container startet nicht

**Symptom:** Der Docker-Container startet und beendet sich sofort wieder.

**Loesung:**

1. Pruefe die Container-Logs: `docker compose logs funkarr`
2. Haeufige Ursachen:
   - Fehlende oder falsche Umgebungsvariablen
   - Volume-Mount-Pfad existiert nicht auf dem Host
   - Port-Konflikt (ein anderer Dienst belegt den konfigurierten Port)
   - Nicht genuegend Speicher oder CPU

## Debug-Logging aktivieren

Fuer detaillierte Fehlersuche setze das globale Log-Level auf `Debug`:

```
Serilog__MinimumLevel__Default=Debug
```

Einzelne Bereiche koennen gezielt lauter geschaltet werden:

| Variable | Was es zeigt |
|----------|-------------|
| `Serilog__MinimumLevel__Default=Debug` | Alle Bereiche |
| `Serilog__MinimumLevel__Override__Akka=Information` | Actor-System-Meldungen |
| `Serilog__MinimumLevel__Override__Microsoft.AspNetCore=Debug` | HTTP-Request-Details |

Gueltige Level: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`.

::: warning
Debug-Logging erzeugt viel Ausgabe. Setze es nur temporaer zur Fehlersuche ein und stelle es danach wieder auf `Information` zurueck.
:::

## Health-Check-Endpunkte

Fuer automatische Ueberwachung stellt FunkArr drei Endpunkte bereit:

| Endpunkt | Zweck |
|----------|-------|
| `/healthz` | Vollstaendiger Health-Check (Datenbank, Actor-System) - gibt 200 oder 503 zurueck |
| `/alive` | Einfacher Liveness-Probe - gibt immer 200 zurueck |
| `/api/system/setup` | Detaillierte Setup-Validierung mit einzelnen Pruefungsergebnissen |

Verwende `/healthz` fuer Docker-Health-Probes:

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:6969/healthz"]
  interval: 30s
  timeout: 5s
  retries: 3
```
