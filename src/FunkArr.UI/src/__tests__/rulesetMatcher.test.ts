import { describe, test, expect } from 'vitest'
import { matchCandidate, tryMatch, type BuilderRule } from '../composables/useRulesetMatcher'
import type { MediathekCandidate } from '../api/rulesets'
import { Strategy, TitlePartType, FilterField } from '../api/enumMaps'

function makeCandidate(overrides: Partial<MediathekCandidate> = {}): MediathekCandidate {
  return {
    title: 'Tatort - Angezählt',
    topic: 'Tatort',
    channel: 'ORF',
    duration: 5508,
    quality: 1080,
    description: null,
    timestamp: Date.now() / 1000,
    ...overrides,
  }
}

function makeRule(overrides: Partial<BuilderRule> = {}): BuilderRule {
  return {
    id: 'rule-1',
    priority: 100,
    strategy: Strategy.SeasonAndEpisodeNumber,
    seasonRegex: 'S(\\d+)',
    episodeRegex: 'E(\\d+)',
    captureGroup: 1,
    titleRules: [],
    ...overrides,
  }
}

describe('matchCandidate', () => {
  test('returns match when rule matches', () => {
    const candidate = makeCandidate({ title: 'Tatort S2026E22' })
    const rules = [makeRule()]
    const result = matchCandidate(candidate, rules)
    expect(result.matchedRuleId).toBe('rule-1')
    expect(result.extractions.season).toBe('2026')
    expect(result.extractions.episode).toBe('22')
  })

  test('returns no match when no rule matches', () => {
    const candidate = makeCandidate({ title: 'No pattern here' })
    const rules = [makeRule()]
    const result = matchCandidate(candidate, rules)
    expect(result.matchedRuleId).toBeNull()
  })

  test('matches first rule by priority order', () => {
    const candidate = makeCandidate({ title: 'Tatort S01E01' })
    const rules = [
      makeRule({ id: 'high', priority: 10 }),
      makeRule({ id: 'low', priority: 100 }),
    ]
    const result = matchCandidate(candidate, rules)
    expect(result.matchedRuleId).toBe('high')
  })

  test('empty rules returns no match', () => {
    const result = matchCandidate(makeCandidate(), [])
    expect(result.matchedRuleId).toBeNull()
  })
})

describe('tryMatch', () => {
  test('SeasonEpisode strategy extracts season and episode', () => {
    const candidate = makeCandidate({ title: 'Show S03E14 Title' })
    const result = tryMatch(candidate, makeRule())
    expect(result).not.toBeNull()
    expect(result!.season).toBe('03')
    expect(result!.episode).toBe('14')
  })

  test('SeasonEpisode strategy returns null on no match', () => {
    const candidate = makeCandidate({ title: 'No pattern' })
    const result = tryMatch(candidate, makeRule())
    expect(result).toBeNull()
  })

  test('AbsoluteEpisode strategy', () => {
    const candidate = makeCandidate({ title: 'Show E42 Title' })
    const rule = makeRule({
      strategy: Strategy.AbsoluteEpisodeNumber,
      episodeRegex: 'E(\\d+)',
    })
    const result = tryMatch(candidate, rule)
    expect(result).not.toBeNull()
    expect(result!.episode).toBe('42')
  })

  test('TitleExact strategy', () => {
    const candidate = makeCandidate({ title: 'Exact Title Match' })
    const rule = makeRule({
      strategy: Strategy.TitleExact,
      titleRules: [{ type: TitlePartType.Static, field: FilterField.Title, pattern: '', captureGroup: null, value: 'Exact Title Match' }],
    })
    const result = tryMatch(candidate, rule)
    expect(result).not.toBeNull()
  })

  test('TitleIncludes strategy', () => {
    const candidate = makeCandidate({ title: 'This Contains the Keyword here' })
    const rule = makeRule({
      strategy: Strategy.TitleIncludes,
      titleRules: [{ type: TitlePartType.Static, field: FilterField.Title, pattern: '', captureGroup: null, value: 'Keyword' }],
    })
    const result = tryMatch(candidate, rule)
    expect(result).not.toBeNull()
  })

  test('invalid regex does not throw', () => {
    const candidate = makeCandidate({ title: 'Test' })
    const rule = makeRule({ seasonRegex: '(invalid[' })
    expect(() => tryMatch(candidate, rule)).not.toThrow()
  })

  test('unknown strategy returns null', () => {
    const candidate = makeCandidate()
    const rule = makeRule({ strategy: 99 })
    const result = tryMatch(candidate, rule)
    expect(result).toBeNull()
  })
})
