<template>
  <div class="max-w-5xl mx-auto">
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

    <div class="space-y-4">
      <section class="bg-surface-raised border border-border-default rounded-lg p-4">
        <h2 class="text-sm font-medium text-text-primary mb-3">{{ $t('feed.lastRun') }}</h2>
        <dl v-if="overview" class="grid grid-cols-2 sm:grid-cols-3 gap-x-6 gap-y-2 text-sm">
          <div>
            <dt class="text-text-secondary">{{ $t('feed.status') }}</dt>
            <dd class="text-text-primary">{{ statusLabel }}</dd>
          </div>
          <div>
            <dt class="text-text-secondary">{{ $t('feed.lastRunAt') }}</dt>
            <dd class="text-text-primary tabular-nums">
              {{ overview.ingest.lastRunAt ? formatRelativeDate(overview.ingest.lastRunAt) : $t('feed.never') }}
            </dd>
          </div>
          <div>
            <dt class="text-text-secondary">{{ $t('feed.newEntries') }}</dt>
            <dd class="text-text-primary tabular-nums">{{ overview.ingest.lastNewItems }}</dd>
          </div>
          <div>
            <dt class="text-text-secondary">{{ $t('feed.published') }}</dt>
            <dd class="text-text-primary tabular-nums">{{ overview.ingest.lastPublished }}</dd>
          </div>
          <div>
            <dt class="text-text-secondary">{{ $t('feed.unmatched') }}</dt>
            <dd class="text-text-primary tabular-nums">{{ overview.ingest.lastUnmatched }}</dd>
          </div>
          <div v-if="overview.ingest.lastGap || overview.ingest.consecutiveFailures > 0">
            <dt class="text-text-secondary">{{ $t('feed.warnings') }}</dt>
            <dd class="text-status-warn">
              <span v-if="overview.ingest.lastGap" class="block">{{ $t('feed.gap') }}</span>
              <span v-if="overview.ingest.consecutiveFailures > 0" class="block">
                {{ $t('feed.failures', { count: overview.ingest.consecutiveFailures }) }}
              </span>
            </dd>
          </div>
        </dl>
        <div v-else-if="error" class="text-sm text-status-fail">{{ error }}</div>
        <div v-else class="text-sm text-text-muted">{{ $t('common.loading') }}</div>
        <p class="text-xs text-text-muted mt-3">{{ $t('feed.hint') }}</p>
      </section>

      <section class="bg-surface-raised border border-border-default rounded-lg p-4">
        <h2 class="text-sm font-medium text-text-primary mb-3">
          {{ $t('feed.entries', { count: overview?.total ?? 0 }) }}
        </h2>
        <div v-if="overview && overview.entries.length === 0" class="text-sm text-text-muted">{{ $t('feed.empty') }}</div>
        <div v-else-if="overview" class="divide-y divide-border-default">
          <div v-for="entry in overview.entries" :key="entry.stableId" class="py-2 text-sm flex items-baseline gap-3">
            <span class="text-text-secondary tabular-nums text-xs w-28 shrink-0">{{ formatRelativeDate(entry.firstSeenAt) }}</span>
            <span class="text-text-primary truncate flex-1" :title="entry.title">{{ entry.title }}</span>
            <span v-if="entry.season && entry.episode" class="text-text-secondary text-xs tabular-nums">S{{ entry.season }}E{{ entry.episode }}</span>
            <span class="text-text-secondary text-xs">{{ entry.channel }}</span>
            <span class="text-text-muted text-xs tabular-nums">{{ entry.quality }}p</span>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { getFeed, triggerFeedCrawl, type FeedOverviewResponse } from '../api/feed'
import { formatRelativeDate } from '../utils/format'
import { useToast } from '../composables/useToast'

const { t } = useI18n()
const { toast } = useToast()

const overview = ref<FeedOverviewResponse | null>(null)
const error = ref<string | null>(null)
const crawling = ref(false)

const statusLabel = computed(() => {
  const ingest = overview.value?.ingest
  if (!ingest) return ''
  if (!ingest.enabled) return t('feed.disabled')
  if (ingest.running) return t('feed.running')
  return ingest.lastOutcome ?? t('feed.never')
})

async function load() {
  try {
    overview.value = await getFeed()
    error.value = null
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
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

onMounted(load)
</script>
