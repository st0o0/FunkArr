---
layout: home

hero:
  name: FunkArr
  text: Mediathek meets *arr
  tagline: Durchsucht ARD, ZDF, ORF, SRF und andere deutschsprachige öffentlich-rechtliche Mediatheken - lädt Inhalte herunter, remuxed und stellt sie über Standard-Newznab- und SABnzbd-APIs bereit.
  actions:
    - theme: brand
      text: Erste Schritte
      link: /getting-started
    - theme: alt
      text: Regelwerk-Katalog
      link: /rulesets/catalog

features:
  - title: Newznab Indexer
    details: FunkArr lässt sich in Prowlarr oder direkt in Sonarr/Radarr als Standard-Newznab-Indexer hinzufügen.
  - title: SABnzbd Download Client
    details: Als SABnzbd-Download-Client hinzufügen - Sonarr und Radarr verwalten Downloads nativ.
  - title: Community-Regelwerke
    details: Chaotische Mediathek-Titel werden in strukturiertes Staffel-/Episodenformat umgewandelt. Automatisch von GitHub synchronisiert.
  - title: Proxy und Geo-Routing
    details: Leite Sender-spezifischen Traffic uber VPN oder Proxy - ideal fur geo-eingeschrankte Inhalte von ORF, SRF und anderen.
  - title: Observability
    details: OpenTelemetry-Tracing und -Metriken fur Downloads, Scoring und Enrichment. Aspire Dashboard oder jedes OTLP-Backend.
  - title: Ein Container
    details: Läuft auf jedem Docker-Host mit SQLite als Standard. Optionales PostgreSQL fur grossere Setups. PUID/PGID-Support.
---
