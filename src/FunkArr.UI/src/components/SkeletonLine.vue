<template>
  <div class="skeleton-line rounded" :class="widthClass" :style="heightStyle" />
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  width?: 'full' | '3/4' | '1/2' | '1/4' | '1/3'
  height?: string
}>(), { width: 'full', height: '0.875rem' })

const widthClass = computed(() => {
  const map: Record<string, string> = { full: 'w-full', '3/4': 'w-3/4', '1/2': 'w-1/2', '1/4': 'w-1/4', '1/3': 'w-1/3' }
  return map[props.width] ?? 'w-full'
})

const heightStyle = computed(() => ({ height: props.height }))
</script>
