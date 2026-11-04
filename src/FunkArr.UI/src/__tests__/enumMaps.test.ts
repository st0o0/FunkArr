import { describe, test, expect } from 'vitest'
import {
  searchSourceName, strategyName, titlePartTypeName, filterFieldName,
  filterOpName, enrichmentMethodName, runtimeModeName, ruleOutcomeName,
  matchMethodName, mediathekVariantName,
} from '../api/enumMaps'

describe('enum name lookups', () => {
  test('searchSourceName', () => {
    expect(searchSourceName(0)).toBe('Sonarr')
    expect(searchSourceName(1)).toBe('Radarr')
    expect(searchSourceName(4)).toBe('Feed')
  })

  test('strategyName', () => {
    expect(strategyName(0)).toBe('seasonAndEpisodeNumber')
    expect(strategyName(3)).toBe('itemTitleIncludes')
  })

  test('titlePartTypeName', () => {
    expect(titlePartTypeName(0)).toBe('static')
    expect(titlePartTypeName(1)).toBe('regex')
  })

  test('filterFieldName', () => {
    expect(filterFieldName(0)).toBe('title')
    expect(filterFieldName(2)).toBe('channel')
  })

  test('filterOpName', () => {
    expect(filterOpName(0)).toBe('eq')
    expect(filterOpName(5)).toBe('regex')
  })

  test('enrichmentMethodName', () => {
    expect(enrichmentMethodName(0)).toBe('title')
    expect(enrichmentMethodName(1)).toBe('airdate')
  })

  test('runtimeModeName', () => {
    expect(runtimeModeName(0)).toBe('tiebreaker')
  })

  test('ruleOutcomeName', () => {
    expect(ruleOutcomeName(0)).toBe('matched')
    expect(ruleOutcomeName(2)).toBe('identificationFailed')
  })

  test('matchMethodName', () => {
    expect(matchMethodName(0)).toBe('RegexExtracted')
  })

  test('mediathekVariantName', () => {
    expect(mediathekVariantName(0)).toBe('standard')
    expect(mediathekVariantName(4)).toBe('originalVersion')
  })

  test('unknown value returns stringified number', () => {
    expect(searchSourceName(99)).toBe('99')
    expect(strategyName(-1)).toBe('-1')
    expect(filterFieldName(100)).toBe('100')
  })
})
