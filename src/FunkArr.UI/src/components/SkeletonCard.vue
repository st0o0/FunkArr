<template>
  <div class="bg-surface-raised rounded-xl border border-border-default p-4 space-y-3">
    <SkeletonLine width="1/3" height="1rem" />
    <SkeletonLine width="3/4" />
    <SkeletonLine width="1/2" />
  </div>
</template>

<script setup lang="ts">
import SkeletonLine from './SkeletonLine.vue'
</script>
