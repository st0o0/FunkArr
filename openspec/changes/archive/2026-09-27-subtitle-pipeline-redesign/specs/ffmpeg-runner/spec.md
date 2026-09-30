## MODIFIED Requirements

### Requirement: FfmpegRunner accepts subtitle language parameter
The `FfmpegRunner.RunAsync` and `BuildArguments` methods SHALL accept an optional `subtitleLanguage` parameter instead of hardcoding `"deu"`.

#### Scenario: Language from parameter
- **WHEN** `subtitlePath` is not null and `subtitleLanguage` is `"deu"`
- **THEN** FFmpeg SHALL be invoked with `-metadata:s:s:0 language=deu`

#### Scenario: Default language when not provided
- **WHEN** `subtitlePath` is not null and `subtitleLanguage` is null
- **THEN** FFmpeg SHALL use `"deu"` as the default language

#### Scenario: No language when no subtitle
- **WHEN** `subtitlePath` is null
- **THEN** no subtitle language metadata SHALL be added

### Requirement: FfmpegRunner interface includes language
The `IFfmpegRunner.RunAsync` method SHALL accept an optional `subtitleLanguage` parameter.

#### Scenario: Interface signature
- **WHEN** a caller invokes `IFfmpegRunner.RunAsync`
- **THEN** the method SHALL accept `string videoUrl`, `string? subtitlePath`, `string outputPath`, `string? proxyUrl`, `string? subtitleLanguage`, `Action<ProgressUpdate> onProgress`, `CancellationToken ct`
