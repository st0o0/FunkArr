<template>
  <div class="max-w-3xl mx-auto">
    <h1 class="text-xl font-semibold text-text-primary tracking-tight mb-5">{{ $t('settings.title') }}</h1>

    <div class="space-y-4">
      <!-- Downloads -->
      <section class="bg-surface-raised border border-border-default rounded-lg p-4">
        <h2 class="text-sm font-medium text-text-primary mb-3">{{ $t('settings.downloads') }}</h2>
        <dl class="grid grid-cols-2 gap-x-6 gap-y-2 text-sm" v-if="downloadSettings">
          <div>
            <dt class="text-text-secondary">{{ $t('settings.concurrentDownloads') }}</dt>
            <dd class="text-text-primary tabular-nums">{{ downloadSettings.concurrentDownloads }}</dd>
          </div>
          <div>
            <dt class="text-text-secondary">{{ $t('settings.schedule') }}</dt>
            <dd class="text-text-primary" v-if="downloadSettings.schedule.length > 0">
              <span v-for="(slot, i) in downloadSettings.schedule" :key="i" class="block tabular-nums">{{ slot.start }} - {{ slot.end }}</span>
            </dd>
            <dd v-else class="text-text-muted">{{ $t('settings.noSchedule') }}</dd>
          </div>
        </dl>
        <div v-else class="text-sm text-text-muted">{{ $t('common.loading') }}</div>
      </section>

      <!-- Metadata Cache -->
      <section class="bg-surface-raised border border-border-default rounded-lg p-4">
        <h2 class="text-sm font-medium text-text-primary mb-3">{{ $t('settings.metadataCache') }}</h2>
        <dl class="grid grid-cols-3 gap-x-6 gap-y-2 text-sm" v-if="cache">
          <div>
            <dt class="text-text-secondary">TVDB</dt>
            <dd class="text-text-primary tabular-nums">{{ cache.tvdbEntries }} {{ $t('settings.entries') }}</dd>
          </div>
          <div>
            <dt class="text-text-secondary">TMDB</dt>
            <dd class="text-text-primary tabular-nums">{{ cache.tmdbEntries }} {{ $t('settings.entries') }}</dd>
          </div>
          <div>
            <dt class="text-text-secondary">{{ $t('settings.oldestEntry') }}</dt>
            <dd class="text-text-primary text-xs tabular-nums">{{ cache.oldestEntry ? new Date(cache.oldestEntry).toLocaleDateString() : '-' }}</dd>
          </div>
        </dl>
        <div v-else class="text-sm text-text-muted">{{ $t('common.loading') }}</div>
      </section>

      <!-- Network Routes -->
      <section class="bg-surface-raised border border-border-default rounded-lg p-4">
        <h2 class="text-sm font-medium text-text-primary mb-3">{{ $t('settings.networkRoutes') }}</h2>
        <div v-if="routes">
          <div v-if="routes.definitions.length === 0" class="text-sm text-text-muted">{{ $t('settings.noRoutes') }}</div>
          <div v-else class="space-y-2">
            <div v-for="def in routes.definitions" :key="def.name"
              class="flex items-center gap-3 text-sm px-3 py-2 bg-surface-elevated/50 rounded"
            >
              <span class="text-text-primary font-medium">{{ def.name }}</span>
              <span v-if="def.name === routes.defaultRoute" class="text-xs px-1.5 py-0.5 rounded bg-accent-dim/20 text-accent">{{ $t('settings.default') }}</span>
              <span v-if="def.proxy" class="text-text-secondary text-xs">{{ def.proxy }}</span>
              <span v-else class="text-text-muted text-xs">{{ $t('settings.directConnection') }}</span>
            </div>
            <div v-if="routes.channelRoutes.length > 0" class="mt-2">
              <h3 class="text-xs text-text-secondary mb-1.5">{{ $t('settings.channelMappings') }}</h3>
              <div v-for="cr in routes.channelRoutes" :key="cr.pattern" class="flex items-center gap-2 text-xs text-text-body px-3 py-1">
                <code class="text-accent">{{ cr.pattern }}</code>
                <svg class="w-3 h-3 text-text-muted" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M4 8h8M9 5l3 3-3 3"/></svg>
                <span>{{ cr.route }}</span>
              </div>
            </div>
          </div>
        </div>
        <div v-else class="text-sm text-text-muted">{{ $t('common.loading') }}</div>
      </section>

      <!-- System -->
      <section class="bg-surface-raised border border-border-default rounded-lg p-4">
        <h2 class="text-sm font-medium text-text-primary mb-3">{{ $t('settings.system') }}</h2>
        <dl class="grid grid-cols-2 gap-x-6 gap-y-2 text-sm" v-if="version && health">
          <div>
            <dt class="text-text-secondary">{{ $t('settings.appVersion') }}</dt>
            <dd class="text-text-primary tabular-nums">{{ version.appVersion }}</dd>
          </div>
          <div>
            <dt class="text-text-secondary">{{ $t('settings.communityRulesets') }}</dt>
            <dd class="text-text-primary">{{ version.communityRulesetVersion ?? '-' }}</dd>
          </div>
          <div v-if="health.checks.ffmpeg">
            <dt class="text-text-secondary">FFmpeg</dt>
            <dd class="text-text-primary">{{ health.checks.ffmpeg.version ?? '-' }}</dd>
          </div>
          <div v-if="health.checks.apiKey">
            <dt class="text-text-secondary">{{ $t('settings.apiKey') }}</dt>
            <dd class="text-text-primary font-mono text-xs">{{ health.checks.apiKey.masked }}</dd>
          </div>
        </dl>
        <div v-else class="text-sm text-text-muted">{{ $t('common.loading') }}</div>
      </section>

      <!-- Logs -->
      <section class="bg-surface-raised border border-border-default rounded-lg p-4">
        <div class="flex items-center justify-between mb-3">
          <h2 class="text-sm font-medium text-text-primary">{{ $t('settings.logs') }}</h2>
          <div class="flex items-center gap-1.5">
            <button
              v-for="lvl in logLevels"
              :key="lvl"
              @click="logFilter = logFilter === lvl ? null : lvl"
              class="px-2 py-0.5 text-xs rounded transition-colors"
              :class="logFilter === lvl ? 'bg-accent text-white' : 'bg-surface-elevated text-text-secondary hover:text-text-body'"
            >{{ lvl }}</button>
          </div>
        </div>
        <div ref="logContainer" class="h-64 overflow-y-auto bg-surface-default rounded border border-border-subtle font-mono text-xs space-y-px">
          <div
            v-for="(entry, i) in filteredLogs"
            :key="i"
            class="px-3 py-1 hover:bg-surface-elevated/40 flex gap-2"
          >
            <span class="text-text-muted tabular-nums shrink-0">{{ formatLogTime(entry.timestamp) }}</span>
            <span class="shrink-0 w-12" :class="logLevelClass(entry.level)">{{ entry.level.substring(0, 4).toUpperCase() }}</span>
            <span v-if="entry.sourceContext" class="text-text-secondary shrink-0 max-w-32 truncate">{{ shortContext(entry.sourceContext) }}</span>
            <span class="text-text-body break-all">{{ entry.message }}</span>
          </div>
          <div v-if="filteredLogs.length === 0" class="px-3 py-4 text-text-muted text-center">{{ $t('settings.noLogs') }}</div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue'
import { getDownloadSettings, type DownloadSettingsResponse } from '../api/downloads'
import { getCacheStats, getSetupHealth, getSystemVersion, getRoutes, getLogs, type CacheStatsResponse, type SetupHealthCheck, type VersionResponse, type RoutesResponse, type LogEntry } from '../api/setup'
import { useLogStream } from '../composables/useLogStream'

const downloadSettings = ref<DownloadSettingsResponse | null>(null)
const cache = ref<CacheStatsResponse | null>(null)
const health = ref<SetupHealthCheck | null>(null)
const version = ref<VersionResponse | null>(null)
const routes = ref<RoutesResponse | null>(null)

const { entries: streamEntries, release } = useLogStream()
const initialLogs = ref<LogEntry[]>([])
const logFilter = ref<string | null>(null)
const logContainer = ref<HTMLElement | null>(null)
const logLevels = ['Information', 'Warning', 'Error']

const allLogs = computed(() => {
  const merged = [...initialLogs.value]
  for (const entry of streamEntries.value) {
    if (!merged.some(e => e.timestamp === entry.timestamp && e.message === entry.message)) {
      merged.push(entry)
    }
  }
  return merged.slice(-500)
})

const filteredLogs = computed(() => {
  if (!logFilter.value) return allLogs.value
  return allLogs.value.filter(e => e.level === logFilter.value)
})

watch(filteredLogs, async () => {
  await nextTick()
  if (logContainer.value) {
    logContainer.value.scrollTop = logContainer.value.scrollHeight
  }
})

function formatLogTime(ts: string): string {
  const d = new Date(ts)
  return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}:${d.getSeconds().toString().padStart(2, '0')}`
}

function logLevelClass(level: string): string {
  switch (level) {
    case 'Error': case 'Fatal': return 'text-status-fail'
    case 'Warning': return 'text-status-warn'
    case 'Debug': case 'Verbose': return 'text-text-muted'
    default: return 'text-text-secondary'
  }
}

function shortContext(ctx: string): string {
  const parts = ctx.split('.')
  return parts[parts.length - 1]
}

onMounted(async () => {
  const results = await Promise.allSettled([
    getDownloadSettings(),
    getCacheStats(),
    getSetupHealth(),
    getSystemVersion(),
    getRoutes(),
    getLogs(),
  ])

  if (results[0].status === 'fulfilled') downloadSettings.value = results[0].value
  if (results[1].status === 'fulfilled') cache.value = results[1].value
  if (results[2].status === 'fulfilled') health.value = results[2].value
  if (results[3].status === 'fulfilled') version.value = results[3].value
  if (results[4].status === 'fulfilled') routes.value = results[4].value
  if (results[5].status === 'fulfilled') initialLogs.value = results[5].value
})

onUnmounted(release)
</script>
