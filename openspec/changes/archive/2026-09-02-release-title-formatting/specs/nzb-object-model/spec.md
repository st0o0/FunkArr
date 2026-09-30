## MODIFIED Requirements

### Requirement: NZB generation uses XmlSerializer object model

The NZB generator SHALL produce NZB XML by serializing an `[XmlRoot("nzb")]`-decorated object model through `XmlHelper.Serialize<T>()`. The generated NZB SHALL include `X-FunkArr-*` custom meta fields alongside the standard `title` meta field, including `X-FunkArr-Category`.

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

#### Scenario: Generated NZB with all custom metas including category

- **WHEN** an NZB is generated for a search result with Channel "NDR", Duration 5348, Size 1632632832, Category "tv"
- **THEN** the output SHALL contain `<meta type="X-FunkArr-Channel">NDR</meta>`, `<meta type="X-FunkArr-Duration">5348</meta>`, `<meta type="X-FunkArr-Size">1632632832</meta>`, and `<meta type="X-FunkArr-Category">tv</meta>`

#### Scenario: Generated NZB without category

- **WHEN** an NZB is generated for a search result with no Category
- **THEN** the output SHALL NOT contain a `<meta type="X-FunkArr-Category">` element

### Requirement: NZB parser reads head/meta elements

The DownloadApi NZB parser SHALL extract title, video URL, subtitle URL, channel, duration, size, and category from `<head><meta>` elements by deserializing the NZB XML into an object model.

#### Scenario: Parse NZB with all custom metas including category

- **WHEN** an NZB containing standard and `X-FunkArr-*` meta elements including `X-FunkArr-Category` is parsed
- **THEN** the parser SHALL return title, VideoUrl, SubtitleUrl, Channel, Duration, Size, and Category from the corresponding meta elements

#### Scenario: Parse NZB with missing optional metas

- **WHEN** an NZB contains `<meta type="X-FunkArr-Url">` but no `<meta type="X-FunkArr-SubtitleUrl">` and no `<meta type="X-FunkArr-Category">`
- **THEN** the parser SHALL return null for SubtitleUrl and null for Category

#### Scenario: Parse NZB with missing video URL

- **WHEN** an NZB contains no `<meta type="X-FunkArr-Url">` element
- **THEN** the parser SHALL return null for VideoUrl
