## ADDED Requirements

### Requirement: NZB generation uses XmlSerializer object model
The NZB generator SHALL produce NZB XML by serializing an `[XmlRoot("nzb")]`-decorated object model through `XmlHelper.Serialize<T>()`.

#### Scenario: Generated NZB structure
- **WHEN** an NZB is generated for title "Test Show" and url "https://example.com/video.mp4"
- **THEN** the output is valid XML with root element `<nzb>`
- **AND** contains `<head>` with `<meta type="title">Test Show</meta>` and `<meta type="url">https://example.com/video.mp4</meta>`
- **AND** contains `<file>` with `<groups>` and `<segments>` elements

### Requirement: NZB metadata uses head/meta elements
Title and URL metadata SHALL be stored in `<head><meta type="...">` elements per the NZB specification, not as XML comments.

#### Scenario: Meta elements in generated NZB
- **WHEN** an NZB is generated
- **THEN** the XML contains `<head>` as first child of `<nzb>`
- **AND** `<head>` contains `<meta type="title">` and `<meta type="url">` elements
- **AND** no XML comments are used for metadata

### Requirement: NZB parser reads head/meta elements
The DownloadApi NZB parser SHALL extract title and URL from `<head><meta>` elements by deserializing the NZB XML into an object model.

#### Scenario: Parse NZB with meta elements
- **WHEN** an NZB containing `<head><meta type="title">My Show</meta><meta type="url">https://example.com/v.mp4</meta></head>` is parsed
- **THEN** the parser returns title "My Show" and url "https://example.com/v.mp4"

#### Scenario: Parse NZB with missing meta
- **WHEN** an NZB contains `<head>` but no `<meta type="url">` element
- **THEN** the parser returns null for url

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
