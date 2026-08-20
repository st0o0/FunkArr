<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api, apiPut } from '@/api/client'
import FilterEditor from '@/components/FilterEditor.vue'
import TitleRuleEditor from '@/components/TitleRuleEditor.vue'
import MatchTestPanel from '@/components/MatchTestPanel.vue'

interface Filter {
  field: string
  op: string
  value: string
}

interface FilterGroup {
  all: FilterNode[]
  any: FilterNode[]
  not: FilterNode[]
}

type FilterNode = Filter | FilterGroup

interface TitleRule {
  type: string
  field?: string
  pattern?: string
  captureGroup?: number
  value?: string
}

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

interface RuleSetFile {
  topic: string
  aliases: string[]
  media: { name: string; tvdbId?: number; imdbId?: string; tmdbId?: number; type: string }
  source: string
  confidence: number
  rules: Rule[]
}

const route = useRoute()
const router = useRouter()
const topic = route.params.topic as string | undefined
const isNew = !topic

const strategies = [
  'SeasonAndEpisodeNumber',
  'ItemTitleExact',
  'ItemTitleIncludes',
  'ItemTitleEqualsAirdate',
  'ByAbsoluteEpisodeNumber',
]

const form = ref<RuleSetFile>({
  topic: '',
  aliases: [],
  media: { name: '', type: 'show' },
  source: 'local',
  confidence: 1,
  rules: [],
})

const aliasesText = ref('')
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)

const showRegex = computed(() => (strategy: string) =>
  strategy === 'SeasonAndEpisodeNumber' || strategy === 'ByAbsoluteEpisodeNumber',
)

function emptyFilterGroup(): FilterGroup {
  return { all: [], any: [], not: [] }
}

function addRule() {
  form.value.rules.push({
    priority: form.value.rules.length,
    filters: emptyFilterGroup(),
    strategy: 'SeasonAndEpisodeNumber',
    titleRules: [],
  })
}

function removeRule(index: number) {
  form.value.rules.splice(index, 1)
}

async function fetchExisting() {
  if (!topic) return
  loading.value = true
  error.value = null
  try {
    const rs = await api<RuleSetFile>(`/api/rulesets/${encodeURIComponent(topic)}`)
    form.value = rs
    aliasesText.value = rs.aliases.join(', ')
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  error.value = null
  form.value.aliases = aliasesText.value
    .split(',')
    .map((s) => s.trim())
    .filter(Boolean)
  const saveTopic = form.value.topic
  try {
    await apiPut(`/api/rulesets/${encodeURIComponent(saveTopic)}`, form.value)
    router.push(`/rulesets/${encodeURIComponent(saveTopic)}`)
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  } finally {
    saving.value = false
  }
}

onMounted(fetchExisting)
</script>

<template>
  <div>
    <button
      class="text-sm text-neutral-500 hover:text-neutral-900 dark:hover:text-neutral-100 mb-4"
      @click="router.back()"
    >
      &larr; Back
    </button>

    <h1 class="text-lg font-bold mb-4">{{ isNew ? 'New Ruleset' : `Edit: ${topic}` }}</h1>

    <p v-if="loading" class="text-center text-neutral-500 py-12">Loading...</p>

    <p v-if="error" class="text-sm text-red-600 dark:text-red-400 mb-4">{{ error }}</p>

    <form v-if="!loading" class="space-y-6" @submit.prevent="save">
      <!-- Topic & Media -->
      <div class="border border-neutral-200 dark:border-neutral-700 rounded p-4 space-y-3">
        <div>
          <label class="block text-xs font-medium text-neutral-500 mb-1">Topic</label>
          <input
            v-model="form.topic"
            type="text"
            :disabled="!isNew"
            class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm w-full disabled:opacity-50"
          />
        </div>
        <div class="grid grid-cols-3 gap-3">
          <div>
            <label class="block text-xs font-medium text-neutral-500 mb-1">Media Name</label>
            <input
              v-model="form.media.name"
              type="text"
              class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm w-full"
            />
          </div>
          <div>
            <label class="block text-xs font-medium text-neutral-500 mb-1">TVDB ID</label>
            <input
              v-model.number="form.media.tvdbId"
              type="number"
              class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm w-full"
            />
          </div>
          <div>
            <label class="block text-xs font-medium text-neutral-500 mb-1">Type</label>
            <select
              v-model="form.media.type"
              class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm w-full"
            >
              <option value="show">show</option>
              <option value="movie">movie</option>
            </select>
          </div>
        </div>
        <div>
          <label class="block text-xs font-medium text-neutral-500 mb-1">Aliases (comma-separated)</label>
          <input
            v-model="aliasesText"
            type="text"
            class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm w-full"
            placeholder="alias1, alias2"
          />
        </div>
      </div>

      <!-- Rules -->
      <div>
        <div class="flex items-center justify-between mb-2">
          <h2 class="text-sm font-bold">Rules</h2>
          <button
            type="button"
            class="text-xs text-blue-600 dark:text-blue-400 hover:underline"
            @click="addRule"
          >
            + Add Rule
          </button>
        </div>

        <div class="space-y-4">
          <div
            v-for="(rule, ri) in form.rules"
            :key="ri"
            class="border border-neutral-200 dark:border-neutral-700 rounded p-4"
          >
            <div class="flex items-center justify-between mb-3">
              <span class="text-sm font-bold">Rule #{{ ri }}</span>
              <button
                type="button"
                class="text-red-600 dark:text-red-400 text-sm hover:underline"
                @click="removeRule(ri)"
              >
                Remove
              </button>
            </div>

            <div class="grid grid-cols-2 gap-3 mb-3">
              <div>
                <label class="block text-xs font-medium text-neutral-500 mb-1">Priority</label>
                <input
                  v-model.number="rule.priority"
                  type="number"
                  class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm w-full"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-neutral-500 mb-1">Strategy</label>
                <select
                  v-model="rule.strategy"
                  class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm w-full"
                >
                  <option v-for="s in strategies" :key="s" :value="s">{{ s }}</option>
                </select>
              </div>
            </div>

            <!-- Regex fields -->
            <div
              v-if="showRegex(rule.strategy)"
              class="grid grid-cols-3 gap-3 mb-3"
            >
              <div>
                <label class="block text-xs font-medium text-neutral-500 mb-1">Season Regex</label>
                <input
                  v-model="rule.seasonRegex"
                  type="text"
                  class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm font-mono w-full"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-neutral-500 mb-1">Episode Regex</label>
                <input
                  v-model="rule.episodeRegex"
                  type="text"
                  class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm font-mono w-full"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-neutral-500 mb-1">Capture Group</label>
                <input
                  v-model.number="rule.captureGroup"
                  type="number"
                  class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm w-full"
                />
              </div>
            </div>

            <!-- Filters -->
            <div class="mb-3">
              <div class="text-xs font-medium text-neutral-500 mb-1">Filters</div>
              <FilterEditor v-model="rule.filters" />
            </div>

            <!-- Title Rules -->
            <TitleRuleEditor v-model="rule.titleRules" />
          </div>
        </div>
      </div>

      <!-- Match Test Panel -->
      <MatchTestPanel
        :topic="form.topic"
        :tvdb-id="form.media.tvdbId"
        :rules="form.rules"
      />

      <!-- Save -->
      <div class="flex justify-end">
        <button
          type="submit"
          class="bg-blue-600 text-white px-3 py-1.5 rounded text-sm disabled:opacity-50"
          :disabled="saving"
        >
          {{ saving ? 'Saving...' : 'Save Ruleset' }}
        </button>
      </div>
    </form>
  </div>
</template>
