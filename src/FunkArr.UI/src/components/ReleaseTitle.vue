<template>
  <div :title="parsed.raw" class="min-w-0">
    <div class="truncate" :class="compact ? 'text-sm' : 'text-sm font-medium'" >
      <span class="text-text-primary font-medium">{{ parsed.series }}</span>
    </div>
    <div v-if="compact && (parsed.season != null || parsed.quality)" class="flex items-center gap-1.5 text-xs text-text-secondary">
      <span v-if="parsed.season != null && parsed.episode != null" class="font-mono shrink-0">S{{ pad(parsed.season) }}E{{ pad(parsed.episode) }}</span>
      <span v-if="parsed.episodeTitle" class="truncate">{{ parsed.episodeTitle }}</span>
      <span v-if="parsed.quality" class="shrink-0">{{ parsed.quality }}</span>
    </div>
    <div v-else-if="!compact && hasSecondLine" class="flex items-center gap-1.5 mt-0.5 min-w-0">
      <span v-if="parsed.season != null && parsed.episode != null" class="text-text-secondary font-mono text-xs shrink-0">
        S{{ pad(parsed.season) }} E{{ pad(parsed.episode) }}
      </span>
      <span v-else-if="parsed.season != null" class="text-text-secondary text-xs shrink-0">
        Season {{ parsed.season }}
      </span>
      <span v-else-if="parsed.date" class="text-text-secondary font-mono text-xs shrink-0">
        {{ parsed.date }}
      </span>
      <span v-if="parsed.episodeTitle" class="text-text-secondary text-xs truncate">
        {{ parsed.episodeTitle }}
      </span>
      <span
        v-if="parsed.quality && !hideQuality"
        class="ml-auto shrink-0 text-[11px] font-medium px-1.5 py-0.5 rounded bg-surface-elevated text-text-secondary"
      >
        {{ parsed.quality }}
      </span>
    </div>
    <div v-else-if="parsed.season != null" class="flex items-center gap-1.5 text-xs text-text-secondary">
      <span class="text-text-secondary font-mono shrink-0">S{{ pad(parsed.season) }}E{{ pad(parsed.episode!) }}</span>
      <span v-if="parsed.episodeTitle" class="truncate">{{ parsed.episodeTitle }}</span>
      <span v-if="parsed.quality && !hideQuality" class="text-text-secondary shrink-0">{{ parsed.quality }}</span>
    </div>
    <div v-else-if="parsed.date" class="flex items-center gap-1.5 text-xs text-text-secondary">
      <span class="text-text-secondary font-mono shrink-0">{{ parsed.date }}</span>
      <span v-if="parsed.episodeTitle" class="truncate">{{ parsed.episodeTitle }}</span>
      <span v-if="parsed.quality && !hideQuality" class="text-text-secondary shrink-0">{{ parsed.quality }}</span>
    </div>
    <div v-else-if="parsed.quality && !hideQuality" class="flex items-center gap-1.5 text-xs text-text-secondary">
      <span class="text-text-secondary shrink-0">{{ parsed.quality }}</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { parseReleaseName } from '../utils/releaseTitle'

const props = defineProps<{
  title: string
  compact?: boolean
  hideQuality?: boolean
}>()

const parsed = computed(() => parseReleaseName(props.title))

const hasSecondLine = computed(() =>
  parsed.value.season != null || parsed.value.date != null || parsed.value.episodeTitle != null || (parsed.value.quality != null && !props.hideQuality)
)

function pad(n: number): string {
  return n < 10 ? `0${n}` : String(n)
}
</script>
