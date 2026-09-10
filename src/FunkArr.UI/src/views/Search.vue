<template>
  <div>
    <h1 class="text-lg font-medium text-text-primary mb-4">Search</h1>

    <div class="space-y-3 mb-6">
      <input
        v-model="query"
        type="text"
        placeholder="Search title or topic..."
        class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-2 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus"
        @keydown.enter="doSearch"
      />
      <div class="flex gap-2">
        <input
          v-model="channelFilter"
          type="text"
          placeholder="Channel"
          class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted w-36 focus:outline-none focus:border-border-focus"
        />
        <input
          v-model="topicFilter"
          type="text"
          placeholder="Topic"
          class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted w-44 focus:outline-none focus:border-border-focus"
        />
        <input
          v-model.number="durationMin"
          type="number"
          placeholder="Min (sec)"
          class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted w-28 focus:outline-none focus:border-border-focus tabular-nums"
        />
        <input
          v-model.number="durationMax"
          type="number"
          placeholder="Max (sec)"
          class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted w-28 focus:outline-none focus:border-border-focus tabular-nums"
        />
      </div>
    </div>

    <div v-if="loading" class="grid gap-2">
      <SkeletonCard v-for="i in 5" :key="i" />
    </div>
    <div v-else-if="error" class="text-status-fail text-sm">{{ error }}</div>

    <EmptyState
      v-else-if="!hasSearched"
      icon='<circle cx="8" cy="8" r="5"/><path d="M11.5 11.5L14 14"/>'
      title="Search the Mediathek"
      description="Enter a search term to browse ARD, ZDF, and other public broadcaster libraries."
    />

    <EmptyState
      v-else-if="results && results.items.length === 0"
      icon='<circle cx="8" cy="8" r="5"/><path d="M11.5 11.5L14 14"/>'
      title="No results"
      description="Try different search terms or adjust filters."
    />

    <template v-else-if="results && results.items.length > 0">
      <div class="text-xs text-text-secondary mb-3 tabular-nums">
        {{ results.totalResults }} results - page {{ currentPage }} of {{ totalPages }}
      </div>

      <div class="grid gap-2">
        <div
          v-for="(item, idx) in results.items"
          :key="idx"
          class="px-4 py-3 bg-surface-raised rounded-lg border border-border-default"
        >
          <div class="flex items-start justify-between gap-3">
            <div class="min-w-0 flex-1">
              <div class="text-sm font-medium text-text-primary truncate" :title="item.title">{{ item.title }}</div>
              <div class="flex items-center gap-2 mt-0.5 text-xs text-text-secondary">
                <span>{{ item.channel }}</span>
                <span>&middot;</span>
                <span>{{ item.topic }}</span>
              </div>
            </div>
            <div class="flex items-center gap-1.5 shrink-0">
              <span
                v-if="item.quality > 0"
                class="text-[10px] font-medium px-1.5 py-0.5 rounded bg-surface-elevated text-text-secondary"
              >{{ item.quality }}p</span>
              <span
                v-if="item.hasHd"
                class="text-[10px] font-medium px-1.5 py-0.5 rounded bg-surface-elevated text-text-body"
              >HD</span>
              <span
                v-if="item.hasSubtitles"
                class="text-[10px] font-medium px-1.5 py-0.5 rounded bg-surface-elevated text-text-secondary"
              >SUB</span>
            </div>
          </div>

          <div class="flex items-center gap-3 mt-1.5 text-xs text-text-secondary">
            <span class="tabular-nums">{{ formatDuration(item.duration) }}</span>
            <span v-if="item.size > 0" class="tabular-nums">{{ formatSize(item.size) }}</span>
            <span v-if="item.timestamp > 0" class="tabular-nums">{{ formatAbsoluteDate(new Date(item.timestamp * 1000).toISOString()) }}</span>
            <a
              v-if="item.websiteUrl"
              :href="item.websiteUrl"
              target="_blank"
              rel="noopener"
              class="text-text-secondary hover:text-text-body transition-colors"
            >Website</a>
          </div>

          <div v-if="item.description" class="mt-1.5">
            <p class="text-xs text-text-secondary line-clamp-2">{{ item.description }}</p>
          </div>
        </div>
      </div>

      <div v-if="totalPages > 1" class="flex items-center justify-between mt-4 text-xs text-text-secondary">
        <span class="tabular-nums">Page {{ currentPage }} of {{ totalPages }}</span>
        <div class="flex gap-1.5">
          <button
            :disabled="currentPage <= 1"
            @click="goToPage(currentPage - 1)"
            class="px-2.5 py-1 rounded-md border border-border-default text-text-secondary hover:bg-surface-elevated disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
          >Prev</button>
          <button
            :disabled="currentPage >= totalPages"
            @click="goToPage(currentPage + 1)"
            class="px-2.5 py-1 rounded-md border border-border-default text-text-secondary hover:bg-surface-elevated disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
          >Next</button>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { searchMediathek, type MediathekSearchResponse } from '../api/mediathek'
import SkeletonCard from '../components/SkeletonCard.vue'
import EmptyState from '../components/EmptyState.vue'
import { formatDuration, formatSize, formatAbsoluteDate } from '../utils/format'

const route = useRoute()
const router = useRouter()

const query = ref((route.query.q as string) || '')
const channelFilter = ref((route.query.channel as string) || '')
const topicFilter = ref((route.query.topic as string) || '')
const durationMin = ref<number | undefined>(route.query.durationMin ? Number(route.query.durationMin) : undefined)
const durationMax = ref<number | undefined>(route.query.durationMax ? Number(route.query.durationMax) : undefined)

const results = ref<MediathekSearchResponse | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)
const hasSearched = ref(false)

const pageSize = 20
const currentPage = ref(Number(route.query.page) || 1)
const totalPages = computed(() => results.value ? Math.max(1, Math.ceil(results.value.totalResults / pageSize)) : 1)

let debounceTimer: ReturnType<typeof setTimeout> | null = null

async function doSearch() {
  const q = query.value.trim()
  const ch = channelFilter.value.trim()
  const tp = topicFilter.value.trim()

  if (!q && !ch && !tp) {
    results.value = null
    hasSearched.value = false
    return
  }

  loading.value = true
  error.value = null
  hasSearched.value = true

  try {
    results.value = await searchMediathek({
      q: q || undefined,
      channel: ch || undefined,
      topic: tp || undefined,
      durationMin: durationMin.value || undefined,
      durationMax: durationMax.value || undefined,
      offset: (currentPage.value - 1) * pageSize,
      limit: pageSize,
    })
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Search failed'
  } finally {
    loading.value = false
  }
}

function syncUrl() {
  const params: Record<string, string> = {}
  if (query.value) params.q = query.value
  if (channelFilter.value) params.channel = channelFilter.value
  if (topicFilter.value) params.topic = topicFilter.value
  if (durationMin.value) params.durationMin = String(durationMin.value)
  if (durationMax.value) params.durationMax = String(durationMax.value)
  if (currentPage.value > 1) params.page = String(currentPage.value)
  router.replace({ query: params })
}

function debouncedSearch() {
  if (debounceTimer) clearTimeout(debounceTimer)
  currentPage.value = 1
  debounceTimer = setTimeout(() => {
    syncUrl()
    doSearch()
  }, 400)
}

function goToPage(page: number) {
  currentPage.value = page
  syncUrl()
  doSearch()
}

watch([query, channelFilter, topicFilter, durationMin, durationMax], debouncedSearch)

onMounted(() => {
  if (query.value || channelFilter.value || topicFilter.value) {
    doSearch()
  }
})
</script>
