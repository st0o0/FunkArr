<template>
  <div>
    <div class="flex items-center justify-between mb-5">
      <h1 class="text-lg font-medium text-text-primary">History</h1>
      <div class="flex items-center gap-2">
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Search..."
          class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted w-44 focus:outline-none focus:border-border-focus"
        />
        <select
          v-model="selectedCategory"
          class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body focus:border-border-focus focus:outline-none transition-colors"
        >
          <option value="">All categories</option>
          <option v-for="cat in categories" :key="cat" :value="cat">{{ cat }}</option>
        </select>
      </div>
    </div>

    <SkeletonTable v-if="loading && !history" :rows="5" :columns="6" />
    <div v-else-if="error" class="text-status-fail text-sm">{{ error }}</div>

    <EmptyState
      v-else-if="history && history.items.length === 0"
      icon='<circle cx="8" cy="8" r="6"/><path d="M8 4v4l2.5 2.5"/>'
      title="No download history"
      description="Completed and failed downloads will appear here."
    />

    <div v-else-if="history">
      <div class="overflow-x-auto rounded-lg border border-border-default">
        <table class="w-full text-sm table-fixed">
          <colgroup>
            <col class="w-auto" />
            <col class="w-16" />
            <col class="w-24" />
            <col class="w-20" />
            <col class="w-24" />
            <col class="w-36" />
            <col class="w-20" />
          </colgroup>
          <thead>
            <tr class="bg-surface-raised text-left text-xs text-text-secondary border-b border-border-default">
              <th class="px-4 py-2.5 font-medium">Title</th>
              <th class="px-3 py-2.5 font-medium text-center">Quality</th>
              <th class="px-3 py-2.5 font-medium text-right">Size</th>
              <th class="px-3 py-2.5 font-medium text-right">Duration</th>
              <th class="px-3 py-2.5 font-medium">Status</th>
              <th class="px-3 py-2.5 font-medium">Completed</th>
              <th class="px-3 py-2.5 font-medium"></th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="item in filteredItems"
              :key="item.downloadId"
              class="border-b border-border-subtle last:border-b-0 hover:bg-surface-elevated/40 transition-colors"
            >
              <td class="px-4 py-2.5 max-w-0">
                <ReleaseTitle :title="item.title" hideQuality />
                <details v-if="item.failMessage" class="mt-1 group">
                  <summary class="text-xs text-status-fail cursor-pointer hover:text-status-fail/80 transition-colors select-none truncate" :title="item.failMessage">
                    {{ item.failMessage.split('\n')[0].slice(0, 80) }}{{ item.failMessage.length > 80 ? '...' : '' }}
                  </summary>
                  <pre class="text-[11px] text-status-fail/80 mt-1 whitespace-pre-wrap font-mono bg-surface-elevated/50 rounded p-2 max-h-32 overflow-y-auto">{{ item.failMessage }}</pre>
                </details>
              </td>
              <td class="px-3 py-2.5 text-center">
                <span
                  v-if="getQuality(item.title)"
                  class="text-[10px] font-medium px-1.5 py-0.5 rounded bg-surface-elevated text-text-secondary"
                >{{ getQuality(item.title) }}</span>
              </td>
              <td class="px-3 py-2.5 text-text-secondary tabular-nums text-right">{{ formatSize(item.totalBytes) }}</td>
              <td class="px-3 py-2.5 text-text-secondary tabular-nums text-right">{{ formatDuration(item.downloadTimeSeconds) }}</td>
              <td class="px-3 py-2.5">
                <span class="inline-flex items-center gap-1.5 text-xs">
                  <span
                    class="w-1.5 h-1.5 rounded-full"
                    :class="item.status === 'Completed' ? 'bg-status-ok' : 'bg-status-fail'"
                  />
                  {{ item.status }}
                </span>
              </td>
              <td class="px-3 py-2.5 text-text-secondary text-xs tabular-nums" :title="formatRelativeDate(item.completedAt)">{{ formatAbsoluteDate(item.completedAt) }}</td>
              <td class="px-3 py-2.5 text-right">
                <div class="flex items-center justify-end gap-1">
                  <button
                    v-if="item.status === 'Failed'"
                    @click="handleRetry(item.downloadId)"
                    class="px-2 py-1 text-xs text-text-secondary hover:text-text-body transition-colors"
                  >
                    Retry
                  </button>
                  <button
                    @click="handleDelete(item.downloadId)"
                    class="p-1 text-text-secondary hover:text-status-fail transition-colors rounded"
                    title="Delete"
                  >
                    <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round">
                      <path d="M4 4l8 8M12 4l-8 8" />
                    </svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="totalPages > 1" class="flex items-center justify-between mt-4 text-xs text-text-secondary">
        <span class="tabular-nums">{{ rangeStart + 1 }}-{{ Math.min(rangeStart + pageSize, history.totalItems) }} of {{ history.totalItems }}</span>
        <div class="flex gap-1.5">
          <button
            :disabled="page <= 1"
            @click="page--"
            class="px-2.5 py-1 rounded-md border border-border-default text-text-secondary hover:bg-surface-elevated disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
          >
            Prev
          </button>
          <button
            :disabled="page >= totalPages"
            @click="page++"
            class="px-2.5 py-1 rounded-md border border-border-default text-text-secondary hover:bg-surface-elevated disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
          >
            Next
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getHistory, deleteHistoryItem, retryDownload, type HistoryResponse } from '../api/downloads'
import SkeletonTable from '../components/SkeletonTable.vue'
import EmptyState from '../components/EmptyState.vue'
import { useToast } from '../composables/useToast'
import { formatSize, formatDuration, formatRelativeDate, formatAbsoluteDate } from '../utils/format'
import ReleaseTitle from '../components/ReleaseTitle.vue'
import { parseReleaseName } from '../utils/releaseTitle'

const { toast } = useToast()

const route = useRoute()
const router = useRouter()

const history = ref<HistoryResponse | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const selectedCategory = ref('')
const searchQuery = ref('')
const categories = ref<string[]>([])
const pageSize = 25

const page = ref(Number(route.query.page) || 1)
const rangeStart = computed(() => (page.value - 1) * pageSize)
const totalPages = computed(() => history.value ? Math.ceil(history.value.totalItems / pageSize) : 1)

const filteredItems = computed(() => {
  if (!history.value || !searchQuery.value) return history.value?.items ?? []
  const q = searchQuery.value.toLowerCase()
  return history.value.items.filter(i => i.title.toLowerCase().includes(q))
})

function getQuality(title: string): string | null {
  return parseReleaseName(title).quality
}

async function fetchData() {
  loading.value = true
  try {
    history.value = await getHistory(rangeStart.value, pageSize, selectedCategory.value || undefined)
    error.value = null
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load history'
  } finally {
    loading.value = false
  }
}

async function fetchCategories() {
  try {
    const all = await getHistory(0, 1000)
    const unique = [...new Set(all.items.map(i => i.category).filter(Boolean))].sort()
    categories.value = unique
  } catch { /* ignore */ }
}

async function handleDelete(id: string) {
  try {
    await deleteHistoryItem(id)
    toast('Entry deleted')
    await fetchData()
  } catch {
    toast('Failed to delete entry', 'error')
  }
}

async function handleRetry(id: string) {
  try {
    await retryDownload(id)
    toast('Retry started')
    await fetchData()
  } catch {
    toast('Failed to retry download', 'error')
  }
}

watch(page, (val) => {
  router.replace({ query: val > 1 ? { page: String(val) } : {} })
  fetchData()
})

watch(selectedCategory, () => {
  page.value = 1
  fetchData()
})

onMounted(() => {
  fetchData()
  fetchCategories()
})
</script>
