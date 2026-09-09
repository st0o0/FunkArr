<template>
  <div>
    <div class="mb-6">
      <h1 class="text-lg font-medium text-text-primary">Dashboard</h1>
    </div>

    <div class="grid gap-3 grid-cols-3 mb-6">
      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
        <div class="text-xs text-text-secondary mb-1">Queued</div>
        <div class="text-xl font-semibold text-text-primary tabular-nums">{{ queuedCount }}</div>
      </div>
      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
        <div class="text-xs text-text-secondary mb-1">Downloading</div>
        <div class="text-xl font-semibold text-text-primary tabular-nums">{{ activeCount }}</div>
      </div>
      <div class="bg-surface-raised rounded-lg border border-border-default px-4 py-3">
        <div class="text-xs text-text-secondary mb-1">Speed</div>
        <div class="text-xl font-semibold text-text-primary tabular-nums">{{ formatSpeed(totalSpeed) }}</div>
      </div>
    </div>

    <div class="grid gap-4 lg:grid-cols-2">
      <HealthWidget />
      <ActiveDownloads />
    </div>
  </div>
</template>

<script setup lang="ts">
import { onUnmounted } from 'vue'
import HealthWidget from '../components/HealthWidget.vue'
import ActiveDownloads from '../components/ActiveDownloads.vue'
import { useGroupedQueue } from '../composables/useGroupedQueue'
import { formatSpeed } from '../utils/format'

const { activeCount, queuedCount, totalSpeed, release } = useGroupedQueue()

onUnmounted(release)
</script>
