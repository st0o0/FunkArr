# Konfiguration

Alle Einstellungen erfolgen über Umgebungsvariablen mit dem Präfix `FunkArr__`. Der doppelte Unterstrich (`__`) trennt verschachtelte Abschnitte - das ist die standardmäßige ASP.NET Core Konfigurationsbindung.

Die Standardwerte funktionieren sofort für die lokale Entwicklung. Für den Produktivbetrieb musst du normalerweise nur `FunkArr__ApiKey` und `FunkArr__Download__Path` setzen.

## Allgemein

| Variable | Standard | Beschreibung |
|----------|----------|-------------|
| `FunkArr__ApiKey` | `funkarr-default-api-key` | API-Schlüssel für Prowlarr/Sonarr/Radarr |
| `FunkArr__DataPath` | `data` | Basispfad für Datenbank, Regelwerke, temporäre Dateien |

### API-Schlüssel

Der API-Schlüssel authentifiziert alle Anfragen von Prowlarr, Sonarr und Radarr. Sowohl die Indexer-API (`/index/api`) als auch die Download-Client-API (`/download/api`) benötigen diesen Schlüssel. Verwende denselben Wert bei der Konfiguration von FunkArr in deinen *arr-Apps.

::: warning
Ändere den Standard-API-Schlüssel im Produktivbetrieb. Jeder mit dem Schlüssel kann über deine FunkArr-Instanz suchen und herunterladen.
:::

### Datenpfad

Der Datenpfad ist das Stammverzeichnis für alle persistenten Daten. FunkArr erstellt darin folgende Struktur:

```
data/
  funkarr.db            # SQLite-Datenbank (wenn kein PostgreSQL verwendet wird)
  rulesets/
    community/          # Automatisch synchronisierte Community-Regelwerke
    local/              # Deine eigenen Regelwerke
    version.txt         # Aktuell installierte Community-Regelwerk-Version
  temp/                 # Temporäre Dateien während Download/Remux
```

## Downloads

| Variable | Standard | Beschreibung |
|----------|----------|-------------|
| `FunkArr__Download__Path` | `data/downloads` | Stamm-Download-Verzeichnis |
| `FunkArr__Download__ConcurrentDownloads` | `3` | Maximale parallele Downloads |

### Download-Pfad

Der Download-Pfad enthält zwei Unterverzeichnisse, die FunkArr automatisch verwaltet:

- **`incomplete/`** - aktive Downloads und laufende Remux-Operationen
- **`complete/`** - fertige Downloads, nach Kategorie-Unterverzeichnissen organisiert

Sonarr und Radarr überwachen das `complete/`-Verzeichnis auf fertige Dateien. Stelle sicher, dass dieser Pfad sowohl für FunkArr als auch für deine *arr-Apps erreichbar ist (typischerweise über ein gemeinsames Docker-Volume-Mount).

### Parallele Downloads

Steuert, wie viele Videos gleichzeitig heruntergeladen und remuxed werden. Jeder Download verwendet einen FFmpeg-Prozess. Erhöhe den Wert bei schnellen Verbindungen mit verfügbarer CPU; verringere ihn bei Timeouts oder Ressourcenproblemen.

### Kategorien

Kategorien ordnen Sonarr/Radarr-Download-Kategorien Unterverzeichnissen innerhalb von `complete/` zu. Konfiguriere eine Kategorie pro *arr-App-Typ:

```
FunkArr__Download__Categories__0__Name=tv
FunkArr__Download__Categories__0__Dir=tv
FunkArr__Download__Categories__1__Name=movies
FunkArr__Download__Categories__1__Dir=movies
```

| Variable | Standard | Beschreibung |
|----------|----------|-------------|
| `FunkArr__Download__Categories__N__Name` | - | Kategoriename wie in Sonarr/Radarr konfiguriert |
| `FunkArr__Download__Categories__N__Dir` | - | Unterverzeichnis innerhalb von `complete/` für diese Kategorie |

Wenn Sonarr eine Download-Anfrage mit der Kategorie `tv` sendet, landet die fertige Datei in `complete/tv/`. Das `N` im Variablennamen ist ein nullbasierter Index - verwende `0`, `1`, `2` usw. für jede Kategorie.

## Regelwerke

| Variable | Standard | Beschreibung |
|----------|----------|-------------|
| `FunkArr__RuleSet__Repository` | `st0o0/funkarr` | GitHub-Repository für Community-Regelwerke |
| `FunkArr__RuleSet__Version` | `latest` | Zu verwendende Regelwerk-Version |
| `FunkArr__RuleSet__RefreshEnabled` | `true` | Automatische Regelwerk-Updates aktivieren |

### Repository

Das GitHub-Repository, in dem Community-Regelwerke als Release-Assets veröffentlicht werden. Ändere dies nur, wenn du einen Fork mit eigenen Regelwerk-Releases betreibst.

### Version

Setze auf `latest`, um immer das neueste Community-Regelwerk-Release zu verwenden. Pinne auf einen bestimmten Versions-Tag (z.B. `rulesets-v0.2.0`), um automatische Updates zu verhindern - nützlich, wenn ein neues Release ein Mapping bricht, auf das du angewiesen bist.

### Aktualisierung

Wenn aktiviert, prüft FunkArr alle 30 Minuten auf neue Regelwerk-Releases und lädt Updates automatisch herunter. Deaktiviere dies, wenn du eine bestimmte Version pinnst oder in einer isolierten Umgebung arbeitest.

## Metadaten

| Variable | Standard | Beschreibung |
|----------|----------|-------------|
| `FunkArr__Tmdb__ApiKey` | _(leer)_ | TMDB API-Schlüssel |
| `FunkArr__Tvdb__ApiKey` | _(leer)_ | TVDB API-Schlüssel |

### TMDB

Ein [TMDB](https://www.themoviedb.org/)-API-Schlüssel ermöglicht die Metadaten-Auflösung für Filme und Serien. FunkArr verwendet ihn, um Mediathek-Einträge dem richtigen TMDB-Titel zuzuordnen und so die Genauigkeit für Radarr und Sonarr zu verbessern.

Einen kostenlosen API-Schlüssel gibt es unter [themoviedb.org/settings/api](https://www.themoviedb.org/settings/api).

### TVDB

Ein [TVDB](https://thetvdb.com/)-API-Schlüssel ermöglicht die Episodenguide-Auflösung. FunkArr nutzt TVDB-Daten, um datumsbasierte Mediathek-Einträge Staffel-/Episodennummern zuzuordnen, wenn das Regelwerk eine Airdate-Strategie verwendet.

Einen API-Schlüssel gibt es unter [thetvdb.com/api-information](https://thetvdb.com/api-information).

::: tip
Beide Schlüssel sind optional, aber empfohlen. Ohne sie verlässt sich FunkArr ausschließlich auf Regelwerk-Muster für die Zuordnung, was funktioniert, aber weniger Ergebnisse liefert.
:::

## Scoring

| Variable | Standard | Beschreibung |
|----------|----------|-------------|
| `FunkArr__Scoring__PoolSize` | `4` | Parallele Scoring-Worker |

Der Scoring-Pool verarbeitet Mediathek-Suchergebnisse parallel gegen Regelwerke. Jeder Worker wertet einen Regelwerk-Match gleichzeitig aus. Erhöhe den Wert für schnellere Suchantworten auf Multi-Core-Systemen; der Standardwert von 4 funktioniert für die meisten Setups gut.

## Match-Verlauf

| Variable | Standard | Beschreibung |
|----------|----------|-------------|
| `FunkArr__MatchHistory__MaxSnapshots` | `100` | Maximale Anzahl von Match-History-Snapshots |
| `FunkArr__MatchHistory__MaxAgeDays` | `30` | Tage, bevor alte Snapshots gelöscht werden |
| `FunkArr__MatchHistory__SnapshotInterval` | `20` | Intervall zwischen Snapshots |

Der Match-Verlauf verfolgt, welche Regelwerk-Zuordnungen erfolgreiche Downloads erzeugt haben. Diese Daten fließen zurück in das Scoring - Regeln, die historisch korrekte Matches erzeugt haben, erhalten einen Konfidenz-Boost.

- **MaxSnapshots** - begrenzt den Speicher für den Match-Verlauf. Höhere Werte liefern mehr historische Daten für das Scoring, verbrauchen aber mehr Speicherplatz.
- **MaxAgeDays** - entfernt Snapshots, die älter als diese Anzahl Tage sind. Mediathek-Inhalte ändern sich regelmäßig, sodass alte Match-Daten weniger relevant werden.
- **SnapshotInterval** - steuert, wie oft neue Snapshots erstellt werden. Niedrigere Werte erfassen detailliertere Daten, erhöhen aber die Datenbankschreibvorgänge.

## PostgreSQL

Standardmäßig verwendet FunkArr SQLite mit der Datenbankdatei unter `{DataPath}/funkarr.db`. Setze `FunkArr__Postgres__Host`, um auf PostgreSQL umzustellen.

| Variable | Standard | Beschreibung |
|----------|----------|-------------|
| `FunkArr__Postgres__Host` | _(leer)_ | PostgreSQL-Host - setzen, um PostgreSQL zu aktivieren |
| `FunkArr__Postgres__Port` | `5432` | PostgreSQL-Port |
| `FunkArr__Postgres__User` | _(leer)_ | PostgreSQL-Benutzer |
| `FunkArr__Postgres__Password` | _(leer)_ | PostgreSQL-Passwort |
| `FunkArr__Postgres__Database` | `funkarr` | PostgreSQL-Datenbankname |

::: info
Wenn `Host` leer ist oder nicht gesetzt wurde, verwendet FunkArr SQLite. Das ist die empfohlene Standardeinstellung für Einzelinstanz-Setups. Verwende PostgreSQL, wenn du externes Datenbankmanagement brauchst oder skalieren möchtest.
:::

## Logging

FunkArr verwendet [Serilog](https://serilog.net/) für strukturiertes Logging. Log-Level können pro Namespace angepasst werden:

| Variable | Standard | Beschreibung |
|----------|----------|-------------|
| `Serilog__MinimumLevel__Default` | `Information` | Globales Mindest-Log-Level |
| `Serilog__MinimumLevel__Override__Akka` | `Warning` | Akka.NET Actor-System Log-Level |
| `Serilog__MinimumLevel__Override__Microsoft.AspNetCore` | `Warning` | ASP.NET Core Request Log-Level |

Gültige Level: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`.

Setze den Standard auf `Debug` zur Fehlersuche. Die Akka- und ASP.NET-Overrides sind standardmäßig auf `Warning` gesetzt, um das Rauschen vom Actor-System und der HTTP-Pipeline zu reduzieren.

## Netzwerk

FunkArr lauscht auf Port **6969** im Container. Mappe ihn auf einen beliebigen Host-Port in deiner Docker Compose:

```yaml
ports:
  - "8080:6969"   # Zugriff über http://localhost:8080
```

## FFmpeg

FFmpeg muss im `PATH` verfügbar sein - es ist im offiziellen Docker-Image enthalten. FunkArr verwendet FFmpeg um:

- Videostreams herunterzuladen (einschließlich HLS `.m3u8`)
- Nach MKV zu remuxen (kopiert Video-/Audio-Codecs ohne Neucodierung)
- Untertitel als SRT-Spuren mit deutschem Sprach-Tag einzubetten

Es gibt keine Konfigurationsoptionen für das FFmpeg-Verhalten. Der Setup-Health-Check (`/api/system/setup`) überprüft, ob FFmpeg verfügbar ist.

## Health Checks

FunkArr stellt drei Health-Endpunkte bereit, von denen keiner konfigurierbar ist:

| Endpunkt | Zweck |
|----------|-------|
| `/healthz` | Vollständiger Health-Check (Datenbank, Actor-System) - gibt 200 oder 503 zurück |
| `/alive` | Einfacher Liveness-Probe - gibt immer 200 zurück |
| `/api/system/setup` | Setup-Validierung - prüft API-Schlüssel, Verzeichnisse, FFmpeg, API-Konnektivität |

Verwende `/healthz` für Container-Orchestrierungs-Health-Probes und `/api/system/setup` in der Web-Oberfläche, um deine Konfiguration zu überprüfen.
