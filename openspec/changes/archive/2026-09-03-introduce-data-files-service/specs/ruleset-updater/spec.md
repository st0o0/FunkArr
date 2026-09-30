## MODIFIED Requirements

### Requirement: RuleSetUpdater compares local version before downloading
The RuleSetUpdater SHALL read `DataPaths.RuleSetVersion` using `IDataFiles.Exists()` and `IDataFiles.ReadText()` and compare with the remote release version. If versions match, the download SHALL be skipped.

#### Scenario: Version matches — skip download
- **WHEN** `IDataFiles.ReadText(dataPaths.RuleSetVersion)` returns `"1.0.0"` and the latest remote release is `community-rulesets-v1.0.0`
- **THEN** the actor SHALL skip the download and log at Debug level

#### Scenario: Version differs — download
- **WHEN** `IDataFiles.ReadText(dataPaths.RuleSetVersion)` returns `"1.0.0"` and the latest remote release is `community-rulesets-v1.1.0`
- **THEN** the actor SHALL download and extract the new version

#### Scenario: No local version file
- **WHEN** `IDataFiles.Exists(dataPaths.RuleSetVersion)` returns false
- **THEN** the actor SHALL always download

### Requirement: RuleSetUpdater extracts ZIP atomically
The RuleSetUpdater SHALL extract downloaded ZIP archives atomically using `IDataFiles`: create a temp directory via `IDataFiles.CreateDirectory()` in `DataPaths.Temp`, extract the ZIP there, then call `IDataFiles.ReplaceDirectory()` to swap with `DataPaths.CommunityRuleSets`. If extraction fails, the existing directory SHALL remain untouched.

#### Scenario: Successful extraction
- **WHEN** a valid ZIP is downloaded
- **THEN** the actor SHALL call `IDataFiles.CreateDirectory()` to create a temp dir under `DataPaths.Temp`
- **AND** extract the ZIP to the temp dir
- **AND** call `IDataFiles.ReplaceDirectory(tempDir, dataPaths.CommunityRuleSets)` to atomically swap
- **AND** the old contents SHALL be deleted

#### Scenario: Corrupted ZIP
- **WHEN** a downloaded ZIP is corrupted or incomplete
- **THEN** the actor SHALL log an error, call `IDataFiles.Remove(tempDir)` to clean up, and retain the existing community rulesets

#### Scenario: Disk full during extraction
- **WHEN** extraction fails due to disk space
- **THEN** the actor SHALL log an error and retain the existing community rulesets (no partial state)

### Requirement: RuleSetUpdater writes version.txt after extraction
After a successful extraction, the RuleSetUpdater SHALL write the version using `IDataFiles.WriteText(dataPaths.RuleSetVersion, version)`.

#### Scenario: Version file written after refresh
- **WHEN** a ZIP from release `community-rulesets-v1.1.0` is successfully extracted
- **THEN** `IDataFiles.WriteText(dataPaths.RuleSetVersion, "1.1.0")` SHALL be called
