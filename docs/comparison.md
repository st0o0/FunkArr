# Vergleich mit Alternativen

Drei Projekte loesen dasselbe Problem: Inhalte aus deutschsprachigen oeffentlich-rechtlichen Mediatheken in Sonarr und Radarr verfuegbar machen. Alle drei stellen eine Newznab-Indexer-API und eine SABnzbd-Download-Client-API bereit, sodass die *arr-Apps sie wie eine normale Usenet-Quelle behandeln.

## Kurzvergleich

|                        | FunkArr              | MediathekArr           | RundfunkArr             |
|------------------------|----------------------|------------------------|-------------------------|
| **Tech-Stack**         | .NET 10 / Akka.NET   | .NET (C#)              | TypeScript / Next.js    |
| **Status**             | Aktiv                | Beta (letztes Release Feb 2025) | Aktiv (v1.3.0, Sep 2026) |
| **Mediatheken**        | ARD, ZDF, ORF, SRF + weitere | ARD, ZDF, ORF, SRF (Beta) | ARD, ZDF, ORF, SRF     |
| **Sonarr**             | Ja                   | Ja                     | Ja                      |
| **Radarr**             | Ja                   | Eingeschraenkt (wenige Filme) | Ja                |
| **Regelwerke**         | Community + eigene   | Nur internes Matching  | JSON-Regelwerke (PR-basiert) |
| **Metadaten**          | TMDB + TVDB          | TVDB + UmlautAdaptarr  | TVDB + TMDB             |
| **Download**           | FFmpeg (HLS + direkt)| Direkter HTTP-Download | Direkter HTTP-Download + yt-dlp |
| **Ausgabe**            | MKV (Remux, keine Neucodierung) | MKV         | MKV (optionales FFmpeg) |
| **Untertitel**         | SRT aus HLS oder separater Download | Ja        | Ja                      |
| **Datenbank**          | SQLite oder PostgreSQL | SQLite                | SQLite (Prisma)         |
| **Web-UI**             | Ja (Vue.js)          | Ja (Setup-Assistent)   | Ja (Next.js)            |
| **Docker**             | Ein Container        | x86 + ARM64            | Multi-Arch (amd64, arm64) |
| **Port**               | 6969                 | 5007                   | 6767                    |

## MediathekArr

[MediathekArr](https://github.com/PCJones/MediathekArr) von PCJones ist das populaerste Projekt in diesem Bereich (370+ Stars). Es verwendet ein .NET-Backend mit einer Mehrkomponenten-Architektur (MediathekArr, MediathekArrLib, MediathekArrServer).

### Staerken

- Groesste Community mit aktivem Discord- und Telegram-Kanal
- UmlautAdaptarr als Begleittool, das deutsche Umlaut-Variationen in Titeln im gesamten *arr-Oekosystem behandelt
- Dreistufiges Match-Konfidenzsystem (CERTAIN / UNCERTAIN / NO.MATCH), das die Sicherheit jedes Ergebnisses anzeigt
- Auto-Konfigurationsassistent, der Indexer und Download-Clients in Sonarr/Prowlarr automatisch einrichten kann

### Unterschiede zu FunkArr

- **Kein Community-Regelwerk-System.** Das Titel-Matching verwendet eingebaute Logik mit drei Konfidenzstufen. Funktioniert gut fuer gaengige Sendungen, kann aber von Nutzern nicht fuer Nischeninhalte erweitert werden.
- **Radarr-Filmunterstuetzung ist eingeschraenkt.** Die interaktive Suche findet einige Filme, automatische Grabs sind aber unzuverlaessig.
- **ORF- und SRF-Unterstuetzung** wurde in beta.12 (Februar 2025) ueber M3U-Download hinzugefuegt, ist aber weniger ausgereift als ARD/ZDF.
- **Benoetigt UmlautAdaptarr** als separaten Begleitdienst fuer korrekte deutsche Titelaufloesung. FunkArr behandelt das nativ.
- **Noch in der Beta** nach ueber 2 Jahren. Das letzte Release (beta.12) enthielt Sicherheitsfixes fuer Command-Injection- und Path-Traversal-Schwachstellen.
- **Kein TMDB.** Nutzt nur TVDB (mit 12h-Caching fuer fehlgeschlagene Abfragen).
- **Kein PostgreSQL.** Die Datenbank ist nicht konfigurierbar.
- **Kein Match-Verlauf oder Scoring.** Jede Suche startet ohne Lerneffekt aus frueheren Ergebnissen.

## RundfunkArr

[RundfunkArr](https://github.com/rundfunkarr/rundfunkarr) ist ein TypeScript/Next.js-Projekt mit einem anderen architektonischen Ansatz.

### Staerken

- Verwendet yt-dlp fuer die HLS-Stream-Aufloesung, was SRF- und ORF-Streams gut handhabt
- Optionale Proxy-Unterstuetzung fuer regionsbeschraenkte Inhalte (nuetzlich fuer SRF/ORF-Zugriff ausserhalb der Schweiz/Oesterreich)
- Sauberes Next.js App Router UI mit direkter Mediathek-Suche, Download-Warteschlange und gefuehrtem Setup-Assistenten
- Aktive Entwicklung mit regelmaessigen Releases, taeglichen Docker-Nightly-Builds und null offenen Issues
- PUID/PGID-Unterstuetzung in Docker fuer korrekte Dateiberechtigungen
- Hierarchische Metadaten-Suche: lokale shows.json, dann TVDB, dann TMDB

### Unterschiede zu FunkArr

- **Regelwerke in einer einzelnen Datei.** Gespeichert als `rulesets.json` mit Regex-basierter Episoden-/Staffel-Erkennung. Neue Regeln hinzufuegen erfordert einen PR zum Repository statt eines eigenstaendigen Release-Zyklus oder Web-UI-Editors.
- **Metadaten-Aufloesung** prueft zuerst eine lokale `shows.json`, dann TVDB/TMDB. Funktioniert gut fuer bekannte Sendungen, erfordert aber manuelle Updates fuer neue.
- **SRF- und ORF-Streams** werden erst beim Download ueber yt-dlp aufgeloest, nicht bei der Suche. Suchergebnisse koennen daher Eintraege enthalten, deren Download fehlschlaegt, wenn die Stream-URL abgelaufen ist.
- **Kein Scoring oder Match-Intelligence.** Ergebnisse basieren auf dem aktuellen Regelwerk-Match ohne historische Gewichtung.
- **Node.js-Laufzeit** hat hoehere Basis-Speichernutzung im Vergleich zu .NET.
- War urspruenglich ebenfalls "MediathekArr" genannt, wurde aber umbenannt, um Verwechslungen mit dem Projekt von PCJones zu vermeiden.

## Welches Projekt fuer welchen Einsatz?

- **FunkArr** wenn du Community-getriebene Regelwerke mit visuellem Editor brauchst, Match-Scoring das sich ueber die Zeit verbessert, PostgreSQL fuer groessere Setups oder zuverlaessige ORF/SRF-Abdeckung.
- **MediathekArr** wenn du hauptsaechlich ARD/ZDF-Inhalte schaust und die groesste Community fuer Support willst.
- **RundfunkArr** wenn du einen Node.js-Stack bevorzugst, Proxy-Unterstuetzung fuer regionsbeschraenkte Inhalte brauchst oder yt-dlps breitere Formatunterstuetzung nutzen moechtest.
