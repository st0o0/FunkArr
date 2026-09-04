<template>
  <div class="rounded-xl border border-border-default overflow-hidden">
    <div class="bg-surface-raised px-4 py-3 flex gap-4">
      <SkeletonLine v-for="i in columns" :key="i" :width="i === 1 ? '1/3' : '1/4'" height="0.75rem" />
    </div>
    <div v-for="r in rows" :key="r" class="px-4 py-3 border-t border-border-subtle flex gap-4">
      <SkeletonLine v-for="i in columns" :key="i" :width="i === 1 ? '1/2' : '1/4'" />
    </div>
  </div>
</template>

<script setup lang="ts">
import SkeletonLine from './SkeletonLine.vue'

withDefaults(defineProps<{
  rows?: number
  columns?: number
}>(), { rows: 5, columns: 4 })
</script>
