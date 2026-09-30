## Why

The current MatchingPipeline applies generic filters (duration threshold, skip-keywords) to all Mediathek search results regardless of the show. German broadcaster titles follow wildly different patterns per show — "Feuer & Flamme" embeds "(S11/E08)", "heute-show" embeds "vom 5. Juni 2026", "Sturm der Liebe" uses absolute episode numbers "(1606)", and "Tatort" uses only episode titles with no numbering at all. Without show-specific matching rules, FunkArr cannot reliably resolve which Mediathek result corresponds to which Sonarr/Radarr episode. MediathekArr and RundfunkArr (Node.js) solve this with 167+ community-curated rulesets — we need the same capability.

## What Changes

- **New RuleSet data model**: per-show JSON files containing prioritized matching rules with filters, regex patterns, title construction rules, and one of five matching strategies.
- **Three-layer RuleSet resolution**: community-sourced rulesets (loaded from configurable GitHub raw JSON URL), auto-generated rulesets (pattern analysis of live Mediathek data), and local user overrides — resolved in order local > generated > community.
- **RuleSetRegistryActor**: new actor that owns the in-memory RuleSet index, handles community refresh (startup + every 60 minutes), and triggers auto-generation for unknown shows.
- **RuleSetGeneratorActor**: child actor that samples Mediathek results for a show, detects title patterns, generates matching regexes, validates against samples, and writes generated RuleSet files.
- **Config volume structure**: `/config/rulesets/` directory with `community/`, `generated/`, and `local/` subdirectories, each containing per-show JSON files.
- **Enhanced SearchActor**: asks RuleSetRegistryActor for applicable rules before filtering, replaces generic MatchingPipeline with RuleSet-driven matching for shows that have rules (falls back to existing pipeline for unknown shows).
- **Five matching strategies**: seasonAndEpisodeNumber, itemTitleExact, itemTitleIncludes, itemTitleEqualsAirdate, byAbsoluteEpisodeNumber.

## Capabilities

### New Capabilities
- `ruleset-data-model`: RuleSet JSON format, domain records (RuleSetFile, Rule, Filter, TitleRule, MatchingStrategy), serialization, and community format transformation.
- `ruleset-registry`: RuleSetRegistryActor — in-memory index, three-layer resolution, community loading from GitHub, scheduled refresh, file watching for local overrides.
- `ruleset-auto-generation`: RuleSetGeneratorActor — Mediathek sampling, pattern detection, regex generation, confidence scoring, file output to generated/ directory.
- `ruleset-matching-engine`: Strategy-based matching pipeline that applies RuleSet rules to Mediathek results — filter evaluation, title construction from TitleRules, strategy dispatch (S/E regex, title matching, airdate matching, absolute episode number).

### Modified Capabilities
- `mediathek-search`: SearchActor integrates with RuleSetRegistryActor — asks for applicable rules before filtering, uses RuleSet-driven matching when available, falls back to existing MatchingPipeline when no RuleSet exists.

## Impact

- **Actors**: New RuleSetRegistryActor (top-level, registered via Akka.Hosting) and RuleSetGeneratorActor (transient child). SearchActor gains a dependency on RuleSetRegistryActor.
- **Startup**: FunkArrActorSystemSetup registers the new actors. FunkArrServiceSetup configures RuleSet settings (source URL, config path, refresh interval).
- **Filesystem**: `/config/rulesets/` directory tree created at startup. Community JSON files written on refresh. Generated JSON files written on auto-generation.
- **Configuration**: New env vars `FunkArr__RulesetSourceUrl` (default: RundfunkArr GitHub raw URL) and `FunkArr__ConfigPath` (default: `/config`). appsettings.json gets default RulesetSourceUrl only.
- **Dependencies**: No new NuGet packages required — uses existing System.Text.Json and HttpClient.
- **API surfaces**: No changes to Newznab or SABnzbd APIs — RuleSets are internal to the search/matching pipeline.
