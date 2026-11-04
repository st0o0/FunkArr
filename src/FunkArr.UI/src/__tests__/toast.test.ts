import { describe, test, expect, vi, beforeEach } from 'vitest'

beforeEach(() => {
  vi.resetModules()
})

describe('useToast', () => {
  test('adds a toast', async () => {
    const { useToast } = await import('../composables/useToast')
    const { toasts, toast } = useToast()
    toast('Hello')
    expect(toasts.value.length).toBe(1)
    expect(toasts.value[0].message).toBe('Hello')
  })

  test('caps at 3 toasts', async () => {
    const { useToast } = await import('../composables/useToast')
    const { toasts, toast } = useToast()
    toast('One')
    toast('Two')
    toast('Three')
    toast('Four')
    expect(toasts.value.length).toBe(3)
    expect(toasts.value[0].message).toBe('Two')
  })

  test('supports variant', async () => {
    const { useToast } = await import('../composables/useToast')
    const { toasts, toast } = useToast()
    toast('Error!', 'error')
    expect(toasts.value[0].variant).toBe('error')
  })

  test('auto-removes after timeout', async () => {
    vi.useFakeTimers()
    const { useToast } = await import('../composables/useToast')
    const { toasts, toast } = useToast()
    toast('Temp')
    expect(toasts.value.length).toBe(1)
    vi.advanceTimersByTime(3500)
    expect(toasts.value.length).toBe(0)
    vi.useRealTimers()
  })
})
