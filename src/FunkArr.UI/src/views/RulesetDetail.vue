<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api, apiDelete } from '@/api/client'
import RuleCard from '@/components/RuleCard.vue'

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

interface RuleSetFile {
  topic: string
  aliases: string[]
  media: { name: string; tvdbId?: number; imdbId?: string; tmdbId?: number; type: string }
  source: string
  confidence: number
  rules: Rule[]
  overrides?: { mode: string; base?: string; add?: Rule[]; remove?: number[] }
}

interface UnmatchedItem {
  itemTitle: string
  ruleFailures: Array<{ ruleIndex: number; failReason: string; detail?: string }>
}

const route = useRoute()
const router = useRouter()
const topic = route.params.topic as string

const ruleset = ref<RuleSetFile | null>(null)
const unmatched = ref<UnmatchedItem[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

function sourceBadgeClass(source: string): string {
  switch (source) {
    case 'generated':
      return 'bg-amber-100 text-amber-700 dark:bg-amber-900 dark:text-amber-300'
    case 'local':
      return 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300'
    default:
      return 'bg-neutral-100 text-neutral-700 dark:bg-neutral-700 dark:text-neutral-300'
  }
}

async function fetchData() {
  loading.value = true
  error.value = null
  try {
    const [rs, um] = await Promise.all([
      api<RuleSetFile>(`/api/rulesets/${encodeURIComponent(topic)}`),
      api<UnmatchedItem[]>(`/api/matches/unmatched?topic=${encodeURIComponent(topic)}`).catch(() => [] as UnmatchedItem[]),
    ])
    ruleset.value = rs
    unmatched.value = um
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  } finally {
    loading.value = false
  }
}

async function deleteOverride() {
  if (!confirm(`Delete local override for "${topic}"?`)) return
  try {
    await apiDelete(`/api/rulesets/${encodeURIComponent(topic)}`)
    await fetchData()
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  }
}

onMounted(fetchData)
</script>

<template>
  <div>
    <button
      class="text-sm text-neutral-500 hover:text-neutral-900 dark:hover:text-neutral-100 mb-4"
      @click="router.push('/rulesets')"
    >
      &larr; Back to Rulesets
    </button>

    <p v-if="loading" class="text-center text-neutral-500 py-12">Loading...</p>

    <p v-else-if="error" class="text-center text-red-600 dark:text-red-400 py-12">{{ error }}</p>

    <template v-else-if="ruleset">
      <div class="border border-neutral-200 dark:border-neutral-700 rounded p-4 mb-4">
        <div class="flex items-center justify-between gap-4">
          <div>
            <h1 class="text-lg font-bold">{{ ruleset.topic }}</h1>
            <div class="flex items-center gap-3 mt-1 text-sm text-neutral-500">
              <span
                class="text-xs font-medium px-2 py-0.5 rounded-full"
                :class="sourceBadgeClass(ruleset.source)"
              >
                {{ ruleset.source }}
              </span>
              <span>{{ ruleset.media.name }}</span>
              <span v-if="ruleset.media.tvdbId" class="font-mono">TVDB:{{ ruleset.media.tvdbId }}</span>
              <span class="capitalize">{{ ruleset.media.type }}</span>
            </div>
            <div v-if="ruleset.aliases.length" class="text-xs text-neutral-500 mt-1">
              Aliases: {{ ruleset.aliases.join(', ') }}
            </div>
          </div>
          <div class="flex gap-2 shrink-0">
            <button
              v-if="ruleset.source !== 'local'"
              class="border border-neutral-300 dark:border-neutral-600 px-3 py-1.5 rounded text-sm"
              @click="router.push(`/rulesets/${encodeURIComponent(topic)}/edit`)"
            >
              Create Local Override
            </button>
            <button
              v-if="ruleset.source === 'local'"
              class="border border-neutral-300 dark:border-neutral-600 px-3 py-1.5 rounded text-sm"
              @click="router.push(`/rulesets/${encodeURIComponent(topic)}/edit`)"
            >
              Edit
            </button>
            <button
              v-if="ruleset.source === 'local'"
              class="bg-red-600 text-white px-3 py-1.5 rounded text-sm"
              @click="deleteOverride"
            >
              Delete Override
            </button>
          </div>
        </div>
      </div>

      <h2 class="text-sm font-bold mb-2">Rules ({{ ruleset.rules.length }})</h2>
      <div class="flex flex-col gap-3 mb-6">
        <RuleCard
          v-for="(rule, i) in ruleset.rules"
          :key="i"
          :rule="rule"
          :index="i"
        />
      </div>

      <div v-if="unmatched.length > 0">
        <h2 class="text-sm font-bold mb-2 text-amber-600 dark:text-amber-400">
          Unmatched Items ({{ unmatched.length }})
        </h2>
        <div class="border border-neutral-200 dark:border-neutral-700 rounded divide-y divide-neutral-200 dark:divide-neutral-700">
          <div v-for="(item, i) in unmatched" :key="i" class="p-3">
            <div class="font-medium text-sm">{{ item.itemTitle }}</div>
            <div v-if="item.ruleFailures.length" class="mt-1 text-xs text-neutral-500 space-y-0.5">
              <div v-for="(f, fi) in item.ruleFailures" :key="fi">
                Rule #{{ f.ruleIndex }}: {{ f.failReason }}
                <span v-if="f.detail" class="font-mono">{{ f.detail }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
