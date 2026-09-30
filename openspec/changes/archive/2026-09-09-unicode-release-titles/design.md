## Context

`ReleaseTitleBuilder.Sanitize()` calls `NormalizeUmlauts()` which replaces a->ae, o->oe, u->ue, ss->ss before building the dotted release title. This was originally done for filesystem safety and scene-release convention. However, Sonarr's internal normalizer maps a->a, o->o (drops the diacritical, keeps single char), so the digraph expansion creates a mismatch that prevents automatic series matching during RSS sync.

The title flows through three places:
1. **Newznab XML `<title>`** - Sonarr parses this to match series/episodes
2. **NZB metadata `<meta type="title">`** - carried through to the download client
3. **File path on disk** - directory and filename for the completed MKV

## Goals / Non-Goals

**Goals:**
- Sonarr can match FunkArr release titles to TVDB series names during RSS sync
- Unicode characters (Umlauts, ss) pass through the title builder unchanged
- Existing special character stripping and dot formatting remain intact

**Non-Goals:**
- Fixing date-based episode numbering (separate change)
- Fixing the Setup wizard URL base documentation (separate change)
- Supporting legacy filesystems that can't handle Unicode (FAT32 etc.)
- Changing the `-FunkArr` release group suffix or any other title format conventions

## Decisions

### Decision: Remove NormalizeUmlauts entirely instead of making it conditional

**Chosen:** Delete the `NormalizeUmlauts()` method and its call in `Sanitize()`.

**Alternative considered:** Keep `NormalizeUmlauts()` and add a `forFileSystem` bool parameter to `Build()`. Rejected because:
- Modern filesystems (ext4, NTFS, APFS, btrfs) all handle Unicode natively
- Docker volume mounts pass Unicode through transparently
- Sonarr renames files on import anyway, so the download filename is transient
- The conditional approach adds complexity for no real benefit
- FunkArr runs in Docker on Linux - ext4 is guaranteed

### Decision: Keep the special character strip list unchanged

Characters like `/:;"'@#?$%^*+=!<>,()&` are still stripped. These are genuinely problematic in filenames and URLs. Only the Umlaut normalization is removed.

### Decision: No migration for existing data

Existing downloads in Sonarr's history already have ASCII titles. These are historical records and don't need updating. New downloads will simply have Unicode titles going forward. Akka persistence journal entries store the original title from when the download was created - no replay issues.

## Risks / Trade-offs

**[Risk] Pipe characters in titles** -> The `|` character is not in the strip list but appears in some Mediathek titles (e.g. "Unser Sandmannchen | 08.09.2026"). Verify it's handled. Mitigation: check the existing strip list and add `|` if missing.

**[Risk] XML encoding** -> Unicode in XML `<title>` elements requires proper UTF-8 encoding. Mitigation: ASP.NET's `XmlSerializer` handles UTF-8 by default, and the existing `Utf8StringWriter` already ensures correct encoding.

**[Trade-off] Mixed titles in download history** -> After deployment, history will contain a mix of ASCII-normalized old titles and Unicode new titles. This is cosmetic only and acceptable for 0.x.
