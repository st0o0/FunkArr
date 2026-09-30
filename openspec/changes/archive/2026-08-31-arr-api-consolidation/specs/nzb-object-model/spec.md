## REMOVED Requirements

### Requirement: NZB model classes are duplicated per adapter
**Reason**: IndexerApi and DownloadApi are merged into a single ArrApi project. The NZB model is now shared within that project — duplication is no longer needed.
**Migration**: Single `Nzb.cs` in `FunkArr.ArrApi` root namespace replaces both `FunkArr.IndexerApi.Models.Nzb` and `FunkArr.DownloadApi.Models.Nzb`.

#### Scenario: No cross-adapter reference
- **WHEN** reviewing project references
- **THEN** IndexerApi does not reference DownloadApi and vice versa
- **AND** both projects contain their own NZB model classes

## ADDED Requirements

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
