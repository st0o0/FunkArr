export interface ParsedTitle {
  series: string
  season: number | null
  episode: number | null
  date: string | null
  episodeTitle: string | null
  quality: string | null
  raw: string
}

const sxxexxRe = /[.\s]S(\d{2,4})E(\d{2,4})[.\s]/i
const dateRe = /[.\s](\d{4}-\d{2}-\d{2})[.\s]/
const qualityRe = /\b(1080|720|480)p\b/i

export function parseReleaseName(raw: string): ParsedTitle {
  const qMatch = raw.match(qualityRe)
  const quality = qMatch ? `${qMatch[1]}p` : null

  const match = raw.match(sxxexxRe)
  if (match) {
    return parseSxxExx(raw, match, quality)
  }

  const dateMatch = raw.match(dateRe)
  if (dateMatch) {
    return parseDate(raw, dateMatch, quality)
  }

  return parseFallback(raw, quality)
}

function parseSxxExx(raw: string, match: RegExpMatchArray, quality: string | null): ParsedTitle {
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

  return { series, season, episode, date: null, episodeTitle, quality, raw }
}

function parseDate(raw: string, match: RegExpMatchArray, quality: string | null): ParsedTitle {
  const date = match[1]
  const dateIdx = raw.indexOf(match[0])

  const seriesPart = raw.slice(0, dateIdx)
  const series = dotsToSpaces(seriesPart)

  const afterDate = raw.slice(dateIdx + match[0].length)
  const germanIdx = afterDate.indexOf('.GERMAN.')
  const endIdx = germanIdx !== -1 ? germanIdx : -1

  let episodeTitle: string | null = null
  if (endIdx > 0) {
    const cleaned = dotsToSpaces(afterDate.slice(0, endIdx))
      .replace(/\s*\|\s*/g, '')
      .trim()
    if (cleaned.length > 0) {
      episodeTitle = cleaned
    }
  } else if (afterDate.length > 0) {
    const cleaned = dotsToSpaces(afterDate).trim()
    if (cleaned.length > 0) {
      episodeTitle = cleaned
    }
  }

  return { series, season: null, episode: null, date, episodeTitle, quality, raw }
}

function parseFallback(raw: string, quality: string | null): ParsedTitle {
  const germanIdx = raw.indexOf('.GERMAN.')
  const contentPart = germanIdx > 0 ? raw.slice(0, germanIdx) : raw
  const text = dotsToSpaces(contentPart)

  const split = splitRepeatedPrefix(text)
  if (split) {
    const staffelMatch = split.between?.match(/Staffel\s+(\d+)/i)
    return {
      series: split.series,
      season: staffelMatch ? parseInt(staffelMatch[1], 10) : null,
      episode: null,
      date: null,
      episodeTitle: split.episodeTitle,
      quality,
      raw,
    }
  }

  return { series: text, season: null, episode: null, date: null, episodeTitle: null, quality, raw }
}

function splitRepeatedPrefix(text: string): { series: string; episodeTitle: string; between: string | null } | null {
  const words = text.split(/\s+/)
  if (words.length < 4) return null

  const maxPrefixLen = Math.min(Math.floor(words.length / 2), 5)
  for (let prefixLen = maxPrefixLen; prefixLen >= 2; prefixLen--) {
    const prefix = words.slice(0, prefixLen)
    for (let pos = prefixLen; pos <= words.length - prefixLen; pos++) {
      if (prefix.every((w, i) => w === words[pos + i])) {
        return {
          series: prefix.join(' '),
          episodeTitle: words.slice(pos).join(' '),
          between: pos > prefixLen ? words.slice(prefixLen, pos).join(' ') : null,
        }
      }
    }
  }

  return null
}

function dotsToSpaces(s: string): string {
  return s
    .replace(/\./g, ' ')
    .replace(/-FunkArr$/i, '')
    .replace(/\s+/g, ' ')
    .trim()
}
