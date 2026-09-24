# So funktioniert FunkArr

Diese Seite erklaert, wie FunkArr intern arbeitet: vom Eingang einer Suchanfrage ueber das Scoring und die Metadaten-Anreicherung bis zum fertigen Download.

## Ablauf einer Suche

Wenn Sonarr oder Radarr eine Suchanfrage an FunkArr senden, durchlaeuft diese mehrere Stufen:

### 1. Newznab-API empfaengt die Anfrage

Sonarr/Radarr senden eine Standard-Newznab-Anfrage an `/index/api`. FunkArr erkennt den Typ anhand des Parameters:

- `t=tvsearch` - Seriensuche (von Sonarr), mit optionaler TVDB-ID, Staffel und Episode
- `t=movie` - Filmsuche (von Radarr), mit optionaler IMDB-ID oder TMDB-ID
- `t=search` - Allgemeine Suche (von Prowlarr), Kategorie bestimmt den Typ

Aus der Anfrage wird ein interner Suchbefehl erstellt und an den SearchManager weitergeleitet.

### 2. SearchManager koordiniert die Suche

Der SearchManager entscheidet anhand der Kategorie, ob Serien, Filme oder beides gesucht wird:

- Kategorie 5000-5999: nur Serien
- Kategorie 2000-2999: nur Filme
- Keine Kategorie: beides parallel, Ergebnisse werden zusammengefuehrt

Jede Suche hat ein Timeout von 30 Sekunden. Wenn bei einer kombinierten Suche ein Teil fehlschlaegt, werden die Ergebnisse des anderen Teils trotzdem zurueckgegeben.

### 3. MediathekViewWeb-Abfrage

Der MediathekViewWebManager sendet die Suchanfrage an die MediathekViewWeb-API. Diese durchsucht die Mediathek-Datenbank (ARD, ZDF, ORF, SRF und weitere Sender).

Suchanfragen koennen nach Topic (Sendungsname), Titel und Beschreibung filtern. Die Ergebnisse enthalten fuer jeden Eintrag:

- Sender, Thema, Titel, Beschreibung
- Video-URLs in verschiedenen Qualitaeten (HD, normal, niedrig)
- Untertitel-URL (falls verfuegbar)
- Dauer, Groesse, Ausstrahlungszeitpunkt

FunkArr begrenzt die gleichzeitigen Abfragen an MediathekViewWeb auf 3, um die API nicht zu ueberlasten. Weitere Anfragen werden automatisch in eine Warteschlange gestellt.

### 4. Regelwerk-Zuordnung (RuleSet Matching)

Wenn eine TVDB-ID oder IMDB-ID in der Anfrage enthalten ist, loedt FunkArr das passende Regelwerk aus dem RuleSet-Store. Das Regelwerk bestimmt, wie Mediathek-Titel in strukturierte Staffel-/Episodenformate umgewandelt werden.

Ist kein Regelwerk vorhanden oder keine ID angegeben, werden die Mediathek-Ergebnisse direkt an das Scoring weitergeleitet.

### 5. Scoring

Die Scoring-Engine wertet jeden Mediathek-Eintrag gegen die Regeln des Regelwerks aus. Dabei werden drei Schritte fuer jede Regel durchlaufen (Regeln sind nach Prioritaet sortiert, die erste passende gewinnt):

**Filter-Pruefung:** Jede Regel kann Filter definieren, die ein Eintrag erfuellen muss. Filter koennen auf verschiedene Felder pruefen (Titel, Thema, Sender, Dauer, Beschreibung) mit Operatoren wie Gleichheit, Enthaelt, Regex, groesser/kleiner. Filter lassen sich mit `all` (alle muessen passen), `any` (einer muss passen) und `not` (keiner darf passen) kombinieren.

**Identifikation:** Wenn ein Eintrag die Filter besteht, versucht die Regel Staffel und Episode zu erkennen. Es gibt fuenf Strategien:

- `SeasonAndEpisodeNumber` - extrahiert Staffel und Episode per Regex aus dem Titel
- `AbsoluteEpisodeNumber` - extrahiert eine absolute Episodennummer per Regex
- `TitleExact` - rekonstruiert den erwarteten Titel aus Teilen und prueft auf exakte Uebereinstimmung
- `TitleIncludes` - prueft ob ein konstruierter Titel im Mediathek-Titel enthalten ist (mit Umlaut-Normalisierung)
- `AirdateExtraction` - extrahiert ein deutsches Datum aus dem Titel (z.B. "15.03.2026" oder "15. Maerz 2026")

**Bewertung:** Jeder Treffer bekommt einen Konfidenzwert (0.0-1.0). Dieser Wert stammt aus der Regel selbst oder dem Standard-Konfidenzwert des Regelwerks.

Das Scoring laeuft parallel ueber einen Pool von Worker-Aktoren. Die Poolgroesse ist konfigurierbar ueber `FunkArr__Scoring__PoolSize` (Standard: 4).

### 6. Metadaten-Anreicherung

Nach dem Scoring werden die Treffer mit externen Metadaten angereichert (siehe Abschnitt [Metadaten-Anreicherung](#metadaten-anreicherung)).

### 7. Newznab-Antwort

Die Ergebnisse werden als Newznab-RSS-Feed zurueckgegeben. Jeder Eintrag enthaelt:

- Titel im Format "Sendungsname S01E05 Episodentitel 1080p"
- Geschaetzte Dateigroesse
- Kategorie (TV oder Film, mit Qualitaetsstufe)
- Metadaten-Attribute (TVDB-ID, IMDB-ID, TMDB-ID, Staffel, Episode)
- NZB-Download-Link (enthaelt die Video-URL als Payload)

Suchergebnisse werden zwischengespeichert, damit eine Folgeanfrage mit Pagination nicht erneut die gesamte Suche ausloest.

## Download-Pipeline

Wenn Sonarr oder Radarr einen Download starten, senden sie eine SABnzbd-Anfrage an `/download/api`. FunkArr simuliert einen SABnzbd-Download-Client.

### Warteschlange

Der DownloadManager verwaltet eine persistente Warteschlange mit folgenden Funktionen:

- **Parallele Downloads:** Standardmaessig werden bis zu 3 Downloads gleichzeitig ausgefuehrt (konfigurierbar ueber `FunkArr__Download__ConcurrentDownloads`).
- **Prioritaeten:** Jeder Download hat eine Prioritaet. Downloads mit hoeherer Prioritaet werden bevorzugt abgearbeitet.
- **Pausieren/Fortsetzen:** Die gesamte Warteschlange kann pausiert und wieder fortgesetzt werden.
- **Force-Start:** Einzelne Downloads koennen sofort gestartet werden, auch wenn die maximale Anzahl paralleler Downloads bereits erreicht ist.
- **Umordnen:** Downloads koennen an eine bestimmte Position in der Warteschlange verschoben oder miteinander getauscht werden.
- **Loeschen:** Wartende oder aktive Downloads koennen aus der Warteschlange entfernt werden.
- **Wiederholen:** Fehlgeschlagene Downloads koennen erneut in die Warteschlange eingereiht werden.

Die Warteschlange ist persistent - sie ueberlebt Neustart des Containers. Aktive Downloads werden nach einem Neustart automatisch erneut gestartet.

### Download-Zeitplan

FunkArr unterstuetzt optionale Download-Zeitfenster. Wenn konfiguriert, werden Downloads nur waehrend der definierten Zeitfenster gestartet. Ausserhalb der Fenster bleiben sie in der Warteschlange. Neue Downloads werden automatisch gestartet, sobald das naechste Zeitfenster beginnt.

### FFmpeg-Remux

Jeder Download wird von einem DownloadWorker verarbeitet:

1. **Untertitel vorbereiten:** Wenn eine Untertitel-URL vorhanden ist, wird die Untertiteldatei heruntergeladen. FunkArr erkennt das Format automatisch:
   - **TTML/XML** - wird nach SRT konvertiert (ueber einen eigenen TTML-zu-SRT-Konverter)
   - **WebVTT** - wird direkt als VTT-Datei verwendet
   - **SRT** - wird direkt verwendet

2. **Video herunterladen und remuxen:** FFmpeg laedt das Video herunter (direkte URL oder HLS-Stream) und verpackt es als MKV-Container:
   - Video- und Audio-Codecs werden kopiert (keine Neucodierung)
   - Untertitel werden als SRT-Spur mit deutschem Sprach-Tag (`language=deu`) eingebettet
   - Der Fortschritt wird live ausgelesen (heruntergeladene Bytes, Zeitposition, Geschwindigkeit)

3. **Fertigstellung:** Die fertige Datei wird vom `incomplete/`-Verzeichnis in das `complete/`-Verzeichnis verschoben, organisiert nach Kategorie-Unterverzeichnis (z.B. `complete/tv/`).

### Download-Verlauf

Jeder abgeschlossene Download (erfolgreich oder fehlgeschlagen) wird im Download-Verlauf aufgezeichnet. Der Verlauf speichert Titel, Kategorie, Dateigroesse, Status, Fehlermeldung (bei Fehler), Download-Dauer und Abschlusszeitpunkt. Du kannst Verlaufseintraege einzeln entfernen.

### Live-Fortschritt

Die Download-Warteschlange kann ueber die API abgefragt werden. Fuer jeden aktiven Download werden angezeigt:

- Aktuell heruntergeladene Bytes
- Aktuelle Zeitposition im Video
- Download-Geschwindigkeit
- Gesamtdauer und -groesse

## Scoring-System

Das Scoring-System (auch "Match Intelligence" genannt) besteht aus zwei Komponenten: der Scoring-Engine und dem Scoring-Verlauf.

### Scoring-Engine

Die Scoring-Engine wertet Mediathek-Eintraege gegen Regelwerk-Regeln aus. Der Ablauf ist im Abschnitt [Scoring](#5-scoring) oben beschrieben.

Wichtig: Jede Auswertung erzeugt einen vollstaendigen Trace. Der Trace dokumentiert fuer jeden Eintrag und jede Regel:

- Welche Filter geprueft wurden und ob sie bestanden haben
- Welche Felder mit welchen Werten verglichen wurden
- Welche Identifikationsstrategie verwendet wurde
- Warum eine Identifikation fehlgeschlagen ist (z.B. "Regex hat nicht gematcht")
- Welche Regel am Ende gematcht hat und mit welchem Konfidenzwert

Diese Traces sind in der Web-UI einsehbar und helfen beim Debuggen von Regelwerken.

### Scoring-Verlauf

Jede Suchanfrage, die ueber ein Regelwerk ausgewertet wird, wird im Scoring-Verlauf des jeweiligen Regelwerks gespeichert. Pro Snapshot werden aufgezeichnet:

- Quelle der Suche (Sonarr, Radarr, Prowlarr oder Test)
- Suchbegriff
- Zeitstempel
- Anzahl der Kandidaten, Treffer und angereicherten Ergebnisse
- Vollstaendige Item-Traces

Aus den Snapshots berechnet FunkArr Statistiken pro Regelwerk:

- **Match-Rate:** Anteil der Mediathek-Eintraege, die von einer Regel erkannt wurden
- **Enrichment-Rate:** Anteil der erkannten Eintraege, die erfolgreich mit Metadaten angereichert wurden
- **Letzter Lauf:** Zeitpunkt der letzten Auswertung

Die Konfigurationsoptionen fuer den Verlauf:

| Variable | Standard | Beschreibung |
|----------|----------|-------------|
| `FunkArr__MatchHistory__MaxSnapshots` | `100` | Maximale Anzahl gespeicherter Snapshots pro Regelwerk |
| `FunkArr__MatchHistory__MaxAgeDays` | `30` | Snapshots aelter als diese Anzahl Tage werden geloescht |
| `FunkArr__MatchHistory__SnapshotInterval` | `20` | Intervall zwischen Snapshot-Persistierungen |

## Metadaten-Anreicherung

FunkArr kann Suchergebnisse mit externen Metadaten anreichern, um Staffel- und Episodennummern zu bestimmen, wenn das Regelwerk allein nicht ausreicht.

### TVDB (Serien)

Fuer Serien laedt FunkArr die Episodenliste von TVDB und versucht, Mediathek-Eintraege den richtigen Episoden zuzuordnen. Dabei werden zwei Methoden nacheinander versucht:

**Titel-Matching:** Vergleicht den Mediathek-Titel mit den TVDB-Episodennamen ueber Levenshtein-Distanz (Aehnlichkeitsmessung). Der Standard-Schwellwert liegt bei 0.7 (70% Aehnlichkeit). Bei Gleichstand zwischen mehreren Episoden wird die Laufzeit als Tiebreaker verwendet.

**Airdate-Matching:** Wenn der Mediathek-Eintrag ein Ausstrahlungsdatum hat, wird die TVDB-Episode mit dem naechsten passenden Datum gesucht. Standard-Toleranz: 7 Tage. Dieses Matching wird nur durchgefuehrt, wenn der beste Titel-Score mindestens 0.3 betraegt (Sicherheitsnetz gegen voellig falsche Zuordnungen).

Die TVDB-Episodenlisten werden zwischengespeichert. Serien mit zukuenftigen Episoden werden 2 Tage gecacht, abgeschlossene Serien 7 Tage.

Konfiguration: `FunkArr__Tvdb__ApiKey`

### TMDB (Filme)

Fuer Filme fragt FunkArr TMDB ab, um Filmdaten aufzuloesen (Titel, Erscheinungsjahr, IMDB-ID). Dabei werden auch alternative Titel beruecksichtigt. Die Zuordnung verwendet:

- **Titel-Aehnlichkeit** ueber Levenshtein-Distanz (Schwellwert: 0.5)
- **Jahres-Validierung** mit einer Standard-Toleranz von 1 Jahr

TMDB-Filmdaten werden 30 Tage zwischengespeichert.

Konfiguration: `FunkArr__Tmdb__ApiKey`

### Ohne API-Schluessel

Wenn keine API-Schluessel konfiguriert sind, verlaesst sich FunkArr ausschliesslich auf Regelwerk-Muster fuer die Zuordnung. Das funktioniert, liefert aber weniger und weniger genaue Ergebnisse.

## Videoqualitaet

Mediathek-Eintraege bieten Videos in bis zu drei Qualitaetsstufen an. FunkArr erstellt fuer jede verfuegbare Stufe einen eigenen Suchergebnis-Eintrag:

| Qualitaet | Quelle | Geschaetzte Bitrate | Beispiel 60 min |
|-----------|--------|---------------------|-----------------|
| 1080p (HD) | `url_video_hd` | ~6.5 Mbit/s | ~490 MB |
| 720p (Normal) | `url_video` | ~3.3 Mbit/s | ~250 MB |
| 480p (Niedrig) | `url_video_low` | ~0.8 Mbit/s | ~60 MB |

Nicht jeder Mediathek-Eintrag hat alle drei Qualitaeten. Wenn nur eine normale URL vorhanden ist, wird nur ein 720p-Ergebnis erstellt.

Die geschaetzte Dateigroesse wird aus der Videodauer und der typischen Bitrate pro Qualitaetsstufe berechnet. Wenn der Mediathek-Eintrag eine tatsaechliche Groesse meldet, wird diese stattdessen verwendet.

Sonarr und Radarr sehen die Qualitaet in der Newznab-Kategorie und im Titel (z.B. "Tatort S01E05 Der letzte Schrei 1080p"). Damit greifen die normalen Qualitaetsprofile und -praeferenzen.
