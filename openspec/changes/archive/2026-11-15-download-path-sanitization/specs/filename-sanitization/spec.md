# Filename Sanitization

## Purpose

Sanitize untrusted strings for use as file and directory names, preventing path traversal and invalid filesystem operations.

## ADDED Requirements

### Requirement: SanitizeFileName strips invalid characters
`DataPaths` SHALL provide an `internal static string SanitizeFileName(string input)` method that replaces characters invalid in filenames with `_` (underscore). Invalid characters SHALL include all characters from `Path.GetInvalidFileNameChars()` plus an explicit set: `/`, `\`, `:`, `*`, `?`, `"`, `<`, `>`, `|`, and the null byte (`\0`).

#### Scenario: Title with path separator
- **WHEN** `SanitizeFileName("Tatort/../../etc/passwd")` is called
- **THEN** the result SHALL be `"Tatort_.._.._etc_passwd"`

#### Scenario: Title with backslash
- **WHEN** `SanitizeFileName("Title\\Subtitle")` is called
- **THEN** the result SHALL be `"Title_Subtitle"`

#### Scenario: Title with colon
- **WHEN** `SanitizeFileName("Tatort: Schwarzer Freitag")` is called
- **THEN** the result SHALL be `"Tatort_ Schwarzer Freitag"`

#### Scenario: Title with null byte
- **WHEN** `SanitizeFileName("Title\0Injection")` is called
- **THEN** the result SHALL be `"Title_Injection"`

#### Scenario: Clean title passes through unchanged
- **WHEN** `SanitizeFileName("Tatort.S2026E01.GERMAN.1080p.WEB.h264")` is called
- **THEN** the result SHALL be `"Tatort.S2026E01.GERMAN.1080p.WEB.h264"` (unchanged)

### Requirement: SanitizeFileName strips traversal sequences
`SanitizeFileName` SHALL replace `..` sequences with `_` to prevent directory traversal, even when individual characters are not in the invalid set.

#### Scenario: Dot-dot traversal
- **WHEN** `SanitizeFileName("..Title")` is called
- **THEN** the result SHALL NOT start with `..`

#### Scenario: Embedded traversal
- **WHEN** `SanitizeFileName("Title/../../../secret")` is called
- **THEN** the result SHALL NOT contain `..`

### Requirement: SanitizeFileName strips leading and trailing dots and spaces
`SanitizeFileName` SHALL strip leading and trailing dots (`.`) and spaces from the result. Windows filesystems reject filenames ending in dots or spaces.

#### Scenario: Trailing dots
- **WHEN** `SanitizeFileName("Title...")` is called
- **THEN** the result SHALL be `"Title"`

#### Scenario: Leading dot
- **WHEN** `SanitizeFileName(".hidden")` is called
- **THEN** the result SHALL be `"hidden"`

### Requirement: SanitizeFileName collapses consecutive underscores
`SanitizeFileName` SHALL collapse consecutive underscores into a single underscore to keep names readable after replacement.

#### Scenario: Multiple invalid chars in sequence
- **WHEN** `SanitizeFileName("Title::/Subtitle")` is called
- **THEN** the result SHALL be `"Title_Subtitle"` (not `"Title___Subtitle"`)

### Requirement: SanitizeFileName truncates long names
`SanitizeFileName` SHALL truncate the sanitized result to 200 characters to stay within filesystem limits while leaving room for extensions and suffixes.

#### Scenario: Very long title
- **WHEN** `SanitizeFileName` is called with a 300-character title
- **THEN** the result SHALL have at most 200 characters

### Requirement: SanitizeFileName returns fallback for empty results
`SanitizeFileName` SHALL return `"unnamed"` if the sanitized result is empty (all characters were invalid or stripped).

#### Scenario: All-invalid title
- **WHEN** `SanitizeFileName("///...")` is called
- **THEN** the result SHALL be `"unnamed"`

#### Scenario: Empty string
- **WHEN** `SanitizeFileName("")` is called
- **THEN** the result SHALL be `"unnamed"`

#### Scenario: Null input
- **WHEN** `SanitizeFileName(null)` is called
- **THEN** the method SHALL throw `ArgumentNullException`
