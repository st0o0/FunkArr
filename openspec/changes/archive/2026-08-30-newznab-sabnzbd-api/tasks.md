## 1. Shared Infrastructure

- [x] 1.1 Add DownloadPath property to FunkArrOptions (default "downloads")
- [x] 1.2 Create ApiKeyFilter endpoint filter in FunkArr.Core — validates apikey query param against FunkArrOptions.ApiKey, returns 403 with format-appropriate error body (XML for indexer, JSON for download)

## 2. Newznab Indexer API — XML Models

- [x] 2.1 Create Newznab XML model records in FunkArr.IndexerApi: Rss, Channel, Response, Item, Guid, Enclosure, NewznabAttribute — with XmlSerializer attributes and newznab namespace
- [x] 2.2 Create caps XML model: Caps, Limits, Registration, Searching, SearchType, Categories, Category, SubCategory
- [x] 2.3 Create NZB XML generation helper — builds minimal NZB with URL/title in comments
- [x] 2.4 Tests: serialize empty RSS response, verify XML structure matches Newznab format; serialize caps, verify all elements present

## 3. Newznab Indexer API — Endpoints

- [x] 3.1 Create IndexerApiEndpoints static class with MapIndexerApi extension method — registers all routes under /index/api with ApiKey filter
- [x] 3.2 Implement ?t=caps handler returning capabilities XML
- [x] 3.3 Implement ?t=tvsearch handler — accepts tvdbid, season, ep, q; returns empty Newznab RSS (stub)
- [x] 3.4 Implement ?t=search handler — accepts q; returns empty Newznab RSS (stub)
- [x] 3.5 Implement ?t=movie handler — accepts imdbid, q; returns empty Newznab RSS (stub)
- [x] 3.6 Implement /index/api/nzb endpoint — base64 decode URL/title params, return NZB XML
- [x] 3.7 Return 404 for unknown t parameter
- [x] 3.8 Tests: endpoint routing, caps content, empty search results, NZB generation, auth rejection

## 4. SABnzbd Download API — Models

- [x] 4.1 Create SABnzbd JSON models in FunkArr.DownloadApi: QueueResponse, QueueSlot, HistoryResponse, HistorySlot, ConfigResponse — as sealed records with System.Text.Json attributes
- [x] 4.2 Create DownloadState singleton service — ConcurrentDictionary-based in-memory queue + history, AddToQueue, GetQueue, GetHistory, DeleteHistoryItem methods
- [x] 4.3 Tests: DownloadState add/get/delete operations, JSON serialization of responses

## 5. SABnzbd Download API — Endpoints

- [x] 5.1 Create DownloadApiEndpoints static class with MapDownloadApi extension method — registers all routes under /download/api with ApiKey filter
- [x] 5.2 Implement ?mode=version handler
- [x] 5.3 Implement ?mode=get_config handler — reads DownloadPath from FunkArrOptions, returns SABnzbd config JSON with categories
- [x] 5.4 Implement ?mode=queue handler — reads from DownloadState
- [x] 5.5 Implement ?mode=history handler — reads from DownloadState, supports name=delete&value=<id> with optional del_files
- [x] 5.6 Implement POST ?mode=addfile handler — parse NZB body for URL/title comments, add to DownloadState queue
- [x] 5.7 Return 400 for unknown mode
- [x] 5.8 Tests: endpoint routing, version/config/queue/history responses, addfile flow, delete flow, auth rejection

## 6. Host Wiring

- [x] 6.1 Register DownloadState as singleton in FunkArrServiceSetup
- [x] 6.2 Call MapIndexerApi and MapDownloadApi from FunkArrApplicationSetup
- [x] 6.3 Full solution build + dotnet format + run all test projects
