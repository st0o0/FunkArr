# Web-Oberfläche

FunkArr hat eine Web-Oberfläche auf Vue.js-Basis. Nach dem Start ist sie unter dem konfigurierten Port erreichbar (Standard: `http://localhost:6969`). Die Seitenleiste links führt zu den Hauptbereichen: **Übersicht**, **Aktivität**, **RuleSets** und **Einstellungen**. **Setup** befindet sich unten in der Seitenleiste, daneben eine Sprachauswahl (English, Deutsch, Österreichisch, Schwizerdütsch), die App-Version und ein Button zum Einklappen der Seitenleiste.

## Übersicht

Die Startseite zeigt vier Kacheln auf einen Blick:

- **System** - Ampel-Indikator (grün/gelb/rot) basierend auf den Health-Checks, mit kurzer Zusammenfassung (Fehlerfrei, Anzahl der Warnungen oder Probleme). Klick führt zum Setup.
- **Letzte Downloads** - Anzahl der zuletzt abgeschlossenen Downloads (aus den letzten 10 Verlaufseinträgen). Klick führt zum Download-Verlauf.
- **Speicher** - Belegter und gesamter Speicherplatz im Complete-Verzeichnis mit Fortschrittsbalken (ab 90 % gelb).
- **Regelwerke** - Gesamtzahl der geladenen Regelwerke und die installierte Community-Regelwerk-Version. Klick führt zur Regelwerk-Liste.

Wenn Downloads aktiv oder in der Warteschlange sind, zeigt ein Fortschrittsbalken die Anzahl aktiver und wartender Downloads, die aktuelle Gesamtgeschwindigkeit und den Gesamtfortschritt, mit Link zur Aktivitäts-Seite.

Darunter listet der Bereich "Letzte Aktivität" die letzten 10 Downloads mit Status, Größe und Zeitpunkt.

## Aktivität

Die Aktivitäts-Seite hat zwei Tabs: **Warteschlange** und **Verlauf**. Ein Suchfeld oben rechts filtert nach Titel, und neben dem Seitentitel wird während laufender Downloads die aktuelle Gesamtgeschwindigkeit angezeigt. Die alten Pfade `/queue` und `/history` leiten hierher weiter.

### Warteschlange

Zeigt alle aktiven und wartenden Downloads:

- **Aktiv** - Laufende Downloads mit Gesamt-Fortschrittsbalken und einer Karte pro Download (Phase: Download/Remuxing, Geschwindigkeit, Fortschritt).
- **Prioritätsbereiche** - Wartende Downloads, unterteilt in drei Prioritätsstufen:
  - **Hoch** - Downloads, die bevorzugt verarbeitet werden
  - **Normal** - Standard-Priorität
  - **Niedrig** - Downloads, die zuletzt verarbeitet werden

Per Drag-and-Drop kannst du wartende Downloads zwischen Prioritätsstufen verschieben und innerhalb einer Stufe umsortieren. Über das Kontextmenü (Rechtsklick) gibt es weitere Optionen:

- **Priorität** - Verschiebt den Download in eine andere Stufe
- **Sofort starten** - Startet den Download unabhängig von der Warteschlange (nur für wartende Downloads)
- **Löschen** - Entfernt den Download aus der Warteschlange (auch bei aktiven Downloads möglich)

Oben rechts im Tab kann die gesamte Download-Pipeline pausiert und wieder fortgesetzt werden. Während der Pause erscheint ein "Pausiert"-Hinweis. Ist ein Zeitplan konfiguriert und das Zeitfenster gerade geschlossen, wird das nächste Fenster ("Nächstes: ...") angezeigt.

### Download-Detail

Klick auf eine Karte in der Warteschlange oder eine Zeile im Verlauf öffnet die Detailansicht (`/activity/:id`). Sie zeigt:

- **Kopfbereich** - Titel, Status-Badge (aktuelle Phase/Wartend, Abgeschlossen oder Fehlgeschlagen), Sender-Badge, Untertitel-Indikator (SUB)
- **Fortschrittsbalken** - Heruntergeladene Bytes / Gesamtgröße, Geschwindigkeit und verbleibende Zeit (bei aktiven Downloads)
- **Details-Raster** - Kategorie, Größe, Phase (aktiv), Priorität, Dateipfad (Verlauf), Dauer (Verlauf), Abschlusszeitpunkt (Verlauf)
- **Fehlermeldung** - Rot hervorgehoben bei fehlgeschlagenen Downloads

Verfügbare Aktionen je nach Status:

| Status | Aktionen |
|--------|----------|
| Aktiv/Wartend | Sofort starten, Löschen |
| Fehlgeschlagen | Erneut versuchen, Löschen |
| Abgeschlossen | Löschen |

Bei Downloads in der Warteschlange aktualisiert sich die Ansicht live über Server-Sent Events.

### Verlauf

Tabellarische Übersicht aller abgeschlossenen und fehlgeschlagenen Downloads mit:

- Titel, Qualität, Dateigröße, Download-Dauer und Abschlusszeitpunkt
- Status (Abgeschlossen/Fehlgeschlagen) mit aufklappbarer Fehlermeldung bei Misserfolg
- Kategorie-Filter; das Suchfeld filtert die Titel auf der aktuellen Seite
- Seitenweise Anzeige (25 Einträge pro Seite)
- Pro Zeile: Fehlgeschlagene Downloads können erneut gestartet werden, jeder Eintrag kann aus dem Verlauf gelöscht werden

Klick auf eine Zeile öffnet das Download-Detail.

## Setup

Der Setup-Assistent führt durch die Einrichtung. Er beginnt mit dem Health-Check und der Dienst-Auswahl, danach folgt ein Schritt pro ausgewähltem Dienst. Abgeschlossene Schritte lassen sich über die Schrittleiste erneut öffnen.

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
| FFmpeg | Ob FFmpeg im PATH gefunden wird (mit Version) |

Jeder Punkt zeigt einen Status (OK, Warnung, Fehler) mit Hinweis zur Behebung bei Fehlern. "Erneut prüfen" führt die Prüfungen noch einmal aus. Weiter geht es nur, solange keine Prüfung fehlgeschlagen ist.

### Schritt 2: Dienste auswählen

Wähle aus, welche *arr-Apps du mit FunkArr verbinden möchtest (mindestens eine):

- **Prowlarr** - Indexer-Verwaltung, fügt FunkArr als Newznab-Indexer hinzu
- **Sonarr** - Serien-Verwaltung, fügt FunkArr als SABnzbd-Download-Client hinzu
- **Radarr** - Film-Verwaltung, fügt FunkArr als SABnzbd-Download-Client hinzu

### Schritt 3+: Dienste konfigurieren

Jeder ausgewählte Dienst bekommt einen eigenen Schritt mit zwei Optionen:

**Automatisch:** Gib die URL und den API-Schlüssel deines *arr-Dienstes ein. Im optionalen Feld "FunkArr-URL" kannst du angeben, wie der Dienst FunkArr erreicht (leer lassen, wenn es dasselbe Netzwerk wie dein Browser ist). Der Button **Indexer erstellen** (bei Sonarr/Radarr zusätzlich **Download-Client erstellen**) legt die Ressourcen automatisch per API-Aufruf an. Fehler werden direkt angezeigt.

**Manuell:** "Manuell konfigurieren" klappt alle nötigen Werte (Name, Host, Port, URL Base, API Key, Kategorie) zum Kopieren auf, dazu Hinweise wie *Show Advanced* in der *arr-App zu aktivieren und die Verbindung zu testen. Bei Sonarr gibt es außerdem einen Tipp zum Serientyp *Daily* für Sendungen, die per Ausstrahlungsdatum identifiziert werden.

## Regelwerke

Der alte Pfad `/search` leitet zur Regelwerk-Liste weiter.

### Liste

Zeigt alle geladenen Regelwerke mit Suchfeld und Sortierung (nach Name oder ID). Filter-Tabs mit Anzahl:

- **Medientyp** - Alle, Serien oder Filme
- **Quelle** - Alle Quellen, Community oder Lokal (lokal schließt zusammengeführte Regelwerke ein); wird nur angezeigt, wenn mehr als eine Quelle vorhanden ist

Jeder Eintrag zeigt Topic, Aliase, Anzahl der Regeln, Metadaten-IDs, Zeitpunkt des letzten Scorings, Match-Rate und Anreicherungsrate. Über den "+ Neu"-Button kannst du ein neues lokales Regelwerk erstellen.

### Detail

Zeigt alle Informationen zu einem einzelnen Regelwerk, mit Breadcrumb zurück zur Liste:

- **Identität** - RuleSet-ID, Aliase, TVDB/IMDB/TMDB-IDs, Quelle (Community, Lokal oder Community + Lokal) und Datum der letzten Community-Aktualisierung
- **Anreicherung** - Ob TMDB/TVDB-Enrichment aktiviert ist und welche Methoden und Toleranzen verwendet werden
- **Matching-Regeln** - Standard-Konfidenz und Liste aller Regeln (ID, Strategie, Priorität, Konfidenz, Anzahl Titelteile und Filter). Regeln lassen sich aufklappen und zeigen Regexe, Titelteile und Filter.

Über Buttons oben gelangst du zur **Scoring-Historie** und zum **Editor**. Bei Regelwerken mit lokaler Datei gibt es zusätzlich **Für Community exportieren** (validiert und exportiert das Regelwerk für das Community-Repository, Validierungsfehler werden aufgelistet) und **Lokal löschen** (entfernt das lokale Overlay nach Bestätigung).

### Builder

Ein visueller Editor zum Erstellen (`/rulesets/new`) und Bearbeiten (`/rulesets/:id/edit`) von Regelwerken. Die Seite ist zweigeteilt:

- **Links: Formular** - Identität (ID in Kebab-Case, beim Bearbeiten gesperrt, Topic, Aliase, Medientyp, Metadaten-IDs), Standard-Konfidenz, einzelne Matching-Regeln (ID, Priorität, Konfidenz, Strategie, Regexe, Titelregeln, Filter) und die Anreicherungs-Einstellungen (Methoden, Schwellwerte, Toleranzen). Validierungsfehler werden oben und beim Speichern aufgelistet.
- **Rechts: Live-Vorschau** - Lädt bis zu 30 Mediathek-Einträge zum eingegebenen Topic (automatisch nach kurzer Verzögerung oder per "Aktualisieren") und zeigt in Echtzeit, welche davon die aktuellen Regeln matchen würden.

Die Live-Vorschau ist nur eine Näherung. Der Button **Vollständiger Test** führt das tatsächliche Scoring und die Anreicherung für die Kandidaten aus und zeigt die vollständigen Ergebnisse: pro Kandidat die Regel-Pipeline (gematcht, Filter fehlgeschlagen, Identifikation fehlgeschlagen, übersprungen), die Identifikation und das Anreicherungsergebnis (z.B. Titel-Match, Ausstrahlungsdatum-Match, Jahr-Match). "Zurück zur Live-Vorschau" wechselt zurück zur schnellen Ansicht.

## Einstellungen

Die Einstellungen-Seite zeigt die aktuelle Konfiguration und System-Informationen in fünf Bereichen (nur lesend).

### Downloads

Zeigt die Anzahl gleichzeitiger Downloads und den konfigurierten Zeitplan. Wenn Download-Zeitfenster definiert sind, werden die aktiven Perioden angezeigt, andernfalls "Immer aktiv".

### Metadaten-Cache

Statistiken zum TVDB- und TMDB-Cache: Anzahl der Einträge und Alter des ältesten Eintrags.

### Netzwerk-Routen

Zeigt die konfigurierten Routen (Name, Proxy-Adresse oder "Direktverbindung", Standard-Route) und die Sender-zu-Route-Zuordnungen.

### System

Allgemeine Systeminformationen:

- App-Version
- Community-Regelwerk-Version
- FFmpeg-Version
- API-Schlüssel (maskiert)

### Logs

Echtzeit-Log-Viewer mit Server-Sent Events. Features:

- **Filterung nach Level** - Information, Warning, Error (Umschalt-Buttons)
- **Strukturierte Anzeige** - Uhrzeit, Level, Quell-Kontext und Nachricht pro Eintrag
- **Auto-Scroll** - Springt automatisch zu den neuesten Einträgen
- **Puffer** - Zeigt die letzten 500 Einträge

Beim Laden der Seite werden die letzten Einträge per HTTP-Anfrage geladen, danach werden neue Einträge live gestreamt.

## Scoring

### Historie

Erreichbar über das Regelwerk-Detail (`/rulesets/:id/history`). Listet alle Scoring-Durchläufe für ein Regelwerk:

- Quelle (Sonarr, Radarr, Prowlarr oder Test)
- Suchanfrage
- Zeitpunkt
- Anzahl der Kandidaten und getroffenen Matches

Die Einträge werden seitenweise angezeigt. Klick auf einen Eintrag öffnet die Detailansicht.

### Detail

Zeigt den vollständigen Scoring-Trace für einen einzelnen Suchdurchlauf. Ein Filter schaltet zwischen allen, gematchten und nicht gematchten Kandidaten um (mit Anzahl). Für jeden Mediathek-Kandidaten siehst du:

- Ob er gematcht wurde, den Score und die zugeordnete Regel-ID
- Sender, Topic, Dauer, Qualität
- Aufklappbare Rule-Traces pro Regel mit Priorität, Ergebnis (gematcht, Filter fehlgeschlagen, Identifikation fehlgeschlagen), Filter-Trace und Identifikationsschritt
