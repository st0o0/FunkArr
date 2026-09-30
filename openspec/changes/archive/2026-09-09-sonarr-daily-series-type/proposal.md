## Why

9 of the Sonarr import warnings were "Invalid season or episode" because series with date-based episode identification (Die Sendung mit der Maus, Bibi Blocksberg, etc.) were configured as "Standard" series type in Sonarr. Sonarr needs "Daily" series type to parse YYYY-MM-DD dates from release titles.

## What Changes

- Add a note to the Sonarr configuration step in the Setup Guide explaining that daily/date-based shows need to be configured as "Daily" series type in Sonarr
- This is a documentation/guidance change, not a code behavior change

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `setup-guide-ui`: Add guidance about Sonarr series type configuration for daily shows

## Impact

- **FunkArr.UI**: `Setup.vue` - add a tip to the Sonarr config step about daily series type
