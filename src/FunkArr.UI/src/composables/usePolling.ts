import { ref, onMounted, onUnmounted } from 'vue'

export function usePolling<T>(fetcher: () => Promise<T>, intervalMs = 3000) {
  const data = ref<T | null>(null) as ReturnType<typeof ref<T | null>>
  const error = ref<string | null>(null)
  const loading = ref(true)
  let timer: ReturnType<typeof setInterval> | null = null

  async function poll() {
    try {
      data.value = await fetcher()
      error.value = null
    } catch (e) {
      error.value = e instanceof Error ? e.message : String(e)
    } finally {
      loading.value = false
    }
  }

  function start() {
    poll()
    timer = setInterval(() => {
      if (!document.hidden) poll()
    }, intervalMs)
  }

  function stop() {
    if (timer) {
      clearInterval(timer)
      timer = null
    }
  }

  onMounted(start)
  onUnmounted(stop)

  return { data, error, loading, refresh: poll }
}
