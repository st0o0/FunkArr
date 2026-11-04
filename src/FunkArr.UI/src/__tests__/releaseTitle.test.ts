import { describe, test, expect } from 'vitest'
import { parseReleaseName } from '../utils/releaseTitle'

describe('parseReleaseName', () => {
  describe('SxxExx format', () => {
    test('extracts series, season, episode, quality', () => {
      const r = parseReleaseName('Tatort.S2026E22.Tatort.Angezählt.GERMAN.1080p.WEB.h264-FunkArr')
      expect(r.series).toBe('Tatort')
      expect(r.season).toBe(2026)
      expect(r.episode).toBe(22)
      expect(r.quality).toBe('1080p')
    })

    test('extracts episode title after SxxExx', () => {
      const r = parseReleaseName('In.aller.Freundschaft.S10E30.Folge.408.Weiterziehen.GERMAN.1080p.WEB.h264-FunkArr')
      expect(r.series).toBe('In aller Freundschaft')
      expect(r.season).toBe(10)
      expect(r.episode).toBe(30)
      expect(r.episodeTitle).toBe('Folge 408 Weiterziehen')
    })

    test('handles 4-digit season numbers', () => {
      const r = parseReleaseName('Show.S2026E01.Title.GERMAN.720p.WEB.h264-FunkArr')
      expect(r.season).toBe(2026)
      expect(r.episode).toBe(1)
    })

    test('handles 720p quality', () => {
      const r = parseReleaseName('Show.S01E01.GERMAN.720p.WEB.h264-FunkArr')
      expect(r.quality).toBe('720p')
    })

    test('handles 480p quality', () => {
      const r = parseReleaseName('Show.S01E01.GERMAN.480p.WEB.h264-FunkArr')
      expect(r.quality).toBe('480p')
    })
  })

  describe('date format', () => {
    test('extracts series and date', () => {
      const r = parseReleaseName('Tagesschau.2026-08-31.GERMAN.720p.WEB.h264-FunkArr')
      expect(r.series).toBe('Tagesschau')
      expect(r.date).toBe('2026-08-31')
      expect(r.season).toBeNull()
      expect(r.episode).toBeNull()
    })

    test('extracts episode title after date', () => {
      const r = parseReleaseName('Show.2026-01-15.Special.Episode.GERMAN.1080p.WEB.h264-FunkArr')
      expect(r.date).toBe('2026-01-15')
      expect(r.episodeTitle).toBe('Special Episode')
    })
  })

  describe('fallback', () => {
    test('uses full title as series when no pattern matches', () => {
      const r = parseReleaseName('Some.Random.Title.GERMAN.1080p.WEB.h264-FunkArr')
      expect(r.series).toBe('Some Random Title')
      expect(r.season).toBeNull()
      expect(r.episode).toBeNull()
    })

    test('strips -FunkArr suffix', () => {
      const r = parseReleaseName('Title-FunkArr')
      expect(r.series).toBe('Title')
    })

    test('detects repeated prefix (series name repeated)', () => {
      const r = parseReleaseName('Elefant.Tiger.Co.Robert.zeigt.Zähne.Elefant.Tiger.Co.Folge.712.GERMAN.480p.WEB.h264-FunkArr')
      expect(r.series).toBe('Elefant Tiger Co')
    })
  })

  describe('edge cases', () => {
    test('no quality marker returns null quality', () => {
      const r = parseReleaseName('Show.S01E01.GERMAN.WEB.h264-FunkArr')
      expect(r.quality).toBeNull()
    })

    test('empty string returns empty series', () => {
      const r = parseReleaseName('')
      expect(r.series).toBe('')
      expect(r.raw).toBe('')
    })

    test('preserves raw title', () => {
      const raw = 'Test.S01E01.GERMAN.1080p.WEB.h264-FunkArr'
      expect(parseReleaseName(raw).raw).toBe(raw)
    })

    test('dots converted to spaces in series name', () => {
      const r = parseReleaseName('In.aller.Freundschaft.S01E01.GERMAN.1080p.WEB.h264-FunkArr')
      expect(r.series).toBe('In aller Freundschaft')
    })
  })
})
