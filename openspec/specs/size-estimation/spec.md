# Size Estimation

## Purpose

Estimate file size from duration and quality tier when the MediathekViewWeb API returns null for an item's size.

## Requirements

### Requirement: Estimate file size when upstream returns null

When the MediathekViewWeb API returns null for an item's size, the system SHALL estimate the file size based on the item's duration and quality tier. The estimation SHALL happen at the API response mapping boundary in `MediathekViewWebManager`.

#### Scenario: HD item with null size

- **WHEN** a MVW API response item has `size: null` and `url_video_hd` is not null
- **THEN** the mapped `MediathekItem.Size` SHALL be `duration × 312500` (approximately 2.5 Mbps)

#### Scenario: SD item with null size

- **WHEN** a MVW API response item has `size: null`, `url_video_hd` is null, and `url_video` is not null
- **THEN** the mapped `MediathekItem.Size` SHALL be `duration × 187500` (approximately 1.5 Mbps)

#### Scenario: Low quality item with null size

- **WHEN** a MVW API response item has `size: null`, `url_video_hd` is null, `url_video` is null, and `url_video_low` is not null
- **THEN** the mapped `MediathekItem.Size` SHALL be `duration × 100000` (approximately 0.8 Mbps)

#### Scenario: Item with known size

- **WHEN** a MVW API response item has a non-null size value
- **THEN** the mapped `MediathekItem.Size` SHALL use the actual size value unchanged

#### Scenario: Item with no video URLs and null size

- **WHEN** a MVW API response item has `size: null` and all video URLs are null
- **THEN** the mapped `MediathekItem.Size` SHALL be 0
