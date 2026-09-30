## ADDED Requirements

### Requirement: Standard TV release title
The system SHALL build release titles in standard format: `{ShowName}.S{Season:D2}E{Episode:D2}.{EpisodeName}.GERMAN.{Quality}.WEB.{Codec}-FA` with spaces replaced by dots and unsafe filename characters removed.

#### Scenario: Standard title with episode name
- **WHEN** showName="Tatort", season=1, episode=3, episodeName="Köpfe", quality=HD720, codec="h264"
- **THEN** the release title SHALL be `Tatort.S01E03.Koepfe.GERMAN.720p.WEB.h264-FA`

#### Scenario: Standard title without episode name
- **WHEN** showName="Tatort", season=1, episode=3, episodeName=null, quality=HD1080, codec="h265"
- **THEN** the release title SHALL be `Tatort.S01E03.GERMAN.1080p.WEB.h265-FA`

#### Scenario: Show name with spaces
- **WHEN** showName="Die Nordreportage", season=2, episode=5, episodeName="Im Hafen"
- **THEN** the release title SHALL be `Die.Nordreportage.S02E05.Im.Hafen.GERMAN.720p.WEB.h264-FA`

### Requirement: Daily TV release title
The system SHALL build daily-format release titles: `{ShowName}.{AirDate:yyyy.MM.dd}.{EpisodeName}.GERMAN.{Quality}.WEB.{Codec}-FA`.

#### Scenario: Daily title with episode name
- **WHEN** showName="Tatort", airDate="2026-08-17", episodeName="Köpfe", quality=HD1080, codec="h264"
- **THEN** the release title SHALL be `Tatort.2026.08.17.Koepfe.GERMAN.1080p.WEB.h264-FA`

#### Scenario: Daily title without episode name
- **WHEN** showName="10 vor 10", airDate="2026-08-20", episodeName=null, quality=HD1080
- **THEN** the release title SHALL be `10.vor.10.2026.08.20.GERMAN.1080p.WEB.h264-FA`

### Requirement: Movie release title
The system SHALL build movie release titles: `{MovieName}.{Year}.GERMAN.{Quality}.WEB.{Codec}-FA`.

#### Scenario: Movie with year
- **WHEN** movieName="Petrocelli", year=2024, quality=HD1080, codec="h264"
- **THEN** the release title SHALL be `Petrocelli.2024.GERMAN.1080p.WEB.h264-FA`

### Requirement: Fallback release title
When no resolved episode metadata is available, the system SHALL build a fallback title: `{Topic}.{Title}.{AirDate:yyyy.MM.dd}.GERMAN.{Quality}.WEB.{Codec}-FA`. If title equals topic or is empty, omit the title portion. If air date is unavailable, omit the date portion.

#### Scenario: Fallback with topic, title, and date
- **WHEN** topic="Tatort", title="Köpfe", timestamp=2026-08-17, quality=HD720
- **THEN** the release title SHALL be `Tatort.Koepfe.2026.08.17.GERMAN.720p.WEB.h264-FA`

#### Scenario: Fallback with title same as topic
- **WHEN** topic="Petrocelli", title="Petrocelli", timestamp=2026-08-10, quality=HD1080
- **THEN** the release title SHALL be `Petrocelli.2026.08.10.GERMAN.1080p.WEB.h264-FA`

#### Scenario: Fallback with empty title
- **WHEN** topic="Feuerwehrmann Sam", title="", timestamp=2026-08-08, quality=HD720
- **THEN** the release title SHALL be `Feuerwehrmann.Sam.2026.08.08.GERMAN.720p.WEB.h264-FA`

### Requirement: Title sanitization
The system SHALL sanitize release title components by: replacing spaces with dots, removing characters not safe for filenames (slashes, colons, quotes, angle brackets), transliterating German umlauts (ä→ae, ö→oe, ü→ue, ß→ss), and collapsing consecutive dots.

#### Scenario: Umlaut transliteration
- **WHEN** the episode name is "Für Österreich"
- **THEN** it SHALL appear as `Fuer.Oesterreich` in the release title

#### Scenario: Special characters removed
- **WHEN** the episode name is "Wer war's? (Teil 1/2)"
- **THEN** it SHALL appear as `Wer.wars.Teil.1.2` in the release title

#### Scenario: Consecutive dots collapsed
- **WHEN** sanitization produces "Tatort..Koepfe...GERMAN"
- **THEN** it SHALL be collapsed to `Tatort.Koepfe.GERMAN`

### Requirement: Episode name length limit
The episode name portion SHALL be truncated at 40 characters (before dot-replacement) to prevent excessively long release titles.

#### Scenario: Long episode name truncated
- **WHEN** the episode name is "Ein ganz besonders langer Episodentitel der nicht enden will"
- **THEN** only the first 40 characters SHALL be used: `Ein.ganz.besonders.langer.Episodentitel`

### Requirement: Quality string mapping
The system SHALL map quality tiers to strings: HD1080→"1080p", HD720→"720p", SD→"480p".

#### Scenario: Quality mapping
- **WHEN** quality is HD1080
- **THEN** the quality string SHALL be "1080p"
