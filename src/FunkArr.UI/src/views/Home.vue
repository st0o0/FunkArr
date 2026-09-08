<template>
  <div>
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-text-primary tracking-tight">Dashboard</h1>
      <p class="text-sm text-text-secondary mt-1">German public broadcaster media library integration for the *arr ecosystem.</p>
    </div>

    <div class="grid gap-3 grid-cols-3 mb-5">
      <div class="bg-surface-raised rounded-xl border border-border-default px-4 py-3 text-center">
        <div class="text-xl font-bold text-text-primary tabular-nums">{{ queuedCount }}</div>
        <div class="text-xs text-text-muted mt-0.5">queued</div>
      </div>
      <div class="bg-surface-raised rounded-xl border border-border-default px-4 py-3 text-center">
        <div class="text-xl font-bold text-brand-400 tabular-nums">{{ activeCount }}</div>
        <div class="text-xs text-text-muted mt-0.5">downloading</div>
      </div>
      <div class="bg-surface-raised rounded-xl border border-border-default px-4 py-3 text-center">
        <div class="text-xl font-bold text-text-primary tabular-nums">{{ formatSpeed(totalSpeed) }}</div>
        <div class="text-xs text-text-muted mt-0.5">total speed</div>
      </div>
    </div>

    <div class="grid gap-5 mb-6 lg:grid-cols-[1fr_1fr]">
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
