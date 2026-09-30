# release-title-format (delta)

## MODIFIED Requirements

### Requirement: Umlaut normalization

ReleaseTitleBuilder SHALL preserve German Umlauts and special characters (a, o, u, A, O, U, ss) as-is in release titles. No ASCII digraph normalization SHALL be applied.

#### Scenario: Umlauts preserved in title

- **WHEN** Build is called with topic "Loewenzahn" containing "o"
- **THEN** the "o" SHALL remain as "o" in the output, not be replaced with "oe"

#### Scenario: All German special characters preserved

- **WHEN** a title contains a, o, u, A, O, U, ss
- **THEN** they SHALL appear unchanged in the output (NOT ae, oe, ue, Ae, Oe, Ue, ss)

#### Scenario: Full title with Umlauts

- **WHEN** Build is called with topic "Uberfuhrung", title "Schone Grusse", quality 720, category "tv"
- **THEN** the result SHALL be `Uberfuhrung.Schone.Grusse.GERMAN.720p.WEB.h264-FunkArr`

#### Scenario: Eszett preserved

- **WHEN** Build is called with topic "Strasse", title "Spass"
- **THEN** the result SHALL be `Strasse.Spass.GERMAN.720p.WEB.h264-FunkArr`

## REMOVED Requirements

### Requirement: Umlaut normalization

**Reason**: ASCII digraph normalization (o->oe, u->ue, a->ae) causes Sonarr title matching failures. Sonarr's internal normalizer maps o->o (single char), creating a mismatch with FunkArr's "oe" digraph. Modern filesystems handle Unicode natively.

**Migration**: No migration needed. New downloads will use Unicode titles. Existing downloads in history retain their ASCII-normalized titles.
