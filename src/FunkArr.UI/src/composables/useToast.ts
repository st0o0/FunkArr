import { ref } from 'vue'

export type ToastVariant = 'success' | 'error' | 'info'

export interface Toast {
  id: number
  message: string
  variant: ToastVariant
}

const toasts = ref<Toast[]>([])
let nextId = 0

export function useToast() {
  function toast(message: string, variant: ToastVariant = 'success') {
    const id = nextId++
    toasts.value.push({ id, message, variant })
    if (toasts.value.length > 3) {
      toasts.value.shift()
    }
    setTimeout(() => {
      toasts.value = toasts.value.filter(t => t.id !== id)
    }, 3000)
  }

  return { toasts, toast }
}
