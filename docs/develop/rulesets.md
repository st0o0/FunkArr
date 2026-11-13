# Regelwerke

Regelwerke bringen FunkArr bei, aus Mediathek-Titeln Staffel- und Episodennummern oder einen Film-Treffer zu machen. Ein Regelwerk für eine Serie, die du schaust, ist einer der nützlichsten Beiträge, und es braucht kein C#. Wenn du eine Serie nur anfragen willst, nimm die [Regelwerk-Anfrage](https://github.com/st0o0/funkarr/issues/new?template=ruleset_request.yml).

## Wo sie liegen

```
data/community/
├── rulesets/              eine JSON-Datei pro Serie oder Film, z. B. babylon-berlin.json
├── ruleset.schema.json    Schema, das jedes Regelwerk erfüllen muss
├── CHANGELOG.md           Release-Notes der Regelwerke (generiert)
└── version.txt
```

Der Dateiname ohne `.json` ist die Regelwerk-ID. Was jedes Feld bedeutet, steht in der Anleitung: [Feld-Referenz](/rulesets/field-reference), [Strategien](/rulesets/strategies), [Filter](/rulesets/filters), [Eigene Regelwerke](/rulesets/custom).

## Eins hinzufügen oder korrigieren

1. Schau in den [Community-Katalog](/rulesets/catalog). Vielleicht gibt es die Serie schon und das Regelwerk braucht nur eine Korrektur.
2. Bau die Regeln in der Web-Oberfläche (*Regelwerke → + Neu*, mit dem Debugger gegen echte Daten) oder mit dem [Online-Builder](/rulesets/builder) und exportiere das JSON.
3. Leg die Datei in `data/community/rulesets/` ab, mit TVDB-ID (Serien) oder TMDB-ID (Filme), denn Sonarr und Radarr gleichen darüber ab.
4. Öffne einen Pull Request mit dem Titel `feat(rulesets): add <show>` oder `fix(rulesets): <what changed>`. Nenn zwei oder drei echte Mediathek-Titel und die Episode, auf die sie passen sollen.

Die CI prüft jede Datei gegen das Schema. Lokal:

```bash
npx -p ajv-cli -p ajv-formats ajv validate -s data/community/ruleset.schema.json \
  -d "data/community/rulesets/*.json" --spec=draft2020 --strict=false
```

## Wie sie zu den Nutzern kommen

Community-Regelwerke werden getrennt von FunkArr veröffentlicht. Ein Merge erzeugt ein `rulesets`-Release mit `rulesets.zip`, und jede laufende Instanz holt es über den `RuleSetUpdater` ab, ohne Container-Update.

<ArchitectureView view="ruleset-sync" />

Der Katalog auf dieser Seite wird aus denselben JSON-Dateien gebaut und aktualisiert sich mit dem nächsten Doku-Deploy.
