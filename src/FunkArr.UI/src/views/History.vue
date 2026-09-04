<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-bold text-text-primary tracking-tight">History</h1>
      <select
        v-model="selectedCategory"
        class="bg-surface-elevated border border-border-default rounded-lg px-3 py-1.5 text-sm text-text-body focus:border-border-focus focus:outline-none transition-colors"
      >
        <option value="">All categories</option>
        <option value="tv">tv</option>
        <option value="movies">movies</option>
        <option value="sonarr">sonarr</option>
        <option value="radarr">radarr</option>
      </select>
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
      <div class="overflow-x-auto rounded-xl border border-border-default">
        <table class="w-full text-sm">
          <thead class="sticky top-0 z-10">
            <tr class="bg-surface-raised text-left text-xs text-text-muted border-b border-border-default">
              <th class="px-4 py-3 font-medium">Title</th>
              <th class="px-4 py-3 font-medium">Category</th>
              <th class="px-4 py-3 font-medium">Size</th>
              <th class="px-4 py-3 font-medium">Duration</th>
              <th class="px-4 py-3 font-medium">Status</th>
              <th class="px-4 py-3 font-medium">Completed</th>
              <th class="px-4 py-3 font-medium w-20"></th>
            </tr>
          </thead>
          <tbody class="bg-surface-raised/50">
            <tr
              v-for="item in history.items"
              :key="item.downloadId"
              class="border-b border-border-subtle last:border-b-0 hover:bg-surface-elevated/60 transition-colors"
            >
              <td class="px-4 py-3 text-text-body max-w-xs truncate">
                {{ item.title }}
                <div v-if="item.failMessage" class="text-xs text-status-fail mt-0.5 truncate" :title="item.failMessage">
                  {{ item.failMessage }}
                </div>
              </td>
              <td class="px-4 py-3 text-text-secondary">{{ item.category }}</td>
              <td class="px-4 py-3 text-text-secondary tabular-nums">{{ formatSize(item.totalBytes) }}</td>
              <td class="px-4 py-3 text-text-secondary tabular-nums">{{ formatDuration(item.downloadTimeSeconds) }}</td>
              <td class="px-4 py-3">
                <span class="inline-flex items-center gap-1.5 text-xs font-medium">
                  <span
                    class="w-2 h-2 rounded-full"
                    :class="item.status === 'Completed' ? 'bg-status-ok' : 'bg-status-fail'"
                  />
                  {{ item.status }}
                </span>
              </td>
              <td class="px-4 py-3 text-text-muted text-xs">{{ formatRelativeDate(item.completedAt) }}</td>
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-1">
                  <button
                    v-if="item.status === 'Failed'"
                    @click="handleRetry(item.downloadId)"
                    class="px-2.5 py-1 text-xs text-brand-400 hover:text-brand-300 transition-colors"
                  >
                    Retry
                  </button>
                  <button
                    @click="handleDelete(item.downloadId)"
                    class="p-1.5 text-status-fail/60 hover:text-status-fail hover:bg-status-fail/10 transition-colors rounded-md"
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

      <div v-if="totalPages > 1" class="flex items-center justify-between mt-4 text-xs text-text-muted">
        <span class="tabular-nums">{{ rangeStart + 1 }}-{{ Math.min(rangeStart + pageSize, history.totalItems) }} of {{ history.totalItems }}</span>
        <div class="flex gap-2">
          <button
            :disabled="page <= 1"
            @click="page--"
            class="px-3 py-1.5 rounded-lg border border-border-default text-text-body hover:border-brand-500/40 disabled:opacity-30 disabled:cursor-not-allowed transition-colors active:scale-[0.98]"
          >
            &larr; Prev
          </button>
          <button
            :disabled="page >= totalPages"
            @click="page++"
            class="px-3 py-1.5 rounded-lg border border-border-default text-text-body hover:border-brand-500/40 disabled:opacity-30 disabled:cursor-not-allowed transition-colors active:scale-[0.98]"
          >
            Next &rarr;
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
import { formatSize, formatDuration, formatRelativeDate } from '../utils/format'

const { toast } = useToast()

const route = useRoute()
const router = useRouter()

const history = ref<HistoryResponse | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const selectedCategory = ref('')
const pageSize = 25

const page = ref(Number(route.query.page) || 1)
const rangeStart = computed(() => (page.value - 1) * pageSize)
const totalPages = computed(() => history.value ? Math.ceil(history.value.totalItems / pageSize) : 1)

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

onMounted(fetchData)
</script>
