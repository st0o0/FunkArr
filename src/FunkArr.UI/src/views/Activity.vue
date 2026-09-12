<template>
  <div class="max-w-3xl mx-auto">
    <div class="flex items-center justify-between mb-5">
      <h1 class="text-xl font-semibold text-text-primary tracking-tight">Activity</h1>
      <div class="flex items-center gap-3 text-xs">
        <span v-if="totalSpeed > 0" class="text-text-secondary tabular-nums">{{ formatSpeed(totalSpeed) }}</span>
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Search..."
          class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted w-40 focus:outline-none focus:border-border-focus"
        />
      </div>
    </div>

    <div class="flex items-center gap-1 mb-4">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        @click="activeTab = tab.id"
        class="px-3 py-1.5 rounded-md text-xs transition-colors"
        :class="activeTab === tab.id
          ? 'bg-surface-elevated text-text-primary'
          : 'text-text-secondary hover:text-text-body'"
      >
        {{ tab.label }}
        <span
          v-if="tab.count > 0"
          class="ml-1 tabular-nums"
          :class="activeTab === tab.id ? 'text-text-body' : 'text-text-muted'"
        >{{ tab.count }}</span>
      </button>
    </div>

    <!-- Active tab -->
    <div v-if="activeTab === 'active'">
      <EmptyState
        v-if="activeItems.length === 0"
        icon='<path d="M8 2v8M5 7l3 3 3-3"/><path d="M2 12h12"/>'
        title="No active downloads"
        description="Downloads appear here when Sonarr or Radarr trigger a search."
      />
      <div v-else class="space-y-2">
        <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-2.5 flex items-center gap-4">
          <div class="flex-1">
            <div class="h-1 bg-surface-elevated rounded-full overflow-hidden">
              <div class="h-full bg-accent rounded-full transition-all duration-700" :style="{ width: `${overallProgress}%` }" />
            </div>
          </div>
          <span class="text-xs text-text-secondary tabular-nums shrink-0">{{ activeItems.length }} active</span>
        </div>
        <template v-for="group in activeGroups" :key="group.series">
          <div v-if="group.items.length === 1" class="rounded-lg border border-border-default overflow-hidden px-4 py-2.5 bg-surface-raised">
            <QueueCard :item="group.items[0]" @cancel="handleCancel" />
          </div>
          <QueueGroupCard v-else :group="group" @cancel="handleCancel" />
        </template>
      </div>
    </div>

    <!-- Queued tab -->
    <div v-if="activeTab === 'queued'">
      <EmptyState
        v-if="queuedItems.length === 0"
        icon='<path d="M8 2v8M5 7l3 3 3-3"/><path d="M2 12h12"/>'
        title="No queued downloads"
        description="Items waiting to download will appear here."
      />
      <div v-else class="space-y-1.5">
        <div
          v-for="item in queuedItems"
          :key="item.downloadId"
          class="flex items-center justify-between px-4 py-2.5 bg-surface-raised rounded-lg border border-border-default"
        >
          <div class="min-w-0 flex-1">
            <ReleaseTitle :title="item.title" compact />
            <div class="flex items-center gap-2 mt-0.5 text-xs text-text-secondary">
              <span>{{ item.category }}</span>
              <span>&middot;</span>
              <span class="tabular-nums">{{ formatSize(item.totalBytes) }}</span>
            </div>
          </div>
          <button
            @click="handleCancel(item.downloadId)"
            class="p-1 text-text-secondary hover:text-status-fail transition-colors rounded shrink-0 ml-3"
            title="Cancel"
          >
            <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round">
              <path d="M4 4l8 8M12 4l-8 8" />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- History tab -->
    <div v-if="activeTab === 'history'">
      <div class="flex items-center gap-2 mb-3">
        <select
          v-model="selectedCategory"
          class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body focus:border-border-focus focus:outline-none transition-colors"
        >
          <option value="">All categories</option>
          <option v-for="cat in categories" :key="cat" :value="cat">{{ cat }}</option>
        </select>
      </div>

      <SkeletonTable v-if="historyLoading && !history" :rows="5" :columns="6" />
      <div v-else-if="historyError" class="text-status-fail text-sm">{{ historyError }}</div>

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
                v-for="item in filteredHistoryItems"
                :key="item.downloadId"
                class="border-b border-border-subtle last:border-b-0 hover:bg-surface-elevated/40 transition-colors"
              >
                <td class="px-4 py-2.5 max-w-0">
                  <ReleaseTitle :title="item.title" hideQuality />
                  <details v-if="item.failMessage" class="mt-1 group">
                    <summary class="text-xs text-status-fail cursor-pointer hover:text-status-fail/80 transition-colors select-none truncate" :title="item.failMessage">
                      {{ item.failMessage.split('\n')[0].slice(0, 80) }}{{ item.failMessage.length > 80 ? '...' : '' }}
                    </summary>
                    <pre class="text-xs text-status-fail/80 mt-1 whitespace-pre-wrap font-mono bg-surface-elevated/50 rounded p-2 max-h-32 overflow-y-auto">{{ item.failMessage }}</pre>
                  </details>
                </td>
                <td class="px-3 py-2.5 text-center">
                  <span
                    v-if="getQuality(item.title)"
                    class="text-xs font-medium px-1.5 py-0.5 rounded bg-surface-elevated text-text-secondary"
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
                      @click="handleDeleteHistory(item.downloadId)"
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
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useGroupedQueue, type QueueGroup } from '../composables/useGroupedQueue'
import { getHistory, getHistoryCategories, deleteQueueItem, deleteHistoryItem, retryDownload, type HistoryResponse } from '../api/downloads'
import QueueGroupCard from '../components/QueueGroupCard.vue'
import QueueCard from '../components/QueueCard.vue'
import EmptyState from '../components/EmptyState.vue'
import SkeletonTable from '../components/SkeletonTable.vue'
import ReleaseTitle from '../components/ReleaseTitle.vue'
import { useToast } from '../composables/useToast'
import { formatSpeed, formatSize, formatDuration, formatRelativeDate, formatAbsoluteDate } from '../utils/format'
import { parseReleaseName } from '../utils/releaseTitle'

const { toast } = useToast()
const { groups, items, activeCount, queuedCount, totalSpeed, release } = useGroupedQueue()

const activeTab = ref<'active' | 'queued' | 'history'>('active')
const searchQuery = ref('')

const activeItems = computed(() => items.value.filter(i => i.status === 'Processing'))
const queuedItems = computed(() => items.value.filter(i => i.status === 'Queued'))

const activeGroups = computed<QueueGroup[]>(() =>
  groups.value.filter(g => g.activeCount > 0).map(g => ({
    ...g,
    items: g.items.filter(i => i.status === 'Processing'),
  }))
)

const overallProgress = computed(() => {
  if (activeItems.value.length === 0) return 0
  const total = activeItems.value.reduce((sum, i) => sum + i.percentage, 0)
  return Math.round(total / activeItems.value.length)
})

const tabs = computed(() => [
  { id: 'active' as const, label: 'Active', count: activeCount.value },
  { id: 'queued' as const, label: 'Queued', count: queuedCount.value },
  { id: 'history' as const, label: 'History', count: 0 },
])

async function handleCancel(id: string) {
  try {
    await deleteQueueItem(id)
    toast('Download cancelled')
  } catch {
    toast('Failed to cancel download', 'error')
  }
}

// History state
const history = ref<HistoryResponse | null>(null)
const historyLoading = ref(false)
const historyError = ref<string | null>(null)
const selectedCategory = ref('')
const categories = ref<string[]>([])
const pageSize = 25
const page = ref(1)
const rangeStart = computed(() => (page.value - 1) * pageSize)
const totalPages = computed(() => history.value ? Math.ceil(history.value.totalItems / pageSize) : 1)

const filteredHistoryItems = computed(() => {
  if (!history.value || !searchQuery.value) return history.value?.items ?? []
  const q = searchQuery.value.toLowerCase()
  return history.value.items.filter(i => i.title.toLowerCase().includes(q))
})

function getQuality(title: string): string | null {
  return parseReleaseName(title).quality
}

async function fetchHistory() {
  historyLoading.value = true
  try {
    history.value = await getHistory(rangeStart.value, pageSize, selectedCategory.value || undefined)
    historyError.value = null
  } catch (e) {
    historyError.value = e instanceof Error ? e.message : 'Failed to load history'
  } finally {
    historyLoading.value = false
  }
}

async function fetchCategories() {
  try {
    categories.value = await getHistoryCategories()
  } catch { /* ignore */ }
}

async function handleDeleteHistory(id: string) {
  try {
    await deleteHistoryItem(id)
    toast('Entry deleted')
    await fetchHistory()
  } catch {
    toast('Failed to delete entry', 'error')
  }
}

async function handleRetry(id: string) {
  try {
    await retryDownload(id)
    toast('Retry started')
    await fetchHistory()
  } catch {
    toast('Failed to retry download', 'error')
  }
}

watch(activeTab, (tab) => {
  if (tab === 'history' && !history.value) {
    fetchHistory()
    fetchCategories()
  }
})

watch(page, () => fetchHistory())

watch(selectedCategory, () => {
  page.value = 1
  fetchHistory()
})

onMounted(() => {
  if (activeTab.value === 'history') {
    fetchHistory()
    fetchCategories()
  }
})

onUnmounted(release)
</script>
