<template>
  <div :title="parsed.raw" class="min-w-0">
    <div class="truncate" :class="compact ? 'text-sm' : 'text-sm font-medium'" >
      <span class="text-text-primary">{{ parsed.series }}</span>
    </div>
    <div v-if="!compact" class="flex items-center gap-1.5 mt-0.5 min-w-0">
      <span v-if="parsed.season != null" class="text-brand-400 font-mono text-xs shrink-0">
        S{{ pad(parsed.season) }} E{{ pad(parsed.episode!) }}
      </span>
      <span v-if="parsed.episodeTitle" class="text-text-secondary text-xs truncate">
        {{ parsed.episodeTitle }}
      </span>
      <span
        v-if="parsed.quality"
        class="ml-auto shrink-0 text-[10px] font-medium px-1.5 py-0.5 rounded bg-surface-elevated text-text-muted"
      >
        {{ parsed.quality }}
      </span>
    </div>
    <div v-else-if="parsed.season != null" class="flex items-center gap-1.5 text-xs text-text-secondary">
      <span class="text-brand-400 font-mono shrink-0">S{{ pad(parsed.season) }}E{{ pad(parsed.episode!) }}</span>
      <span v-if="parsed.episodeTitle" class="truncate">{{ parsed.episodeTitle }}</span>
      <span v-if="parsed.quality" class="text-text-muted shrink-0">{{ parsed.quality }}</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { parseReleaseName } from '../utils/releaseTitle'

const props = defineProps<{
  title: string
  compact?: boolean
}>()

const parsed = computed(() => parseReleaseName(props.title))

function pad(n: number): string {
  return n < 10 ? `0${n}` : String(n)
}
</script>
