## Context

German public broadcaster shows often use air dates rather than season/episode numbering. FunkArr correctly formats these as YYYY-MM-DD in release titles. Sonarr's "Daily" series type can parse these, but users must manually set this type per series.

## Goals / Non-Goals

**Goals:**
- Users know to configure daily shows correctly in Sonarr

**Non-Goals:**
- Auto-detecting or auto-configuring series type (Sonarr API doesn't support changing type after creation easily)
- Changing FunkArr's title format for daily shows

## Decisions

### Decision: Add a tip to the Setup Guide Sonarr step

A short note under the Sonarr config table explaining that shows identified by air date (like Die Sendung mit der Maus, Bibi Blocksberg, etc.) need to be set to "Daily" series type in Sonarr for automatic import to work.

## Risks / Trade-offs

None - documentation only.
