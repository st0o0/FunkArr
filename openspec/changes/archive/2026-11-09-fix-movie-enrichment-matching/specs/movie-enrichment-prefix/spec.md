## ADDED Requirements

### Requirement: Prefix match for movie enrichment

When a candidate title contains the TMDB title as a prefix (before a ` - ` or ` (`
delimiter), the enrichment must match with confidence 0.95. Suffix stripping handles
` - Spielfilm...`, ` - Drama...`, ` (Kurzfilm)`, ` - Genre, Country Year` patterns.
The existing Levenshtein path remains unchanged — prefix matching is additive.
Comparison is case-insensitive with umlaut normalization.
