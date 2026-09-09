<template>
  <div class="bg-surface-raised rounded-xl border border-border-default overflow-hidden">
    <div class="flex items-center justify-between px-5 py-3.5 border-b border-border-subtle">
      <div class="flex items-center gap-2">
        <h2 class="text-sm font-semibold text-text-primary">Active Downloads</h2>
        <span v-if="totalSpeed > 0" class="text-xs text-text-muted tabular-nums">{{ formatSpeed(totalSpeed) }}</span>
      </div>
      <router-link to="/queue" class="text-xs text-brand-400 hover:text-brand-300 transition-colors">View Queue</router-link>
    </div>

    <div class="p-5">
      <div v-if="activeItems.length === 0 && queuedCount === 0" class="py-4 text-center">
        <svg class="w-6 h-6 mx-auto mb-2 text-text-muted" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
          <path d="M8 2v8M5 7l3 3 3-3"/><path d="M2 12h12"/>
        </svg>
        <p class="text-text-muted text-xs">No active downloads</p>
        <p class="text-text-muted text-[11px] mt-1">Searches from Sonarr or Radarr will appear here</p>
      </div>

      <div v-else class="space-y-4">
        <div v-for="item in displayItems" :key="item.downloadId" class="space-y-2">
          <div class="flex justify-between text-sm">
            <ReleaseTitle :title="item.title" compact class="mr-3" />
            <span class="text-text-secondary shrink-0 tabular-nums text-xs">{{ item.percentage }}% &middot; {{ formatSpeed(item.speed) }}</span>
          </div>
          <div class="h-1.5 bg-surface-elevated rounded-full overflow-hidden">
            <div
              class="h-full rounded-full transition-all duration-700 ease-out"
              :class="item.percentage < 100 ? 'bg-brand-500' : 'bg-status-ok'"
              :style="{ width: `${item.percentage}%` }"
            />
          </div>
        </div>

        <div v-if="overflowCount > 0" class="text-xs text-brand-400">
          +{{ overflowCount }} more downloading
        </div>
        <div v-if="queuedCount > 0" class="text-xs text-text-muted">
          {{ queuedCount }} queued
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onUnmounted } from 'vue'
import { useQueueStream } from '../composables/useQueueStream'
import { formatSpeed } from '../utils/format'
import ReleaseTitle from './ReleaseTitle.vue'

const { items, release } = useQueueStream()

const maxDisplay = 3
const activeItems = computed(() => items.value.filter(i => i.status === 'Processing'))
const displayItems = computed(() => activeItems.value.slice(0, maxDisplay))
const overflowCount = computed(() => Math.max(0, activeItems.value.length - maxDisplay))
const queuedCount = computed(() => items.value.filter(i => i.status === 'Queued').length)
const totalSpeed = computed(() => activeItems.value.reduce((sum, i) => sum + i.speed, 0))

onUnmounted(release)
</script>
