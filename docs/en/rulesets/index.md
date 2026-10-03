# Rulesets

Mediathek titles are messy - "Tatort" episodes might appear as "Tatort: Der letzte Schrei" with no season or episode number. Rulesets map these titles to structured season/episode format so Sonarr can match them.

## How it works

A ruleset is a JSON file that defines rules for a specific show or movie. Each rule matches Mediathek entries by topic and title patterns and extracts season/episode information.

FunkArr loads rulesets from two sources:

1. **Community rulesets** - auto-synced from GitHub releases, covering the most popular shows and movies. See the [catalog](./catalog) for the full list.
2. **Local rulesets** - created in the web UI's RuleSet builder, stored in `data/rulesets/local/`. A local ruleset is merged with the community ruleset of the same ID (see [Custom Rulesets](./custom)).

## Auto-updates

On startup and every 30 minutes afterwards, FunkArr queries the GitHub releases of the configured repository (`FunkArr__RuleSet__Repository`, default `st0o0/funkarr`) for a release tagged `rulesets-v<version>`. If that version differs from the installed one (`data/rulesets/version.txt`), FunkArr downloads the `rulesets.zip` release asset, replaces the `data/rulesets/community/` folder and reloads all rulesets. Changes to files in the ruleset folders are also picked up automatically.

| Variable | Default | Description |
|----------|---------|-------------|
| `FunkArr__RuleSet__Repository` | `st0o0/funkarr` | GitHub repository for community rulesets |
| `FunkArr__RuleSet__Version` | `latest` | Pin a specific version or `latest` |
| `FunkArr__RuleSet__RefreshEnabled` | `true` | Set to `false` to disable automatic updates |
