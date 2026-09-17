import { ref, watch, type Ref } from 'vue'
import { searchMediathek, type MediathekCandidate } from '../api/rulesets'

export function useMediathekAutoFetch(topic: Ref<string>) {
  const candidates = ref<MediathekCandidate[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  let timeout: ReturnType<typeof setTimeout> | null = null

  async function fetchCandidates(query: string) {
    if (!query.trim()) {
      candidates.value = []
      error.value = null
      return
    }

    loading.value = true
    error.value = null

    try {
      candidates.value = await searchMediathek(query, 30)
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to fetch candidates'
      candidates.value = []
    } finally {
      loading.value = false
    }
  }

  function refresh() {
    fetchCandidates(topic.value)
  }

  watch(topic, (val) => {
    if (timeout) clearTimeout(timeout)
    timeout = setTimeout(() => fetchCandidates(val), 800)
  }, { immediate: true })

  return { candidates, loading, error, refresh }
}
