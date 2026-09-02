<template>
  <div>
    <h1 class="text-2xl font-bold mb-4">RuleSets</h1>

    <div v-if="loading" class="text-gray-500">Loading...</div>
    <div v-else-if="error" class="text-red-600">{{ error }}</div>
    <div v-else-if="rulesets.length === 0" class="text-gray-500">No rulesets registered.</div>

    <div v-else class="grid gap-4">
      <router-link
        v-for="rs in rulesets"
        :key="rs.ruleSetId"
        :to="`/rulesets/${rs.ruleSetId}`"
        class="block p-4 bg-white rounded border border-gray-200 hover:border-gray-400 transition-colors"
      >
        <div class="flex items-baseline gap-3 mb-1">
          <span class="font-mono text-sm font-semibold text-gray-900">{{ rs.ruleSetId }}</span>
          <span class="text-gray-600">{{ rs.topic }}</span>
        </div>
        <div v-if="rs.aliases.length > 0" class="text-xs text-gray-500 mb-1">
          Aliases: {{ rs.aliases.join(', ') }}
        </div>
        <div class="flex gap-3 text-xs text-gray-400">
          <span v-if="rs.tvdbId">TVDB {{ rs.tvdbId }}</span>
          <span v-if="rs.imdbId">IMDB {{ rs.imdbId }}</span>
          <span v-if="rs.tmdbId">TMDB {{ rs.tmdbId }}</span>
        </div>
      </router-link>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { listRuleSets, type RuleSetEntry } from '../api/rulesets'

const rulesets = ref<RuleSetEntry[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    rulesets.value = await listRuleSets()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load rulesets'
  } finally {
    loading.value = false
  }
})
</script>
