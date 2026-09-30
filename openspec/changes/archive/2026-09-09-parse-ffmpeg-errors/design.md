## Context

`FfmpegRunner` catches `FFMpegException` and stores `ex.FFMpegErrorOutput` (capped at 4096 chars) as the fail message. This stderr contains the full FFmpeg version banner, library versions, and build flags before the actual error. The error line is typically the last meaningful line starting with "Error" or containing "HTTP error".

## Goals / Non-Goals

**Goals:**
- Failed downloads show a one-line human-readable error message
- Common FFmpeg errors are recognized and cleanly formatted

**Non-Goals:**
- Full FFmpeg log viewer in the UI (could be a future enhancement)
- Changing how FFmpeg errors are logged to Serilog (keep full stderr in logs)

## Decisions

### Decision: Extract last error line from stderr, fall back to capped stderr

Parse stderr from the bottom, looking for lines matching known error patterns:
- `HTTP error \d+ .*` (e.g., "HTTP error 403 Forbidden")
- `Error opening input.*` (e.g., "Error opening input file https://...")
- `Invalid data found when processing input`
- `Server returned \d+ .*`

If a known pattern matches, use that line (trimmed) as the fail message. If no pattern matches, use the last non-empty line. This gives a clean message for common failures while still capturing unknown errors.

### Decision: Log full stderr, store extracted error

Continue logging the full stderr to Serilog at Warning level for debugging. Only the extracted message goes into the persistence DTO's FailMessage field.

## Risks / Trade-offs

**[Trade-off] Less detail in FailMessage** -> Advanced users lose the full stderr in the UI history. Mitigation: full output remains in application logs. A future "Show details" expander could expose it.
