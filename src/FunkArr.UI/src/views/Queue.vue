<template>
  <div class="max-w-4xl mx-auto">
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-bold text-text-primary tracking-tight">Downloads</h1>
      <div v-if="groups.length > 0" class="flex items-center gap-3 text-xs">
        <span class="text-text-muted tabular-nums">{{ formatSpeed(totalSpeed) }}</span>
        <button
          @click="toggleAll"
          class="text-brand-400 hover:text-brand-300 transition-colors"
        >
          {{ allExpanded ? 'Collapse All' : 'Expand All' }}
        </button>
      </div>
    </div>

    <EmptyState
      v-if="items.length === 0"
      icon='<path d="M8 2v8M5 7l3 3 3-3"/><path d="M2 12h12"/>'
      title="No active downloads"
      description="Downloads appear here when Sonarr or Radarr trigger a search."
    />

    <div v-else class="space-y-3">
      <QueueGroupCard
        v-for="group in groups"
        :key="group.series"
        :group="group"
        :default-expanded="allExpanded || undefined"
        @cancel="handleCancel"
      />

      <div class="text-xs text-text-muted pt-2 tabular-nums">
        {{ items.length }} {{ items.length === 1 ? 'item' : 'items' }}
        &middot; {{ queuedCount }} queued
        &middot; {{ activeCount }} downloading
        &middot; {{ groups.length }} {{ groups.length === 1 ? 'series' : 'series' }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onUnmounted } from 'vue'
import QueueGroupCard from '../components/QueueGroupCard.vue'
import EmptyState from '../components/EmptyState.vue'
import { useGroupedQueue } from '../composables/useGroupedQueue'
import { deleteQueueItem } from '../api/downloads'
import { useToast } from '../composables/useToast'
import { formatSpeed } from '../utils/format'

const { toast } = useToast()
const { groups, items, activeCount, queuedCount, totalSpeed, release } = useGroupedQueue()

const allExpanded = ref(false)

function toggleAll() {
  allExpanded.value = !allExpanded.value
}

async function handleCancel(id: string) {
  try {
    await deleteQueueItem(id)
    toast('Download cancelled')
  } catch {
    toast('Failed to cancel download', 'error')
  }
}

onUnmounted(release)
</script>
