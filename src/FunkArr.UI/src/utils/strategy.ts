import { Strategy, strategyName } from '../api/enumMaps'

export function strategyLabel(strategy: number, t: (key: string) => string): string {
  switch (strategy) {
    case Strategy.SeasonAndEpisodeNumber: return t('builder.seasonEpisodeNumber')
    case Strategy.AbsoluteEpisodeNumber: return t('builder.absoluteEpisodeNumber')
    case Strategy.TitleExact: return t('builder.titleExactMatch')
    case Strategy.TitleIncludes: return t('builder.titleIncludes')
    case Strategy.AirdateExtraction: return t('builder.titleEqualsAirdate')
    default: return strategyName(strategy) || '(none)'
  }
}
