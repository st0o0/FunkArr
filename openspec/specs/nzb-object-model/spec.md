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

#### Scenario: Generated NZB with all custom metas including category
- **WHEN** an NZB is generated for a search result with Channel "NDR", Duration 5348, Size 1632632832, Category "tv"
- **THEN** the output SHALL contain `<meta type="X-FunkArr-Channel">NDR</meta>`, `<meta type="X-FunkArr-Duration">5348</meta>`, `<meta type="X-FunkArr-Size">1632632832</meta>`, and `<meta type="X-FunkArr-Category">tv</meta>`

#### Scenario: Generated NZB without category
- **WHEN** an NZB is generated for a search result with no Category
- **THEN** the output SHALL NOT contain a `<meta type="X-FunkArr-Category">` element

### Requirement: NZB metadata uses head/meta elements
Title and URL metadata SHALL be stored in `<head><meta type="...">` elements per the NZB specification, not as XML comments. Custom FunkArr metadata SHALL use the `X-` prefix as specified by the NZB standard.

#### Scenario: Meta elements in generated NZB
- **WHEN** an NZB is generated
- **THEN** the XML contains `<head>` as first child of `<nzb>`
- **AND** `<head>` contains `<meta type="title">` and `<meta type="X-FunkArr-Url">` elements
- **AND** no XML comments are used for metadata

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

### Requirement: Single NZB model shared within ArrApi
The NZB object model SHALL exist once in the `FunkArr.ArrApi.Newznab.Models` namespace (file `Newznab/Models/Nzb.cs`). Both Newznab endpoints (NZB generation) and SABnzbd endpoints (NZB parsing) SHALL use this single model.

#### Scenario: One Nzb class in project
- **WHEN** examining the ArrApi project
- **THEN** exactly one `Nzb` class SHALL exist, in the `FunkArr.ArrApi.Newznab.Models` namespace

#### Scenario: NZB generation uses shared model
- **WHEN** `NzbService` creates an NZB
- **THEN** it SHALL instantiate `FunkArr.ArrApi.Newznab.Models.Nzb`

#### Scenario: NZB parsing uses shared model
- **WHEN** `NzbService` deserializes NZB XML
- **THEN** it SHALL deserialize into `FunkArr.ArrApi.Newznab.Models.Nzb`

### Requirement: String interpolation removed from NZB generation
The `NzbGenerator.Generate` method SHALL NOT use string interpolation or string templates to build XML.

#### Scenario: No raw XML strings
- **WHEN** reviewing NzbGenerator code
- **THEN** no interpolated string (`$"..."` or `$"""..."""`) produces XML content
### Requirement: Typed NzbPayload defines the NZB id
`FunkArr.ArrApi.Newznab.Models` SHALL contain `NzbPayload(Title, Url, SubtitleUrl?, Channel, Duration, Size, Category)` with typed members (`int` duration, `long` size, `MediaType` category). `NzbPayload` SHALL expose `string Encode()` and `static NzbPayload? Decode(string id)` and SHALL be the only code that knows the NZB id format. The id SHALL be the Base64 encoding of a UTF-8 JSON document. No serialization attribute SHALL be added to Messages or Persistence types.

#### Scenario: Encode/decode round trip
- **WHEN** a payload with title "Tatort\tFolge", a url, subtitle url, channel, duration 5400, size 1234567890 and category Show is encoded and decoded
- **THEN** the decoded payload SHALL equal the original, including the tab in the title

#### Scenario: Optional subtitle
- **WHEN** a payload without subtitle url is encoded and decoded
- **THEN** the decoded `SubtitleUrl` SHALL be null

#### Scenario: Invalid id
- **WHEN** `Decode` is called with a string that is not valid Base64 or not valid payload JSON
- **THEN** it SHALL return null

#### Scenario: Missing mandatory field
- **WHEN** `Decode` is called with a valid JSON document lacking `Url`
- **THEN** it SHALL return null

### Requirement: Single encoding and decoding site
Search item construction SHALL call `NzbPayload.Encode()` and `NzbService.GetNzb` SHALL call `NzbPayload.Decode()`. No other code SHALL split, join, or Base64-encode NZB ids by hand.

#### Scenario: Search result link carries encoded payload
- **WHEN** a search item is converted to an RSS item
- **THEN** the `t=get` link id SHALL equal `NzbPayload.Encode()` of the item's payload, URL-escaped

#### Scenario: t=get uses Decode
- **WHEN** `t=get&id=<encoded>` is requested with a valid id
- **THEN** the generated NZB `head/meta` elements SHALL carry title, url, subtitle url, channel, duration, size, and category from the decoded payload

#### Scenario: t=get with invalid id
- **WHEN** `t=get&id=not-valid` is requested
- **THEN** the response SHALL be Newznab error 201 "Incorrect parameter"

### Requirement: NZB parsing yields a payload
The `addfile` flow SHALL read an uploaded NZB's `head/meta` elements into an `NzbPayload`. `NzbParseResult` and the legacy `url` meta fallback SHALL NOT exist.

#### Scenario: Uploaded NZB parsed
- **WHEN** an NZB generated by `t=get` is uploaded via `addfile`
- **THEN** the resulting `AddDownload` SHALL carry the same title, url, subtitle url, channel, duration, size, and category as the payload used to generate it

#### Scenario: NZB without video url
- **WHEN** an uploaded NZB has no `X-FunkArr-Url` meta
- **THEN** the response SHALL be `{"status":false,"error":"Invalid NZB format: missing video URL"}`
