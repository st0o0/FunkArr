import type { RuleSetEntry, RuleSetDetail, ScoringSnapshot, ScoringDetail, ItemTrace, RuleSetListResponse } from '../../src/api/rulesets'

let counter = 0

export function makeRuleSetEntry(overrides?: Partial<RuleSetEntry>): RuleSetEntry {
  const id = `rs-${++counter}`
  return {
    ruleSetId: id,
    topic: 'Tatort',
    aliases: ['Tatort - Münster'],
    tvdbId: 83214,
    imdbId: 'tt0806922',
    tmdbId: null,
    mediaName: 'Tatort',
    mediaType: 0,
    ruleCount: 3,
    sourceType: 0,
    lastScoringRun: new Date().toISOString(),
    matchRate: 0.85,
    enrichmentRate: 0.72,
    ...overrides,
  }
}

export function makeRuleSetDetail(overrides?: Partial<RuleSetDetail>): RuleSetDetail {
  return {
    ruleSetId: 'rs-detail-1',
    identity: {
      topic: 'Tatort',
      aliases: ['Tatort - Münster'],
      tvdbId: 83214,
      imdbId: 'tt0806922',
      tmdbId: null,
    },
    source: {
      communityPath: 'rulesets/community/tatort.json',
      localPath: null,
      communityModified: new Date().toISOString(),
      localModified: null,
    },
    defaultConfidence: 80,
    rules: [
      {
        id: 'rule-1',
        priority: 100,
        confidence: null,
        strategy: 0,
        seasonRegex: null,
        episodeRegex: null,
        captureGroup: null,
        filters: { all: [{ field: 0, op: 0, value: 'Tatort' }], any: null, not: null },
        titleRules: [{ type: 0, field: 0, pattern: 'S(\\d+)E(\\d+)', captureGroup: 1, value: null }],
      },
    ],
    enrichment: {
      enabled: true,
      methods: [0, 1],
      title: { threshold: 80 },
      airdate: { tolerance: 2 },
      runtime: { tolerance: 120, mode: 0 },
      year: { tolerance: 1 },
    },
    ...overrides,
  }
}

export function makeRuleSetListResponse(rulesets?: RuleSetEntry[]): RuleSetListResponse {
  return {
    communityVersion: 'v0.31',
    rulesets: rulesets ?? [makeRuleSetEntry()],
  }
}

export function makeScoringSnapshot(overrides?: Partial<ScoringSnapshot>): ScoringSnapshot {
  return {
    requestId: `req-${++counter}`,
    source: 0,
    query: 'Tatort S2026E22',
    timestamp: new Date().toISOString(),
    candidateCount: 15,
    matchedCount: 3,
    ...overrides,
  }
}

export function makeItemTrace(overrides?: Partial<ItemTrace>): ItemTrace {
  return {
    candidate: {
      title: 'Tatort - Angezählt',
      topic: 'Tatort',
      channel: 'ORF',
      duration: 5508,
      quality: 1080,
      description: null,
      timestamp: Date.now() / 1000,
    },
    matched: true,
    score: 95,
    matchedRuleId: 'rule-1',
    identification: { season: '2026', episode: '22', title: 'Angezählt' },
    ruleTraces: [],
    enrichmentTrace: null,
    ...overrides,
  }
}
