<template>
  <div class="bg-surface-raised rounded-xl border border-border-default overflow-hidden">
    <div class="flex items-center justify-between px-5 py-3.5 border-b border-border-subtle">
      <h2 class="text-sm font-semibold text-text-primary">Active Downloads</h2>
      <router-link to="/queue" class="text-xs text-brand-400 hover:text-brand-300 transition-colors">View Queue</router-link>
    </div>

    <div class="p-5">
      <div v-if="activeItems.length === 0 && queuedCount === 0" class="text-text-muted text-sm py-2">
        No active downloads
      </div>

      <div v-else class="space-y-4">
        <div v-for="item in activeItems" :key="item.downloadId" class="space-y-2">
          <div class="flex justify-between text-sm">
            <span class="text-text-body truncate mr-3 font-medium">{{ item.title }}</span>
            <span class="text-text-secondary shrink-0 tabular-nums text-xs">{{ item.percentage }}% &middot; {{ formatSpeed(item.speed) }}</span>
          </div>
          <div class="h-1.5 bg-surface-elevated rounded-full overflow-hidden">
            <div
              class="h-full rounded-full transition-all duration-500"
              :class="item.percentage < 100 ? 'bg-brand-500' : 'bg-status-ok'"
              :style="{ width: `${item.percentage}%` }"
            />
          </div>
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

const { items, release } = useQueueStream()

const activeItems = computed(() => items.value.filter(i => i.status === 'Processing'))
const queuedCount = computed(() => items.value.filter(i => i.status === 'Queued').length)

onUnmounted(release)
</script>
