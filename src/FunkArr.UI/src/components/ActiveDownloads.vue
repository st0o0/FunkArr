<template>
  <div class="bg-surface-raised rounded-lg border border-border-default">
    <div class="flex items-center justify-between px-4 py-3 border-b border-border-subtle">
      <div class="flex items-center gap-2">
        <h2 class="text-sm font-medium text-text-primary">Active Downloads</h2>
        <span v-if="totalSpeed > 0" class="text-xs text-text-secondary tabular-nums">{{ formatSpeed(totalSpeed) }}</span>
      </div>
      <router-link to="/queue" class="text-xs text-accent hover:text-accent/80 transition-colors">View All</router-link>
    </div>

    <div class="p-4">
      <div v-if="activeItems.length === 0 && queuedCount === 0" class="py-6 text-center">
        <p class="text-text-secondary text-xs">No active downloads</p>
        <p class="text-text-muted text-xs mt-1">Searches from Sonarr or Radarr will appear here</p>
      </div>

      <div v-else class="space-y-3">
        <div v-for="item in displayItems" :key="item.downloadId" class="space-y-1.5">
          <div class="flex justify-between text-sm">
            <ReleaseTitle :title="item.title" compact class="mr-3" />
            <span class="text-text-secondary shrink-0 tabular-nums text-xs">{{ item.percentage }}% &middot; {{ formatSpeed(item.speed) }}</span>
          </div>
          <div class="h-1 bg-surface-elevated rounded-full overflow-hidden">
            <div
              class="h-full rounded-full transition-all duration-700 ease-out"
              :class="item.percentage < 100 ? 'bg-accent' : 'bg-status-ok'"
              :style="{ width: `${item.percentage}%` }"
            />
          </div>
        </div>

        <div v-if="overflowCount > 0" class="text-xs text-text-secondary">
          +{{ overflowCount }} more downloading
        </div>
        <div v-if="queuedCount > 0" class="text-xs text-text-secondary">
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
