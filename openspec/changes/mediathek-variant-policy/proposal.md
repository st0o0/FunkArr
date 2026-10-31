# Proposal

## Why

Mediatheken publish several variants of the same broadcast: audio description ("Audiodeskription", ARD calls it "Hörfassung"), sign language, plain language and original version. Sonarr and Radarr grab releases without human review during RSS sync and automatic searches, so a wrong variant silently ends up on disk (#203).

Today this is handled per ruleset with `not` filters. Only 4 of 98 community rulesets do it, and the filters only work where scoring runs. Items without a ruleset or without a matching config bypass scoring, and unmatched items are still returned as releases, so a ruleset filter cannot reliably keep a variant out of the results.

A hard global exclusion is not acceptable either: people who rely on audio description must still be able to get it.

## What Changes

- Every Mediathek item gets a **variant**: `Standard`, `AudioDescription`, `SignLanguage`, `PlainLanguage` or `OriginalVersion`. It is detected from title and topic with central patterns, on every search path (scored, unmatched, no ruleset).
- A **variant policy** per non-standard variant: `Exclude` or `Allow`, configured under `FunkArr:Variants`. Default: all non-standard variants are excluded.
- **Excluded** items are removed from search results on every path. Where the item was scored, the history trace records it as excluded with the variant as reason, without evaluating ruleset rules.
- **Allowed** non-standard variants carry a tag in the release title (`AD`, `DGS`, `EinfacheSprache`, `OV`) so users can build Sonarr/Radarr Custom Formats. Original-version releases do not carry the `GERMAN` token.
- The policy is shown read-only in the web UI settings, like the existing download settings. The scoring detail view shows the exclusion reason.

## Capabilities

### New Capabilities

- `mediathek-variants`: detection of broadcast variants, the configurable variant policy, and exclusion of excluded variants from search results.

### Modified Capabilities

- `scene-release`: the release title includes a variant tag for allowed non-standard variants and omits `GERMAN` for original versions.
- `scoring-trace-model`: an item trace can record that a candidate was excluded because of its variant.

## Impact

- **Messages / Persistence:** new `MediathekVariant` enum; `ScoreCandidate` and `ItemTrace` gain an optional variant/exclusion field; the persisted trace DTO is extended (extend-only, older events read as `Standard` / not excluded).
- **Search:** variant detection at item intake (`SourceInfo`), policy applied in TV and movie worker states, title tag in `SceneRelease`. Workers read the policy via `IOptionsMonitor`.
- **Scoring:** `ScoringEngine` records excluded candidates without evaluating rules.
- **Core / Host:** `VariantOptions` bound to `FunkArr:Variants`.
- **Api / UI:** read-only variant policy endpoint and settings section; exclusion reason in the scoring detail view; i18n for en and de (de-AT/de-CH fall back to de).
- **Behavior change:** variants that were returned before are now excluded by default. Users who want them set the policy to `Allow`.
- **Rulesets:** the existing `not` filters in community rulesets keep working but become redundant (cleanup in the rulesets repo, out of scope).
