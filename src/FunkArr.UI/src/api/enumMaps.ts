export const Strategy = { Unknown: 0, SeasonAndEpisodeNumber: 1, AbsoluteEpisodeNumber: 2, TitleExact: 3, TitleIncludes: 4, AirdateExtraction: 5 } as const
export const TitlePartType = { Unknown: 0, Static: 1, Regex: 2 } as const
export const FilterField = { Unknown: 0, Title: 1, Topic: 2, Channel: 3, Description: 4, Duration: 5, Timestamp: 6 } as const
export const FilterOp = { Unknown: 0, Eq: 1, Contains: 2, NotContains: 3, GreaterThan: 4, LessThan: 5, Regex: 6 } as const
export const EnrichmentMethod = { Unknown: 0, Title: 1, Airdate: 2 } as const
export const RuntimeMode = { Unknown: 0, Tiebreaker: 1, Filter: 2 } as const
export const RuleOutcome = { Unknown: 0, Matched: 1, FilterFailed: 2, IdentificationFailed: 3 } as const
export const SearchSource = { Unknown: 0, Sonarr: 1, Radarr: 2, Prowlarr: 3, Feed: 4 } as const
export const MatchMethod = { Unknown: 0, RegexExtracted: 1, TitleMatch: 2, AirdateMatch: 3, YearMatch: 4 } as const

const searchSourceNames: Record<number, string> = { 1: 'Sonarr', 2: 'Radarr', 3: 'Prowlarr', 4: 'Feed' }
const strategyNames: Record<number, string> = { 1: 'seasonAndEpisodeNumber', 2: 'byAbsoluteEpisodeNumber', 3: 'itemTitleExact', 4: 'itemTitleIncludes', 5: 'itemTitleEqualsAirdate' }
const titlePartTypeNames: Record<number, string> = { 1: 'static', 2: 'regex' }
const filterFieldNames: Record<number, string> = { 1: 'title', 2: 'topic', 3: 'channel', 4: 'description', 5: 'duration', 6: 'timestamp' }
const filterOpNames: Record<number, string> = { 1: 'eq', 2: 'contains', 3: 'notContains', 4: 'greaterThan', 5: 'lessThan', 6: 'regex' }
const enrichmentMethodNames: Record<number, string> = { 1: 'title', 2: 'airdate' }
const runtimeModeNames: Record<number, string> = { 1: 'tiebreaker', 2: 'filter' }
const ruleOutcomeNames: Record<number, string> = { 1: 'matched', 2: 'filterFailed', 3: 'identificationFailed' }
const mediathekVariantNames: Record<number, string> = { 1: 'standard', 2: 'audioDescription', 3: 'signLanguage', 4: 'plainLanguage', 5: 'originalVersion' }
const matchMethodNames: Record<number, string> = { 1: 'RegexExtracted', 2: 'TitleMatch', 3: 'AirdateMatch', 4: 'YearMatch' }

export function searchSourceName(v: number): string { return searchSourceNames[v] ?? String(v) }
export function strategyName(v: number): string { return strategyNames[v] ?? String(v) }
export function titlePartTypeName(v: number): string { return titlePartTypeNames[v] ?? String(v) }
export function filterFieldName(v: number): string { return filterFieldNames[v] ?? String(v) }
export function filterOpName(v: number): string { return filterOpNames[v] ?? String(v) }
export function enrichmentMethodName(v: number): string { return enrichmentMethodNames[v] ?? String(v) }
export function runtimeModeName(v: number): string { return runtimeModeNames[v] ?? String(v) }
export function ruleOutcomeName(v: number): string { return ruleOutcomeNames[v] ?? String(v) }
export function matchMethodName(v: number): string { return matchMethodNames[v] ?? String(v) }
export function mediathekVariantName(v: number): string { return mediathekVariantNames[v] ?? String(v) }
