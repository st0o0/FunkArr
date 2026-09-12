<template>
  <div class="max-w-3xl mx-auto">
    <h1 class="text-xl font-semibold text-text-primary tracking-tight mb-4">RuleSets</h1>

    <div class="bg-surface-raised rounded-lg border border-border-default p-3 mb-4 space-y-3">
      <div class="flex items-center gap-2">
        <div class="relative flex-1">
          <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-text-muted" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round">
            <circle cx="7" cy="7" r="4.5"/><path d="M10.5 10.5L14 14"/>
          </svg>
          <input
            v-model="search"
            type="text"
            placeholder="Search rulesets..."
            class="w-full bg-surface-elevated border border-border-default rounded-md pl-8 pr-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus"
          />
        </div>
        <select
          v-model="sortBy"
          class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body focus:outline-none focus:border-border-focus"
        >
          <option value="topic">Sort by Name</option>
          <option value="id">Sort by ID</option>
        </select>
        <router-link
          to="/rulesets/new"
          class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-accent text-surface-base font-medium rounded-md hover:bg-accent/90 text-sm transition-colors whitespace-nowrap"
        >
          + New
        </router-link>
      </div>

      <div v-if="!loading && rulesets.length > 0" class="flex items-center gap-4">
        <div class="flex items-center gap-1">
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
        <span class="text-text-muted">|</span>
        <div class="flex items-center gap-1">
          <button
            v-for="tab in sourceTabs"
            :key="tab.value"
            class="px-2.5 py-1 text-xs rounded-md transition-colors"
            :class="sourceFilter === tab.value
              ? 'bg-accent/15 text-accent font-medium'
              : 'text-text-secondary hover:text-text-body hover:bg-surface-elevated'"
            @click="sourceFilter = tab.value"
          >
            {{ tab.label }} ({{ tab.count }})
          </button>
        </div>
      </div>
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
          <div class="min-w-0">
            <span class="text-sm font-medium text-text-primary truncate block">{{ rs.mediaName || rs.topic }}</span>
          </div>
          <div class="flex items-center gap-1.5 shrink-0 ml-3">
            <span
              class="text-[11px] font-medium px-1.5 py-0.5 rounded bg-surface-overlay/60 text-text-body"
            >{{ resolvedMediaType(rs) }}</span>
            <span
              class="text-[11px] font-medium px-1.5 py-0.5 rounded"
              :class="{
                'bg-surface-overlay/60 text-text-body': rs.sourceType === 'community',
                'bg-accent/15 text-accent': rs.sourceType === 'local',
                'bg-surface-overlay/60 text-text-primary': rs.sourceType === 'merged',
              }"
            >{{ rs.sourceType }}</span>
          </div>
        </div>
        <div v-if="rs.mediaName && rs.mediaName !== rs.topic" class="text-xs text-text-body mb-0.5">
          {{ rs.topic }}
        </div>
        <div v-if="rs.aliases.length > 0" class="text-xs text-text-secondary mb-0.5">
          {{ rs.aliases.join(', ') }}
        </div>
        <div class="flex items-center gap-2 text-xs text-text-secondary mt-1">
          <span class="text-text-body">{{ rs.ruleCount }} {{ rs.ruleCount === 1 ? 'rule' : 'rules' }}</span>
          <span v-if="rs.imdbId"><span class="text-text-muted">IMDB</span> {{ rs.imdbId }}</span>
          <span v-if="rs.tvdbId"><span class="text-text-muted">TVDB</span> {{ rs.tvdbId }}</span>
          <span v-if="rs.tmdbId"><span class="text-text-muted">TMDB</span> {{ rs.tmdbId }}</span>
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
const sourceFilter = ref<'all' | 'community' | 'local'>('all')

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

const sourceTabs = computed(() => {
  const community = rulesets.value.filter(rs => rs.sourceType === 'community').length
  const local = rulesets.value.filter(rs => rs.sourceType === 'local' || rs.sourceType === 'merged').length
  return [
    { label: 'All Sources', value: 'all' as const, count: rulesets.value.length },
    { label: 'Community', value: 'community' as const, count: community },
    { label: 'Local', value: 'local' as const, count: local },
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

  if (sourceFilter.value === 'community') {
    result = result.filter(rs => rs.sourceType === 'community')
  } else if (sourceFilter.value === 'local') {
    result = result.filter(rs => rs.sourceType === 'local' || rs.sourceType === 'merged')
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
