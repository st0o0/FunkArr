## Why

Zero URL validation exists from NZB payload ingestion through to ffmpeg execution. Anyone with the API key (every Arr instance on the compose network) can craft an NZB with a `file://`, `ftp://`, or internal `http://` URL as the video source. ffmpeg reads it and writes the output to the complete directory. This is a server-side request forgery (SSRF) vulnerability that should be closed before any public release.

## What Changes

- Add URL scheme allowlist (http, https only) at the two entry points where external URLs enter the download pipeline
- Validate both `Url` and `SubtitleUrl` fields — both reach ffmpeg
- Reject non-allowed schemes early, before the URL reaches the domain layer
- Entry point 1: `NzbPayload.Decode()` — Base64 JSON payload from Newznab `t=get` responses
- Entry point 2: `NzbService.FromNzb()` — `<meta type="funkarr-url">` inside uploaded NZB XML

## Capabilities

### New Capabilities

- `url-scheme-validation`: URL scheme allowlist guard for download pipeline entry points

### Modified Capabilities

_(none — no existing spec-level requirements change)_

## Impact

- `FunkArr.ArrApi` — two files modified, one new validation helper
- No persistence changes, no message changes, no domain changes
- Downloads with non-http(s) URLs will be rejected at the ArrApi boundary instead of reaching ffmpeg
- Existing valid downloads (all http/https from Mediathek CDNs) are unaffected
