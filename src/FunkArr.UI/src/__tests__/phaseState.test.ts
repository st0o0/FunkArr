import { describe, test, expect } from 'vitest'
import { isStepDone, isStepFailed, isStepCurrent, stepClass, stepLabelClass } from '../utils/phaseState'

describe('isStepDone', () => {
  test('all steps done when completed', () => {
    expect(isStepDone(0, 'completed', -1)).toBe(true)
    expect(isStepDone(1, 'completed', -1)).toBe(true)
    expect(isStepDone(2, 'completed', -1)).toBe(true)
  })

  test('steps before current phase are done when active', () => {
    expect(isStepDone(0, 'active', 1)).toBe(true)
    expect(isStepDone(1, 'active', 1)).toBe(false)
    expect(isStepDone(2, 'active', 1)).toBe(false)
  })

  test('no steps done when failed', () => {
    expect(isStepDone(0, 'failed', -1)).toBe(false)
    expect(isStepDone(1, 'failed', -1)).toBe(false)
  })
})

describe('isStepFailed', () => {
  test('first step shows failure when failed', () => {
    expect(isStepFailed(0, 'failed')).toBe(true)
  })

  test('other steps do not show failure', () => {
    expect(isStepFailed(1, 'failed')).toBe(false)
    expect(isStepFailed(2, 'failed')).toBe(false)
  })

  test('no failure when active or completed', () => {
    expect(isStepFailed(0, 'active')).toBe(false)
    expect(isStepFailed(0, 'completed')).toBe(false)
  })
})

describe('isStepCurrent', () => {
  test('matches current phase when active', () => {
    expect(isStepCurrent(1, 'active', 1)).toBe(true)
    expect(isStepCurrent(0, 'active', 1)).toBe(false)
  })

  test('no current step when completed or failed', () => {
    expect(isStepCurrent(0, 'completed', -1)).toBe(false)
    expect(isStepCurrent(0, 'failed', -1)).toBe(false)
  })
})

describe('stepClass', () => {
  test('done step gets green class', () => {
    expect(stepClass(0, 'completed', -1)).toBe('bg-green-500/20 text-green-400')
  })

  test('failed step gets red class', () => {
    expect(stepClass(0, 'failed', -1)).toBe('bg-red-500/20 text-red-400')
  })

  test('current step gets accent class', () => {
    expect(stepClass(1, 'active', 1)).toBe('bg-accent text-white')
  })

  test('pending step gets default class', () => {
    expect(stepClass(2, 'active', 1)).toBe('bg-surface-elevated text-text-secondary')
  })
})

describe('stepLabelClass', () => {
  test('done step gets green text', () => {
    expect(stepLabelClass(0, 'completed', -1)).toBe('text-green-400')
  })

  test('failed step gets red text', () => {
    expect(stepLabelClass(0, 'failed', -1)).toBe('text-red-400')
  })

  test('current step gets accent text', () => {
    expect(stepLabelClass(1, 'active', 1)).toBe('text-accent font-medium')
  })

  test('pending step gets secondary text', () => {
    expect(stepLabelClass(2, 'active', 1)).toBe('text-text-secondary')
  })
})
