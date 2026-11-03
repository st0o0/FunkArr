import { ref, computed, watch } from 'vue'
import { getFeed, triggerFeedCrawl, type FeedOverviewResponse } from '../api/feed'
import { useToast } from './useToast'
import { useI18n } from 'vue-i18n'

const PAGE_SIZE_KEY = 'feed-page-size'

function loadPageSize(): number {
  const stored = localStorage.getItem(PAGE_SIZE_KEY)
  const parsed = stored ? parseInt(stored, 10) : NaN
  return [25, 50, 100].includes(parsed) ? parsed : 25
}

export function useFeed() {
  const { t } = useI18n()
  const { toast } = useToast()

  const overview = ref<FeedOverviewResponse | null>(null)
  const error = ref<string | null>(null)
  const loading = ref(false)
  const crawling = ref(false)

  const page = ref(1)
  const pageSize = ref(loadPageSize())
  const search = ref('')
  const channelFilter = ref('')
  const typeFilter = ref('')
  const qualityFilter = ref<number | null>(null)

  let searchTimeout: ReturnType<typeof setTimeout> | null = null

  const totalPages = computed(() => {
    if (!overview.value) return 1
    return Math.max(1, Math.ceil(overview.value.filteredTotal / pageSize.value))
  })

  const channels = computed(() => {
    if (!overview.value) return []
    const set = new Set(overview.value.entries.map(e => e.channel))
    return [...set].sort()
  })

  async function load() {
    loading.value = true
    try {
      overview.value = await getFeed({
        limit: pageSize.value,
        offset: (page.value - 1) * pageSize.value,
        type: typeFilter.value || undefined,
        channel: channelFilter.value || undefined,
        quality: qualityFilter.value ?? undefined,
        q: search.value || undefined
      })
      error.value = null
    } catch (e) {
      error.value = e instanceof Error ? e.message : String(e)
    } finally {
      loading.value = false
    }
  }

  async function crawl() {
    crawling.value = true
    try {
      await triggerFeedCrawl()
      toast(t('feed.crawlStarted'))
    } catch (e) {
      toast(e instanceof Error ? e.message : String(e), 'error')
    } finally {
      crawling.value = false
    }
  }

  function resetToFirstPage() {
    page.value = 1
    load()
  }

  function setSearch(value: string) {
    search.value = value
    if (searchTimeout) clearTimeout(searchTimeout)
    searchTimeout = setTimeout(resetToFirstPage, 300)
  }

  function setPageSize(size: number) {
    pageSize.value = size
    localStorage.setItem(PAGE_SIZE_KEY, String(size))
    resetToFirstPage()
  }

  watch(page, () => load())
  watch(channelFilter, resetToFirstPage)
  watch(typeFilter, resetToFirstPage)
  watch(qualityFilter, resetToFirstPage)

  return {
    overview,
    error,
    loading,
    crawling,
    page,
    pageSize,
    totalPages,
    search,
    channelFilter,
    typeFilter,
    qualityFilter,
    channels,
    load,
    crawl,
    setSearch,
    setPageSize
  }
}
