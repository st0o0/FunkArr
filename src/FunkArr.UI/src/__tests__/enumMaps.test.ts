import { describe, test, expect } from 'vitest'
import {
  searchSourceName, strategyName, titlePartTypeName, filterFieldName,
  filterOpName, enrichmentMethodName, runtimeModeName, ruleOutcomeName,
  matchMethodName, mediathekVariantName,
} from '../api/enumMaps'

describe('enum name lookups', () => {
  test('searchSourceName', () => {
    expect(searchSourceName(1)).toBe('Sonarr')
    expect(searchSourceName(2)).toBe('Radarr')
    expect(searchSourceName(4)).toBe('Feed')
  })

  test('strategyName', () => {
    expect(strategyName(1)).toBe('seasonAndEpisodeNumber')
    expect(strategyName(4)).toBe('itemTitleIncludes')
  })

  test('titlePartTypeName', () => {
    expect(titlePartTypeName(1)).toBe('static')
    expect(titlePartTypeName(2)).toBe('regex')
  })

  test('filterFieldName', () => {
    expect(filterFieldName(1)).toBe('title')
    expect(filterFieldName(3)).toBe('channel')
  })

  test('filterOpName', () => {
    expect(filterOpName(1)).toBe('eq')
    expect(filterOpName(6)).toBe('regex')
  })

  test('enrichmentMethodName', () => {
    expect(enrichmentMethodName(1)).toBe('title')
    expect(enrichmentMethodName(2)).toBe('airdate')
  })

  test('runtimeModeName', () => {
    expect(runtimeModeName(1)).toBe('tiebreaker')
  })

  test('ruleOutcomeName', () => {
    expect(ruleOutcomeName(1)).toBe('matched')
    expect(ruleOutcomeName(3)).toBe('identificationFailed')
  })

  test('matchMethodName', () => {
    expect(matchMethodName(1)).toBe('RegexExtracted')
  })

  test('mediathekVariantName', () => {
    expect(mediathekVariantName(1)).toBe('standard')
    expect(mediathekVariantName(5)).toBe('originalVersion')
  })

  test('unknown value returns stringified number', () => {
    expect(searchSourceName(99)).toBe('99')
    expect(strategyName(-1)).toBe('-1')
    expect(filterFieldName(100)).toBe('100')
  })
})
