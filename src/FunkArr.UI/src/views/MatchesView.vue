<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { api } from '@/api/client'

// --- Types ---

interface MatchedTrace {
  itemTitle: string
  ruleIndex: number
  strategy: string
  confidence: number
  season: number
  episode: number
  episodeName: string
}

interface FilteredTrace {
  itemTitle: string
  filterField: string
  filterOp: string
  filterValue: string
  actualValue: string
  reason: string
}

interface RuleFailure {
  ruleIndex: number
  failReason: string
  detail?: string
}

interface UnmatchedTrace {
  itemTitle: string
  ruleFailures: RuleFailure[]
}

interface MatchRecord {
  id: string
  timestamp: string
  searchTopic: string
  tvdbId?: number
  season?: number
  episode?: number
  source: string
  totalResults: number
  matched: MatchedTrace[]
  filtered: FilteredTrace[]
  unmatched: UnmatchedTrace[]
}

interface TopicStats {
  topic: string
  searchCount: number
  totalItemsEvaluated: number
  matchedCount: number
  filteredCount: number
  unmatchedCount: number
  matchRate: number
  perRuleHitCounts: Record<string, number>
}

interface UnmatchedItem {
  itemTitle: string
  itemTopic: string
  itemDuration: number
  itemChannel: string
  ruleFailures: RuleFailure[]
}

interface UnmatchedGroup {
  topic: string
  items: UnmatchedItem[]
}

// --- State ---

type SubView = 'recent' | 'topics' | 'unmatched'

const router = useRouter()
const activeView = ref<SubView>('recent')

const recentData = ref<MatchRecord[]>([])
const topicsData = ref<TopicStats[]>([])
const unmatchedData = ref<UnmatchedGroup[]>([])

const loading = ref(false)
const error = ref<string | null>(null)
const expandedIds = ref<Set<string>>(new Set())

// --- Computed ---

const sortedTopics = computed(() =>
  [...topicsData.value].sort((a, b) => a.matchRate - b.matchRate),
)

const sortedUnmatched = computed(() =>
  [...unmatchedData.value].sort((a, b) => b.items.length - a.items.length),
)

// --- Methods ---

function toggleExpand(id: string) {
  const next = new Set(expandedIds.value)
  if (next.has(id)) {
    next.delete(id)
  } else {
    next.add(id)
  }
  expandedIds.value = next
}

async function switchView(view: SubView) {
  activeView.value = view
  await fetchData(view)
}

async function fetchData(view: SubView) {
  loading.value = true
  error.value = null
  try {
    switch (view) {
      case 'recent':
        recentData.value = await api<MatchRecord[]>('/api/matches/recent')
        break
      case 'topics':
        topicsData.value = await api<TopicStats[]>('/api/matches/topics')
        break
      case 'unmatched':
        unmatchedData.value = await api<UnmatchedGroup[]>('/api/matches/unmatched')
        break
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  } finally {
    loading.value = false
  }
}

function formatTime(ts: string): string {
  return new Date(ts).toLocaleString()
}

function formatPercent(rate: number): string {
  return `${(rate * 100).toFixed(1)}%`
}

function formatDuration(seconds: number): string {
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return `${m}:${String(s).padStart(2, '0')}`
}

function goToRuleset(topic: string) {
  router.push(`/rulesets/${encodeURIComponent(topic)}`)
}

onMounted(() => fetchData('recent'))
</script>

<template>
  <div>
    <!-- Sub-view toggle -->
    <div class="flex gap-1 mb-4">
      <button
        v-for="view in (['recent', 'topics', 'unmatched'] as const)"
        :key="view"
        class="px-3 py-1.5 text-sm rounded transition-colors"
        :class="activeView === view
          ? 'bg-neutral-800 text-white dark:bg-neutral-200 dark:text-neutral-900'
          : 'text-neutral-600 dark:text-neutral-400 hover:bg-neutral-100 dark:hover:bg-neutral-800'"
        @click="switchView(view)"
      >
        {{ view.charAt(0).toUpperCase() + view.slice(1) }}
      </button>
    </div>

    <!-- Loading / Error -->
    <p v-if="loading" class="text-center text-neutral-500 py-12">Loading...</p>
    <p v-else-if="error" class="text-center text-red-600 dark:text-red-400 py-12">{{ error }}</p>

    <!-- Recent sub-view -->
    <template v-else-if="activeView === 'recent'">
      <p v-if="recentData.length === 0" class="text-center text-neutral-500 py-12">
        No recent matches
      </p>
      <div v-else class="flex flex-col gap-3">
        <div
          v-for="record in recentData"
          :key="record.id"
          class="border border-neutral-200 dark:border-neutral-700 rounded p-4"
        >
          <!-- Header -->
          <button
            class="w-full text-left flex items-center justify-between gap-4"
            @click="toggleExpand(record.id)"
          >
            <div class="min-w-0">
              <span class="text-sm font-medium">{{ record.searchTopic }}</span>
              <span v-if="record.season != null" class="text-xs text-neutral-500 ml-2">
                S{{ String(record.season).padStart(2, '0') }}E{{ String(record.episode).padStart(2, '0') }}
              </span>
              <span class="text-xs text-neutral-400 ml-2">{{ formatTime(record.timestamp) }}</span>
            </div>
            <div class="flex gap-3 text-xs shrink-0">
              <span class="text-green-600 dark:text-green-400">{{ record.matched.length }} matched</span>
              <span class="text-amber-600 dark:text-amber-400">{{ record.filtered.length }} filtered</span>
              <span class="text-red-600 dark:text-red-400">{{ record.unmatched.length }} unmatched</span>
              <span class="text-neutral-400">{{ record.totalResults }} total</span>
            </div>
          </button>

          <!-- Expanded details -->
          <div v-if="expandedIds.has(record.id)" class="mt-4 space-y-4">
            <!-- Matched -->
            <div v-if="record.matched.length > 0">
              <p class="text-sm font-medium text-neutral-500 uppercase tracking-wide mb-2">Matched</p>
              <div class="space-y-1">
                <div
                  v-for="(m, i) in record.matched"
                  :key="i"
                  class="text-xs flex items-baseline gap-2 py-1 border-b border-neutral-100 dark:border-neutral-800 last:border-0"
                >
                  <span class="font-mono text-neutral-400 w-6 shrink-0">#{{ m.ruleIndex }}</span>
                  <span class="truncate">{{ m.itemTitle }}</span>
                  <span class="text-neutral-500 shrink-0">{{ m.strategy }}</span>
                  <span class="text-neutral-400 shrink-0">{{ (m.confidence * 100).toFixed(0) }}%</span>
                  <span class="text-neutral-500 shrink-0">
                    S{{ String(m.season).padStart(2, '0') }}E{{ String(m.episode).padStart(2, '0') }}
                  </span>
                  <span v-if="m.episodeName" class="text-neutral-400 truncate">{{ m.episodeName }}</span>
                </div>
              </div>
            </div>

            <!-- Filtered -->
            <div v-if="record.filtered.length > 0">
              <p class="text-sm font-medium text-neutral-500 uppercase tracking-wide mb-2">Filtered</p>
              <div class="space-y-1">
                <div
                  v-for="(f, i) in record.filtered"
                  :key="i"
                  class="text-xs flex items-baseline gap-2 py-1 border-b border-neutral-100 dark:border-neutral-800 last:border-0"
                >
                  <span class="truncate">{{ f.itemTitle }}</span>
                  <span class="font-mono text-neutral-500 shrink-0">{{ f.filterField }} {{ f.filterOp }} {{ f.filterValue }}</span>
                  <span class="text-neutral-400 shrink-0">(was: {{ f.actualValue }})</span>
                </div>
              </div>
            </div>

            <!-- Unmatched -->
            <div v-if="record.unmatched.length > 0">
              <p class="text-sm font-medium text-neutral-500 uppercase tracking-wide mb-2">Unmatched</p>
              <div class="space-y-1">
                <div
                  v-for="(u, i) in record.unmatched"
                  :key="i"
                  class="text-xs py-1 border-b border-neutral-100 dark:border-neutral-800 last:border-0"
                >
                  <span>{{ u.itemTitle }}</span>
                  <div v-for="(rf, j) in u.ruleFailures" :key="j" class="ml-4 text-neutral-500">
                    <span class="font-mono">#{{ rf.ruleIndex }}</span> {{ rf.failReason }}
                    <span v-if="rf.detail" class="text-neutral-400"> - {{ rf.detail }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Topics sub-view -->
    <template v-else-if="activeView === 'topics'">
      <p v-if="topicsData.length === 0" class="text-center text-neutral-500 py-12">
        No topic data
      </p>
      <div v-else class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="text-left text-xs text-neutral-500 uppercase tracking-wide border-b border-neutral-200 dark:border-neutral-700">
              <th class="py-2 pr-4">Topic</th>
              <th class="py-2 pr-4 text-right">Searches</th>
              <th class="py-2 pr-4 text-right">Evaluated</th>
              <th class="py-2 pr-4 text-right">Matched</th>
              <th class="py-2 text-right">Match Rate</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="t in sortedTopics"
              :key="t.topic"
              class="border-b border-neutral-100 dark:border-neutral-800 cursor-pointer hover:bg-neutral-50 dark:hover:bg-neutral-800/50"
              :class="t.matchRate < 0.75 ? 'bg-amber-50 dark:bg-amber-950/20' : ''"
              @click="goToRuleset(t.topic)"
            >
              <td class="py-2 pr-4 font-mono text-sm">{{ t.topic }}</td>
              <td class="py-2 pr-4 text-right tabular-nums">{{ t.searchCount }}</td>
              <td class="py-2 pr-4 text-right tabular-nums">{{ t.totalItemsEvaluated }}</td>
              <td class="py-2 pr-4 text-right tabular-nums">{{ t.matchedCount }}</td>
              <td class="py-2 text-right tabular-nums" :class="t.matchRate < 0.75 ? 'text-amber-600 dark:text-amber-400' : ''">
                {{ formatPercent(t.matchRate) }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>

    <!-- Unmatched sub-view -->
    <template v-else-if="activeView === 'unmatched'">
      <p v-if="unmatchedData.length === 0" class="text-center text-neutral-500 py-12">
        No unmatched items
      </p>
      <div v-else class="space-y-4">
        <div
          v-for="group in sortedUnmatched"
          :key="group.topic"
          class="border border-neutral-200 dark:border-neutral-700 rounded p-4"
        >
          <div class="flex items-center justify-between mb-3">
            <button
              class="text-sm font-mono font-medium hover:underline"
              @click="goToRuleset(group.topic)"
            >
              {{ group.topic }}
            </button>
            <span class="text-xs text-neutral-500">{{ group.items.length }} items</span>
          </div>
          <div class="space-y-2">
            <div
              v-for="(item, i) in group.items"
              :key="i"
              class="text-xs border-b border-neutral-100 dark:border-neutral-800 last:border-0 pb-2 last:pb-0"
            >
              <div class="flex items-baseline gap-2">
                <span class="truncate">{{ item.itemTitle }}</span>
                <span class="text-neutral-400 shrink-0">{{ formatDuration(item.itemDuration) }}</span>
                <span class="text-neutral-400 shrink-0">{{ item.itemChannel }}</span>
              </div>
              <div v-for="(rf, j) in item.ruleFailures" :key="j" class="ml-4 text-neutral-500 mt-0.5">
                <span class="font-mono">#{{ rf.ruleIndex }}</span> {{ rf.failReason }}
                <span v-if="rf.detail" class="text-neutral-400"> - {{ rf.detail }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
