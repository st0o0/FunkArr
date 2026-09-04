<template>
  <div class="flex flex-col h-full">
    <h2 class="text-xs font-semibold uppercase tracking-wider mb-3 text-text-muted">Debugger</h2>

    <!-- Tabs -->
    <div class="flex gap-1 mb-3">
      <button
        v-for="tab in ['Manual', 'Fetch'] as const"
        :key="tab"
        class="px-3 py-1.5 text-xs font-medium rounded-lg transition-colors"
        :class="activeTab === tab
          ? 'bg-brand-600 text-white'
          : 'bg-surface-elevated text-text-muted hover:text-text-secondary'"
        @click="activeTab = tab"
      >{{ tab }}</button>
    </div>

    <!-- Manual Input -->
    <div v-if="activeTab === 'Manual'" class="mb-3">
      <div class="bg-surface-raised rounded-xl border border-border-default p-3 space-y-2">
        <input v-model="manualForm.title" placeholder="Title" class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50" />
        <div class="grid grid-cols-2 gap-2">
          <input v-model="manualForm.topic" placeholder="Topic" class="bg-surface-elevated border border-border-default rounded-lg px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50" />
          <input v-model="manualForm.channel" placeholder="Channel" class="bg-surface-elevated border border-border-default rounded-lg px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50" />
        </div>
        <div class="grid grid-cols-2 gap-2">
          <input v-model.number="manualForm.durationMin" type="number" placeholder="Duration (min)" class="bg-surface-elevated border border-border-default rounded-lg px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50" />
          <input v-model.number="manualForm.quality" type="number" placeholder="Quality" class="bg-surface-elevated border border-border-default rounded-lg px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50" />
        </div>
        <textarea v-model="manualForm.description" placeholder="Description (optional)" rows="2" class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50 resize-none" />
        <input v-model="manualForm.timestamp" type="datetime-local" class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-1.5 text-sm text-text-body focus:outline-none focus:border-brand-500/50" />
        <button
          class="w-full px-3 py-1.5 bg-surface-elevated text-text-secondary border border-border-default rounded-lg hover:bg-surface-raised text-sm transition-colors"
          @click="addManualCandidate"
        >Add Candidate</button>
      </div>
    </div>

    <!-- Fetch Input -->
    <div v-if="activeTab === 'Fetch'" class="mb-3">
      <div class="bg-surface-raised rounded-xl border border-border-default p-3 space-y-2">
        <div class="flex gap-2">
          <input
            v-model="fetchQuery"
            placeholder="Search MediathekViewWeb..."
            class="flex-1 bg-surface-elevated border border-border-default rounded-lg px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50"
            @keyup.enter="doFetch"
          />
          <button
            class="px-3 py-1.5 bg-brand-600 text-white rounded-lg hover:bg-brand-500 text-sm transition-colors whitespace-nowrap"
            :disabled="fetchLoading || !fetchQuery.trim()"
            @click="doFetch"
          >{{ fetchLoading ? 'Searching...' : 'Search' }}</button>
        </div>

        <div v-if="fetchError" class="text-status-fail text-xs">{{ fetchError }}</div>

        <div v-if="fetchResults.length > 0" class="space-y-1">
          <div class="flex items-center justify-between">
            <span class="text-xs text-text-muted">{{ fetchResults.length }} results</span>
            <button class="text-xs text-brand-400 hover:text-brand-300 transition-colors" @click="toggleSelectAll">
              {{ allFetchSelected ? 'Deselect All' : 'Select All' }}
            </button>
          </div>
          <div class="max-h-40 overflow-y-auto space-y-0.5">
            <label
              v-for="(item, idx) in fetchResults"
              :key="idx"
              class="flex items-start gap-2 p-1.5 rounded hover:bg-surface-elevated/50 cursor-pointer text-xs"
            >
              <input type="checkbox" v-model="fetchSelected[idx]" class="mt-0.5" />
              <div class="min-w-0">
                <div class="text-text-body truncate">{{ item.title }}</div>
                <div class="text-text-muted">{{ item.channel }} &middot; {{ item.topic }}</div>
              </div>
            </label>
          </div>
          <button
            class="w-full px-3 py-1.5 bg-surface-elevated text-text-secondary border border-border-default rounded-lg hover:bg-surface-raised text-sm transition-colors"
            :disabled="selectedFetchCount === 0"
            @click="addFetchCandidates"
          >Add {{ selectedFetchCount }} Candidate(s)</button>
        </div>
        <div v-else-if="fetchSearched && !fetchLoading" class="text-text-muted text-xs">No results found</div>
      </div>
    </div>

    <!-- Candidate List -->
    <div v-if="candidates.length > 0" class="mb-3">
      <div class="flex items-center justify-between mb-1">
        <span class="text-xs font-semibold uppercase tracking-wider text-text-muted">Candidates ({{ candidates.length }})</span>
        <button class="text-xs text-text-muted hover:text-status-fail transition-colors" @click="candidates = []">Clear All</button>
      </div>
      <div class="space-y-1 max-h-32 overflow-y-auto">
        <div
          v-for="(c, idx) in candidates"
          :key="idx"
          class="flex items-center justify-between bg-surface-raised rounded-lg border border-border-default px-3 py-1.5 text-xs"
        >
          <div class="min-w-0">
            <span class="text-text-body truncate">{{ c.title }}</span>
            <span class="text-text-muted ml-2">{{ c.topic }}</span>
          </div>
          <button class="text-text-muted hover:text-status-fail ml-2 shrink-0" @click="candidates.splice(idx, 1)">
            <svg class="w-3.5 h-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M4 4l8 8M12 4l-8 8"/></svg>
          </button>
        </div>
      </div>
    </div>

    <!-- Test Button -->
    <button
      class="w-full px-4 py-2 rounded-lg text-sm font-medium transition-colors mb-3"
      :class="canTest
        ? 'bg-brand-600 text-white hover:bg-brand-500'
        : 'bg-surface-elevated text-text-muted cursor-not-allowed'"
      :disabled="!canTest || testing"
      @click="runTest"
    >{{ testing ? 'Testing...' : 'Test' }}</button>

    <div v-if="testError" class="text-status-fail text-xs mb-3">{{ testError }}</div>

    <!-- Results -->
    <div v-if="results" class="flex-1 overflow-y-auto space-y-2">
      <div class="flex items-center justify-between mb-1">
        <span class="text-xs font-semibold uppercase tracking-wider text-text-muted">Results</span>
        <span class="text-xs text-text-muted">
          {{ results.filter(r => r.matched).length }}/{{ results.length }} matched
        </span>
      </div>

      <div
        v-for="(item, idx) in sortedResults"
        :key="idx"
        class="bg-surface-raised rounded-xl border border-border-default text-sm"
        :class="item.matched ? 'border-l-4 border-l-status-ok' : 'border-l-4 border-l-border-default'"
      >
        <!-- Result Header -->
        <div class="p-3 cursor-pointer" @click="toggleExpand(idx)">
          <div class="flex items-baseline gap-2 mb-0.5">
            <span class="font-semibold text-text-body truncate">{{ item.candidateTitle }}</span>
            <span
              class="text-xs px-2 py-0.5 rounded-full font-medium shrink-0"
              :class="item.matched ? 'bg-status-ok/10 text-status-ok' : 'bg-surface-elevated text-text-muted'"
            >{{ item.matched ? 'Matched' : 'No Match' }}</span>
          </div>
          <div class="text-xs text-text-muted">
            {{ item.candidateChannel }} &middot; {{ item.candidateTopic }} &middot;
            {{ Math.floor(item.candidateDuration / 60) }}min
            <template v-if="item.matched">
              &middot; <span class="text-status-ok font-medium">{{ item.matchedRuleId }}</span>
              &middot; score {{ item.score.toFixed(2) }}
            </template>
          </div>
        </div>

        <!-- Expanded Trace -->
        <div v-if="expanded[idx]" class="border-t border-border-default px-3 pb-3 pt-2">
          <div class="text-xs font-semibold uppercase tracking-wider text-text-muted mb-2">Rule Pipeline</div>
          <div class="space-y-2">
            <div
              v-for="(rt, ri) in item.ruleTraces"
              :key="ri"
              class="pl-3 border-l-2"
              :class="outcomeBorderClass(rt.outcome, item.matched && ri > item.ruleTraces.findIndex(r => r.outcome === 'matched'))"
            >
              <!-- Rule Header -->
              <div class="flex items-center gap-2 text-xs mb-1">
                <span class="font-mono text-brand-400">{{ rt.ruleId }}</span>
                <span class="text-text-muted">prio {{ rt.priority }}</span>
                <span
                  class="px-1.5 py-0.5 rounded text-[10px] font-medium"
                  :class="outcomeBadgeClass(rt.outcome, item.matched && ri > item.ruleTraces.findIndex(r => r.outcome === 'matched'))"
                >{{ isSkipped(rt, item, ri) ? 'Skipped' : outcomeLabel(rt.outcome) }}</span>
              </div>

              <!-- Filter Trace -->
              <div v-if="rt.filterTrace && !isSkipped(rt, item, ri)" class="ml-2 mb-1">
                <FilterGroupTraceView :group="rt.filterTrace" />
              </div>

              <!-- Identification Trace -->
              <div v-if="rt.identificationTrace && !isSkipped(rt, item, ri)" class="ml-2 text-xs">
                <div class="flex items-center gap-2">
                  <span class="text-text-muted">Identification:</span>
                  <span class="font-mono text-text-secondary">{{ rt.identificationTrace.strategy }}</span>
                </div>
                <div v-if="!rt.identificationTrace.attempted" class="text-text-muted ml-4">Not attempted</div>
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

const activeTab = ref<'Manual' | 'Fetch'>('Manual')

// Manual input
const manualForm = reactive({
  title: '',
  topic: '',
  channel: '',
  durationMin: 0,
  quality: 720,
  description: '',
  timestamp: '',
})

const candidates = ref<TestCandidate[]>([])

function addManualCandidate() {
  if (!manualForm.title.trim()) return
  candidates.value.push({
    title: manualForm.title,
    topic: manualForm.topic,
    channel: manualForm.channel,
    duration: manualForm.durationMin * 60,
    quality: manualForm.quality,
    description: manualForm.description || null,
    timestamp: manualForm.timestamp
      ? Math.floor(new Date(manualForm.timestamp).getTime() / 1000)
      : 0,
  })
  manualForm.title = ''
  manualForm.topic = ''
  manualForm.channel = ''
  manualForm.durationMin = 0
  manualForm.quality = 720
  manualForm.description = ''
  manualForm.timestamp = ''
}

// Fetch input
const fetchQuery = ref('')
const fetchResults = ref<MediathekCandidate[]>([])
const fetchSelected = reactive<Record<number, boolean>>({})
const fetchLoading = ref(false)
const fetchSearched = ref(false)
const fetchError = ref<string | null>(null)

const allFetchSelected = computed(() =>
  fetchResults.value.length > 0 && fetchResults.value.every((_, i) => fetchSelected[i])
)

const selectedFetchCount = computed(() =>
  fetchResults.value.filter((_, i) => fetchSelected[i]).length
)

function toggleSelectAll() {
  const selectAll = !allFetchSelected.value
  fetchResults.value.forEach((_, i) => { fetchSelected[i] = selectAll })
}

async function doFetch() {
  if (!fetchQuery.value.trim()) return
  fetchLoading.value = true
  fetchError.value = null
  fetchSearched.value = true
  try {
    fetchResults.value = await searchMediathek(fetchQuery.value)
    Object.keys(fetchSelected).forEach(k => delete fetchSelected[Number(k)])
    fetchResults.value.forEach((_, i) => { fetchSelected[i] = true })
  } catch (e) {
    fetchError.value = e instanceof Error ? e.message : 'Search failed'
    fetchResults.value = []
  } finally {
    fetchLoading.value = false
  }
}

function addFetchCandidates() {
  fetchResults.value.forEach((item, idx) => {
    if (fetchSelected[idx]) {
      candidates.value.push({
        title: item.title,
        topic: item.topic,
        channel: item.channel,
        duration: item.duration,
        quality: item.quality,
        description: item.description,
        timestamp: item.timestamp,
      })
    }
  })
  fetchResults.value = []
  Object.keys(fetchSelected).forEach(k => delete fetchSelected[Number(k)])
  fetchSearched.value = false
}

// Testing
const testing = ref(false)
const testError = ref<string | null>(null)
const results = ref<ItemTrace[] | null>(null)
const expanded = reactive<Record<number, boolean>>({})

const canTest = computed(() =>
  candidates.value.length > 0 && props.builderState.rules.length > 0
)

function hasFilters(filters: { all: unknown[]; any: unknown[]; not: unknown[] }): boolean {
  return filters.all.length > 0 || filters.any.length > 0 || filters.not.length > 0
}

async function runTest() {
  if (!canTest.value) return
  testing.value = true
  testError.value = null
  Object.keys(expanded).forEach(k => delete expanded[Number(k)])

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
    const response = await testRuleSet(config, candidates.value)
    results.value = response.itemTraces
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
  if (skipped) return 'bg-surface-elevated text-text-muted'
  switch (outcome) {
    case 'matched': return 'bg-status-ok/10 text-status-ok'
    case 'filterFailed': return 'bg-status-fail/10 text-status-fail'
    case 'identificationFailed': return 'bg-amber-500/10 text-amber-500'
    default: return 'bg-surface-elevated text-text-muted'
  }
}
</script>
