# Download Options — Delta

## MODIFIED Requirements

### Requirement: DownloadOptions validation
The system SHALL validate `DownloadOptions` on startup using `IValidateOptions<DownloadOptions>` registered via `AddOptionsWithValidateOnStart<DownloadOptions, DownloadOptionsValidator>()`. The validator SHALL use `Terp.Options.ValidationFailures` to collect failures. Validation SHALL reject `DownloadTimeSlot` entries where `Start == End`.

#### Scenario: Valid configuration
- **WHEN** `DownloadSchedule` contains slots with `Start != End`
- **THEN** validation SHALL pass

#### Scenario: Zero-length time slot
- **WHEN** a `DownloadTimeSlot` has `Start == End`
- **THEN** validation SHALL fail with message `"DownloadSchedule[{i}]: Start and End must differ (both are {slot.Start})."`
- **AND** the message text SHALL be identical to the current output
