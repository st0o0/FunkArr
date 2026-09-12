<template>
  <div class="max-w-3xl mx-auto">
    <h1 class="text-xl font-semibold text-text-primary tracking-tight mb-4">RuleSets</h1>

    <div class="flex items-center gap-2 mb-3">
      <input
        v-model="search"
        type="text"
        placeholder="Search rulesets..."
        class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted flex-1 focus:outline-none focus:border-border-focus"
      />
      <select
        v-model="sortBy"
        class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body focus:outline-none focus:border-border-focus"
      >
        <option value="topic">Sort by Name</option>
        <option value="id">Sort by ID</option>
      </select>
      <router-link
        to="/rulesets/new"
        class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-surface-elevated border border-border-default text-text-body rounded-md hover:bg-surface-overlay text-sm transition-colors whitespace-nowrap"
      >
        + New
      </router-link>
    </div>

    <div v-if="!loading && rulesets.length > 0" class="flex items-center gap-1 mb-3">
      <button
        v-for="tab in typeTabs"
        :key="tab.value"
        class="px-2.5 py-1 text-xs rounded-md transition-colors"
        :class="typeFilter === tab.value
          ? 'bg-accent/15 text-accent font-medium'
          : 'text-text-secondary hover:text-text-body hover:bg-surface-elevated'"
        @click="typeFilter = tab.value"
      >
        {{ tab.label }} ({{ tab.count }})
      </button>
    </div>

    <div v-if="loading" class="grid gap-2">
      <SkeletonCard v-for="i in 3" :key="i" />
    </div>
    <div v-else-if="error" class="text-status-fail text-sm">{{ error }}</div>
    <EmptyState
      v-else-if="rulesets.length === 0"
      icon='<path d="M2 4h12M2 8h12M2 12h8"/><circle cx="13" cy="12" r="1.5"/>'
      title="No rulesets registered"
      description="RuleSets define how media entries are matched to episodes."
    >
      <router-link
        to="/rulesets/new"
        class="inline-flex items-center gap-1.5 mt-3 px-3 py-1.5 bg-surface-elevated border border-border-default text-text-body rounded-md hover:bg-surface-overlay text-sm transition-colors"
      >
        Create First RuleSet
      </router-link>
    </EmptyState>
    <EmptyState
      v-else-if="filteredRulesets.length === 0"
      icon='<circle cx="8" cy="8" r="5"/><path d="M11.5 11.5L14 14"/>'
      title="No matching rulesets"
      description="Try a different search term or filter."
    />

    <div class="text-xs text-text-secondary mb-2" v-if="!loading && rulesets.length > 0">
      {{ filteredRulesets.length }} {{ filteredRulesets.length === 1 ? 'ruleset' : 'rulesets' }}
      <span v-if="filteredRulesets.length !== rulesets.length"> of {{ rulesets.length }}</span>
    </div>

    <div v-if="!loading && filteredRulesets.length > 0" class="grid gap-1.5">
      <router-link
        v-for="rs in filteredRulesets"
        :key="rs.ruleSetId"
        :to="`/rulesets/${rs.ruleSetId}`"
        class="block px-4 py-3 bg-surface-raised rounded-lg border border-border-default hover:bg-surface-elevated transition-colors"
      >
        <div class="flex items-center justify-between mb-0.5">
          <div class="flex items-baseline gap-2 min-w-0">
            <span class="text-sm font-medium text-text-primary truncate">{{ rs.topic }}</span>
            <span class="font-mono text-xs text-text-secondary shrink-0">{{ rs.ruleSetId }}</span>
          </div>
          <div class="flex items-center gap-1.5 shrink-0 ml-2">
            <span
              class="text-[11px] font-medium px-1.5 py-0.5 rounded bg-surface-elevated"
              :class="resolvedMediaType(rs) === 'movie' ? 'text-text-body' : 'text-text-secondary'"
            >{{ resolvedMediaType(rs) }}</span>
            <span
              class="text-[11px] font-medium px-1.5 py-0.5 rounded"
              :class="{
                'bg-surface-elevated text-text-secondary': rs.sourceType === 'community',
                'bg-accent/10 text-accent': rs.sourceType === 'local',
                'bg-surface-elevated text-text-body': rs.sourceType === 'merged',
              }"
            >{{ rs.sourceType }}</span>
          </div>
        </div>
        <div v-if="rs.mediaName" class="text-xs text-text-body mb-0.5">{{ rs.mediaName }}</div>
        <div v-if="rs.aliases.length > 0" class="text-xs text-text-secondary mb-0.5">
          {{ rs.aliases.join(', ') }}
        </div>
        <div class="flex items-center gap-3 text-xs text-text-secondary">
          <span>{{ rs.ruleCount }} {{ rs.ruleCount === 1 ? 'rule' : 'rules' }}</span>
          <span v-if="rs.tvdbId">TVDB {{ rs.tvdbId }}</span>
          <span v-if="rs.imdbId">IMDB {{ rs.imdbId }}</span>
          <span v-if="rs.tmdbId">TMDB {{ rs.tmdbId }}</span>
          <template v-if="rs.lastScoringRun">
            <span>&middot;</span>
            <span>Last scored {{ formatRelativeDate(rs.lastScoringRun) }}</span>
          </template>
          <template v-if="rs.matchRate !== null">
            <span>&middot;</span>
            <span>{{ formatPercent(rs.matchRate) }} match rate</span>
          </template>
        </div>
      </router-link>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { listRuleSets, type RuleSetEntry } from '../api/rulesets'
import SkeletonCard from '../components/SkeletonCard.vue'
import EmptyState from '../components/EmptyState.vue'
import { formatRelativeDate, formatPercent } from '../utils/format'

const rulesets = ref<RuleSetEntry[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const search = ref('')
const sortBy = ref<'topic' | 'id'>('topic')
const typeFilter = ref<'all' | 'show' | 'movie'>('all')

function resolvedMediaType(rs: RuleSetEntry): string {
  return rs.mediaType || 'show'
}

const typeTabs = computed(() => {
  const all = rulesets.value.length
  const movies = rulesets.value.filter(rs => resolvedMediaType(rs) === 'movie').length
  const shows = all - movies
  return [
    { label: 'All', value: 'all' as const, count: all },
    { label: 'Shows', value: 'show' as const, count: shows },
    { label: 'Movies', value: 'movie' as const, count: movies },
  ]
})

const sortedRulesets = computed(() =>
  [...rulesets.value].sort((a, b) =>
    sortBy.value === 'id'
      ? a.ruleSetId.localeCompare(b.ruleSetId)
      : a.topic.localeCompare(b.topic, 'de'))
)

const filteredRulesets = computed(() => {
  let result = sortedRulesets.value

  if (typeFilter.value !== 'all') {
    result = result.filter(rs => resolvedMediaType(rs) === typeFilter.value)
  }

  const term = search.value.toLowerCase()
  if (term) {
    result = result.filter(rs => {
      const haystack = [
        rs.ruleSetId,
        rs.topic,
        rs.mediaName ?? '',
        ...rs.aliases,
        rs.tvdbId?.toString() ?? '',
        rs.imdbId ?? '',
        rs.tmdbId?.toString() ?? '',
      ].join(' ').toLowerCase()
      return haystack.includes(term)
    })
  }

  return result
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
