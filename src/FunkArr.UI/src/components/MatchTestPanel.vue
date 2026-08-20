<script setup lang="ts">
import { ref } from 'vue'
import { apiPost, API_BASE } from '@/api/client'

interface Rule {
  priority: number
  filters: FilterGroup
  strategy: string
  confidence?: number
  seasonRegex?: string
  episodeRegex?: string
  captureGroup?: number
  titleRules: TitleRule[]
}

interface FilterGroup {
  all: FilterNode[]
  any: FilterNode[]
  not: FilterNode[]
}

type FilterNode = Filter | FilterGroup

interface Filter {
  field: string
  op: string
  value: string
}

interface TitleRule {
  type: string
  field?: string
  pattern?: string
  captureGroup?: number
  value?: string
}

interface TestResult {
  matched: Array<{ itemTitle: string; ruleIndex: number; strategy: string; season: number; episode: number; episodeName: string }>
  filtered: Array<{ itemTitle: string; filterField: string; filterOp: string; filterValue: string; reason: string }>
  unmatched: Array<{ itemTitle: string; ruleFailures: Array<{ ruleIndex: number; failReason: string; detail?: string }> }>
  totalItems: number
}

const props = defineProps<{
  topic: string
  tvdbId?: number
  rules: Rule[]
}>()

const result = ref<TestResult | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)
const matchedOpen = ref(true)
const filteredOpen = ref(true)
const unmatchedOpen = ref(true)

async function runTest() {
  loading.value = true
  error.value = null
  result.value = null
  try {
    result.value = await apiPost<TestResult>(`${API_BASE}/rulesets/test`, {
      topic: props.topic,
      tvdbId: props.tvdbId,
      rules: props.rules,
    })
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  } finally {
    loading.value = false
  }
}

function formatEpisode(m: { season: number; episode: number }): string {
  return `S${String(m.season).padStart(2, '0')}E${String(m.episode).padStart(2, '0')}`
}
</script>

<template>
  <div class="border border-neutral-200 dark:border-neutral-700 rounded p-4">
    <div class="flex items-center justify-between mb-3">
      <span class="text-sm font-bold">Match Test</span>
      <button
        type="button"
        class="bg-blue-600 text-white px-3 py-1.5 rounded text-sm disabled:opacity-50"
        :disabled="loading"
        @click="runTest"
      >
        {{ loading ? 'Testing...' : 'Test against Mediathek' }}
      </button>
    </div>

    <div v-if="loading" class="flex justify-center py-6">
      <div class="h-5 w-5 border-2 border-blue-600 border-t-transparent rounded-full animate-spin" />
    </div>

    <p v-if="error" class="text-sm text-red-600 dark:text-red-400">{{ error }}</p>

    <template v-if="result">
      <div class="text-xs text-neutral-500 mb-3">
        {{ result.totalItems }} total items
      </div>

      <!-- Matched -->
      <div class="mb-3">
        <button
          type="button"
          class="flex items-center gap-2 text-sm font-medium text-green-700 dark:text-green-400 mb-1"
          @click="matchedOpen = !matchedOpen"
        >
          <span>{{ matchedOpen ? '&#9660;' : '&#9654;' }}</span>
          Matched ({{ result.matched.length }})
        </button>
        <div v-if="matchedOpen && result.matched.length > 0" class="ml-4 space-y-1">
          <div v-for="(m, i) in result.matched" :key="i" class="text-sm">
            <span class="font-mono text-xs text-green-600 dark:text-green-400">{{ formatEpisode(m) }}</span>
            <span class="ml-2">{{ m.episodeName }}</span>
            <span class="ml-2 text-xs text-neutral-500">{{ m.itemTitle }}</span>
          </div>
        </div>
        <div v-if="matchedOpen && result.matched.length === 0" class="ml-4 text-sm text-neutral-500">
          No matched items
        </div>
      </div>

      <!-- Filtered -->
      <div class="mb-3">
        <button
          type="button"
          class="flex items-center gap-2 text-sm font-medium text-neutral-600 dark:text-neutral-400 mb-1"
          @click="filteredOpen = !filteredOpen"
        >
          <span>{{ filteredOpen ? '&#9660;' : '&#9654;' }}</span>
          Filtered ({{ result.filtered.length }})
        </button>
        <div v-if="filteredOpen && result.filtered.length > 0" class="ml-4 space-y-1">
          <div v-for="(f, i) in result.filtered" :key="i" class="text-sm">
            <span>{{ f.itemTitle }}</span>
            <span class="ml-2 text-xs text-neutral-500">{{ f.reason }}</span>
          </div>
        </div>
        <div v-if="filteredOpen && result.filtered.length === 0" class="ml-4 text-sm text-neutral-500">
          No filtered items
        </div>
      </div>

      <!-- Unmatched -->
      <div>
        <button
          type="button"
          class="flex items-center gap-2 text-sm font-medium text-amber-600 dark:text-amber-400 mb-1"
          @click="unmatchedOpen = !unmatchedOpen"
        >
          <span>{{ unmatchedOpen ? '&#9660;' : '&#9654;' }}</span>
          Unmatched ({{ result.unmatched.length }})
        </button>
        <div v-if="unmatchedOpen && result.unmatched.length > 0" class="ml-4 space-y-2">
          <div v-for="(u, i) in result.unmatched" :key="i" class="text-sm">
            <div class="font-medium">{{ u.itemTitle }}</div>
            <div v-for="(f, fi) in u.ruleFailures" :key="fi" class="ml-2 text-xs text-neutral-500">
              Rule #{{ f.ruleIndex }}: {{ f.failReason }}
              <span v-if="f.detail" class="font-mono">{{ f.detail }}</span>
            </div>
          </div>
        </div>
        <div v-if="unmatchedOpen && result.unmatched.length === 0" class="ml-4 text-sm text-neutral-500">
          No unmatched items
        </div>
      </div>
    </template>
  </div>
</template>
