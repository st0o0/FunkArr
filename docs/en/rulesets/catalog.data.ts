import { readdirSync, readFileSync } from 'node:fs'
import { resolve, join } from 'node:path'

export interface RulesetEntry {
  name: string
  type: 'show' | 'movie'
  imdbId?: string
  tmdbId?: string
  ruleCount: number
  confidence: number
}

export interface CatalogData {
  shows: RulesetEntry[]
  movies: RulesetEntry[]
}

declare const data: CatalogData
export { data }

export default {
  watch: ['../../../data/community/rulesets/*.json'],
  load(): CatalogData {
    const dir = resolve(__dirname, '../../../data/community/rulesets')
    const files = readdirSync(dir).filter(f => f.endsWith('.json')).sort()

    const entries = files.map(file => {
      const json = JSON.parse(readFileSync(join(dir, file), 'utf-8'))
      return {
        name: json.media?.name ?? json.topic,
        type: (json.media?.type ?? 'show') as 'show' | 'movie',
        imdbId: json.media?.imdbId,
        tmdbId: json.media?.tmdbId,
        ruleCount: json.rules?.length ?? 0,
        confidence: json.confidence ?? 0
      }
    })

    return {
      shows: entries.filter(e => e.type !== 'movie').sort((a, b) => a.name.localeCompare(b.name)),
      movies: entries.filter(e => e.type === 'movie').sort((a, b) => a.name.localeCompare(b.name))
    }
  }
}
