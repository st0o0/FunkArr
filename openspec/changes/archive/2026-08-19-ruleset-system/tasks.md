## 1. Data Model & Configuration

- [x] 1.1 Create `RuleSet/` namespace folder with domain records: RuleSetFile, Rule, Filter, FilterOp, MatchingStrategy, TitleRule, MediaReference. All sealed records, nullable-enabled, System.Text.Json serialization attributes.
- [x] 1.2 Create RuleSetSettings configuration record (SourceUrl, ConfigPath, RefreshIntervalMinutes) with defaults. Register in FunkArrServiceSetup. Add default RulesetSourceUrl to appsettings.json.
- [x] 1.3 Implement TopicSlugGenerator — static method converting topic strings to filesystem-safe slugs (lowercase, umlaut expansion, special char to hyphens, collapse).
- [x] 1.4 Write unit tests for domain record serialization (round-trip JSON), TopicSlugGenerator edge cases (umlauts, special characters, multi-hyphen collapse).

## 2. Community Format Transformation

- [x] 2.1 Create CommunityRuleSetParser — parses the upstream flat JSON array format (JSON-in-JSON string fields for filters and titleRegexRules) into typed RuleSetFile records, grouped by topic, rules sorted by priority, source="community", confidence=1.0.
- [x] 2.2 Create RuleSetFileWriter — writes RuleSetFile records as per-show JSON files to a target directory, using slugified filenames.
- [x] 2.3 Write unit tests for CommunityRuleSetParser with sample upstream JSON (including JSON-in-JSON strings, multiple entries per topic, all five strategy types).

## 3. RuleSet Registry Actor

- [x] 3.1 Create RuleSetRegistryActor with messages: GetRulesForTopic(topic, tvdbId?), RefreshCommunity, GenerationComplete(ruleSetFile), ReloadLocal. State: two dictionaries (byTopic, byTvdbId) built from loaded files.
- [x] 3.2 Implement startup loading — read all JSON files from community/, generated/, local/ directories. Create missing directories. Skip malformed files with warning log. Build in-memory index with layer priority (local > generated > community).
- [x] 3.3 Implement community refresh — HTTP GET source URL, parse with CommunityRuleSetParser, clear and rewrite community/ directory, rebuild community entries in index. Schedule via ScheduleTellRepeatedly (60min default). Handle fetch failures gracefully (log warning, keep existing files).
- [x] 3.4 Implement GetRulesForTopic handler — lookup by topic (exact match), optionally filter by tvdbId. Return matching rules sorted by priority. On cache miss with tvdbId: check if generation already in progress, if not spawn RuleSetGeneratorActor.
- [x] 3.5 Register RuleSetRegistryActor in FunkArrActorSystemSetup via Akka.Hosting.
- [x] 3.6 Write actor tests for RuleSetRegistryActor: startup loading, three-layer resolution priority, community refresh cycle, generation trigger on cache miss, duplicate generation prevention.

## 4. Matching Engine

- [x] 4.1 Create RuleSetMatchingEngine — static class with method EvaluateRules(item, rules, tvdbEpisodes) → MatchedEpisodeInfo?. Implements filter evaluation, title construction, strategy dispatch.
- [x] 4.2 Implement filter evaluation — duration unit conversion (seconds to minutes), greaterThan/lessThan numeric comparison, exactMatch/contains string comparison, regex matching. All filters in a rule must pass (AND logic).
- [x] 4.3 Implement title construction from TitleRules — sequential application of regex (capture group extraction) and static (literal append) rules. Return null if any regex fails.
- [x] 4.4 Implement seasonAndEpisodeNumber strategy — extract season/episode via regex, lookup in TVDB episode data.
- [x] 4.5 Implement itemTitleExact strategy — construct title, exact case-insensitive match against TVDB episode names, disambiguate by air date when multiple matches.
- [x] 4.6 Implement itemTitleIncludes strategy — construct title, contains-match against TVDB episode names.
- [x] 4.7 Implement itemTitleEqualsAirdate strategy — construct title, parse as German date (long "dd. MMMM yyyy" and short "dd.MM.yyyy" formats), match against TVDB air dates.
- [x] 4.8 Implement byAbsoluteEpisodeNumber strategy — extract absolute number via episodeRegex, lookup in TVDB absolute numbering.
- [x] 4.9 Write unit tests for each matching strategy with real-world title samples from the Mediathek (Feuer & Flamme S/E, heute-show airdate, Sturm der Liebe absolute, Tatort title-exact, Checker Tobi title-includes).

## 5. Auto-Generation

- [x] 5.1 Create RuleSetGeneratorActor — transient child of registry, receives GenerateRuleSet(tvdbId, showName), performs Mediathek sampling, pattern analysis, regex generation, validation, file output. Reports GenerationComplete or GenerationFailed to parent.
- [x] 5.2 Implement topic detection — query Mediathek with show name, collect unique topics from results, match by exact → contains → single-topic fallback.
- [x] 5.3 Implement accessibility variant filtering — strip/exclude results with "(Audiodeskription)", "(Gebärdensprache)", "(Gebardensprache)", "(klare Sprache)" in title.
- [x] 5.4 Implement pattern analysis — count S/E patterns, date patterns, absolute episode patterns, topic-prefix+separator patterns in first 15 samples. Apply thresholds and priority to select strategy.
- [x] 5.5 Implement regex generation — derive concrete regex patterns from sample titles for the selected strategy (parenthesized S/E, bare S/E, Staffel/Folge, date formats, absolute number formats).
- [x] 5.6 Implement duration filter derivation — compute median duration of samples, generate greaterThan filter with value = median * 0.5.
- [x] 5.7 Implement confidence scoring — run generated ruleset against samples, compute match rate, map to confidence (>60% → 0.8, 30-60% → 0.5, <30% → 0.3 with fallback strategy).
- [x] 5.8 Write unit tests for pattern analysis and regex generation using fixture data modeled on real Mediathek responses.

## 6. SearchActor Integration

- [x] 6.1 Modify SearchActor to resolve RuleSetRegistryActor from IActorRegistry and Ask for rules before processing results in TvSearch and MovieSearch handlers.
- [x] 6.2 Implement branching logic — when rules are returned, use RuleSetMatchingEngine; when no rules, fall back to existing MatchingPipeline.
- [x] 6.3 Extend TvdbClient to support episode list queries (all episodes for a show/season) and absolute episode number lookup, needed by the matching engine strategies.
- [x] 6.4 Update existing SearchActor tests to verify both paths (with ruleset, without ruleset). Add integration-style test with a sample ruleset and mock Mediathek/TVDB responses.

## 7. Docker & Config

- [x] 7.1 Ensure /config/rulesets/ directory structure (community/, generated/, local/) is created at startup by the registry actor.
- [x] 7.2 Update docker-compose.example.yml with FunkArr__RulesetSourceUrl environment variable documentation.
- [x] 7.3 Add default RulesetSourceUrl to appsettings.json (only this + log level, nothing else).
