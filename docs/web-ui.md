# Web-Oberfläche

FunkArr hat eine Web-Oberfläche auf Vue.js-Basis. Nach dem Start ist sie unter dem konfigurierten Port erreichbar (Standard: `http://localhost:6969`). Die Seitenleiste links führt zu allen Bereichen.

## Dashboard

Die Startseite zeigt vier Kacheln auf einen Blick:

- **Systemstatus** - Ampel-Indikator (grün/gelb/rot) basierend auf den Health-Checks. Klick führt zum Setup.
- **Letzte Downloads** - Anzahl der letzten abgeschlossenen Downloads. Klick führt zur Download-Historie.
- **Speicher** - Belegter und verfügbarer Speicherplatz im Complete-Verzeichnis mit Fortschrittsbalken.
- **Regelwerke** - Gesamtzahl der geladenen Regelwerke und die installierte Community-Regelwerk-Version.

Wenn Downloads aktiv sind, zeigt ein Fortschrittsbalken die aktuelle Gesamtgeschwindigkeit und die Anzahl wartender Downloads.

Darunter listet der Bereich "Letzte Aktivität" die letzten 10 Downloads mit Status, Größe und Zeitpunkt.

## Aktivität

Die Aktivitäts-Seite hat zwei Tabs: **Warteschlange** und **Verlauf**.

### Warteschlange

Zeigt alle aktiven und wartenden Downloads. Die Warteschlange ist in drei Prioritätsstufen unterteilt:

- **Hoch** - Downloads, die bevorzugt verarbeitet werden
- **Normal** - Standard-Priorität
- **Niedrig** - Downloads, die zuletzt verarbeitet werden

Per Drag-and-Drop kannst du Downloads zwischen Prioritätsstufen verschieben und innerhalb einer Stufe umsortieren. Über das Kontextmenü (Rechtsklick) gibt es weitere Optionen:

- **Priorität ändern** - Verschiebt den Download in eine andere Stufe
- **Sofort starten** - Startet den Download unabhängig von der Warteschlange
- **Abbrechen** - Entfernt den Download aus der Warteschlange

Oben rechts kann die gesamte Download-Pipeline pausiert und wieder fortgesetzt werden. Wenn ein Zeitplan aktiv ist, wird das nächste Fenster angezeigt.

### Download-Detail

Klick auf eine Karte in der Warteschlange oder einen Eintrag im Verlauf offnet die Detailansicht (`/activity/:id`). Sie zeigt:

- **Kopfbereich** - Titel, Status-Badge (Phase/Abgeschlossen/Fehlgeschlagen), Sender-Badge, Untertitel-Indikator (SUB)
- **Fortschrittsbalken** - Heruntergeladene Bytes / Gesamtgrose, Geschwindigkeit und verbleibende Zeit (bei aktiven Downloads)
- **Details-Raster** - Kategorie, Grose, Phase (aktiv), Prioritat, Dateipfad (Verlauf), Dauer (Verlauf), Abschlusszeitpunkt (Verlauf)
- **Fehlermeldung** - Rot hervorgehoben bei fehlgeschlagenen Downloads

Verfugbare Aktionen je nach Status:

| Status | Aktionen |
|--------|----------|
| Aktiv/Wartend | Sofort starten, Loschen |
| Fehlgeschlagen | Erneut versuchen, Loschen |

Bei aktiven Downloads aktualisiert sich die Ansicht live uber Server-Sent Events.

### Verlauf

Tabellarische Übersicht aller abgeschlossenen und fehlgeschlagenen Downloads mit:

- Titel, Qualität, Dateigröße und Download-Dauer
- Status (Abgeschlossen/Fehlgeschlagen) mit Fehlermeldung bei Misserfolg
- Kategorie-Filter und Suchfeld
- Fehlgeschlagene Downloads können erneut gestartet werden

## Setup

Der Setup-Assistent führt in drei Schritten durch die Einrichtung:

### Schritt 1: System-Health-Check

Prüft automatisch acht Punkte:

| Prüfung | Was wird geprüft |
|----------|------------------|
| API-Schlüssel | Ob der Standard-Schlüssel geändert wurde |
| MediathekViewWeb | Erreichbarkeit der Mediathek-API |
| Datenverzeichnis | Schreibzugriff auf den Datenpfad |
| Complete-Verzeichnis | Schreibzugriff auf den Download-Ausgabepfad |
| Incomplete-Verzeichnis | Schreibzugriff auf den temporären Download-Pfad |
| Indexer-API | Ob die Newznab-API antwortet |
| Download-API | Ob die SABnzbd-API antwortet |
| FFmpeg | Ob FFmpeg im PATH gefunden wird |

Jeder Punkt zeigt einen Status (OK, Warnung, Fehler) mit Hinweis zur Behebung bei Problemen.

### Schritt 2: Dienste auswählen

Wähle aus, welche *arr-Apps du mit FunkArr verbinden möchtest:

- **Prowlarr** - Indexer-Verwaltung
- **Sonarr** - Serien-Verwaltung
- **Radarr** - Film-Verwaltung

### Schritt 3: Dienste konfigurieren

Für jeden ausgewählten Dienst gibt es zwei Optionen:

**Automatisch:** Gib die URL und den API-Schlüssel deines *arr-Dienstes ein. FunkArr erstellt den Indexer (und bei Sonarr/Radarr auch den Download-Client) automatisch per API-Aufruf.

**Manuell:** Klappt die manuellen Einstellungen auf, die alle nötigen Werte (Name, Host, Port, URL Base, API Key, Kategorie) zum Kopieren bereitstellen.

## Regelwerke

### Liste

Zeigt alle geladenen Regelwerke mit Suchfeld und Sortierung (nach Name oder ID). Filter nach:

- **Medientyp** - Serien oder Filme
- **Quelle** - Community, lokal oder zusammengeführt

Über den "Neu"-Button kannst du ein neues lokales Regelwerk erstellen.

### Detail

Zeigt alle Informationen zu einem einzelnen Regelwerk:

- **Identität** - Topic, Aliase, TVDB/IMDB/TMDB-IDs, Quelle (Community/Lokal/Zusammengeführt)
- **Anreicherung** - Ob TMDB/TVDB-Enrichment aktiviert ist und welche Methoden verwendet werden
- **Regeln** - Liste aller Matching-Regeln mit deren Konfiguration

Von hier aus gibt es Links zur Scoring-Historie und zum Editor.

### Builder

Ein visueller Editor zum Erstellen und Bearbeiten von Regelwerken. Die Seite ist zweigeteilt:

- **Links: Formular** - Identität (ID, Topic, Aliase, Medientyp, Metadaten-IDs), Konfidenz, und einzelne Regeln mit Strategie, Filtern und Titelmapping
- **Rechts: Live-Vorschau** - Zeigt in Echtzeit, welche Mediathek-Einträge die aktuelle Konfiguration matchen würde

Änderungen im Formular aktualisieren die Vorschau sofort, sodass du Regeln direkt gegen echte Mediathek-Daten testen kannst.

## Einstellungen

Die Einstellungen-Seite zeigt die aktuelle Konfiguration und System-Informationen in funf Bereichen.

### Downloads

Zeigt die Anzahl gleichzeitiger Downloads und den konfigurierten Zeitplan. Wenn Download-Zeitfenster definiert sind, werden die aktiven Perioden angezeigt.

### Metadaten-Cache

Statistiken zum TVDB- und TMDB-Cache: Anzahl der Eintrge und Alter des altesten Eintrags.

### Netzwerk-Routen

Zeigt die konfigurierten Routen (Name, Proxy-Adresse, Standard-Route) und die Sender-zu-Route-Zuordnungen.

### System

Allgemeine Systeminformationen:

- App-Version
- Community-Regelwerk-Version
- FFmpeg-Version
- API-Schlussel (maskiert)

### Logs

Echtzeit-Log-Viewer mit Server-Sent Events. Features:

- **Filterung nach Level** - Information, Warning, Error
- **Strukturierte Anzeige** - Zeitstempel, Level, Quell-Kontext und Nachricht pro Eintrag
- **Auto-Scroll** - Springt automatisch zu den neuesten Eintrgen
- **Puffer** - Zeigt die letzten 500 Eintrge

Beim Laden der Seite werden die letzten Eintrge per HTTP-Anfrage geladen, danach werden neue Eintrge live gestreamt.

## Scoring

### Historie

Erreichbar über das Regelwerk-Detail. Listet alle Scoring-Durchläufe für ein Regelwerk:

- Quelle (z.B. Sonarr, Radarr, manuelle Suche)
- Suchanfrage
- Zeitpunkt
- Anzahl der Kandidaten und getroffenen Matches

Klick auf einen Eintrag öffnet die Detailansicht.

### Detail

Zeigt den vollständigen Scoring-Trace für einen einzelnen Suchdurchlauf. Für jeden Mediathek-Kandidaten siehst du:

- Ob er gematcht wurde (mit Score und zugeordneter Regel-ID)
- Sender, Topic, Dauer, Qualität
- Aufklappbare Rule-Traces pro Regel mit dem Ergebnis (Matched, Filter fehlgeschlagen, kein Match) und den einzelnen Filterschritten
