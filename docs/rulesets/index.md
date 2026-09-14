# Rulesets

Mediathek titles are messy — "Tatort" episodes might appear as "Tatort: Der letzte Schrei" with no season or episode number. Rulesets map these titles to structured season/episode format so Sonarr can match them.

## How it works

A ruleset is a JSON file that defines rules for a specific show or movie. Each rule matches Mediathek entries by topic and title patterns and extracts season/episode information.

FunkArr loads rulesets from two sources:

1. **Community rulesets** — auto-synced from GitHub releases, covering the most popular shows and movies. See the [catalog](./catalog) for the full list.
2. **Local rulesets** — created in the web UI's RuleSet builder, stored in your data directory. Local rules override community rules for the same show.

## Auto-updates

The community rulesets are checked for new versions every 30 minutes. When a new release is published on GitHub, FunkArr downloads and applies the update automatically. You can pin a specific version via `FunkArr__RuleSet__Version` if needed.
