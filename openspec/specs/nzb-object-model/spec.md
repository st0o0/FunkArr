## ADDED Requirements

### Requirement: NZB generation uses XmlSerializer object model
The NZB generator SHALL produce NZB XML by serializing an `[XmlRoot("nzb")]`-decorated object model through `XmlHelper.Serialize<T>()`. The generated NZB SHALL include `X-FunkArr-*` custom meta fields alongside the standard `title` meta field.

#### Scenario: Generated NZB structure
- **WHEN** an NZB is generated for title "Test Show" and url "https://example.com/video.mp4"
- **THEN** the output is valid XML with root element `<nzb>`
- **AND** contains `<head>` with `<meta type="title">Test Show</meta>`
- **AND** contains `<meta type="X-FunkArr-Url">https://example.com/video.mp4</meta>`
- **AND** contains `<file>` with `<groups>` and `<segments>` elements

#### Scenario: Generated NZB with subtitle
- **WHEN** an NZB is generated for a search result with SubtitleUrl "https://api.ardmediathek.de/.../subtitle.ttml"
- **THEN** the output SHALL contain `<meta type="X-FunkArr-SubtitleUrl">https://api.ardmediathek.de/.../subtitle.ttml</meta>`

#### Scenario: Generated NZB without subtitle
- **WHEN** an NZB is generated for a search result with no subtitle URL
- **THEN** the output SHALL NOT contain a `<meta type="X-FunkArr-SubtitleUrl">` element

#### Scenario: Generated NZB with all custom metas
- **WHEN** an NZB is generated for a search result with Channel "NDR", Duration 5348, Size 1632632832
- **THEN** the output SHALL contain `<meta type="X-FunkArr-Channel">NDR</meta>`, `<meta type="X-FunkArr-Duration">5348</meta>`, and `<meta type="X-FunkArr-Size">1632632832</meta>`

### Requirement: NZB metadata uses head/meta elements
Title and URL metadata SHALL be stored in `<head><meta type="...">` elements per the NZB specification, not as XML comments. Custom FunkArr metadata SHALL use the `X-` prefix as specified by the NZB standard.

#### Scenario: Meta elements in generated NZB
- **WHEN** an NZB is generated
- **THEN** the XML contains `<head>` as first child of `<nzb>`
- **AND** `<head>` contains `<meta type="title">` and `<meta type="X-FunkArr-Url">` elements
- **AND** no XML comments are used for metadata

### Requirement: NZB parser reads head/meta elements
The DownloadApi NZB parser SHALL extract title, video URL, subtitle URL, channel, duration, and size from `<head><meta>` elements by deserializing the NZB XML into an object model.

#### Scenario: Parse NZB with all custom metas
- **WHEN** an NZB containing standard and `X-FunkArr-*` meta elements is parsed
- **THEN** the parser SHALL return title, VideoUrl, SubtitleUrl, Channel, Duration, and Size from the corresponding meta elements

#### Scenario: Parse NZB with missing optional metas
- **WHEN** an NZB contains `<meta type="X-FunkArr-Url">` but no `<meta type="X-FunkArr-SubtitleUrl">`
- **THEN** the parser SHALL return null for SubtitleUrl

#### Scenario: Parse NZB with missing video URL
- **WHEN** an NZB contains no `<meta type="X-FunkArr-Url">` element
- **THEN** the parser SHALL return null for VideoUrl

### Requirement: Single NZB model shared within ArrApi
The NZB object model SHALL exist once in the `FunkArr.ArrApi` root namespace. Both Newznab endpoints (NZB generation) and SABnzbd endpoints (NZB parsing) SHALL use this single model.

#### Scenario: One Nzb class in project
- **WHEN** examining the ArrApi project
- **THEN** exactly one `Nzb` class SHALL exist in the `FunkArr.ArrApi` namespace

#### Scenario: NZB generation uses shared model
- **WHEN** NzbGenerator creates an NZB
- **THEN** it SHALL instantiate `FunkArr.ArrApi.Nzb`

#### Scenario: NZB parsing uses shared model
- **WHEN** NzbParser deserializes NZB XML
- **THEN** it SHALL deserialize into `FunkArr.ArrApi.Nzb`

### Requirement: String interpolation removed from NZB generation
The `NzbGenerator.Generate` method SHALL NOT use string interpolation or string templates to build XML.

#### Scenario: No raw XML strings
- **WHEN** reviewing NzbGenerator code
- **THEN** no interpolated string (`$"..."` or `$"""..."""`) produces XML content
