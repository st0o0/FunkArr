export interface RuleSetEntry {
  ruleSetId: string
  topic: string
  aliases: string[]
  tvdbId: number | null
  imdbId: string | null
  tmdbId: number | null
}

export interface RuleSetDetailRule {
  id: string
  priority: number
  confidence: number | null
  strategy: string
  filterSummary: string | null
  seasonPattern: string | null
  episodePattern: string | null
  matchMode: string | null
  titleParts: string[] | null
}

export interface RuleSetIdentity {
  topic: string
  aliases: string[]
  tvdbId: number | null
  imdbId: string | null
  tmdbId: number | null
}

export interface RuleSetSource {
  communityPath: string | null
  localPath: string | null
  communityModified: string | null
  localModified: string | null
}

export interface RuleSetDetail {
  ruleSetId: string
  identity: RuleSetIdentity
  source: RuleSetSource
  defaultConfidence: number
  rules: RuleSetDetailRule[]
}

export interface ScoringSnapshot {
  requestId: string
  source: string
  query: string
  timestamp: string
  candidateCount: number
  matchedCount: number
}

export interface ScoringHistoryResult {
  ruleSetId: string
  totalCount: number
  snapshots: ScoringSnapshot[]
}

export interface RuleTrace {
  ruleId: string
  priority: number
  outcome: 'matched' | 'filterFailed' | 'identificationFailed'
  filterTrace: unknown
  identificationTrace: unknown
}

export interface ItemTrace {
  candidateTitle: string
  candidateTopic: string
  candidateChannel: string
  candidateDuration: number
  candidateQuality: number
  candidateDescription: string | null
  candidateTimestamp: number
  matched: boolean
  score: number
  matchedRuleId: string | null
  identification: unknown
  ruleTraces: RuleTrace[]
}

export interface ScoringDetail {
  requestId: string
  source: string
  query: string
  timestamp: string
  itemTraces: ItemTrace[]
}

async function fetchJson<T>(url: string): Promise<T> {
  const res = await fetch(url)
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`)
  }
  return res.json()
}

export function listRuleSets(): Promise<RuleSetEntry[]> {
  return fetchJson('/api/rulesets')
}

export function getRuleSetDetail(id: string): Promise<RuleSetDetail> {
  return fetchJson(`/api/rulesets/${encodeURIComponent(id)}`)
}

export function getScoringHistory(id: string, offset = 0, limit = 20): Promise<ScoringHistoryResult> {
  return fetchJson(`/api/rulesets/${encodeURIComponent(id)}/history?offset=${offset}&limit=${limit}`)
}

export function getScoringDetail(id: string, requestId: string): Promise<ScoringDetail> {
  return fetchJson(`/api/rulesets/${encodeURIComponent(id)}/history/${encodeURIComponent(requestId)}`)
}
