## Why

When a download fails, the History page shows the full FFmpeg stderr output as the error message - including the complete version string, build configuration, and library versions (~500 chars of compiler flags). The actual error (e.g., "HTTP error 403 Forbidden") is buried at the end. Users must scroll through noise to find the cause.

## What Changes

- Extract meaningful error lines from FFmpeg stderr instead of storing the raw output
- Parse known FFmpeg error patterns (HTTP errors, file not found, codec errors) into human-readable messages
- Store the extracted error as the primary fail message, keep full stderr available for debugging

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `ffmpeg-runner`: Parse FFmpeg stderr to extract meaningful error messages instead of storing raw output

## Impact

- **FunkArr.Download**: `FfmpegRunner.cs` - replace `CapStderr()` with an error extraction method
- **Persistence**: The `FailMessage` field in persistence DTOs will contain shorter, cleaner messages going forward. Existing records with raw stderr are unaffected.
- **UI**: History page will show readable errors without any frontend changes needed
