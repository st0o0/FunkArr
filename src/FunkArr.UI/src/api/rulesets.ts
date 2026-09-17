export interface RuleSetEntry {
  ruleSetId: string
  topic: string
  aliases: string[]
  tvdbId: number | null
  imdbId: string | null
  tmdbId: number | null
  mediaName: string | null
  mediaType: number | null
  ruleCount: number
  sourceType: number
  lastScoringRun: string | null
  matchRate: number | null
}

export interface FilterConditionOutput {
  field: string
  op: string
  value: string
}

export interface FilterGroupOutput {
  all: FilterConditionOutput[] | null
  any: FilterConditionOutput[] | null
  not: FilterConditionOutput[] | null
}

export interface TitleRuleOutput {
  type: string
  field: string | null
  pattern: string | null
  captureGroup: number | null
  value: string | null
}

export interface RuleSetDetailRule {
  id: string
  priority: number
  confidence: number | null
  strategy: string
  seasonRegex: string | null
  episodeRegex: string | null
  captureGroup: number | null
  filters: FilterGroupOutput | null
  titleRules: TitleRuleOutput[] | null
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
    name: string
    type: string
    tvdbId?: number
    imdbId?: string
    tmdbId?: number
  }
  confidence?: number
  standalone?: boolean
  disable?: string[]
  rules?: RuleSetWriteRule[]
}

export interface RuleSetWriteRule {
  id: string
  priority: number
  confidence?: number
  strategy: string
  seasonRegex?: string
  episodeRegex?: string
  captureGroup?: number
  filters?: {
    all?: FilterConditionInput[]
    any?: FilterConditionInput[]
    not?: FilterConditionInput[]
  }
  titleRules?: TitleRuleInput[]
}

export interface FilterConditionInput {
  field: string
  op: string
  value: string
}

export interface TitleRuleInput {
  type: string
  field?: string
  pattern?: string
  captureGroup?: number
  value?: string
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

export interface RuleSetListResponse {
  communityVersion: string | null
  rulesets: RuleSetEntry[]
}

export function listRuleSets(): Promise<RuleSetListResponse> {
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

export interface ValidationError {
  field: string
  message: string
}

export class ValidationFailedError extends Error {
  readonly errors: ValidationError[]
  constructor(errors: ValidationError[]) {
    super('Validation failed')
    this.errors = errors
  }
}

async function throwIfNotOk(res: Response): Promise<void> {
  if (res.ok) return
  if (res.status === 422) {
    const body = await res.json()
    if (body.errors) {
      throw new ValidationFailedError(body.errors)
    }
  }
  throw new Error(`${res.status} ${res.statusText}`)
}

export async function createRuleSet(data: RuleSetWriteRequest): Promise<{ ruleSetId: string }> {
  const res = await fetch('/api/rulesets', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
  await throwIfNotOk(res)
  return res.json()
}

export async function updateRuleSet(id: string, data: RuleSetWriteRequest): Promise<void> {
  const res = await fetch(`/api/rulesets/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
  await throwIfNotOk(res)
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

export async function searchMediathek(query: string, limit = 20): Promise<MediathekCandidate[]> {
  const data = await fetchJson<{ items: MediathekCandidate[] }>(`/api/mediathek/search?q=${encodeURIComponent(query)}&limit=${limit}`)
  return data.items
}

export async function exportRuleSet(id: string): Promise<void> {
  const res = await fetch(`/api/rulesets/${encodeURIComponent(id)}/export`)
  if (!res.ok) {
    if (res.status === 422) {
      const body = await res.json()
      if (body.errors) {
        throw new ValidationFailedError(body.errors)
      }
    }
    throw new Error(res.status === 404 ? 'No local ruleset found to export' : `${res.status} ${res.statusText}`)
  }
  const blob = await res.blob()
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `${id}.json`
  a.click()
  URL.revokeObjectURL(url)
}
