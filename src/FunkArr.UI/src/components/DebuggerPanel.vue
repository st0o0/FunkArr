<template>
  <div class="flex flex-col">
    <!-- Tabs -->
    <div class="flex border-b border-border-subtle mb-3">
      <button
        v-for="tab in ['Search', 'Test Results'] as const"
        :key="tab"
        class="px-3 py-2 text-xs font-medium transition-colors border-b-2 -mb-px"
        :class="activeTab === tab
          ? 'border-accent text-text-primary'
          : 'border-transparent text-text-secondary hover:text-text-body'"
        @click="activeTab = tab"
      >{{ tab }}</button>
    </div>

    <!-- Search Tab -->
    <div v-if="activeTab === 'Search'" class="flex flex-col">
      <div class="space-y-2">
        <div class="flex gap-1.5">
          <div class="relative flex-1">
            <svg class="absolute left-2.5 top-1/2 -translate-y-1/2 w-3 h-3 text-text-muted" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round">
              <circle cx="7" cy="7" r="4.5"/><path d="M10.5 10.5L14 14"/>
            </svg>
            <input
              v-model="searchQuery"
              placeholder="Search Mediathek..."
              class="w-full bg-surface-elevated border border-border-default rounded-md pl-7 pr-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus"
              @keyup.enter="doSearch"
            />
          </div>
          <button
            class="px-3 py-1.5 text-xs font-medium rounded-md transition-colors shrink-0"
            :class="searchQuery.trim()
              ? 'bg-accent text-surface-base hover:bg-accent/90'
              : 'bg-surface-elevated text-text-muted cursor-not-allowed'"
            :disabled="!searchQuery.trim() || searchLoading"
            @click="doSearch"
          >{{ searchLoading ? '...' : 'Search' }}</button>
        </div>
        <div class="flex gap-1.5">
          <input
            v-model="channelFilter"
            placeholder="Channel"
            class="flex-1 bg-surface-elevated border border-border-default rounded-md px-2.5 py-1 text-xs text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus"
          />
          <input
            v-model="topicFilter"
            placeholder="Topic"
            class="flex-1 bg-surface-elevated border border-border-default rounded-md px-2.5 py-1 text-xs text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus"
          />
        </div>
      </div>

      <div v-if="searchError" class="text-status-fail text-xs mt-3">{{ searchError }}</div>

      <div v-if="searchResults.length > 0" class="mt-3">
        <div class="flex items-center justify-between mb-2">
          <span class="text-xs text-text-body">{{ searchResults.length }} results · {{ selectedCount }} selected</span>
          <button class="text-xs text-accent hover:text-accent/80 transition-colors" @click="toggleSelectAll">
            {{ allSelected ? 'Deselect All' : 'Select All' }}
          </button>
        </div>

        <div class="max-h-[45vh] overflow-y-auto space-y-0.5 mb-3">
          <label
            v-for="(item, idx) in searchResults"
            :key="idx"
            class="flex items-start gap-2 p-2 rounded-md hover:bg-surface-elevated/50 cursor-pointer text-xs"
          >
            <input type="checkbox" v-model="selected[idx]" class="mt-0.5 accent-accent" />
            <div class="min-w-0 flex-1">
              <div class="text-text-primary truncate font-medium">{{ item.title }}</div>
              <div class="text-text-secondary">
                {{ item.channel }} · {{ item.topic }} ·
                {{ Math.floor(item.duration / 60) }}min
                <span v-if="item.quality > 0"> · {{ item.quality }}p</span>
              </div>
            </div>
          </label>
        </div>

        <button
          class="w-full px-3 py-1.5 rounded-md text-sm transition-colors"
          :class="canTest
            ? 'bg-accent text-surface-base font-medium hover:bg-accent/90'
            : 'bg-surface-elevated text-text-muted cursor-not-allowed border border-border-default'"
          :disabled="!canTest || testing"
          @click="runTest"
        >{{ testing ? 'Testing...' : `Test Rules (${selectedCount})` }}</button>
      </div>

      <div v-else-if="hasSearched && !searchLoading" class="mt-3 text-center py-4">
        <p class="text-text-secondary text-xs">No results found</p>
      </div>

      <div v-else-if="!hasSearched && !searchLoading" class="mt-3 text-center py-4">
        <p class="text-text-muted text-xs">Search the Mediathek to find test candidates</p>
      </div>
    </div>

    <!-- Test Results Tab -->
    <div v-if="activeTab === 'Test Results'" class="flex-1 overflow-y-auto">
      <div v-if="!results" class="text-text-secondary text-xs text-center py-4">Run a search and test rules to see results.</div>
      <div v-else class="space-y-2">
        <div class="flex items-center justify-between mb-1">
          <span class="text-xs text-text-secondary">
            {{ results.filter(r => r.matched).length }}/{{ results.length }} matched
          </span>
        </div>

        <div
          v-for="(item, idx) in sortedResults"
          :key="idx"
          class="bg-surface-raised rounded-lg border border-border-default text-sm"
        >
          <div class="p-3 cursor-pointer" @click="toggleExpand(idx)">
            <div class="flex items-baseline gap-2 mb-0.5">
              <span class="font-semibold text-text-body truncate">{{ item.candidateTitle }}</span>
              <span
                class="text-xs px-1.5 py-0.5 rounded shrink-0"
                :class="item.matched ? 'bg-surface-elevated text-status-ok' : 'bg-surface-elevated text-text-secondary'"
              >{{ item.matched ? 'Matched' : 'No Match' }}</span>
            </div>
            <div class="text-xs text-text-secondary">
              {{ item.candidateChannel }} &middot; {{ item.candidateTopic }} &middot;
              {{ Math.floor(item.candidateDuration / 60) }}min
              <template v-if="item.matched">
                &middot; <span class="text-status-ok font-medium">{{ item.matchedRuleId }}</span>
                &middot; score {{ item.score.toFixed(2) }}
              </template>
            </div>
          </div>

          <div v-if="expanded[idx]" class="border-t border-border-default px-3 pb-3 pt-2">
            <div class="text-xs font-semibold text-text-secondary mb-2">Rule Pipeline</div>
            <div class="space-y-2">
              <div
                v-for="(rt, ri) in item.ruleTraces"
                :key="ri"
                class="pl-3 border-l-2"
                :class="outcomeBorderClass(rt.outcome, item.matched && ri > item.ruleTraces.findIndex(r => r.outcome === 'matched'))"
              >
                <div class="flex items-center gap-2 text-xs mb-1">
                  <span class="font-mono text-text-body">{{ rt.ruleId }}</span>
                  <span class="text-text-secondary">prio {{ rt.priority }}</span>
                  <span
                    class="px-1.5 py-0.5 rounded text-[11px] font-medium"
                    :class="outcomeBadgeClass(rt.outcome, item.matched && ri > item.ruleTraces.findIndex(r => r.outcome === 'matched'))"
                  >{{ isSkipped(rt, item, ri) ? 'Skipped' : outcomeLabel(rt.outcome) }}</span>
                </div>

                <div v-if="rt.filterTrace && !isSkipped(rt, item, ri)" class="ml-2 mb-1">
                  <FilterGroupTraceView :group="rt.filterTrace" />
                </div>

                <div v-if="rt.identificationTrace && !isSkipped(rt, item, ri)" class="ml-2 text-xs">
                  <div class="flex items-center gap-2">
                    <span class="text-text-secondary">Identification:</span>
                    <span class="font-mono text-text-secondary">{{ rt.identificationTrace.strategy }}</span>
                  </div>
                  <div v-if="!rt.identificationTrace.attempted" class="text-text-secondary ml-4">Not attempted</div>
                  <div v-else-if="rt.identificationTrace.detail" class="text-status-fail ml-4">{{ rt.identificationTrace.detail }}</div>
                  <div v-else class="ml-4 text-status-ok">
                    <template v-if="item.identification">
                      <span v-if="item.identification.season" class="mr-2">Season: <span class="font-mono">{{ item.identification.season }}</span></span>
                      <span v-if="item.identification.episode" class="mr-2">Episode: <span class="font-mono">{{ item.identification.episode }}</span></span>
                      <span v-if="item.identification.title">Title: <span class="font-mono">{{ item.identification.title }}</span></span>
                    </template>
                    <span v-else>OK</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from 'vue'
import {
  testRuleSet,
  searchMediathek,
  type TestCandidate,
  type ItemTrace,
  type MediathekCandidate,
} from '../api/rulesets'
import FilterGroupTraceView from './FilterGroupTraceView.vue'

const props = defineProps<{
  builderState: {
    confidence: number
    rules: {
      id: string
      priority: number
      confidence: number | null
      strategy: string
      seasonRegex: string
      episodeRegex: string
      captureGroup: number | null
      filters: {
        all: { field: string; op: string; value: string }[]
        any: { field: string; op: string; value: string }[]
        not: { field: string; op: string; value: string }[]
      }
      titleRules: { type: string; field: string; pattern: string; captureGroup: number | null; value: string }[]
    }[]
  }
}>()

const activeTab = ref<'Search' | 'Test Results'>('Search')

// Search
const searchQuery = ref('')
const channelFilter = ref('')
const topicFilter = ref('')
const searchResults = ref<MediathekCandidate[]>([])
const selected = reactive<Record<number, boolean>>({})
const searchLoading = ref(false)
const hasSearched = ref(false)
const searchError = ref<string | null>(null)

const allSelected = computed(() =>
  searchResults.value.length > 0 && searchResults.value.every((_, i) => selected[i])
)

const selectedCount = computed(() =>
  searchResults.value.filter((_, i) => selected[i]).length
)

function toggleSelectAll() {
  const selectAll = !allSelected.value
  searchResults.value.forEach((_, i) => { selected[i] = selectAll })
}

async function doSearch() {
  const q = searchQuery.value.trim()
  if (!q && !channelFilter.value.trim() && !topicFilter.value.trim()) {
    searchResults.value = []
    hasSearched.value = false
    return
  }

  searchLoading.value = true
  searchError.value = null
  hasSearched.value = true

  try {
    searchResults.value = await searchMediathek(q || topicFilter.value || channelFilter.value)
    Object.keys(selected).forEach(k => delete selected[Number(k)])
    searchResults.value.forEach((_, i) => { selected[i] = true })
  } catch (e) {
    searchError.value = e instanceof Error ? e.message : 'Search failed'
    searchResults.value = []
  } finally {
    searchLoading.value = false
  }
}

// Testing
const testing = ref(false)
const testError = ref<string | null>(null)
const results = ref<ItemTrace[] | null>(null)
const expanded = reactive<Record<number, boolean>>({})

const canTest = computed(() =>
  selectedCount.value > 0 && props.builderState.rules.length > 0
)

function hasFilters(filters: { all: unknown[]; any: unknown[]; not: unknown[] }): boolean {
  return filters.all.length > 0 || filters.any.length > 0 || filters.not.length > 0
}

async function runTest() {
  if (!canTest.value) return
  testing.value = true
  testError.value = null
  Object.keys(expanded).forEach(k => delete expanded[Number(k)])

  const candidates: TestCandidate[] = searchResults.value
    .filter((_, i) => selected[i])
    .map(item => ({
      title: item.title,
      topic: item.topic,
      channel: item.channel,
      duration: item.duration,
      quality: item.quality,
      description: item.description,
      timestamp: item.timestamp,
    }))

  const config = {
    defaultConfidence: props.builderState.confidence,
    rules: props.builderState.rules.map(r => ({
      id: r.id,
      priority: r.priority,
      confidence: r.confidence,
      strategy: r.strategy,
      seasonRegex: r.seasonRegex || null,
      episodeRegex: r.episodeRegex || null,
      captureGroup: r.captureGroup,
      filters: hasFilters(r.filters) ? {
        all: r.filters.all.length > 0 ? r.filters.all : undefined,
        any: r.filters.any.length > 0 ? r.filters.any : undefined,
        not: r.filters.not.length > 0 ? r.filters.not : undefined,
      } : null,
      titleRules: r.titleRules.length > 0 ? r.titleRules : null,
    })),
  }

  try {
    const response = await testRuleSet(config, candidates)
    results.value = response.itemTraces
    activeTab.value = 'Test Results'
  } catch (e) {
    testError.value = e instanceof Error ? e.message : 'Test failed'
    results.value = null
  } finally {
    testing.value = false
  }
}

const sortedResults = computed(() => {
  if (!results.value) return []
  return [...results.value].sort((a, b) => {
    if (a.matched && !b.matched) return -1
    if (!a.matched && b.matched) return 1
    return 0
  })
})

function toggleExpand(idx: number) {
  expanded[idx] = !expanded[idx]
}

function isSkipped(_rt: ItemTrace['ruleTraces'][0], item: ItemTrace, ri: number): boolean {
  if (!item.matched) return false
  const matchIdx = item.ruleTraces.findIndex(r => r.outcome === 'matched')
  return matchIdx >= 0 && ri > matchIdx
}

function outcomeLabel(outcome: string): string {
  switch (outcome) {
    case 'matched': return 'Matched'
    case 'filterFailed': return 'Filter Failed'
    case 'identificationFailed': return 'ID Failed'
    default: return outcome
  }
}

function outcomeBorderClass(outcome: string, skipped: boolean): string {
  if (skipped) return 'border-border-default'
  switch (outcome) {
    case 'matched': return 'border-status-ok'
    case 'filterFailed': return 'border-status-fail'
    case 'identificationFailed': return 'border-amber-500'
    default: return 'border-border-default'
  }
}

function outcomeBadgeClass(outcome: string, skipped: boolean): string {
  if (skipped) return 'bg-surface-elevated text-text-secondary'
  switch (outcome) {
    case 'matched': return 'bg-status-ok/10 text-status-ok'
    case 'filterFailed': return 'bg-status-fail/10 text-status-fail'
    case 'identificationFailed': return 'bg-amber-500/10 text-amber-500'
    default: return 'bg-surface-elevated text-text-secondary'
  }
}
</script>
