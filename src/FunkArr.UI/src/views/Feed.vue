<template>
  <div class="max-w-5xl mx-auto">
    <!-- Header -->
    <div class="flex items-center justify-between mb-5">
      <h1 class="text-xl font-semibold text-text-primary tracking-tight">{{ $t('feed.title') }}</h1>
      <button
        type="button"
        class="text-sm px-3 py-1.5 rounded bg-accent-dim/20 text-accent hover:bg-accent-dim/30 transition-colors disabled:opacity-50"
        :disabled="crawling || overview?.ingest.running || overview?.ingest.enabled === false"
        @click="crawl"
      >
        {{ $t('feed.crawlNow') }}
      </button>
    </div>

    <!-- Stat Cards -->
    <div v-if="overview" class="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-4">
      <div class="bg-surface-raised border border-border-default rounded-lg p-3">
        <div class="text-xs text-text-secondary">{{ $t('feed.totalEntries') }}</div>
        <div class="text-lg font-semibold text-text-primary tabular-nums">{{ overview.total }}</div>
        <div class="text-xs text-text-muted">{{ $t('feed.totalLabel') }}</div>
      </div>
      <div class="bg-surface-raised border border-border-default rounded-lg p-3">
        <div class="text-xs text-text-secondary">{{ $t('feed.lastCrawl') }}</div>
        <div class="text-lg font-semibold text-accent tabular-nums">+{{ overview.ingest.lastPublished }}</div>
        <div class="text-xs text-text-muted">
          {{ overview.ingest.lastRunAt ? formatRelativeDate(overview.ingest.lastRunAt) : $t('feed.never') }}
        </div>
      </div>
      <div class="bg-surface-raised border border-border-default rounded-lg p-3">
        <div class="text-xs text-text-secondary">{{ $t('feed.unmatched') }}</div>
        <div class="text-lg font-semibold text-text-primary tabular-nums">{{ overview.ingest.lastUnmatched }}</div>
        <div class="text-xs text-text-muted">{{ $t('feed.unmatchedLabel') }}</div>
      </div>
      <div class="bg-surface-raised border border-border-default rounded-lg p-3">
        <div class="text-xs text-text-secondary">{{ $t('feed.statusCard') }}</div>
        <div class="text-lg font-semibold tabular-nums" :class="statusColor">{{ statusLabel }}</div>
        <div v-if="overview.ingest.consecutiveFailures > 0" class="text-xs text-status-warn">
          {{ $t('feed.failures', { count: overview.ingest.consecutiveFailures }) }}
        </div>
        <div v-else-if="overview.ingest.lastGap" class="text-xs text-status-warn">{{ $t('feed.gap') }}</div>
      </div>
    </div>

    <!-- Status line -->
    <div v-if="overview" class="text-xs text-text-muted mb-4">
      {{ $t('feed.statusLine', { outcome: statusLabel }) }}
    </div>

    <!-- Filter Bar -->
    <div class="flex flex-wrap items-center gap-2 mb-4">
      <input
        type="text"
        :placeholder="$t('feed.searchPlaceholder')"
        :value="search"
        @input="setSearch(($event.target as HTMLInputElement).value)"
        class="flex-1 min-w-[200px] px-3 py-1.5 text-sm rounded-md bg-surface-raised border border-border-default text-text-primary placeholder-text-muted focus:outline-none focus:ring-1 focus:ring-accent"
      />
      <select
        :value="channelFilter"
        @change="channelFilter = ($event.target as HTMLSelectElement).value"
        class="px-2 py-1.5 text-sm rounded-md bg-surface-raised border border-border-default text-text-primary"
      >
        <option value="">{{ $t('feed.allChannels') }}</option>
        <option v-for="ch in allChannels" :key="ch" :value="ch">{{ ch }}</option>
      </select>
      <select
        :value="typeFilter"
        @change="typeFilter = ($event.target as HTMLSelectElement).value"
        class="px-2 py-1.5 text-sm rounded-md bg-surface-raised border border-border-default text-text-primary"
      >
        <option value="">{{ $t('feed.allTypes') }}</option>
        <option value="show">{{ $t('feed.typeShow') }}</option>
        <option value="movie">{{ $t('feed.typeMovie') }}</option>
      </select>
      <select
        :value="qualityFilter ?? ''"
        @change="qualityFilter = ($event.target as HTMLSelectElement).value ? Number(($event.target as HTMLSelectElement).value) : null"
        class="px-2 py-1.5 text-sm rounded-md bg-surface-raised border border-border-default text-text-primary"
      >
        <option value="">{{ $t('feed.allQualities') }}</option>
        <option value="480">480p</option>
        <option value="720">720p</option>
        <option value="1080">1080p</option>
      </select>
    </div>

    <!-- Entry Table -->
    <section class="bg-surface-raised border border-border-default rounded-lg overflow-hidden">
      <div v-if="loading && !overview" class="p-4 text-sm text-text-muted">{{ $t('common.loading') }}</div>
      <div v-else-if="error" class="p-4 text-sm text-status-fail">{{ error }}</div>
      <div v-else-if="overview && overview.entries.length === 0" class="p-4 text-sm text-text-muted">{{ $t('feed.empty') }}</div>
      <table v-else-if="overview" class="w-full text-sm">
        <thead>
          <tr class="border-b border-border-default text-left text-xs text-text-secondary">
            <th class="px-4 py-2 font-medium">{{ $t('feed.colTime') }}</th>
            <th class="px-4 py-2 font-medium">{{ $t('feed.colTitle') }}</th>
            <th class="px-4 py-2 font-medium">{{ $t('feed.colChannel') }}</th>
            <th class="px-4 py-2 font-medium">{{ $t('feed.colQuality') }}</th>
            <th class="px-4 py-2 font-medium">{{ $t('feed.colType') }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-border-default">
          <tr v-for="entry in overview.entries" :key="entry.stableId" class="hover:bg-surface-elevated/50">
            <td class="px-4 py-2 text-text-secondary tabular-nums text-xs whitespace-nowrap">{{ formatRelativeDate(entry.firstSeenAt) }}</td>
            <td class="px-4 py-2 text-text-primary truncate max-w-md" :title="entry.title">
              {{ entry.title }}
              <span v-if="entry.season && entry.episode" class="ml-1 text-xs text-text-secondary tabular-nums">S{{ entry.season }}E{{ entry.episode }}</span>
            </td>
            <td class="px-4 py-2 text-text-secondary text-xs">{{ entry.channel }}</td>
            <td class="px-4 py-2 text-text-muted text-xs tabular-nums">{{ entry.quality }}p</td>
            <td class="px-4 py-2 text-text-secondary text-xs">{{ entry.mediaType === 0 ? $t('feed.typeShow') : $t('feed.typeMovie') }}</td>
          </tr>
        </tbody>
      </table>

      <!-- Pagination -->
      <div v-if="overview && overview.filteredTotal > 0" class="flex items-center justify-between px-4 py-3 border-t border-border-default text-xs text-text-secondary">
        <span class="tabular-nums">
          {{ (page - 1) * pageSize + 1 }}-{{ Math.min(page * pageSize, overview.filteredTotal) }}
          {{ $t('common.of') }} {{ overview.filteredTotal }}
          <span v-if="overview.filteredTotal !== overview.total" class="text-text-muted">({{ overview.total }} {{ $t('feed.totalLabel') }})</span>
        </span>
        <div class="flex items-center gap-3">
          <div class="flex gap-1">
            <button
              v-for="size in [25, 50, 100]"
              :key="size"
              @click="setPageSize(size)"
              class="px-2 py-0.5 rounded transition-colors"
              :class="pageSize === size ? 'bg-accent/20 text-accent' : 'text-text-muted hover:text-text-secondary'"
            >
              {{ size }}
            </button>
          </div>
          <div class="flex gap-1.5">
            <button
              :disabled="page <= 1"
              @click="page--"
              class="px-2.5 py-1 rounded-md border border-border-default text-text-secondary hover:bg-surface-elevated disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
            >
              {{ $t('feed.prev') }}
            </button>
            <button
              :disabled="page >= totalPages"
              @click="page++"
              class="px-2.5 py-1 rounded-md border border-border-default text-text-secondary hover:bg-surface-elevated disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
            >
              {{ $t('feed.next') }}
            </button>
          </div>
        </div>
      </div>
    </section>

    <!-- Hint -->
    <p class="text-xs text-text-muted mt-3">{{ $t('feed.hint') }}</p>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { formatRelativeDate } from '../utils/format'
import { useFeed } from '../composables/useFeed'
import { useI18n } from 'vue-i18n'
import { getFeed } from '../api/feed'

const { t } = useI18n()
const {
  overview, error, loading, crawling,
  page, pageSize, totalPages,
  search, channelFilter, typeFilter, qualityFilter,
  load, crawl, setSearch, setPageSize
} = useFeed()

const allChannels = ref<string[]>([])

const outcomeLabels: Record<string, string> = {
  completed: 'feed.outcomeCompleted',
  completed_with_gap: 'feed.outcomePartial',
  failed: 'feed.outcomeFailed',
  unchanged: 'feed.outcomeUnchanged'
}

const statusLabel = computed(() => {
  const ingest = overview.value?.ingest
  if (!ingest) return ''
  if (!ingest.enabled) return t('feed.disabled')
  if (ingest.running) return t('feed.running')
  const key = ingest.lastOutcome ? outcomeLabels[ingest.lastOutcome] : null
  return key ? t(key) : t('feed.never')
})

const statusColor = computed(() => {
  const outcome = overview.value?.ingest.lastOutcome
  if (!outcome) return 'text-text-muted'
  if (outcome === 'completed') return 'text-status-ok'
  if (outcome === 'completed_with_gap') return 'text-status-warn'
  if (outcome === 'failed') return 'text-status-fail'
  return 'text-text-primary'
})

async function loadChannels() {
  try {
    const full = await getFeed({ limit: 500 })
    const set = new Set(full.entries.map(e => e.channel))
    allChannels.value = [...set].sort()
  } catch {}
}

onMounted(async () => {
  await load()
  await loadChannels()
})
</script>
