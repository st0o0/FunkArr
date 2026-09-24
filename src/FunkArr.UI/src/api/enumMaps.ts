export const Strategy = { SeasonAndEpisodeNumber: 0, AbsoluteEpisodeNumber: 1, TitleExact: 2, TitleIncludes: 3, AirdateExtraction: 4 } as const
export const TitlePartType = { Static: 0, Regex: 1 } as const
export const FilterField = { Title: 0, Topic: 1, Channel: 2, Description: 3, Duration: 4, Timestamp: 5 } as const
export const FilterOp = { Eq: 0, Contains: 1, NotContains: 2, GreaterThan: 3, LessThan: 4, Regex: 5 } as const
export const EnrichmentMethod = { Title: 0, Airdate: 1 } as const
export const RuntimeMode = { Tiebreaker: 0, Filter: 1 } as const
export const RuleOutcome = { Matched: 0, FilterFailed: 1, IdentificationFailed: 2 } as const
export const MatchMethod = { RegexExtracted: 0, TitleMatch: 1, AirdateMatch: 2, YearMatch: 3 } as const

const strategyNames: Record<number, string> = { 0: 'seasonAndEpisodeNumber', 1: 'byAbsoluteEpisodeNumber', 2: 'itemTitleExact', 3: 'itemTitleIncludes', 4: 'itemTitleEqualsAirdate' }
const titlePartTypeNames: Record<number, string> = { 0: 'static', 1: 'regex' }
const filterFieldNames: Record<number, string> = { 0: 'title', 1: 'topic', 2: 'channel', 3: 'description', 4: 'duration', 5: 'timestamp' }
const filterOpNames: Record<number, string> = { 0: 'eq', 1: 'contains', 2: 'notContains', 3: 'greaterThan', 4: 'lessThan', 5: 'regex' }
const enrichmentMethodNames: Record<number, string> = { 0: 'title', 1: 'airdate' }
const runtimeModeNames: Record<number, string> = { 0: 'tiebreaker', 1: 'filter' }
const ruleOutcomeNames: Record<number, string> = { 0: 'matched', 1: 'filterFailed', 2: 'identificationFailed' }
const matchMethodNames: Record<number, string> = { 0: 'RegexExtracted', 1: 'TitleMatch', 2: 'AirdateMatch', 3: 'YearMatch' }

export function strategyName(v: number): string { return strategyNames[v] ?? String(v) }
export function titlePartTypeName(v: number): string { return titlePartTypeNames[v] ?? String(v) }
export function filterFieldName(v: number): string { return filterFieldNames[v] ?? String(v) }
export function filterOpName(v: number): string { return filterOpNames[v] ?? String(v) }
export function enrichmentMethodName(v: number): string { return enrichmentMethodNames[v] ?? String(v) }
export function runtimeModeName(v: number): string { return runtimeModeNames[v] ?? String(v) }
export function ruleOutcomeName(v: number): string { return ruleOutcomeNames[v] ?? String(v) }
export function matchMethodName(v: number): string { return matchMethodNames[v] ?? String(v) }
