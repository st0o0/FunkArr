# Rulesets

Rulesets teach FunkArr how to turn Mediathek titles into season and episode numbers or a movie match. A ruleset for a show you watch is one of the most useful contributions, and it needs no C#. If you only want to ask for a show, use the [ruleset request form](https://github.com/st0o0/funkarr/issues/new?template=ruleset_request.yml).

## Where they live

```
data/community/
├── rulesets/              one JSON file per show or movie, e.g. babylon-berlin.json
├── ruleset.schema.json    schema every ruleset must satisfy
├── CHANGELOG.md           ruleset release notes (generated)
└── version.txt
```

The file name without `.json` is the ruleset ID. What each field means is in the user guide: [Field reference](/en/rulesets/field-reference), [Strategies](/en/rulesets/strategies), [Filters](/en/rulesets/filters), [Custom rulesets](/en/rulesets/custom).

## Adding or fixing one

1. Check the [community catalog](/en/rulesets/catalog). Maybe the show has a ruleset that needs a fix instead.
2. Build the rules in the web UI (*Rulesets → + New*, with the debugger against live data) or with the [online builder](/en/rulesets/builder), and export the JSON.
3. Add the file to `data/community/rulesets/` with the TVDB ID (shows) or TMDB ID (movies), since Sonarr and Radarr match on them.
4. Open a pull request titled `feat(rulesets): add <show>` or `fix(rulesets): <what changed>`. Include two or three real Mediathek titles and the episode they should map to.

CI checks every file against the schema. Locally:

```bash
npx -p ajv-cli -p ajv-formats ajv validate -s data/community/ruleset.schema.json \
  -d "data/community/rulesets/*.json" --spec=draft2020 --strict=false
```

## How they reach users

Community rulesets are released separately from FunkArr. A merge creates a `rulesets` release with `rulesets.zip`, and every running instance picks it up through `RuleSetUpdater`, without a container update.

<ArchitectureView view="ruleset-sync" />

The catalog on this site is built from the same JSON files and updates with the next docs deploy.
