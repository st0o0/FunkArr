# Regelwerke

Mediathek-Titel sind chaotisch — „Tatort"-Episoden erscheinen z.B. als „Tatort: Der letzte Schrei" ohne Staffel- oder Episodennummer. Regelwerke ordnen diese Titel einem strukturierten Staffel-/Episodenformat zu, damit Sonarr sie zuordnen kann.

## Funktionsweise

Ein Regelwerk ist eine JSON-Datei, die Regeln für eine bestimmte Sendung oder einen Film definiert. Jede Regel gleicht Mediathek-Einträge anhand von Thema und Titelmustern ab und extrahiert Staffel-/Episodeninformationen.

FunkArr lädt Regelwerke aus zwei Quellen:

1. **Community-Regelwerke** — automatisch von GitHub-Releases synchronisiert, decken die beliebtesten Sendungen und Filme ab. Siehe den [Katalog](./catalog) für die vollständige Liste.
2. **Lokale Regelwerke** — im Regelwerk-Builder der Web-Oberfläche erstellt, in deinem Datenverzeichnis gespeichert. Lokale Regeln überschreiben Community-Regeln für dieselbe Sendung.

## Automatische Updates

Die Community-Regelwerke werden alle 30 Minuten auf neue Versionen geprüft. Wenn ein neues Release auf GitHub veröffentlicht wird, lädt FunkArr das Update automatisch herunter und wendet es an. Du kannst bei Bedarf eine bestimmte Version über `FunkArr__RuleSet__Version` festlegen.
