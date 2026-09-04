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

export interface FilterNodeTrace {
  field: string | null
  op: string | null
  expectedValue: string | null
  actualValue: string | null
  passed: boolean
  skipped: boolean
  group: FilterGroupTrace | null
}

export interface FilterGroupTrace {
  operator: string
  passed: boolean
  nodes: FilterNodeTrace[]
}

export interface IdentificationTraceDetail {
  strategy: string | null
  attempted: boolean
  detail: string | null
}

export interface TracedIdentification {
  season: string | null
  episode: string | null
  title: string | null
}

export interface RuleTrace {
  ruleId: string
  priority: number
  outcome: 'matched' | 'filterFailed' | 'identificationFailed'
  filterTrace: FilterGroupTrace | null
  identificationTrace: IdentificationTraceDetail | null
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
  identification: TracedIdentification | null
  ruleTraces: RuleTrace[]
}

export interface ScoringDetail {
  requestId: string
  source: string
  query: string
  timestamp: string
  itemTraces: ItemTrace[]
}

export interface RuleSetWriteRequest {
  ruleSetId?: string
  topic: string
  aliases?: string[]
  media?: {
    tvdbId?: number | null
    imdbId?: string | null
    tmdbId?: number | null
  }
  confidence?: number
  standalone?: boolean
  disable?: string[]
  rules?: RuleSetWriteRule[]
}

export interface RuleSetWriteRule {
  id: string
  priority: number
  confidence?: number | null
  strategy: string
  seasonRegex?: string | null
  episodeRegex?: string | null
  captureGroup?: number | null
  filters?: {
    all?: FilterConditionInput[]
    any?: FilterConditionInput[]
    not?: FilterConditionInput[]
  } | null
  titleRules?: TitleRuleInput[] | null
}

export interface FilterConditionInput {
  field: string
  op: string
  value: string
}

export interface TitleRuleInput {
  type: string
  field?: string | null
  pattern?: string | null
  captureGroup?: number | null
  value?: string | null
}

export interface TestScoringRequest {
  config: {
    defaultConfidence: number
    rules: RuleSetWriteRule[]
  }
  candidates: TestCandidate[]
}

export interface TestCandidate {
  title: string
  topic: string
  channel: string
  duration: number
  quality: number
  description: string | null
  timestamp: number
}

export interface TestScoringResponse {
  itemTraces: ItemTrace[]
}

export interface MediathekCandidate {
  title: string
  topic: string
  channel: string
  duration: number
  quality: number
  description: string | null
  timestamp: number
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

export function getRuleSetRaw(id: string): Promise<RuleSetWriteRequest> {
  return fetchJson(`/api/rulesets/${encodeURIComponent(id)}/raw`)
}

export function getScoringHistory(id: string, offset = 0, limit = 20): Promise<ScoringHistoryResult> {
  return fetchJson(`/api/rulesets/${encodeURIComponent(id)}/history?offset=${offset}&limit=${limit}`)
}

export function getScoringDetail(id: string, requestId: string): Promise<ScoringDetail> {
  return fetchJson(`/api/rulesets/${encodeURIComponent(id)}/history/${encodeURIComponent(requestId)}`)
}

export async function createRuleSet(data: RuleSetWriteRequest): Promise<{ ruleSetId: string }> {
  const res = await fetch('/api/rulesets', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`)
  }
  return res.json()
}

export async function updateRuleSet(id: string, data: RuleSetWriteRequest): Promise<void> {
  const res = await fetch(`/api/rulesets/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`)
  }
}

export async function deleteRuleSet(id: string): Promise<void> {
  const res = await fetch(`/api/rulesets/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  })
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`)
  }
}

export async function testRuleSet(config: TestScoringRequest['config'], candidates: TestCandidate[]): Promise<TestScoringResponse> {
  const res = await fetch('/api/rulesets/test', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ config, candidates }),
  })
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`)
  }
  return res.json()
}

export function searchMediathek(query: string, limit = 20): Promise<MediathekCandidate[]> {
  return fetchJson(`/api/mediathek/search?q=${encodeURIComponent(query)}&limit=${limit}`)
}
