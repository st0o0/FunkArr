<template>
  <div class="max-w-3xl mx-auto">
    <h1 class="text-xl font-semibold text-text-primary tracking-tight mb-4">{{ $t('rulesets.title') }}</h1>

    <div class="bg-surface-raised rounded-lg border border-border-default p-3 mb-4 space-y-3">
      <div class="flex items-center gap-2">
        <div class="relative flex-1">
          <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-text-muted" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round">
            <circle cx="7" cy="7" r="4.5"/><path d="M10.5 10.5L14 14"/>
          </svg>
          <input
            v-model="search"
            type="text"
            :placeholder="$t('rulesets.searchPlaceholder')"
            class="w-full bg-surface-elevated border border-border-default rounded-md pl-8 pr-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus"
          />
        </div>
        <select
          v-model="sortBy"
          class="bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body focus:outline-none focus:border-border-focus"
        >
          <option value="topic">{{ $t('rulesets.sortByName') }}</option>
          <option value="id">{{ $t('rulesets.sortById') }}</option>
        </select>
        <router-link
          to="/rulesets/new"
          class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-accent text-surface-base font-medium rounded-md hover:bg-accent/90 text-sm transition-colors whitespace-nowrap"
        >
          {{ $t('rulesets.new') }}
        </router-link>
      </div>

      <div v-if="!loading && rulesets.length > 0" class="flex items-center gap-4">
        <div class="flex items-center gap-1">
          <button
            v-for="tab in typeTabs"
            :key="tab.label"
            class="px-2.5 py-1 text-xs rounded-md transition-colors"
            :class="typeFilter === tab.value
              ? 'bg-accent/15 text-accent font-medium'
              : 'text-text-secondary hover:text-text-body hover:bg-surface-elevated'"
            @click="typeFilter = tab.value"
          >
            {{ tab.label }} ({{ tab.count }})
          </button>
        </div>
        <template v-if="hasMultipleSources">
          <span class="text-text-muted">|</span>
          <div class="flex items-center gap-1">
            <button
              v-for="tab in sourceTabs"
              :key="tab.label"
              class="px-2.5 py-1 text-xs rounded-md transition-colors"
              :class="sourceFilter === tab.value
                ? 'bg-accent/15 text-accent font-medium'
                : 'text-text-secondary hover:text-text-body hover:bg-surface-elevated'"
              @click="sourceFilter = tab.value"
            >
              {{ tab.label }} ({{ tab.count }})
            </button>
          </div>
        </template>
      </div>
    </div>

    <div v-if="loading" class="grid gap-2">
      <SkeletonCard v-for="i in 3" :key="i" />
    </div>
    <div v-else-if="error" class="text-status-fail text-sm">{{ error }}</div>
    <EmptyState
      v-else-if="rulesets.length === 0"
      icon='<path d="M2 4h12M2 8h12M2 12h8"/><circle cx="13" cy="12" r="1.5"/>'
      :title="$t('rulesets.noRulesetsRegistered')"
      :description="$t('rulesets.noRulesetsHint')"
    >
      <router-link
        to="/rulesets/new"
        class="inline-flex items-center gap-1.5 mt-3 px-3 py-1.5 bg-surface-elevated border border-border-default text-text-body rounded-md hover:bg-surface-overlay text-sm transition-colors"
      >
        {{ $t('rulesets.createFirst') }}
      </router-link>
    </EmptyState>
    <EmptyState
      v-else-if="filteredRulesets.length === 0"
      icon='<circle cx="8" cy="8" r="5"/><path d="M11.5 11.5L14 14"/>'
      :title="$t('rulesets.noMatchingRulesets')"
      :description="$t('rulesets.noMatchingHint')"
    />

    <div class="text-xs text-text-secondary mb-2" v-if="!loading && rulesets.length > 0">
      {{ $t('rulesets.rulesetCount', { count: filteredRulesets.length }) }}
      <span v-if="filteredRulesets.length !== rulesets.length"> {{ $t('common.of') }} {{ rulesets.length }}</span>
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
              v-if="rs.mediaType === MediaType.Movie"
              class="text-[11px] font-medium px-1.5 py-0.5 rounded bg-surface-overlay/60 text-text-body"
            >{{ mediaTypeLabel(rs) }}</span>
            <span
              v-if="hasMultipleSources"
              class="text-[11px] font-medium px-1.5 py-0.5 rounded"
              :class="{
                'bg-surface-overlay/60 text-text-body': rs.sourceType === SourceType.Community,
                'bg-accent/15 text-accent': rs.sourceType === SourceType.Local,
                'bg-surface-overlay/60 text-text-primary': rs.sourceType === SourceType.Merged,
              }"
            >{{ sourceTypeLabel(rs.sourceType) }}</span>
          </div>
        </div>
        <div v-if="rs.mediaName && rs.mediaName !== rs.topic" class="text-xs text-text-body mb-0.5">
          {{ rs.topic }}
        </div>
        <div v-if="rs.aliases.length > 0" class="text-xs text-text-secondary mb-0.5">
          {{ rs.aliases.slice(0, 2).join(', ') }}<span v-if="rs.aliases.length > 2" class="text-text-muted"> +{{ rs.aliases.length - 2 }}</span>
        </div>
        <div class="flex items-center gap-2 text-xs text-text-secondary mt-1">
          <span class="text-text-body">{{ $t('rulesets.ruleCount', { count: rs.ruleCount }) }}</span>
          <span v-if="rs.imdbId"><span class="text-text-muted">IMDB</span> {{ rs.imdbId }}</span>
          <span v-if="rs.tvdbId"><span class="text-text-muted">TVDB</span> {{ rs.tvdbId }}</span>
          <span v-if="rs.tmdbId"><span class="text-text-muted">TMDB</span> {{ rs.tmdbId }}</span>
          <template v-if="rs.lastScoringRun">
            <span>&middot;</span>
            <span>{{ $t('rulesets.lastScored', { time: formatRelativeDate(rs.lastScoringRun) }) }}</span>
          </template>
          <template v-if="rs.matchRate !== null">
            <span>&middot;</span>
            <span>{{ $t('rulesets.matchRate', { rate: formatPercent(rs.matchRate) }) }}</span>
          </template>
          <template v-if="rs.enrichmentRate !== null">
            <span>&middot;</span>
            <span>{{ $t('rulesets.enrichmentRate', { rate: formatPercent(rs.enrichmentRate) }) }}</span>
          </template>
        </div>
      </router-link>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { listRuleSets, type RuleSetEntry } from '../api/rulesets'
import SkeletonCard from '../components/SkeletonCard.vue'
import EmptyState from '../components/EmptyState.vue'
import { formatRelativeDate, formatPercent } from '../utils/format'
import { SourceType, MediaType } from '../api/enums'

const { t } = useI18n()

const rulesets = ref<RuleSetEntry[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const search = ref('')
const sortBy = ref<'topic' | 'id'>('topic')
const typeFilter = ref<number | null>(null)
const sourceFilter = ref<number | null>(null)

function resolvedMediaType(rs: RuleSetEntry): number {
  return rs.mediaType ?? MediaType.Show
}

function mediaTypeLabel(rs: RuleSetEntry): string {
  return resolvedMediaType(rs) === MediaType.Movie ? 'movie' : 'show'
}

function sourceTypeLabel(type: number): string {
  switch (type) {
    case SourceType.Community: return 'community'
    case SourceType.Local: return 'local'
    case SourceType.Merged: return 'merged'
    default: return 'unknown'
  }
}

const typeTabs = computed(() => {
  const all = rulesets.value.length
  const movies = rulesets.value.filter(rs => resolvedMediaType(rs) === MediaType.Movie).length
  const shows = all - movies
  return [
    { label: t('rulesets.all'), value: null as number | null, count: all },
    { label: t('rulesets.shows'), value: MediaType.Show as number | null, count: shows },
    { label: t('rulesets.movies'), value: MediaType.Movie as number | null, count: movies },
  ]
})

const hasMultipleSources = computed(() => {
  const types = new Set(rulesets.value.map(rs => rs.sourceType))
  return types.size > 1
})

const sourceTabs = computed(() => {
  const community = rulesets.value.filter(rs => rs.sourceType === SourceType.Community).length
  const local = rulesets.value.filter(rs => rs.sourceType === SourceType.Local || rs.sourceType === SourceType.Merged).length
  return [
    { label: t('rulesets.allSources'), value: null as number | null, count: rulesets.value.length },
    { label: t('rulesets.community'), value: SourceType.Community as number | null, count: community },
    { label: t('rulesets.local'), value: SourceType.Local as number | null, count: local },
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

  if (typeFilter.value !== null) {
    result = result.filter(rs => resolvedMediaType(rs) === typeFilter.value)
  }

  if (sourceFilter.value === SourceType.Community) {
    result = result.filter(rs => rs.sourceType === SourceType.Community)
  } else if (sourceFilter.value === SourceType.Local) {
    result = result.filter(rs => rs.sourceType === SourceType.Local || rs.sourceType === SourceType.Merged)
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
    const response = await listRuleSets()
    rulesets.value = response.rulesets
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load rulesets'
  } finally {
    loading.value = false
  }
})
</script>
