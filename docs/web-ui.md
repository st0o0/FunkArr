# Web-Oberflaeche

FunkArr hat eine Web-Oberflaeche auf Vue.js-Basis. Nach dem Start ist sie unter dem konfigurierten Port erreichbar (Standard: `http://localhost:6969`). Die Seitenleiste links fuehrt zu allen Bereichen.

## Dashboard

Die Startseite zeigt vier Kacheln auf einen Blick:

- **Systemstatus** - Ampel-Indikator (gruen/gelb/rot) basierend auf den Health-Checks. Klick fuehrt zum Setup.
- **Letzte Downloads** - Anzahl der letzten abgeschlossenen Downloads. Klick fuehrt zur Download-Historie.
- **Speicher** - Belegter und verfuegbarer Speicherplatz im Complete-Verzeichnis mit Fortschrittsbalken.
- **Regelwerke** - Gesamtzahl der geladenen Regelwerke und die installierte Community-Regelwerk-Version.

Wenn Downloads aktiv sind, zeigt ein Fortschrittsbalken die aktuelle Gesamtgeschwindigkeit und die Anzahl wartender Downloads.

Darunter listet der Bereich "Letzte Aktivitaet" die letzten 10 Downloads mit Status, Groesse und Zeitpunkt.

## Aktivitaet

Die Aktivitaets-Seite hat zwei Tabs: **Warteschlange** und **Verlauf**.

### Warteschlange

Zeigt alle aktiven und wartenden Downloads. Die Warteschlange ist in drei Prioritaetsstufen unterteilt:

- **Hoch** - Downloads, die bevorzugt verarbeitet werden
- **Normal** - Standard-Prioritaet
- **Niedrig** - Downloads, die zuletzt verarbeitet werden

Per Drag-and-Drop kannst du Downloads zwischen Prioritaetsstufen verschieben und innerhalb einer Stufe umsortieren. Ueber das Kontextmenue (Rechtsklick) gibt es weitere Optionen:

- **Prioritaet aendern** - Verschiebt den Download in eine andere Stufe
- **Sofort starten** - Startet den Download unabhaengig von der Warteschlange
- **Abbrechen** - Entfernt den Download aus der Warteschlange

Oben rechts kann die gesamte Download-Pipeline pausiert und wieder fortgesetzt werden. Wenn ein Zeitplan aktiv ist, wird das naechste Fenster angezeigt.

### Verlauf

Tabellarische Uebersicht aller abgeschlossenen und fehlgeschlagenen Downloads mit:

- Titel, Qualitaet, Dateigroesse und Download-Dauer
- Status (Abgeschlossen/Fehlgeschlagen) mit Fehlermeldung bei Misserfolg
- Kategorie-Filter und Suchfeld
- Fehlgeschlagene Downloads koennen erneut gestartet werden

## Setup

Der Setup-Assistent fuehrt in drei Schritten durch die Einrichtung:

### Schritt 1: System-Health-Check

Prueft automatisch acht Punkte:

| Pruefung | Was wird geprueft |
|----------|------------------|
| API-Schluessel | Ob der Standard-Schluessel geaendert wurde |
| MediathekViewWeb | Erreichbarkeit der Mediathek-API |
| Datenverzeichnis | Schreibzugriff auf den Datenpfad |
| Complete-Verzeichnis | Schreibzugriff auf den Download-Ausgabepfad |
| Incomplete-Verzeichnis | Schreibzugriff auf den temporaeren Download-Pfad |
| Indexer-API | Ob die Newznab-API antwortet |
| Download-API | Ob die SABnzbd-API antwortet |
| FFmpeg | Ob FFmpeg im PATH gefunden wird |

Jeder Punkt zeigt einen Status (OK, Warnung, Fehler) mit Hinweis zur Behebung bei Problemen.

### Schritt 2: Dienste auswaehlen

Waehle aus, welche *arr-Apps du mit FunkArr verbinden moechtest:

- **Prowlarr** - Indexer-Verwaltung
- **Sonarr** - Serien-Verwaltung
- **Radarr** - Film-Verwaltung

### Schritt 3: Dienste konfigurieren

Fuer jeden ausgewaehlten Dienst gibt es zwei Optionen:

**Automatisch:** Gib die URL und den API-Schluessel deines *arr-Dienstes ein. FunkArr erstellt den Indexer (und bei Sonarr/Radarr auch den Download-Client) automatisch per API-Aufruf.

**Manuell:** Klappt die manuellen Einstellungen auf, die alle noetigen Werte (Name, Host, Port, URL Base, API Key, Kategorie) zum Kopieren bereitstellen.

## Regelwerke

### Liste

Zeigt alle geladenen Regelwerke mit Suchfeld und Sortierung (nach Name oder ID). Filter nach:

- **Medientyp** - Serien oder Filme
- **Quelle** - Community, lokal oder zusammengefuehrt

Ueber den "Neu"-Button kannst du ein neues lokales Regelwerk erstellen.

### Detail

Zeigt alle Informationen zu einem einzelnen Regelwerk:

- **Identitaet** - Topic, Aliase, TVDB/IMDB/TMDB-IDs, Quelle (Community/Lokal/Zusammengefuehrt)
- **Anreicherung** - Ob TMDB/TVDB-Enrichment aktiviert ist und welche Methoden verwendet werden
- **Regeln** - Liste aller Matching-Regeln mit deren Konfiguration

Von hier aus gibt es Links zur Scoring-Historie und zum Editor.

### Builder

Ein visueller Editor zum Erstellen und Bearbeiten von Regelwerken. Die Seite ist zweigeteilt:

- **Links: Formular** - Identitaet (ID, Topic, Aliase, Medientyp, Metadaten-IDs), Konfidenz, und einzelne Regeln mit Strategie, Filtern und Titelmapping
- **Rechts: Live-Vorschau** - Zeigt in Echtzeit, welche Mediathek-Eintraege die aktuelle Konfiguration matchen wuerde

Aenderungen im Formular aktualisieren die Vorschau sofort, sodass du Regeln direkt gegen echte Mediathek-Daten testen kannst.

## Scoring

### Historie

Erreichbar ueber das Regelwerk-Detail. Listet alle Scoring-Durchlaeufe fuer ein Regelwerk:

- Quelle (z.B. Sonarr, Radarr, manuelle Suche)
- Suchanfrage
- Zeitpunkt
- Anzahl der Kandidaten und getroffenen Matches

Klick auf einen Eintrag oeffnet die Detailansicht.

### Detail

Zeigt den vollstaendigen Scoring-Trace fuer einen einzelnen Suchdurchlauf. Fuer jeden Mediathek-Kandidaten siehst du:

- Ob er gematcht wurde (mit Score und zugeordneter Regel-ID)
- Sender, Topic, Dauer, Qualitaet
- Aufklappbare Rule-Traces pro Regel mit dem Ergebnis (Matched, Filter fehlgeschlagen, kein Match) und den einzelnen Filterschritten
