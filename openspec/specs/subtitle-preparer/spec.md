# Subtitle Preparer

## Purpose

~~Removed.~~ SubtitlePreparer functionality has been absorbed into Remuxer. The class and its ISubtitlePreparer interface are deleted. Remuxer receives IHttpClientFactory via DI and handles subtitle download internally using the same named HttpClient pattern (`route:{routeName}`).

## Requirements

_All requirements removed. See remuxer spec for the replacement._
