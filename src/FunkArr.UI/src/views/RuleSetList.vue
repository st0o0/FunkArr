<template>
  <div>
    <h1 class="text-2xl font-bold text-text-primary tracking-tight mb-5">RuleSets</h1>

    <div class="flex items-center gap-3 mb-5">
      <input
        v-model="search"
        type="text"
        placeholder="Search rulesets..."
        class="bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body placeholder-text-muted w-full focus:outline-none focus:border-brand-500/50"
      />
      <router-link
        to="/rulesets/new"
        class="inline-flex items-center gap-2 px-4 py-2 bg-brand-600 text-white rounded-lg hover:bg-brand-500 text-sm transition-colors whitespace-nowrap"
      >
        New RuleSet
      </router-link>
    </div>

    <div v-if="loading" class="text-text-muted text-sm">Loading...</div>
    <div v-else-if="error" class="text-status-fail text-sm">{{ error }}</div>
    <div v-else-if="rulesets.length === 0" class="text-text-muted text-sm">No rulesets registered.</div>
    <div v-else-if="filteredRulesets.length === 0" class="text-text-muted text-sm">No matching rulesets.</div>

    <div v-else class="grid gap-2.5">
      <router-link
        v-for="rs in filteredRulesets"
        :key="rs.ruleSetId"
        :to="`/rulesets/${rs.ruleSetId}`"
        class="block p-4 bg-surface-raised rounded-xl border border-border-default hover:border-brand-500/30 hover:bg-surface-elevated/50 transition-colors"
      >
        <div class="flex items-baseline gap-3 mb-1">
          <span class="font-mono text-sm font-semibold text-brand-400">{{ rs.ruleSetId }}</span>
          <span class="text-text-body">{{ rs.topic }}</span>
        </div>
        <div v-if="rs.aliases.length > 0" class="text-xs text-text-muted mb-1">
          Aliases: {{ rs.aliases.join(', ') }}
        </div>
        <div class="flex gap-3 text-xs text-text-muted">
          <span v-if="rs.tvdbId">TVDB {{ rs.tvdbId }}</span>
          <span v-if="rs.imdbId">IMDB {{ rs.imdbId }}</span>
          <span v-if="rs.tmdbId">TMDB {{ rs.tmdbId }}</span>
        </div>
      </router-link>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { listRuleSets, type RuleSetEntry } from '../api/rulesets'

const rulesets = ref<RuleSetEntry[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const search = ref('')

const filteredRulesets = computed(() => {
  const term = search.value.toLowerCase()
  if (!term) return rulesets.value
  return rulesets.value.filter(rs => {
    const haystack = [
      rs.ruleSetId,
      rs.topic,
      ...rs.aliases,
      rs.tvdbId?.toString() ?? '',
      rs.imdbId ?? '',
      rs.tmdbId?.toString() ?? '',
    ].join(' ').toLowerCase()
    return haystack.includes(term)
  })
})

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
