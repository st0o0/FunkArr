## Why

FunkArr normalizes German Umlauts to ASCII digraphs (o->oe, u->ue, a->ae) in Newznab release titles. Sonarr's internal title normalizer maps o->o, u->u, a->a instead. This mismatch ("loewenzahn" vs "lowenzahn") prevents Sonarr from matching releases to series during RSS sync and manual import. In E2E testing, 22 of 39 Sonarr import warnings were "series title mismatch" caused by this divergence.

## What Changes

- Remove Umlaut-to-ASCII normalization from `ReleaseTitleBuilder.Sanitize()`. Umlauts (a, o, u, ss) pass through unchanged in release titles.
- Special character stripping (`:`, `?`, etc.), space-to-dot conversion, and dot collapsing remain unchanged.
- Newznab `<title>` and NZB metadata both carry the Unicode title.
- Download file paths use the same Unicode title. Modern filesystems (ext4, NTFS, APFS) handle Unicode natively; Docker volumes likewise.
- Update the `release-title-format` spec to reflect the new behavior.
- Update all affected tests.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `release-title-format`: Remove the "Umlaut normalization" requirement. Umlauts and ss remain as-is in titles instead of being replaced with ASCII digraphs. All scenario examples that show normalized output change to Unicode equivalents.

## Impact

- **FunkArr.Core**: `ReleaseTitleBuilder.cs` - remove `NormalizeUmlauts()` call from `Sanitize()`
- **FunkArr.Search.Tests**: `ReleaseTitleBuilderTests.cs` - update all assertions that expect ASCII-normalized Umlauts
- **Newznab API**: Release titles in RSS/search responses will contain Unicode characters. This is valid XML (UTF-8 encoded) and Sonarr handles it correctly.
- **Download paths**: File and directory names will contain Unicode. No compatibility issues on Linux (ext4) or Windows (NTFS). Docker volume mounts pass through Unicode transparently.
- **Existing downloads**: No migration needed. Only new downloads get Unicode titles. Existing completed downloads in Sonarr's history are unaffected.
- **Breaking for consumers**: Any external tool that assumed ASCII-only release titles may need adjustment, but this is unlikely given FunkArr's early version (0.x).
