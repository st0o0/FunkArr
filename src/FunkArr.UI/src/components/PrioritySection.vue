<template>
  <div v-if="items.length > 0 || showEmpty">
    <div class="flex items-center gap-2 mb-2 mt-4">
      <div class="h-px flex-1" :class="lineColor" />
      <span class="text-xs font-medium px-2" :class="labelColor">
        {{ label }} <span v-if="items.length > 0" class="text-text-muted">({{ items.length }})</span>
      </span>
      <div class="h-px flex-1" :class="lineColor" />
    </div>

    <VueDraggable
      v-model="localItems"
      :group="{ name: 'queue', pull: true, put: true }"
      handle=".drag-handle"
      :animation="150"
      ghost-class="opacity-30"
      drag-class="shadow-lg"
      @start="onDragStart"
      @end="onDragEnd"
      @update="onUpdate"
      @add="onAdd"
      class="space-y-1.5"
      :class="{ 'min-h-[48px] border-2 border-dashed border-border-default/50 rounded-lg': showEmpty && items.length === 0 }"
    >
      <div
        v-for="element in localItems"
        :key="element.downloadId"
        class="px-4 py-2.5 bg-surface-raised rounded-lg border border-border-default"
      >
        <QueueCard :item="element" draggable @menu="(e: MouseEvent, item: QueueItem) => $emit('menu', e, item)" />
      </div>
    </VueDraggable>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { VueDraggable } from 'vue-draggable-plus'
import type { QueueItem } from '../api/downloads'
import QueueCard from './QueueCard.vue'

const props = defineProps<{
  items: QueueItem[]
  priority: 'High' | 'Normal' | 'Low'
  label: string
  showEmpty?: boolean
}>()

const emit = defineEmits<{
  menu: [event: MouseEvent, item: QueueItem]
  reorder: [item: QueueItem, newIndex: number]
  priorityChange: [item: QueueItem, newIndex: number, newPriority: string]
  dragStart: []
  dragEnd: []
}>()

const localItems = ref<QueueItem[]>([...props.items])

watch(() => props.items, (newItems) => {
  localItems.value = [...newItems]
}, { deep: true })

const lineColor = {
  High: 'bg-status-warn/30',
  Normal: 'bg-border-default',
  Low: 'bg-status-info/30',
}[props.priority]

const labelColor = {
  High: 'text-status-warn',
  Normal: 'text-text-secondary',
  Low: 'text-status-info',
}[props.priority]

function onDragStart() {
  emit('dragStart')
}

function onDragEnd() {
  emit('dragEnd')
}

function onUpdate(evt: { oldIndex?: number; newIndex?: number }) {
  if (evt.newIndex == null) return
  const item = localItems.value[evt.newIndex]
  if (item) emit('reorder', item, evt.newIndex)
}

function onAdd(evt: { newIndex?: number }) {
  if (evt.newIndex == null) return
  const item = localItems.value[evt.newIndex]
  if (item) emit('priorityChange', item, evt.newIndex, props.priority)
}
</script>
