## MODIFIED Requirements

### Requirement: MovieSearchWorker queries MediathekViewWeb with pagination

The MovieSearchWorker SHALL use the Limit and Offset values from the incoming MovieSearchCommand when constructing the MediathekQuery. If Limit is null, the worker SHALL use a default Size of 50. If Offset is null, the worker SHALL use 0.

#### Scenario: Search with explicit limit and offset

- **WHEN** a MovieSearchCommand with Limit=100 and Offset=20 is received
- **THEN** the MediathekQuery SHALL use Size=100 and Offset=20

#### Scenario: Search with null pagination (defaults)

- **WHEN** a MovieSearchCommand with Limit=null and Offset=null is received
- **THEN** the MediathekQuery SHALL use Size=50 and Offset=0

#### Scenario: Search with only limit specified

- **WHEN** a MovieSearchCommand with Limit=25 and Offset=null is received
- **THEN** the MediathekQuery SHALL use Size=25 and Offset=0
