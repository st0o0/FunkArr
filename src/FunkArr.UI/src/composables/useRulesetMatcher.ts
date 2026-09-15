import { ref, watch, type Ref } from 'vue'
import type { MediathekCandidate } from '../api/rulesets'

interface BuilderRule {
  id: string
  priority: number
  strategy: string
  seasonRegex: string
  episodeRegex: string
  captureGroup: number | null
  titleRules: { type: string; field: string; pattern: string; captureGroup: number | null; value: string }[]
}

export interface MatchExtractions {
  season?: string
  episode?: string
  title?: string
  airdate?: string
}

export interface MatchResult {
  candidate: MediathekCandidate
  matchedRuleId: string | null
  extractions: MatchExtractions
}

export function useRulesetMatcher(
  candidates: Ref<MediathekCandidate[]>,
  rules: Ref<BuilderRule[]>,
) {
  const results = ref<MatchResult[]>([])
  const matchedCount = ref(0)
  const total = ref(0)

  let timeout: ReturnType<typeof setTimeout> | null = null

  function runMatching() {
    const items = candidates.value
    const ruleList = [...rules.value].sort((a, b) => a.priority - b.priority)
    const matched: MatchResult[] = []

    for (const candidate of items) {
      const result = matchCandidate(candidate, ruleList)
      matched.push(result)
    }

    results.value = matched
    matchedCount.value = matched.filter(r => r.matchedRuleId !== null).length
    total.value = items.length
  }

  watch([candidates, rules], () => {
    if (timeout) clearTimeout(timeout)
    timeout = setTimeout(runMatching, 500)
  }, { deep: true })

  return { results, matchedCount, total }
}

function matchCandidate(candidate: MediathekCandidate, rules: BuilderRule[]): MatchResult {
  for (const rule of rules) {
    const extractions = tryMatch(candidate, rule)
    if (extractions !== null) {
      return { candidate, matchedRuleId: rule.id, extractions }
    }
  }
  return { candidate, matchedRuleId: null, extractions: {} }
}

function tryMatch(candidate: MediathekCandidate, rule: BuilderRule): MatchExtractions | null {
  switch (rule.strategy) {
    case 'seasonAndEpisodeNumber':
      return matchSeasonEpisode(candidate.title, rule)
    case 'byAbsoluteEpisodeNumber':
      return matchAbsoluteEpisode(candidate.title, rule)
    case 'itemTitleExact':
      return matchTitle(candidate, rule, 'exact')
    case 'itemTitleIncludes':
      return matchTitle(candidate, rule, 'includes')
    case 'itemTitleEqualsAirdate':
      return matchAirdate(candidate, rule)
    default:
      return null
  }
}

function matchSeasonEpisode(title: string, rule: BuilderRule): MatchExtractions | null {
  if (!rule.seasonRegex && !rule.episodeRegex) return null

  const group = rule.captureGroup ?? 1
  let season: string | undefined
  let episode: string | undefined

  if (rule.seasonRegex) {
    const m = safeExec(rule.seasonRegex, title)
    if (!m) return null
    season = m[group] ?? m[0]
  }

  if (rule.episodeRegex) {
    const m = safeExec(rule.episodeRegex, title)
    if (!m) return null
    episode = m[group] ?? m[0]
  }

  if (!season && !episode) return null
  return { season, episode }
}

function matchAbsoluteEpisode(title: string, rule: BuilderRule): MatchExtractions | null {
  if (!rule.episodeRegex) return null
  const group = rule.captureGroup ?? 1
  const m = safeExec(rule.episodeRegex, title)
  if (!m) return null
  const episode = m[group] ?? m[0]
  return episode ? { episode } : null
}

function matchTitle(candidate: MediathekCandidate, rule: BuilderRule, mode: 'exact' | 'includes'): MatchExtractions | null {
  if (rule.titleRules.length === 0) return null

  const parts: string[] = []

  for (const tr of rule.titleRules) {
    if (tr.type === 'static') {
      if (!tr.value) return null
      parts.push(tr.value)
    } else if (tr.type === 'regex') {
      const fieldValue = getField(candidate, tr.field)
      if (!fieldValue || !tr.pattern) return null
      const group = tr.captureGroup ?? 1
      const m = safeExec(tr.pattern, fieldValue)
      if (!m) return null
      parts.push(m[group] ?? m[0])
    }
  }

  const constructed = parts.join('')
  if (!constructed) return null

  if (mode === 'exact') {
    return candidate.title === constructed ? { title: constructed } : null
  }
  return candidate.title.includes(constructed) ? { title: constructed } : null
}

function matchAirdate(candidate: MediathekCandidate, rule: BuilderRule): MatchExtractions | null {
  for (const tr of rule.titleRules) {
    if (tr.type === 'regex' && tr.pattern) {
      const fieldValue = getField(candidate, tr.field) ?? candidate.title
      const m = safeExec(tr.pattern, fieldValue)
      if (m) {
        return { airdate: m[1] ?? m[0] }
      }
    }
  }
  return null
}

function getField(candidate: MediathekCandidate, field: string): string | null {
  switch (field) {
    case 'title': return candidate.title
    case 'topic': return candidate.topic
    case 'channel': return candidate.channel
    case 'description': return candidate.description
    default: return candidate.title
  }
}

function safeExec(pattern: string, input: string): RegExpExecArray | null {
  try {
    return new RegExp(pattern).exec(input)
  } catch {
    return null
  }
}
