## MODIFIED Requirements

### Requirement: FormatTitle produces scene-style release strings

`SceneRelease` SHALL provide an instance method `FormatTitle(int quality)` that composes the fields into a scene-style release title. The format SHALL be `{MediaName}.{Identifier}.{EpisodeTitle}.{VariantTag}.GERMAN.{Quality}.WEB.h264-FunkArr` with Identifier omitted when null and VariantTag omitted for the `Standard` variant. Spaces SHALL be replaced with dots, special characters removed, consecutive dots collapsed, and leading/trailing dots stripped.

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

#### Scenario: Variant tags

- **WHEN** FormatTitle(1080) is called with MediaName="Tatort", Identifier="S2026E18", EpisodeTitle="Herz aus Eis" and the variant is AudioDescription, SignLanguage or PlainLanguage
- **THEN** the result SHALL be "Tatort.S2026E18.Herz.aus.Eis.AD.GERMAN.1080p.WEB.h264-FunkArr", "Tatort.S2026E18.Herz.aus.Eis.DGS.GERMAN.1080p.WEB.h264-FunkArr" or "Tatort.S2026E18.Herz.aus.Eis.EinfacheSprache.GERMAN.1080p.WEB.h264-FunkArr" respectively

#### Scenario: Original version omits GERMAN

- **WHEN** FormatTitle(1080) is called with MediaName="The Usual Suspects", Identifier="1995", EpisodeTitle="" and the variant is OriginalVersion
- **THEN** the result SHALL be "The.Usual.Suspects.1995.OV.1080p.WEB.h264-FunkArr"
