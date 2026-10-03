# Regelwerke

Mediathek-Titel sind chaotisch - „Tatort"-Episoden erscheinen z.B. als „Tatort: Der letzte Schrei" ohne Staffel- oder Episodennummer. Regelwerke ordnen diese Titel einem strukturierten Staffel-/Episodenformat zu, damit Sonarr sie zuordnen kann.

## Funktionsweise

Ein Regelwerk ist eine JSON-Datei, die Regeln für eine bestimmte Sendung oder einen Film definiert. Jede Regel gleicht Mediathek-Einträge anhand von Thema und Titelmustern ab und extrahiert Staffel-/Episodeninformationen.

FunkArr lädt Regelwerke aus zwei Quellen:

1. **Community-Regelwerke** - automatisch von GitHub-Releases synchronisiert, decken die beliebtesten Sendungen und Filme ab. Siehe den [Katalog](./catalog) für die vollständige Liste.
2. **Lokale Regelwerke** - im Regelwerk-Builder der Web-Oberfläche erstellt, unter `data/rulesets/local/` gespeichert. Lokale Regelwerke werden mit dem Community-Regelwerk derselben ID zusammengeführt (siehe [Eigene Regelwerke](./custom)).

## Automatische Updates

Beim Start und danach alle 30 Minuten fragt FunkArr die GitHub-Releases des Repositorys (`FunkArr__RuleSet__Repository`, Standard `st0o0/funkarr`) ab und sucht ein Release mit dem Tag `rulesets-v<Version>`. Weicht die Version von der lokal installierten ab (`data/rulesets/version.txt`), lädt FunkArr das Release-Asset `rulesets.zip` herunter, ersetzt den Ordner `data/rulesets/community/` und liest alle Regelwerke neu ein. Änderungen an Dateien in den Regelwerk-Ordnern werden ebenfalls automatisch erkannt.

| Variable | Standard | Beschreibung |
|----------|----------|-------------|
| `FunkArr__RuleSet__Repository` | `st0o0/funkarr` | GitHub-Repository für Community-Regelwerke |
| `FunkArr__RuleSet__Version` | `latest` | Bestimmte Version festlegen oder `latest` |
| `FunkArr__RuleSet__RefreshEnabled` | `true` | Mit `false` werden automatische Updates deaktiviert |
