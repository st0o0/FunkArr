import { describe, test, expect } from 'vitest'
import { formatSize, formatSpeed, formatDuration, formatPercent } from '../utils/format'

describe('formatSize', () => {
  test('formats 0 bytes', () => {
    expect(formatSize(0)).toBe('0 B')
  })

  test('formats bytes below 1 KB', () => {
    expect(formatSize(512)).toBe('512 B')
  })

  test('formats exactly 1 KB', () => {
    expect(formatSize(1024)).toBe('1.0 KB')
  })

  test('formats KB range', () => {
    expect(formatSize(500_000)).toBe('488.3 KB')
  })

  test('formats exactly 1 MB', () => {
    expect(formatSize(1_048_576)).toBe('1.0 MB')
  })

  test('formats MB range', () => {
    expect(formatSize(637_000_000)).toBe('607.5 MB')
  })

  test('formats exactly 1 GB', () => {
    expect(formatSize(1_073_741_824)).toBe('1.0 GB')
  })

  test('formats GB range', () => {
    expect(formatSize(4_300_000_000)).toBe('4.0 GB')
  })
})

describe('formatSpeed', () => {
  test('formats bytes/s', () => {
    expect(formatSpeed(500)).toBe('500 B/s')
  })

  test('formats KB/s', () => {
    expect(formatSpeed(5_000)).toBe('4.9 KB/s')
  })

  test('formats MB/s', () => {
    expect(formatSpeed(12_400_000)).toBe('11.8 MB/s')
  })
})

describe('formatDuration', () => {
  test('formats 0 or negative as dash', () => {
    expect(formatDuration(0)).toBe('-')
    expect(formatDuration(-5)).toBe('-')
  })

  test('formats seconds only', () => {
    expect(formatDuration(45)).toBe('0m 45s')
  })

  test('formats minutes and seconds', () => {
    expect(formatDuration(125)).toBe('2m 05s')
  })

  test('formats hours and minutes', () => {
    expect(formatDuration(3661)).toBe('1h 01m')
  })
})

describe('formatPercent', () => {
  test('formats decimal to percentage', () => {
    expect(formatPercent(0.833)).toBe('83.3%')
  })

  test('formats 0', () => {
    expect(formatPercent(0)).toBe('0.0%')
  })

  test('formats 1 as 100%', () => {
    expect(formatPercent(1)).toBe('100.0%')
  })
})
