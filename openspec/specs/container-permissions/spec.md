## Requirements

### Requirement: Configurable runtime user identity
The container SHALL accept `PUID` and `PGID` environment variables to control which UID and GID the FunkArr process runs as. Both SHALL default to `1654` when not specified.

#### Scenario: Default identity without PUID/PGID
- **WHEN** the container starts without PUID or PGID environment variables
- **THEN** the FunkArr process SHALL run as UID 1654 and GID 1654

#### Scenario: Custom PUID and PGID
- **WHEN** the container starts with `PUID=1000` and `PGID=1000`
- **THEN** the FunkArr process SHALL run as UID 1000 and GID 1000

#### Scenario: Only PUID specified
- **WHEN** the container starts with `PUID=1000` and no PGID
- **THEN** the FunkArr process SHALL run as UID 1000 and GID 1654

### Requirement: Root mode bypass
The container SHALL skip user creation and privilege dropping when PUID or PGID is set to `0`, running the process as root.

#### Scenario: PUID set to zero
- **WHEN** the container starts with `PUID=0`
- **THEN** the FunkArr process SHALL run as root (UID 0)

#### Scenario: PGID set to zero
- **WHEN** the container starts with `PGID=0`
- **THEN** the FunkArr process SHALL run as root (UID 0)

### Requirement: Data directory ownership
The entrypoint SHALL set ownership of `/app/data` (recursively) to the configured PUID:PGID before starting the application.

#### Scenario: Data directory chown on startup
- **WHEN** the container starts with `PUID=1000` and `PGID=1000`
- **THEN** all files and directories under `/app/data` SHALL be owned by UID 1000 and GID 1000

#### Scenario: Ownership transition from previous config
- **WHEN** `/app/data` contains files owned by UID 1654 and the container starts with `PUID=1000`
- **THEN** all files under `/app/data` SHALL be re-owned to UID 1000

### Requirement: Media volume not modified
The entrypoint SHALL NOT chown or modify ownership of the `/media` volume or any user-configured download paths outside `/app/data`.

#### Scenario: Media volume untouched
- **WHEN** the container starts with any PUID/PGID values
- **THEN** file ownership under `/media` SHALL remain unchanged

### Requirement: Privilege dropping via su-exec
The entrypoint SHALL use `su-exec` to drop from root to the configured user before executing the .NET application. The `su-exec` call SHALL use `exec` to replace the shell process.

#### Scenario: Process runs as non-root
- **WHEN** the container starts with `PUID=1000` and `PGID=1000`
- **THEN** the `dotnet FunkArr.dll` process SHALL have effective UID 1000 and GID 1000
- **AND** there SHALL be no intermediate shell process between the entrypoint and the .NET process

### Requirement: No umask workaround
The entrypoint SHALL NOT set `umask 000` or any other permissive umask. File permissions SHALL be determined by the process's default umask (0022).

#### Scenario: Default umask
- **WHEN** the container starts
- **THEN** the FunkArr process SHALL run with umask 0022
- **AND** newly created files SHALL have permissions 644 and directories 755
