<template>
  <nav class="mb-4 text-sm text-text-muted flex items-center gap-1.5">
    <template v-for="(item, i) in items" :key="i">
      <router-link v-if="item.to" :to="item.to" class="hover:text-text-secondary transition-colors">{{ item.label }}</router-link>
      <span v-else class="text-text-secondary">{{ item.label }}</span>
      <svg v-if="i < items.length - 1" class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M6 4l4 4-4 4"/></svg>
    </template>
  </nav>
</template>

<script setup lang="ts">
defineProps<{
  items: { label: string; to?: string }[]
}>()
</script>
