<template>
  <div class="bg-surface-raised rounded-lg border border-border-default py-10 text-center">
    <svg class="w-6 h-6 mx-auto mb-2 text-text-muted" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" v-html="icon" />
    <p class="text-sm text-text-secondary">{{ title }}</p>
    <p v-if="description" class="text-xs text-text-muted mt-1">{{ description }}</p>
    <slot />
  </div>
</template>

<script setup lang="ts">
defineProps<{
  icon: string
  title: string
  description?: string
}>()
</script>
