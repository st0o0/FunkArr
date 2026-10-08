## ADDED Requirements

### Requirement: FFmpeg limits thread count for stream-copy
The argument builder SHALL apply `-threads 1` as an input option on all FFmpeg
invocations. Stream-copy operations are I/O-bound and do not benefit from
multiple threads; limiting to one thread reduces CPU scheduler contention
under cgroup CPU limits.

#### Scenario: Direct MP4 includes thread limit
- **WHEN** building arguments for a direct MP4 URL
- **THEN** FFmpeg SHALL be invoked with `-threads 1`

#### Scenario: HLS includes thread limit
- **WHEN** building arguments for an HLS URL
- **THEN** FFmpeg SHALL be invoked with `-threads 1`

#### Scenario: Thread limit is an input option
- **WHEN** building arguments for any input
- **THEN** `-threads 1` SHALL appear before the input URL in the command line (as an input option, not an output option)
