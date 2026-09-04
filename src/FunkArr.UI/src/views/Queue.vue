<template>
  <div class="max-w-4xl mx-auto">
    <h1 class="text-2xl font-bold text-text-primary tracking-tight mb-6">Downloads</h1>

    <EmptyState
      v-if="items.length === 0"
      icon='<path d="M8 2v8M5 7l3 3 3-3"/><path d="M2 12h12"/>'
      title="No active downloads"
      description="Downloads appear here when Sonarr or Radarr trigger a search."
    />

    <div v-else class="space-y-3">
      <QueueCard
        v-for="item in items"
        :key="item.downloadId"
        :item="item"
        @cancel="handleCancel"
      />

      <div class="text-xs text-text-muted pt-2 tabular-nums">
        {{ items.length }} {{ items.length === 1 ? 'item' : 'items' }}
        &middot; {{ queuedCount }} queued
        &middot; {{ activeCount }} downloading
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onUnmounted } from 'vue'
import QueueCard from '../components/QueueCard.vue'
import { useQueueStream } from '../composables/useQueueStream'
import { deleteQueueItem } from '../api/downloads'
import { useToast } from '../composables/useToast'

const { toast } = useToast()
import EmptyState from '../components/EmptyState.vue'

const { items, release } = useQueueStream()

const activeCount = computed(() => items.value.filter(i => i.status === 'Processing').length)
const queuedCount = computed(() => items.value.filter(i => i.status === 'Queued').length)

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
