# scene-release

## Purpose

Transforms enriched Mediathek items into Newznab-compatible scene-style releases. The SceneRelease record captures resolved display and identification data, formats release titles, and expands quality variants into SearchResultItems for Sonarr/Radarr consumption.

## Requirements

### Requirement: SceneRelease record captures resolved release data

FunkArr.Search SHALL define a `SceneRelease` sealed record with four fields: `MediaName` (string), `Identifier` (string?), `EpisodeTitle` (string), and `Item` (EnrichedItem). The record represents a Mediathek item fully resolved into scene-release form, ready for title formatting and quality expansion.

#### Scenario: SceneRelease fields

- **WHEN** a SceneRelease is created
- **THEN** it SHALL contain `MediaName` (the series/film name), `Identifier` (the S##E## or year string, nullable), `EpisodeTitle` (the clean episode/film title), and `Item` (the source EnrichedItem)

### Requirement: ForShow factory resolves TV show releases

`SceneRelease` SHALL provide a static `ForShow(EnrichedItem item, string? mediaName)` method that constructs a SceneRelease for TV content. The identifier SHALL be resolved from the item's `Identity` using season/episode priority: season+episode first, episode-only second, airdate fallback third.

#### Scenario: Show with season and episode

- **WHEN** ForShow is called with Identity.Season="2026" and Identity.Episode="18"
- **THEN** Identifier SHALL be "S2026E18"

#### Scenario: Show with episode only

- **WHEN** ForShow is called with Identity.Season=null and Identity.Episode="5"
- **THEN** Identifier SHALL be "S01E05"

#### Scenario: Show with single-digit episode padding

- **WHEN** ForShow is called with Identity.Episode="3"
- **THEN** the episode SHALL be zero-padded to "S01E03"

#### Scenario: Show with airdate fallback

- **WHEN** ForShow is called with Identity.Season=null, Identity.Episode=null, and Source.AiredAt=2024-09-20
- **THEN** Identifier SHALL be "2024-09-20"

#### Scenario: Show with no identification data

- **WHEN** ForShow is called with no season, episode, or airdate
- **THEN** Identifier SHALL be null

#### Scenario: Display from item

- **WHEN** ForShow is called and item.Display is set
- **THEN** MediaName and EpisodeTitle SHALL come from item.Display

### Requirement: ForMovie factory resolves movie releases

`SceneRelease` SHALL provide a static `ForMovie(EnrichedItem item, string? mediaName)` method that constructs a SceneRelease for movie content. The identifier SHALL be the enriched movie year from `Identity.Year` when available, falling back to the broadcast year from `Source.AiredAt`.

#### Scenario: Movie with enriched year

- **WHEN** ForMovie is called with Identity.Year=1995 and Source.AiredAt=2026-09-27
- **THEN** Identifier SHALL be "1995" (the TMDB year, not the broadcast year)

#### Scenario: Movie without enrichment falls back to broadcast year

- **WHEN** ForMovie is called with Identity.Year=null and Source.AiredAt=2024-03-15
- **THEN** Identifier SHALL be "2024"

#### Scenario: Movie with no year data

- **WHEN** ForMovie is called with Identity.Year=null and Source.AiredAt=null
- **THEN** Identifier SHALL be null

### Requirement: FormatTitle produces scene-style release strings

`SceneRelease` SHALL provide an instance method `FormatTitle(int quality)` that composes the fields into a scene-style release title. The format SHALL be `{MediaName}.{Identifier}.{EpisodeTitle}.GERMAN.{Quality}.WEB.h264-FunkArr` with Identifier omitted when null. Spaces SHALL be replaced with dots, special characters removed, consecutive dots collapsed, and leading/trailing dots stripped.

#### Scenario: Full TV release title

- **WHEN** FormatTitle(1080) is called with MediaName="Tatort", Identifier="S2026E18", EpisodeTitle="Herz aus Eis"
- **THEN** the result SHALL be "Tatort.S2026E18.Herz.aus.Eis.GERMAN.1080p.WEB.h264-FunkArr"

#### Scenario: Movie release title with year

- **WHEN** FormatTitle(1080) is called with MediaName="The Usual Suspects", Identifier="1995", EpisodeTitle="Eine Frau mit berauschenden Talenten"
- **THEN** the result SHALL be "The.Usual.Suspects.1995.Eine.Frau.mit.berauschenden.Talenten.GERMAN.1080p.WEB.h264-FunkArr"

#### Scenario: Title without identifier

- **WHEN** FormatTitle(720) is called with MediaName="Tagesschau", Identifier=null, EpisodeTitle="20 Uhr"
- **THEN** the result SHALL be "Tagesschau.20.Uhr.GERMAN.720p.WEB.h264-FunkArr"

#### Scenario: Special characters removed

- **WHEN** EpisodeTitle contains `/:;"'@#?$%^*+=!<>,()`
- **THEN** those characters SHALL be removed from the output

#### Scenario: Umlauts preserved

- **WHEN** MediaName or EpisodeTitle contains German umlauts or eszett
- **THEN** they SHALL remain unchanged (no ASCII normalization)

#### Scenario: Quality mapping

- **WHEN** quality is 1080, 720, 480, or 270
- **THEN** the label SHALL be "1080p", "720p", "480p", or "270p" respectively

### Requirement: Expand produces SearchResultItems per quality variant

`SceneRelease` SHALL provide an instance method `Expand()` that returns `SearchResultItem[]` with one entry per available quality variant (HD/SD/Low URLs from `Item.Source`). Each result SHALL use the formatted title for that quality, the variant's URL, and the estimated or actual size.

#### Scenario: Source with all three URLs

- **WHEN** Expand is called and Item.Source has UrlHd, Url, and UrlLow
- **THEN** three SearchResultItems SHALL be returned with qualities 1080, 720, 480

#### Scenario: Source with only normal URL

- **WHEN** Expand is called and Item.Source has only Url (no UrlHd, no UrlLow)
- **THEN** one SearchResultItem SHALL be returned with quality 720

#### Scenario: Source with no URLs

- **WHEN** Expand is called and Item.Source has no video URLs
- **THEN** an empty array SHALL be returned

#### Scenario: Size estimation when source size is zero

- **WHEN** Item.Source.Size is 0
- **THEN** size SHALL be estimated from duration and quality tier

#### Scenario: Metadata populated on results

- **WHEN** Expand produces SearchResultItems
- **THEN** each item's Metadata SHALL contain the Identity's TvdbId, ImdbId, TmdbId, Season, and Episode, plus Match confidence and method
