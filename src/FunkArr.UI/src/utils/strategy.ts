export function strategyLabel(strategy: string, t: (key: string) => string): string {
  switch (strategy) {
    case 'seasonAndEpisodeNumber': return t('builder.seasonEpisodeNumber')
    case 'byAbsoluteEpisodeNumber': return t('builder.absoluteEpisodeNumber')
    case 'itemTitleExact': return t('builder.titleExactMatch')
    case 'itemTitleIncludes': return t('builder.titleIncludes')
    case 'itemTitleEqualsAirdate': return t('builder.titleEqualsAirdate')
    default: return strategy || '(none)'
  }
}
