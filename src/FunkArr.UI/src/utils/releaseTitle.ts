export interface ParsedTitle {
  series: string
  season: number | null
  episode: number | null
  episodeTitle: string | null
  quality: string | null
  raw: string
}

const sxxexxRe = /[.\s]S(\d{2,4})E(\d{2,4})[.\s]/i
const qualityRe = /\b(1080|720|480)p\b/i

export function parseReleaseName(raw: string): ParsedTitle {
  const match = raw.match(sxxexxRe)
  if (!match) {
    return { series: dotsToSpaces(raw), season: null, episode: null, episodeTitle: null, quality: null, raw }
  }

  const season = parseInt(match[1], 10)
  const episode = parseInt(match[2], 10)
  const firstIdx = raw.indexOf(match[0])

  const seriesPart = raw.slice(0, firstIdx)
  const series = dotsToSpaces(seriesPart)

  const afterFirst = raw.slice(firstIdx + match[0].length)

  const secondMatch = afterFirst.match(sxxexxRe)
  const germanIdx = afterFirst.indexOf('.GERMAN.')
  const endIdx = secondMatch
    ? afterFirst.indexOf(secondMatch[0])
    : germanIdx !== -1
      ? germanIdx
      : -1

  let episodeTitle: string | null = null
  if (endIdx > 0) {
    const titlePart = afterFirst.slice(0, endIdx)
    const cleaned = dotsToSpaces(titlePart)
      .replace(/\s*\|\s*/g, '')
      .replace(/\s*-\s*$/, '')
      .replace(/^\s*-\s*/, '')
      .trim()
    if (cleaned.length > 0) {
      episodeTitle = cleaned
    }
  }

  const qMatch = raw.match(qualityRe)
  const quality = qMatch ? `${qMatch[1]}p` : null

  return { series, season, episode, episodeTitle, quality, raw }
}

function dotsToSpaces(s: string): string {
  return s
    .replace(/\./g, ' ')
    .replace(/-FunkArr$/i, '')
    .replace(/\s+/g, ' ')
    .trim()
}
