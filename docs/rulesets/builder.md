# Regelwerk-Builder

Erstelle visuell ein FunkArr-Regelwerk und exportiere es als valides JSON - ohne laufende FunkArr-Instanz. Der Builder läuft vollständig im Browser; es werden keine Daten an einen Server gesendet.

<RulesetBuilder />

## So funktioniert der Builder

Links befindet sich das Formular, rechts die Live-Vorschau des erzeugten JSON. Jede Änderung wird sofort in die Vorschau übernommen.

### Identität und Media

| Feld | Beschreibung |
|---|---|
| Topic | Pflicht. Thema der Sendung, muss dem Mediathek-Themenfeld entsprechen. |
| Aliases | Alternative Themennamen. Mit **+ Add Alias** hinzufügen, mit &times; entfernen. |
| Name | Pflicht. Anzeigename für Sonarr/Radarr. Folgt automatisch dem Topic, bis du ihn manuell änderst. |
| Type | `show` oder `movie`. |
| Confidence | Standard-Konfidenz des Regelwerks (0-1, Vorgabe `1.0`). |
| TVDB ID / IMDB ID / TMDB ID | Externe IDs. Mindestens eine wird empfohlen. |

### Regeln

Mit **+ Add Rule** fügst du eine Regel hinzu. Jede Regel ist eine einklappbare Karte mit:

- **Rule ID**, **Priority** und optional **Confidence** (überschreibt die Regelwerk-Konfidenz)
- **Strategy** - eine der fünf [Strategien](./strategies). Je nach Auswahl erscheinen passende Felder:
  - `seasonAndEpisodeNumber`: Season Regex, Episode Regex, Capture Group
  - `byAbsoluteEpisodeNumber`: Episode Regex, Capture Group
  - `itemTitleExact` und `itemTitleIncludes`: Title Rules mit **+ Static** (fester Text) und **+ Regex** (Feld, Muster, Capture Group)
  - `itemTitleEqualsAirdate`: keine zusätzlichen Felder, das Datum wird automatisch erkannt
- **Filters** - Bedingungen in den Abschnitten **ALL** (UND), **ANY** (ODER) und **NOT** (NICHT). ANY und NOT sind eingeklappt und lassen sich per Klick öffnen. Jede Bedingung besteht aus Feld, Operator und Wert. Siehe [Filter](./filters).

### Validierung

Der Builder prüft während der Eingabe und zeigt Meldungen unter der JSON-Vorschau und an den betroffenen Feldern:

- **Fehler:** fehlendes Topic oder Media-Name, fehlende oder doppelte Rule ID, ungültige Rule ID (kebab-case, mindestens 3 Zeichen), fehlende Strategie, ungültige Regex-Muster
- **Warnungen:** keine Regeln definiert, keine externe ID (`tvdbId`, `imdbId`, `tmdbId`) gesetzt

### Export und Import

- **Copy** kopiert das JSON in die Zwischenablage, **Download** speichert es als `<topic>.json`.
- **Import JSON** lädt ein vorhandenes Regelwerk zur Bearbeitung in den Builder.

::: warning Grenzen des Builders
Der Builder deckt die Kernfelder ab (`topic`, `aliases`, `media`, `confidence`, `rules`). Nicht unterstützt werden `standalone`, `disable`, `enrichment` sowie verschachtelte Filtergruppen; sie gehen beim Import verloren. Ergänze solche Felder bei Bedarf manuell im exportierten JSON (siehe [Feld-Referenz](./field-reference)).
:::

Das exportierte Regelwerk kannst du als lokales Regelwerk verwenden - siehe [Eigene Regelwerke](./custom).
