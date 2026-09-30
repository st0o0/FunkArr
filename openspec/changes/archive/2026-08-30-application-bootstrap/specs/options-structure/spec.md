## ADDED Requirements

### Requirement: FunkArrOptions cross-cutting options class
The system SHALL define `FunkArrOptions` as a sealed class in `FunkArr/Configuration/`
with the following properties:
- `ApiKey` (string, default `"funkarr-default-api-key"`)
- `PersistencePath` (string, default `"data/funkarr.db"`)

The class SHALL define `public const string SectionName = "FunkArr"`.

#### Scenario: Default values without configuration
- **WHEN** no `FunkArr` section is provided
- **THEN** `ApiKey` equals `"funkarr-default-api-key"` and `PersistencePath` equals `"data/funkarr.db"`

### Requirement: FunkArrOptions registration in DI
`FunkArrServiceSetup` SHALL register `FunkArrOptions` via:
```
services.AddOptions<FunkArrOptions>()
    .Bind(configuration.GetSection(FunkArrOptions.SectionName))
    .ValidateOnStart();
```

#### Scenario: Options resolved from DI
- **WHEN** `IOptions<FunkArrOptions>` is injected
- **THEN** the value reflects the bound configuration section
